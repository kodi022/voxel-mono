using System.Collections.Generic;
using Godot;
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

    public bool Simulating { get; private set; } = false;
    public bool UsingTick { get; private set; } = false;
    public bool BlocksGenerated { get; private set; } = false;
    public bool MeshGenerating { get; private set; } = false;
    public int Lod { get; set; } = 0;

    // position hash
    public Dictionary<int, Node> ChunkEntities { get; private set; } = [];

    public HashSet<BlockVec3> TickedBlocks { get; private set; } = [];

    // true if chunk at Directions[index] exists
    public bool[] AdjacentChunks { get; set; } = [false, false, false, false, false, false];

    private static readonly ChunkVec3[] neighborOffset =
    {
        new ( 1,  0,  0), new (-1,  0,  0),
        new ( 0,  1,  0), new ( 0, -1,  0),
        new ( 0,  0,  1), new ( 0,  0, -1),
    };

    private readonly Region region;
    private readonly World3D world3D;

    // * enabling / disabling

    public Chunk(Region region, ChunkVec3 worldPosition, int lod)
    {
        this.region = region;
        world3D = ChunkManager.Current.GetWorld3D();
        Blocks = new BlockInstance[ChunkSize, ChunkSize, ChunkSize];

        ChunkPosition = worldPosition;
        ChunkPositionHash = ChunkPosition.GetVecHash();
        Lod = lod;
    }

    public void CleanupChunk()
    {
        if (meshInstanceData.MeshRid.IsValid)
            RenderingServer.FreeRid(meshInstanceData.MeshRid);

        if (meshInstanceData.PhysicsMeshRid.IsValid)
            PhysicsServer3D.FreeRid(meshInstanceData.PhysicsMeshRid);

        if (meshInstanceData.PhysicsMeshShape.IsValid)
            PhysicsServer3D.FreeRid(meshInstanceData.PhysicsMeshShape);

        noise.Dispose();
        noise = null;
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

    public void TickBlocks()
    {
        if (!UsingTick) return;

        foreach (var pos in TickedBlocks)
        {
            var instance = Blocks[pos.X, pos.Y, pos.Z];
            instance.BlockInfo.OnTick(new GenericInfo() { BlockLocalPosition = pos, Chunk = this, BlockInstance = instance });
        }

        //ChunkManager.UpdateChunk(ChunkPosition);
    }

    /// <summary> Finds Chunk. null if not found </summary>
    public static Chunk ChunkFind(ChunkVec3 pos)
    {
        var regionPos = (RegionVec3)pos;

        if (ChunkManager.Regions.TryGetValue(regionPos.GetVecHash(), out Region region))
        {
            return region.ChunkGet(pos);
        }

        return null;
    }

    public static void ChunkHitBlock(in DamageInfo info)
    {
        var chunk = ChunkFind((ChunkVec3)info.BlockPosition);
        chunk?.HitBlock(info);
    }

    public static void ChunkBreakBlock(in BlockVec3 pos)
    {
        var chunk = ChunkFind((ChunkVec3)pos);
        chunk?.SetBlock(pos, "base:air");
    }

    public static void ChunkPlaceBlock(in BlockVec3 pos, in string blockId)
    {
        var chunk = ChunkFind((ChunkVec3)pos);
        chunk?.SetBlock(pos, blockId);
    }

    public static BlockInstance ChunkGetBlock(in BlockVec3 pos)
    {
        var chunk = ChunkFind((ChunkVec3)pos);
        return chunk?.GetBlock(pos);
    }

    public void HitBlock(DamageInfo info)
    {
        var pos = info.BlockPosition.ToLocal(ChunkPosition);
        if (!pos.IsInside(ChunkSize)) return;

        var block = Blocks[pos.X, pos.Y, pos.Z];
        if (block.BlockInfo == 0 || block.BlockInfo.Unbreakable) return;

        info.BlockInstance = block;
        info.Chunk = this;
        Blocks[pos.X, pos.Y, pos.Z].BlockInfo.OnHit(info);
    }

    public void SetBlock(BlockVec3 pos, in string blockId)
    {
        pos = pos.ToLocal(ChunkPosition);
        if (!pos.IsInside(ChunkSize)) return;

        var block = Blocks[pos.X, pos.Y, pos.Z];
        if (blockId == "base:air") if (block == 0 || block.BlockInfo.Unbreakable) return;
        else if (block.BlockInfo.Unbreakable) return;

        if (block != blockId)
        {
            var worldPos = pos + (BlockVec3)ChunkPosition;

            // Remove old block from ticking if it was ticking
            if (block != 0 && block.BlockInfo.UsesTick)
            {
                TickedBlocks.Remove(pos);
            }

            Blocks[pos.X, pos.Y, pos.Z] = (BlockInstance)blockId;
            var hp = Blocks[pos.X, pos.Y, pos.Z].BlockInfo.HpRange.Y;
            Blocks[pos.X, pos.Y, pos.Z].Hp = hp;
            Blocks[pos.X, pos.Y, pos.Z].GeneratedMaxHp = hp;

            // Add new block to ticking if it uses tick
            if (Blocks[pos.X, pos.Y, pos.Z].BlockInfo.UsesTick)
            {
                TickedBlocks.Add(pos);
                UsingTick = true;
            }

            if (pos.X == ChunkSize - 1) ChunkManager.UpdateChunk(ChunkPosition + neighborOffset[0]);
            if (pos.X == 0) ChunkManager.UpdateChunk(ChunkPosition + neighborOffset[1]);
            if (pos.Y == ChunkSize - 1) ChunkManager.UpdateChunk(ChunkPosition + neighborOffset[2]);
            if (pos.Y == 0) ChunkManager.UpdateChunk(ChunkPosition + neighborOffset[3]);
            if (pos.Z == ChunkSize - 1) ChunkManager.UpdateChunk(ChunkPosition + neighborOffset[4]);
            if (pos.Z == 0) ChunkManager.UpdateChunk(ChunkPosition + neighborOffset[5]);

            ChunkManager.UpdateChunk(ChunkPosition);
        }
    }

    public void SetBlocks(in BlockVec3[] poss, in string blockId)
    {
        bool change = false;
        List<ChunkVec3> neighborUpdate = [];
        foreach (var pos in poss)
        {
            var locPos = pos.ToLocal(ChunkPosition);
            if (!locPos.IsInside(ChunkSize)) continue;

            var block = Blocks[locPos.X, locPos.Y, locPos.Z];
            if (blockId == "base:air") if (block == 0 || block.BlockInfo.Unbreakable) continue;
            else if (block.BlockInfo.Unbreakable) continue;

            if (locPos.X == ChunkSize - 1) neighborUpdate.Add(ChunkPosition + neighborOffset[0]);
            if (locPos.X == 0) neighborUpdate.Add(ChunkPosition + neighborOffset[1]);
            if (locPos.Y == ChunkSize - 1) neighborUpdate.Add(ChunkPosition + neighborOffset[2]);
            if (locPos.Y == 0) neighborUpdate.Add(ChunkPosition + neighborOffset[3]);
            if (locPos.Z == ChunkSize - 1) neighborUpdate.Add(ChunkPosition + neighborOffset[4]);
            if (locPos.Z == 0) neighborUpdate.Add(ChunkPosition + neighborOffset[5]);

            Blocks[locPos.X, locPos.Y, locPos.Z] = (BlockInstance)blockId;
            var hp = Blocks[locPos.X, locPos.Y, locPos.Z].BlockInfo.HpRange.Y;
            Blocks[locPos.X, locPos.Y, locPos.Z].Hp = hp;
            Blocks[locPos.X, locPos.Y, locPos.Z].GeneratedMaxHp = hp;
            change = true;
        }

        if (change)
        {
            ChunkManager.UpdateChunk(ChunkPosition);

            List<ChunkVec3> neighborUpdated = [];
            foreach (var pos in neighborUpdate)
            {
                if (!neighborUpdated.Contains(pos))
                {
                    ChunkManager.UpdateChunk(pos);
                    neighborUpdated.Add(pos);
                }
            }
        }
    }

    public BlockInstance GetBlock(BlockVec3 pos)
    {
        if (Blocks is null) return null;

        pos = pos.ToLocal(ChunkPosition);
        if (!pos.IsInside(ChunkSize)) return null;

        var block = Blocks[pos.X, pos.Y, pos.Z];
        if (block is null) return null;

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