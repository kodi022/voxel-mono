using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Voxel.World;

namespace Voxel;

public partial class ChunkManager : Node
{
	public static int Seed { get; private set; } = 8237358;

	public static Dictionary<int, Region> AllRegions { get; private set; } = [];

	public static List<int> GeneratingChunks { get; private set; } = [];

	private static readonly PackedScene sceneRegion = GD.Load<PackedScene>("res://Scenes/region.tscn");
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

	public static void SpawnChunk(Vector3 position)
	{
		var regionPos = position.ToRegionPosition();
		var regionPosHash = HashCode.Combine(regionPos);

		if (GeneratingChunks.Count >= 64) return;

		if (AllRegions.TryGetValue(regionPosHash, out Region region))
		{
			var chunk = region.GetChunk(position);

			if (chunk is not null && !chunk.Generating && !chunk.Visible)
			{
				chunk.Enable(region);
			}

			return;
		}
		else
		{
			region = (Region)sceneRegion.Instantiate();
			region.Name = "region_" + regionPos;
			AllRegions.Add(regionPosHash, region);
			root.CallDeferred(Node.MethodName.AddChild, region);
		}
	}

	public static void DestroyChunk(Vector3 position)
	{
		var chunk = FindChunk(position);
		if (chunk is not null && chunk.Visible)
		{
			chunk.Disable();
		}
	}

	/// <summary> Finds Active chunks </summary>
	public static Chunk FindChunk(Vector3 position)
	{
		position = position.ToRegionPosition();

		if (AllRegions.TryGetValue(HashCode.Combine(position), out Region region))
		{
			return region.GetChunk(position);
		}

		return null;
	}
}
