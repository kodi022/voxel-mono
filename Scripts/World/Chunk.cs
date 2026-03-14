using Godot;
using System;
using Voxel.Resource;

namespace Voxel.World;

public partial class Chunk
{
    public const int ChunkSize = 16;
    public static readonly int RegionSize = ChunkSize * ChunkSize;

    public int[,,] BlockBiome { get; set; }
    public BlockInstance[,,] Blocks { get; set; }

    public readonly ChunkVec3 ChunkPosition;
    public readonly int ChunkPositionHash;

    public bool Visible { get; private set; } = false;
    public bool Simulating { get; private set; } = false;

    public bool Generating { get; set; } = false;

    private readonly Region region;
    private readonly World3D world3D;

    // * enabling / disabling

    public Chunk(Region region, ChunkVec3 worldPosition)
    {
        this.region = region;
        world3D = region.GetWorld3D();
        Blocks ??= new BlockInstance[ChunkSize, ChunkSize, ChunkSize];

        ChunkPosition = worldPosition;
        ChunkPositionHash = ChunkPosition.GetVecHash();

        region.ChunkGenerate(this);
    }

    public void FreeMeshes()
    {
        if (GeneratedMesh)
            RenderingServer.FreeRid(meshInstance);

        if (GeneratedPhysicsMesh)
            PhysicsServer3D.FreeRid(physicsMeshInstance);

        Visible = false;
    }

    public void EnableSimulation()
    {
        // CreatePhysics();
        Simulating = true;
    }

    public void DisableSimulation()
    {
        // if (GeneratedPhysicsMesh)
        //     PhysicsServer3D.FreeRid(physicsMeshInstance);

        Simulating = false;
    }

    // * modifying blocks

    public static void ChunkMineBlock(in BlockVec3 pos)
    {
        var chunk = ChunkManager.FindChunk((ChunkVec3)pos);
        chunk?.SetBlock(pos, "base:air");
    }

    public static void ChunkPlaceBlock(in BlockVec3 pos, in string blockId)
    {
        var chunk = ChunkManager.FindChunk((ChunkVec3)pos);
        chunk?.SetBlock(pos, blockId);
    }

    public static BlockInstance ChunkSelectBlock(in BlockVec3 pos)
    {
        var chunk = ChunkManager.FindChunk((ChunkVec3)pos);
        return chunk?.GetBlock(pos);
    }

    public void SetBlocks(in BlockVec3[] poss, in string blockId)
    {
        bool change = false;
        foreach (var pos in poss)
        {
            var locPos = pos.ToLocal(ChunkPosition);
            if (!locPos.IsInside(ChunkSize)) continue;

            var block = Blocks[locPos.X, locPos.Y, locPos.Z];
            if (blockId == "base:air") if (block.IsAir || block.Unbreakable) continue;
            else if (block.Unbreakable) continue;

            Blocks[locPos.X, locPos.Y, locPos.Z] = (BlockInstance)blockId;
            change = true;
        }

        if (change) region.ChunkUpdate(this);
    }

    public void SetBlock(BlockVec3 pos, in string blockId)
    {
        pos = pos.ToLocal(ChunkPosition);
        if (!pos.IsInside(ChunkSize)) return;

        var block = Blocks[pos.X, pos.Y, pos.Z];
        if (blockId == "base:air") if (block.IsAir || block.Unbreakable) return;
        else if (block.Unbreakable) return;

        if (block != blockId)
        {
            Blocks[pos.X, pos.Y, pos.Z] = (BlockInstance)blockId;
            region.ChunkUpdate(this);
        }
    }

    public BlockInstance GetBlock(BlockVec3 pos)
    {
        if (Blocks is null) return null;

        pos = pos.ToLocal(ChunkPosition);
        if (!pos.IsInside(ChunkSize)) return null;

        var block = Blocks[pos.X, pos.Y, pos.Z];
        if (block is null) return null;
        if (block.IsAir || block.Unbreakable) return null;

        return Blocks[pos.X, pos.Y, pos.Z];
    }

    // no api for threading
    // public Block GetBlock(BlockVec3 pos, in ChunkVec3 globalPosition)
    // {
    //     pos = pos.ToLocal(globalPosition);
    //     if (!pos.IsInside(ChunkSize)) return (Block)"block:air";

    //     return Blocks[pos.X, pos.Y, pos.Z];
    // }
}