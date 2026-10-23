using Voxel.World;

namespace Voxel;

public partial class ChunkManager : Node3D
{
	public static ChunkManager Current { get; private set; }
	public static int Seed { get; set; } = 5000;
	public static bool IsMenu { get; set; } = false;

	public static Dictionary<int, Region> Regions { get; private set; } = [];

	public static List<int> GeneratingChunks { get; private set; } = [];

	private static Window root;

	// lod of -1 to force update without changing lod

	private static readonly Dictionary<int, List<ChunkVec3>> chunksToUpdate = [];

	private static readonly Dictionary<int, List<ChunkVec3>> chunksToSpawn = [];
	private static readonly HashSet<ChunkVec3> chunksToDestroy = [];

	private static int physicsProcessTick = 0;

	public override void _EnterTree()
	{
		Current = this;
		world3D = GetWorld3D();
		ResourceManager.Ready();
	}

	public override void _Notification(int what)
	{
		if (what == NotificationWMCloseRequest)
		{
			foreach (var region in Regions)
			{
				foreach (var chunk in region.Value.Chunks)
				{
					chunk.Value.CleanupChunk();
				}
			}
		}
	}

	public override void _Ready()
	{
		if (Seed > int.MaxValue - 10)
		{
			Seed = int.MaxValue - 10;
		}

		GetTree().AutoAcceptQuit = false;

		if (!IsMenu)
		{
			var plrScene = GD.Load<PackedScene>("res://Scenes/player.tscn");
			var plr = plrScene.Instantiate();
			root = GetTree().Root;
			root.CallDeferred(Node.MethodName.AddChild, plr);
		}
	}

