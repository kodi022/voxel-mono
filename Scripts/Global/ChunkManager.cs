using Godot;
using System;
using System.Collections.Generic;
using Voxel.World;

namespace Voxel;

public partial class ChunkManager : Node
{
	public static ChunkManager Current { get; private set; }
	public static int Seed { get; set; } = 5000;

	public static Dictionary<int, Region> Regions { get; private set; } = [];

	public static List<int> GeneratingChunks { get; private set; } = [];

	private static readonly PackedScene sceneRegion = GD.Load<PackedScene>("res://Scenes/region.tscn");
	private static Window root;

	private static List<ChunkVec3> chunksToUpdate = [];
	private static List<ChunkVec3> chunksToSpawn = [];

	public override void _EnterTree()
	{
		Current = this;
		ResourceManager.Ready();
	}

	public override void _Ready()
	{
		if (Seed > int.MaxValue - 10)
		{
			Seed = int.MaxValue - 10;
		}

		GetTree().AutoAcceptQuit = false;

		var plrScene = GD.Load<PackedScene>("res://Scenes/player.tscn");
		var plr = plrScene.Instantiate();
		root = GetTree().Root;
		root.CallDeferred(Node.MethodName.AddChild, plr);
	}

	public static void CloseGame()
	{
		Current._Notification((int)NotificationWMCloseRequest);
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

			GetTree().Quit();
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		int changedChunks = 0;
		if (chunksToUpdate.Count > 0)
		{
			for (int i = 0; i < 12; i++)
			{
				if (changedChunks > 16) break;
				if (chunksToUpdate.Count == 0) break;

				var chunkPos = chunksToUpdate[0];
				var regionPos = (RegionVec3)chunkPos;
				var regionPosHash = regionPos.GetVecHash();

				if (Regions.TryGetValue(regionPosHash, out Region region))
				{
					if (region.ChunkGet(chunkPos) is Chunk chunk) region.ChunkUpdate(chunk);
					changedChunks++;
				}
				chunksToUpdate.Remove(chunkPos);
			}
		}

		if (GeneratingChunks.Count >= 128) return;
		if (chunksToSpawn.Count > 0)
		{
			for (int i = 0; i < 12; i++)
			{
				if (chunksToSpawn.Count == 0) break;

				var chunkPos = chunksToSpawn[0];
				if (GeneratingChunks.Contains(chunkPos.GetVecHash())) continue;

				var regionPos = (RegionVec3)chunkPos;
				var regionPosHash = regionPos.GetVecHash();

				if (Regions.TryGetValue(regionPosHash, out Region region))
				{
					region.ChunkCreate(chunkPos);
					changedChunks++;
				}
				else
				{
					region = (Region)sceneRegion.Instantiate();
					region.RegionPosition = regionPos;
					Regions.Add(regionPosHash, region);
					root.CallDeferred(Node.MethodName.AddChild, region);
				}

				chunksToSpawn.Remove(chunkPos);
			}
		}
	}

	public static void UpdateChunk(ChunkVec3 pos)
	{
		if (!chunksToUpdate.Contains(pos))
		{
			chunksToUpdate.Add(pos);
		}
	}

	public static void SpawnChunk(ChunkVec3 pos)
	{
		if (!chunksToSpawn.Contains(pos))
		{
			chunksToSpawn.Add(pos);
		}
	}

	public static void SpawnChunks(List<ChunkVec3> poss)
	{
		foreach (var pos in poss)
		{
			if (!chunksToSpawn.Contains(pos))
			{
				chunksToSpawn.Add(pos);
			}
		}
	}

	public static void SpawnChunksOverride(List<ChunkVec3> poss)
	{
		chunksToSpawn = poss;
	}

	public static void DestroyChunk(ChunkVec3 pos)
	{
		var regionPosHash = ((RegionVec3)pos).GetVecHash();

		if (Regions.TryGetValue(regionPosHash, out Region region))
		{
			region.ChunkDestroy(pos.GetVecHash());
		}
	}

	public static void DestroyChunks(List<ChunkVec3> poss)
	{
		foreach (var pos in poss)
		{
			var regionPosHash = ((RegionVec3)pos).GetVecHash();
			if (Regions.TryGetValue(regionPosHash, out Region region))
			{
				region.ChunkDestroy(pos.GetVecHash());
			}
		}
	}

	/// <summary> Finds Active chunks </summary>
	public static Chunk FindChunk(ChunkVec3 pos)
	{
		var regionPos = (RegionVec3)pos;

		if (Regions.TryGetValue(regionPos.GetVecHash(), out Region region))
		{
			return region.ChunkGet(pos);
		}

		return null;
	}
}
