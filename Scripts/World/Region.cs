using Voxel.Resource;

namespace Voxel.World;

public partial class Region
{
	public RegionVec3 RegionPosition { get; internal set; }
	public int RegionPositionHash { get; private set; }

	public List<StructureInstance> Structures = [];
	public Dictionary<int, Chunk> Chunks { get; private set; } = [];
	public HashSet<int> TickedChunks { get; private set; } = [];
	//public Dictionary<int, int> ModifiedBlocks;

	// Called when the node enters the scene tree for the first time. Also called when made visible.
	public void Ready()
	{
		RegionPositionHash = RegionPosition.GetVecHash();

		// structures
		ChunkVec3 chunkPos = (ChunkVec3)RegionPosition;
		for (int x = 0; x < Chunk.ChunkSize; x++) for (int y = 0; y < Chunk.ChunkSize; y++) for (int z = 0; z < Chunk.ChunkSize; z++)
		{
			var newPos = chunkPos + new ChunkVec3(x, y, z);
			bool add = false;
			foreach (var structure in ResourceManager.StructureRegistry)
			{
				if (Global.GetSeededRandom((BlockVec3)newPos, structure.Value.HashId) < structure.Value.ChancePerChunk)
				{
					Structures.Add(new StructureInstance(structure.Value.HashId, structure.Value.FullId, structure.Value.Priority, newPos));
					structure.Value.GenerateBlocks(Structures.Last(), newPos);
					add = true;
					break;
				}
			}
			if (add) continue;
		}
	}

	public void ChunkCreate(ChunkVec3 pos, int lod)
	{
		var posHash = pos.GetVecHash();
		if (!Chunks.ContainsKey(posHash))
		{
			var chunk = new Chunk(this, pos, lod);
			Chunks.Add(posHash, chunk);

			for (int i = 0; i < 6; i++)
			{
				var adjPos = pos + Chunk.Directions[i];
				if (Chunk.ChunkFind(adjPos) is Chunk adjChunk)
				{
					chunk.AdjacentChunks[i] = true;
					var dir = i % 2 == 0 ? i + 1 : i - 1;
					adjChunk.AdjacentChunks[dir] = true;
					//if (!adjChunk.AdjacentChunks.Contains(false) && adjChunk.BlocksGenerated) ChunkManager.UpdateChunk(adjChunk.ChunkPosition);
				}
			}

			ChunkManager.Current.RegionChunkGenerate(this, chunk);
		}
	}

	public Chunk ChunkGet(int posHash)
	{
		if (Chunks.TryGetValue(posHash, out Chunk chunk))
		{
			return chunk;
		}

		return null;
	}

	public Chunk ChunkGet(ChunkVec3 pos)
	{
		if (Chunks.TryGetValue(pos.GetVecHash(), out Chunk chunk))
		{
			return chunk;
		}

		return null;
	}


}