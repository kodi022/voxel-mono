using Godot;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Voxel.World;

public partial class Chunk
{
    // up, down, left, right, forward, backward
    // for array indexing and normals
    public static readonly Vector3B[] Directions = [
        new ( 0,  1,  0),
        new ( 0, -1,  0),
        new ( 0,  0,  1),
        new ( 0,  0, -1),
        new ( 1,  0,  0),
        new (-1,  0,  0),
    ];

    // FastNoiseLite BiomeNoise = new()
    // {
    //     Seed = ChunkManager.Seed + 1,
    //     NoiseType = FastNoiseLite.NoiseTypeEnum.Cellular,
    //     Frequency = 0.001f
    // };

    public Task GenerateBlockData()
    {
        //Stopwatch a = new();
        //a.Start();
        // ! string bad replace with id or something later
        string[,,] tempBlocks = new string[ChunkSize, ChunkSize, ChunkSize];

        for (sbyte x = 0; x < ChunkSize; x++)
            for (sbyte z = 0; z < ChunkSize; z++)
                for (sbyte y = 0; y < ChunkSize; y++)
                    tempBlocks[x, y, z] = "base:air";

        List<BlockGenLayer> layers = [
            new BlockGenLayerBase(),
            new BlockGenLayerOre(),
            new BlockGenLayerEdge()
        ];

        foreach (var layer in layers)
        {
            for (sbyte x = 0; x < ChunkSize; x++)
            {
                for (sbyte z = 0; z < ChunkSize; z++)
                {
                    for (sbyte y = 0; y < ChunkSize; y++)
                    {
                        var pos = new Vector3I(x + (int)WorldPosition.X, y + (int)WorldPosition.Y, z + (int)WorldPosition.Z);
                        var blockGenInput = new BlockGenInput() { Chunk = this, CurrentBlocks = tempBlocks, Position = pos, IndexPosition = new(x, y, z) };
                        string blockId = layer.Generate(ref blockGenInput);

                        // ! remove later
                        if (new Vector2(pos.X, pos.Z).DistanceSquaredTo(new Vector2(-5f, -5f)) < 8f)
                        {
                            continue;
                        }

                        tempBlocks[x, y, z] = blockId;
                    }
                }
            }
        }

        FastNoiseLite NoiseHealth = new()
        {
            Seed = ChunkManager.Seed + 2,
            NoiseType = FastNoiseLite.NoiseTypeEnum.Value,
        };

        for (sbyte x = 0; x < ChunkSize; x++)
        {
            for (sbyte z = 0; z < ChunkSize; z++)
            {
                for (sbyte y = 0; y < ChunkSize; y++)
                {
                    var block = ResourceManager.GetBlock(tempBlocks[x, y, z]);
                    if (!block.IsAir && !block.Unbreakable)
                    {
                        var blockHpSize = block.HpRange.Y - block.HpRange.X;
                        block.Hp = NoiseHealth.GetNoise3D(x, y, z) * blockHpSize + block.HpRange.X;
                    }

                    Blocks[x, y, z] = block;
                }
            }
        }
        //a.Stop();
        //GD.Print(a.ElapsedMilliseconds);
        return Task.CompletedTask;
    }

    private string GenerateBlockAtPosition(ref BlockGenInput input)
    {
        List<BlockGenLayer> layers = [
            new BlockGenLayerBase(),
            new BlockGenLayerOre(),
            new BlockGenLayerEdge()
        ];

        string block = "";
        foreach (var layer in layers)
        {
            block = layer.Generate(ref input);
        }
        return block;
    }

    public struct BlockGenInput
    {
        public Chunk Chunk;
        public string[,,] CurrentBlocks;
        public Vector3I Position;
        public Vector3I IndexPosition;
    }

    public class BlockGenLayer
    {
        public virtual string Generate(ref BlockGenInput input)
        {
            return "base:air";
        }
    }

    public class BlockGenLayerBase : BlockGenLayer
    {
        public string baseBlock = "base:stone";

        public override string Generate(ref BlockGenInput input)
        {
            FastNoiseLite RandomMain1 = new()
            {
                Seed = ChunkManager.Seed,
                NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin,
                Frequency = 0.002f,
                DomainWarpEnabled = true,
                DomainWarpAmplitude = 3,
            };
            FastNoiseLite RandomMain2 = new()
            {
                Seed = ChunkManager.Seed,
                NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex,
                Frequency = 0.013f
            };

            string block = "base:air";
            var rand = RandomMain1.GetNoise3D(input.Position.X, input.Position.Y, input.Position.Z) +
                RandomMain2.GetNoise3D(input.Position.X, input.Position.Y, input.Position.Z) * 0.5f;

            var top = Mathf.Max(0, input.Position.Y * 0.1f);
            if (rand > 0.2f + top)
            {
                if (new Vector3(input.Position.X, input.Position.Y, input.Position.Z).DistanceSquaredTo(new Vector3(-0.5f, -0.5f, -0.5f)) > 40f)
                    block = "base:stone";
            }

            return block;
        }
    }

    public class BlockGenLayerOre : BlockGenLayer
    {
        public override string Generate(ref BlockGenInput input)
        {
            FastNoiseLite Noise = new()
            {
                Seed = ChunkManager.Seed,
                NoiseType = FastNoiseLite.NoiseTypeEnum.Value,
                Frequency = 2.51f,
                DomainWarpEnabled = true,
                DomainWarpAmplitude = 5,
            };

            string block = input.CurrentBlocks[input.IndexPosition.X, input.IndexPosition.Y, input.IndexPosition.Z];
            if (block == "base:air") return block;

            var val = Noise.GetNoise3D(input.Position.X, input.Position.Y, input.Position.Z) * 10000;
            var ores = ResourceManager.BlockOreRegistry.OrderBy((a) => a.Value.HashId % val);

            foreach (var ore in ores)
            {
                var threshold = ore.Value.ChancePerChunk;// / (ChunkSize * ChunkSize * ChunkSize);
                if (Noise.GetNoise3D(input.Position.X, input.Position.Y, input.Position.Z) > 1 - threshold)
                {
                    block = ore.Value.FullId;
                    // ! walk ore vein around, settings blocks
                }
            }

            return block;
        }
    }

    public class BlockGenLayerEdge : BlockGenLayer
    {
        public override string Generate(ref BlockGenInput input)
        {
            string block = input.CurrentBlocks[input.IndexPosition.X, input.IndexPosition.Y, input.IndexPosition.Z];
            if (!input.Position.IsInside(-500, 500))
            {
                block = "base:irrefragabiles";
            }
            return block;
        }
    }

    // public class BlockGenNoise
    // {
    //     [Export]
    //     FastNoiseLite.NoiseTypeEnum NoiseType
    // }
}