	public override void _Process(double delta)
	{
		ProcessDebug(delta);

		var updated = 0;
		foreach (var lodPossKVP in chunksToUpdate)
		{
			if (updated > 8) break;
			if (lodPossKVP.Value.Count == 0) continue;

			// update as much as possible of lod

			for (int i = 0; i < 64; i++)
			{
				if (updated > 8) break;
				if (lodPossKVP.Value.Count == 0) continue;

				var pos = lodPossKVP.Value.First();

				var regionPos = (RegionVec3)pos;
				var regionPosHash = regionPos.GetVecHash();

				if (Regions.TryGetValue(regionPosHash, out Region region))
				{
					if (region.ChunkGet(pos) is Chunk chunk)
					{
						if (lodPossKVP.Key == -1)
						{
							RegionChunkUpdate(region, chunk);
						}
						else
						{
							if (chunk.Lod != lodPossKVP.Key)
							{
								chunk.Lod = lodPossKVP.Key;
								RegionChunkUpdate(region, chunk);
							}
						}
					}
				}
				lodPossKVP.Value.RemoveAt(0);
				updated++;
			}
			GD.Print($"upd _ {lodPossKVP.Key} -- {lodPossKVP.Value.Count} ({0})");
		}

		if (GeneratingChunks.Count < 16)
		{
			var spawned = 0;
			foreach (var lodPossKVP in chunksToSpawn)
			{
				if (lodPossKVP.Value.Count == 0) continue;
				if (spawned > 4) break;

				for (int i = 0; i < 8; i++)
				{
					if (lodPossKVP.Value.Count == 0) break;
					var pos = lodPossKVP.Value.First();

					var regionPos = (RegionVec3)pos;
					var regionPosHash = regionPos.GetVecHash();
					if (Regions.TryGetValue(regionPosHash, out Region region))
					{
						region.ChunkCreate(pos, lodPossKVP.Key);
					}
					else
					{
						region = new Region { RegionPosition = regionPos };
						region.Ready();
						Regions.Add(regionPosHash, region);
						region.ChunkCreate(pos, lodPossKVP.Key);
					}
					lodPossKVP.Value.RemoveAt(0);
					spawned++;
				}
				GD.Print($"spn _ {lodPossKVP.Key} -- {lodPossKVP.Value.Count} ({spawned})");
			}
		}

		if (chunksToDestroy.Count > 0)
		{
			for (int i = 0; i < 8; i++)
			{
				var pos = chunksToDestroy.First();
				var regionPosHash = ((RegionVec3)pos).GetVecHash();
				if (Regions.TryGetValue(regionPosHash, out Region region))
				{
					RegionChunkDestroy(region, pos.GetVecHash());
				}
				chunksToDestroy.Remove(pos);
			}
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (physicsProcessTick >= 1)
		{
			physicsProcessTick = 0;

			foreach (var region in Regions)
			{
				foreach (var posHash in region.Value.TickedChunks)
				{
					region.Value.Chunks[posHash].TickBlocks();
				}
			}
		}
		else
		{
			physicsProcessTick++;
		}
	}

	public static void UpdateChunk(ChunkVec3 pos, int lod = -1)
	{
		if (!chunksToUpdate.TryAdd(lod, [pos]))
		{
			chunksToUpdate[lod].Add(pos);
		}
	}

	public static void SpawnChunk(ChunkVec3 pos, int lod)
	{
		if (!chunksToSpawn.TryAdd(lod, [pos]))
		{
			chunksToSpawn[lod].Add(pos);
		}
	}

	public static void SpawnChunks(Dictionary<int, List<ChunkVec3>> list)
	{
		foreach (var lodPossKVP in list)
		{
			if (!chunksToSpawn.TryAdd(lodPossKVP.Key, lodPossKVP.Value))
			{
				chunksToSpawn[lodPossKVP.Key].AddRange(lodPossKVP.Value);
			}
		}
	}

	public static void SpawnChunksOverride(Dictionary<int, List<ChunkVec3>> list)
	{
		chunksToSpawn.Clear();
		SpawnChunks(list);
	}

	public static void DestroyChunk(ChunkVec3 pos)
	{
		chunksToDestroy.Add(pos);
	}

	public static void DestroyChunks(HashSet<ChunkVec3> poss)
	{
		foreach (var pos in poss)
		{
			chunksToDestroy.Add(pos);
		}
	}

	public static void DestroyChunkImmediate(ChunkVec3 pos)
	{
		var regionPosHash = ((RegionVec3)pos).GetVecHash();
		if (Regions.TryGetValue(regionPosHash, out Region region))
		{
			RegionChunkDestroy(region, pos.GetVecHash());
		}
	}

	public static void DestroyChunksImmediate(HashSet<ChunkVec3> poss)
	{
		chunksToDestroy.Clear();

		foreach (var pos in poss)
		{
			var regionPosHash = ((RegionVec3)pos).GetVecHash();
			if (Regions.TryGetValue(regionPosHash, out Region region))
			{
				RegionChunkDestroy(region, pos.GetVecHash());
			}
		}
	}

	public static void DestroyAllChunksImmediate()
	{
		foreach (var region in Regions)
		{
			foreach (var chunk in region.Value.Chunks)
			{
				RegionChunkDestroy(region.Value, chunk.Key);
			}
		}
	}

	// * region code

	// region

	/// <summary> Finds Region. null if not found </summary>
	public static Region FindRegion(RegionVec3 pos)
	{
		if (Regions.TryGetValue(pos.GetVecHash(), out Region region))
		{
			return region;
		}

		return null;
	}
	// chunk

	// necessary because CallDeferred is required yet chunks are not Node's

	public async void RegionChunkGenerate(Region region, Chunk chunk)
	{
		GeneratingChunks.Add(chunk.ChunkPositionHash);

		await Task.Run(async () =>
		{
			await chunk.GenerateBlockData();
			await chunk.GenerateMeshData();
			CallDeferred(nameof(RegionChunkFinish), region.RegionPositionHash, chunk.ChunkPositionHash);
		});
	}

	// necessary because CallDeferred is required yet chunks are not Node's
	public async void RegionChunkUpdate(Region region, Chunk chunk)
	{
		GeneratingChunks.Add(chunk.ChunkPositionHash);

		await Task.Run(async () =>
		{
			if (!chunk.BlocksGenerated && chunk.Lod == 0)
			{
				await chunk.GenerateBlockData();
			}

			await chunk.GenerateMeshData();
			CallDeferred(nameof(RegionChunkFinish), region.RegionPositionHash, chunk.ChunkPositionHash);
		});
	}

	// necessary because CallDeferred cannot call functions on other objects. arg has to be Godot.Variant
	public static void RegionChunkFinish(int regionPosHash, int chunkPosHash)
	{
		var region = Regions[regionPosHash];
		var chunk = region.ChunkGet(chunkPosHash);
		if (chunk is null) return;

		chunk.CreateMesh();
		chunk.CreatePhysics();

		if (!chunk.UsingTick) region.TickedChunks.Remove(chunk.ChunkPositionHash);
		if (chunk.UsingTick) region.TickedChunks.Add(chunk.ChunkPositionHash);

		GeneratingChunks.Remove(chunk.ChunkPositionHash);
	}

	public static void RegionChunkDestroy(Region region, int posHash)
	{
		if (region.Chunks.TryGetValue(posHash, out Chunk chunk))
		{
			if (chunk is not null)
			{
				GeneratingChunks.Remove(chunk.ChunkPositionHash);
				region.Chunks[posHash].CleanupChunk();
				region.Chunks[posHash] = null;
				region.Chunks.Remove(posHash);
			}

			if (region.Chunks.Count == 0)
			{
				Regions.Remove(region.RegionPositionHash);
			}
		}
	}
}
