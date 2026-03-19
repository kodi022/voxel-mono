using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Voxel.Resource;

namespace Voxel.World;

public partial class Region : Node3D
{
	public RegionVec3 RegionPosition { get; internal set; }
	public int RegionPositionHash { get; private set; }

	public List<StructureInstance> Structures = [];
	public Dictionary<int, Chunk> Chunks { get; private set; } = [];
	//public Dictionary<int, int> ModifiedBlocks;

	private bool ready = false;

	// Called when the node enters the scene tree for the first time. Also called when made visible.
	public override void _Ready()
	{
		ready = true;
		Name = "region_" + RegionPosition;
		RegionPositionHash = RegionPosition.GetVecHash();

		ChunkVec3 chunkPos = (ChunkVec3)RegionPosition;
		for (int x = 0; x < Chunk.ChunkSize; x++) for (int y = 0; y < Chunk.ChunkSize; y++) for (int z = 0; z < Chunk.ChunkSize; z++)
		{
			var newPos = chunkPos + new ChunkVec3(x, y, z);
			bool add = false;
			foreach (var structure in ResourceManager.StructureRegistry)
			{
				if (Global.GetSeededRandom((BlockVec3)newPos, structure.Value.HashId) < structure.Value.ChancePerChunk)
				{
					Structures.Add(new StructureInstance()
					{
						HashId = structure.Value.HashId,
						Priority = structure.Value.Priority,
						Position = newPos,
					});
					structure.Value.GenerateBlocks(newPos, Structures.Last());
					add = true;
					break;
				}
			}
			if (add) continue;
		}
	}

	public void ChunkCreate(ChunkVec3 pos)
	{
		if (!ready) return;

		var posHash = pos.GetVecHash();
		if (!Chunks.ContainsKey(posHash))
		{
			var chunk = new Chunk(this, pos);
			Chunks.Add(posHash, chunk);

			for (int i = 0; i < 6; i++)
			{
				var adjPos = pos + Chunk.Directions[i];
				if (ChunkManager.FindChunk(adjPos) is Chunk adjChunk)
				{
					chunk.AdjacentChunks[i] = true;
					var dir = i % 2 == 0 ? i + 1 : i - 1;
					adjChunk.AdjacentChunks[dir] = true;
					if (!adjChunk.AdjacentChunks.Contains(false) && adjChunk.BlocksGenerated) ChunkManager.UpdateChunk(adjChunk.ChunkPosition);
				}
			}

			ChunkGenerate(chunk);
		}
	}

	public Chunk ChunkGet(int posHash)
	{
		if (!ready) return null;

		if (Chunks.TryGetValue(posHash, out Chunk chunk))
		{
			return chunk;
		}

		return null;
	}

	public Chunk ChunkGet(ChunkVec3 pos)
	{
		if (!ready) return null;

		if (Chunks.TryGetValue(pos.GetVecHash(), out Chunk chunk))
		{
			return chunk;
		}

		return null;
	}

	public void ChunkDestroy(int posHash)
	{
		if (Chunks.TryGetValue(posHash, out Chunk chunk))
		{
			if (chunk is not null)
			{
				ChunkManager.GeneratingChunks.Remove(chunk.ChunkPositionHash);
				Chunks[posHash].CleanupChunk();
				Chunks[posHash] = null;
				Chunks.Remove(posHash);
			}

			if (Chunks.Count == 0)
			{
				ChunkManager.Regions.Remove(RegionPositionHash);
				QueueFree();
			}
		}
	}

	// necessary because CallDeferred is required yet chunks are not Node's
	public async void ChunkGenerate(Chunk chunk)
	{
		ChunkManager.GeneratingChunks.Add(chunk.ChunkPositionHash);

		await Task.Run(async () =>
		{
			await chunk.GenerateBlockData();
			await chunk.GenerateMeshData();
			CallDeferred(nameof(ChunkFinish), chunk.ChunkPositionHash);
		});
	}

	// necessary because CallDeferred is required yet chunks are not Node's
	public async void ChunkUpdate(Chunk chunk)
	{
		await Task.Run(async () =>
		{
			await chunk.GenerateMeshData();
			CallDeferred(nameof(ChunkFinish), chunk.ChunkPositionHash);
		});
	}

	// necessary because CallDeferred cannot call functions on other objects. arg has to be Godot.Variant
	private void ChunkFinish(int chunkPosHash)
	{
		var chunk = ChunkGet(chunkPosHash);
		if (chunk is null) return;

		chunk.CreateMesh();
		chunk.CreatePhysics();

		ChunkManager.GeneratingChunks.Remove(chunk.ChunkPositionHash);
	}
}