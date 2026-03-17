using Godot;
using System;
using System.Collections.Generic;
using Voxel.World;

namespace Voxel;

public partial class ChunkManagerMenu : ChunkManager
{
    private readonly List<ChunkVec3> chunksToSpawn = [];
    private readonly PackedScene sceneRegion = GD.Load<PackedScene>("res://Scenes/region.tscn");
    private Window root;

    public override void _Ready()
    {
        GetTree().AutoAcceptQuit = false;
        root = GetTree().Root;

        for (int x = -3; x < 3; x++) for (int y = -3; y < 3; y++) for (int z = -3; z < 3; z++)
        {
            chunksToSpawn.Add(new ChunkVec3(x, y, z));
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (chunksToSpawn.Count > 0)
        {
            for (int i = 0; i < 2; i++)
            {
                if (chunksToSpawn.Count == 0) break;

                var chunkPos = chunksToSpawn[0];
                if (GeneratingChunks.Contains(chunkPos.GetVecHash())) continue;

                var regionPos = (RegionVec3)chunkPos;
                var regionPosHash = regionPos.GetVecHash();

                if (Regions.TryGetValue(regionPosHash, out Region region))
                {
                    region.ChunkCreate(chunkPos);
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
}