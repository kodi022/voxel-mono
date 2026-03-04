using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Voxel.World;

namespace Voxel;

public partial class ChunkManager : Node
{
	public static int Seed { get; private set; } = 8237358;

	public static Dictionary<int, Chunk> AllChunks { get; private set; } = [];
	public static Dictionary<int, Region> HashRegions { get; private set; } = [];

	public static List<int> InitializingChunks { get; private set; } = [];

	private static readonly PackedScene sceneChunk = GD.Load<PackedScene>("res://Scenes/chunk.tscn");
	private static Window root;

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

	public static void SpawnChunk(Vector3 position, int LOD)
	{
		position = position.ToChunkPosition();
		var posHash = HashCode.Combine(position);

		if (InitializingChunks.Count > 32 || InitializingChunks.Contains(posHash)) return;

		if (AllChunks.TryGetValue(posHash, out Chunk chunk))
		{
			if (chunk.Initialized)
			{
				if (!chunk.Visible || chunk.LOD != LOD)
				{
					chunk.ClearMeshOnReady = false;

					chunk.LOD = LOD;
					if (LOD == 0) chunk.SetProcessing(true);
					else chunk.SetProcessing(false);

					chunk._Ready();
					chunk.Visible = true;
				}
			}
			return;
		}

		chunk = AllChunks.FirstOrDefault(c => !c.Value.Visible).Value;
		if (chunk is not null)
		{
			if (chunk.Initialized)
			{
				AllChunks.Remove(chunk.PositionHash);
				AllChunks.Add(posHash, chunk);
				chunk.WorldPosition = position;
				chunk.PositionHash = posHash;
				chunk.ClearMeshOnReady = true;

				chunk.LOD = LOD;
				if (LOD == 0) chunk.SetProcessing(true);
				else chunk.SetProcessing(false);

				chunk._Ready();
				chunk.Visible = true;
			}
			return;
		}

		chunk = (Chunk)sceneChunk.Instantiate();
		chunk.WorldPosition = position;
		chunk.PositionHash = posHash;
		chunk.ClearMeshOnReady = false;

		chunk.LOD = LOD;
		if (LOD == 0) chunk.SetProcessing(true);
		else chunk.SetProcessing(false);

		AllChunks.Add(posHash, chunk);
		root.CallDeferred(Node.MethodName.AddChild, chunk);
	}

	public static void DestroyChunk(Vector3 position)
	{
		var chunk = FindChunk(position);
		if (chunk is not null && chunk.Initialized & chunk.Visible)
		{
			chunk.Visible = false;
			chunk.ProcessMode = ProcessModeEnum.Disabled;
		}
	}

	/// <summary> Finds Active chunks </summary>
	public static Chunk FindChunk(Vector3 position)
	{
		position = position.ToChunkPosition();

		if (AllChunks.TryGetValue(HashCode.Combine(position), out Chunk val))
			return val;

		return null;
	}
}
