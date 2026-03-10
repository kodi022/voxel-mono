using Godot;
using System;
using System.Collections.Generic;
using Voxel.World;

namespace Voxel;

public partial class ChunkManager : Node
{
	public static int Seed { get; private set; } = 8237358;

	public static Dictionary<int, Region> Regions { get; private set; } = [];

	// ! not removing properly
	public static List<int> GeneratingChunks { get; private set; } = [];

	private static readonly PackedScene sceneRegion = GD.Load<PackedScene>("res://Scenes/region.tscn");
	private static Window root;

	private static List<Vector3> chunksToSpawn = [];

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		if (Seed > int.MaxValue - 10)
		{
			Seed = int.MaxValue - 10;
		}

		var plrScene = GD.Load<PackedScene>("res://Scenes/player.tscn");
		var plr = plrScene.Instantiate();
		root = GetTree().Root;
		root.CallDeferred(Node.MethodName.AddChild, plr);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (GeneratingChunks.Count >= 16) return;

		if (chunksToSpawn.Count > 0)
		{
			for (int i = 0; i < 16; i++)
			{
				if (i >= chunksToSpawn.Count) break;

				Vector3 chunkPos = chunksToSpawn[i];
				if (GeneratingChunks.Contains(HashCode.Combine(chunkPos))) continue;

				var regionPos = chunkPos.ToRegionPosition();
				var regionPosHash = HashCode.Combine(regionPos);

				if (Regions.TryGetValue(regionPosHash, out Region region))
				{
					region.ChunkCreate(chunkPos);
				}
				else
				{
					region = (Region)sceneRegion.Instantiate();
					region.Name = "region_" + chunkPos.ToRegionPosition();
					Regions.Add(regionPosHash, region);
					root.CallDeferred(Node.MethodName.AddChild, region);
				}

				chunksToSpawn.Remove(chunkPos);
			}
		}
	}

	public static void SpawnChunk(Vector3 position)
	{
		if (!chunksToSpawn.Contains(position))
		{
			chunksToSpawn.Add(position);
		}
	}

	public static void SpawnChunks(List<Vector3> positions)
	{
		foreach (var pos in positions)
		{
			if (!chunksToSpawn.Contains(pos))
			{
				chunksToSpawn.Add(pos);
			}
		}
	}

	public static void SpawnChunksOverride(List<Vector3> positions)
	{
		chunksToSpawn = positions;
	}

	public static void DestroyChunk(Vector3 position)
	{
		var regionPos = position.ToRegionPosition();
		var regionPosHash = HashCode.Combine(regionPos);

		if (Regions.TryGetValue(regionPosHash, out Region region))
		{
			region.ChunkDestroy(HashCode.Combine(position.ToChunkPosition()));
		}
	}

	public static void DestroyChunks(List<Vector3> positions)
	{
		foreach (var position in positions)
		{
			var regionPos = position.ToRegionPosition();
			var regionPosHash = HashCode.Combine(regionPos);
			if (Regions.TryGetValue(regionPosHash, out Region region))
			{
				region.ChunkDestroy(HashCode.Combine(position.ToChunkPosition()));
			}
		}
	}

	/// <summary> Finds Active chunks </summary>
	public static Chunk FindChunk(Vector3 position)
	{
		var regionPos = position.ToRegionPosition();

		if (Regions.TryGetValue(HashCode.Combine(regionPos), out Region region))
		{
			return region.ChunkGet(position);
		}

		return null;
	}
}
