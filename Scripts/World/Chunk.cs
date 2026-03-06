using Godot;
using System;
using Voxel.Resource;

namespace Voxel.World;

public partial class Chunk
{
    public const int ChunkSize = 16;

    public Block[,,] Blocks { get; set; }

    public Vector3 WorldPosition { get; set; }
    public int PositionHash { get; set; }
    public bool Visible = false;
    public bool Generating { get; set; } = false;

    private Region region;
    private World3D world3D;

    // * enabling / disabling

    public void Enable(Region region)
    {
        this.region = region;
        world3D = region.GetWorld3D();

        Blocks ??= new Block[ChunkSize, ChunkSize, ChunkSize];

        region.ChunkGenerate(this);
    }

    public void Disable()
    {
        DestroyMeshes();
    }

    // * modifying blocks

    public static void ChunkMineBlock(in Vector3 position)
    {
        var chunk = ChunkManager.FindChunk(position);
        chunk?.SetBlock(position, "base:air");
    }

    public static void ChunkPlaceBlock(in Vector3 position, in string blockId)
    {
        var chunk = ChunkManager.FindChunk(position);
        chunk?.SetBlock(position, blockId);
    }

    public static Block ChunkSelectBlock(in Vector3 position)
    {
        var chunk = ChunkManager.FindChunk(position);
        return chunk?.GetBlock(position);
    }

    public void SetBlocks(in Vector3[] positions, in string blockId)
    {
        bool change = false;
        foreach (var pos in positions)
        {
            var bPos = pos.ToBlockLocalPosition(WorldPosition);
            if (!bPos.IsInside(ChunkSize)) return;

            Blocks[(int)bPos.X, (int)bPos.Y, (int)bPos.Z] = (Block)blockId;
            change = true;
        }

        if (change) region.ChunkUpdate(this);
    }

    public void SetBlock(in Vector3 position, in string blockId)
    {
        var pos = position.ToBlockLocalPosition(WorldPosition);
        if (!pos.IsInside(ChunkSize)) return;

        bool change = Blocks[(int)pos.X, (int)pos.Y, (int)pos.Z] != blockId;
        Blocks[(int)pos.X, (int)pos.Y, (int)pos.Z] = (Block)blockId;
        if (change) region.ChunkUpdate(this);
    }

    public Block GetBlock(in Vector3 position)
    {
        var pos = position.ToBlockLocalPosition(WorldPosition);
        if (!pos.IsInside(ChunkSize)) return (Block)"block:air";

        return Blocks[(int)pos.X, (int)pos.Y, (int)pos.Z];
    }

    // no api for threading
    public Block GetBlock(in Vector3 position, in Vector3 globalPosition)
    {
        var pos = position.ToBlockLocalPosition(globalPosition);
        if (!pos.IsInside(ChunkSize)) return (Block)"block:air";

        return Blocks[(int)pos.X, (int)pos.Y, (int)pos.Z];
    }

    private static void IterateChunk(Action<sbyte, sbyte, sbyte> action)
    {
        for (sbyte x = 0; x < ChunkSize; x++)
        {
            for (sbyte z = 0; z < ChunkSize; z++)
            {
                for (sbyte y = 0; y < ChunkSize; y++)
                {
                    action.Invoke(x, y, z);
                }
            }
        }
    }

    private static Texture2D LoadTextureFromBlock(Texture2D resourceTexture, string resourceTexturePath)
    {
        if (resourceTexture is not null) return resourceTexture;
        if (!string.IsNullOrEmpty(resourceTexturePath)) return GD.Load<Texture2D>(resourceTexturePath);
        return MissingTexture;
    }
}