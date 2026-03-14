using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;
using Voxel.Resource;

namespace Voxel.World;

public partial class Region : Node3D
{
	public RegionVec3 RegionPosition { get; internal set; }
	public int RegionPositionHash { get; private set; }

	public List<Structure> Structures;
	public Dictionary<int, Chunk> Chunks { get; private set; } = [];
	//public Dictionary<int, int> ModifiedBlocks;

	private bool ready = false;

	// Called when the node enters the scene tree for the first time. Also called when made visible.
	public override void _Ready()
	{
		ready = true;
		Name = "region_" + RegionPosition;
		RegionPositionHash = RegionPosition.GetVecHash();
	}

	public void ChunkCreate(ChunkVec3 pos)
	{
		if (!ready) return;

		var posHash = pos.GetVecHash();
		if (!Chunks.ContainsKey(posHash))
		{
			var chunk = new Chunk(this, pos);
			Chunks.Add(posHash, chunk);
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
				Chunks[posHash].FreeMeshes();
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
		chunk.Generating = true;
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
		chunk.Generating = true;
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

		chunk.Generating = false;

		chunk.CreateMesh();
		chunk.CreatePhysics();

		ChunkManager.GeneratingChunks.Remove(chunk.ChunkPositionHash);
	}
}