using Godot;
using System;
using System.Collections.Generic;
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

    // static noise because it seems godot does not free old instanced noises
    // previously created FastNoiseLite inside of NoiseLayers, godot quickly kept >2000000 of them
    // ! make noise settings struct, get from list using struct hash
    FastNoiseLite noise = new();

    FastNoiseLite SetNoiseSettings(int seedOffset, FastNoiseLite.NoiseTypeEnum noiseType, float frequency = 1f, float warpAmplitude = 0f)
    {
        bool warpEnabled = warpAmplitude != 0;
        noise.Seed = ChunkManager.Seed + (seedOffset % 10);
        noise.NoiseType = noiseType;
        noise.Frequency = frequency;
        noise.DomainWarpEnabled = warpEnabled;
        noise.DomainWarpAmplitude = warpAmplitude;
        noise.DomainWarpType = FastNoiseLite.DomainWarpTypeEnum.BasicGrid;
        return noise;
    }

    public Task GenerateBlockData()
    {
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
                        var pos = new Vector3(x, y, z) + ChunkPosition.ToVector3Scaled();
                        var blockGenInput = new BlockGenInput() { Chunk = this, CurrentBlocks = tempBlocks, Position = BlockVec3.FromVector3(pos), IndexPosition = new(x, y, z) };
                        string blockId = layer.Generate(ref blockGenInput);

                        tempBlocks[x, y, z] = blockId;
                    }
                }
            }
        }

        var noise = SetNoiseSettings(0, FastNoiseLite.NoiseTypeEnum.Value);

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
                        block.Hp = noise.GetNoise3D(x, y, z) * blockHpSize + block.HpRange.X;
                    }

                    Blocks[x, y, z] = block;
                }
            }
        }

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
        public BlockVec3 Position;
        public BlockVec3 IndexPosition;
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
            var rand = 0f;
            var noise = input.Chunk.SetNoiseSettings(0, FastNoiseLite.NoiseTypeEnum.Perlin, 1f / 1187f);
            rand += noise.GetNoise3Dv(input.Position.ToVector3());
            noise = input.Chunk.SetNoiseSettings(0, FastNoiseLite.NoiseTypeEnum.Perlin, 1f / 461f);
            rand += noise.GetNoise3Dv(input.Position.ToVector3()) * 0.4f;
            noise = input.Chunk.SetNoiseSettings(0, FastNoiseLite.NoiseTypeEnum.Value, 1f / 59f);
            rand += noise.GetNoise3Dv(input.Position.ToVector3()) * 0.2f;

            string block = "base:air";
            var top = Mathf.Max(0, input.Position.Y * 0.01f);
            if (rand > -0.2f + top)
            {
                // spawn air sphere
                if (new Vector3(input.Position.X, input.Position.Y, input.Position.Z).DistanceSquaredTo(new Vector3(-0.5f, -0.5f, -0.5f)) > 40f)
                    block = "base:stone";
            }

            // ! remove later
            if (new Vector2(input.Position.X, input.Position.Z).DistanceSquaredTo(new Vector2(6f, 0f)) < 8f)
            {
                block = "base:air";
            }

            return block;
        }
    }

    public class BlockGenLayerOre : BlockGenLayer
    {
        private static float GetSeededRandom(BlockVec3 position, int seedOffset)
        {
            int hash = position.GetVecHash() ^ ChunkManager.Seed ^ seedOffset;
            return Math.Abs(hash % 10000000 / 10000000f);
        }

        public override string Generate(ref BlockGenInput input)
        {
            string block = input.CurrentBlocks[input.IndexPosition.X, input.IndexPosition.Y, input.IndexPosition.Z];
            if (block == "base:air") return block;

            var ores = ResourceManager.BlockOreRegistry.OrderBy((a) => a.Value.AmountPerChunk);
            foreach (var ore in ores)
            {
                var threshold = ore.Value.AmountPerChunk / (ChunkSize * ChunkSize * ChunkSize);
                if (GetSeededRandom(input.Position, ore.Value.HashId) < threshold)
                {
                    block = ore.Value.FullId;

                    var gSize = ore.Value.GroupSize;
                    if (gSize < 2) break;

                    var chance = 1f;
                    var reduct = 1f / gSize;
                    var rand = GetSeededRandom(input.Position + Vector3I.Left, ore.Value.HashId);
                    var pos = Directions[(int)(rand * 6f)] + input.Position;
                    var localPos = Directions[(int)(rand * 6f)] + input.IndexPosition;
                    for (int i = 0; i < gSize; i++)
                    {
                        if (!localPos.IsInside(ChunkSize)) break;
                        if (input.CurrentBlocks[localPos.X, localPos.Y, localPos.Z] == "base:air") break;
                        if (GetSeededRandom(pos, ore.Value.HashId) > chance) break;

                        chance -= reduct;
                        input.CurrentBlocks[localPos.X, localPos.Y, localPos.Z] = block;
                        rand = GetSeededRandom(pos, ore.Value.HashId + 11);
                        pos = Directions[(int)(rand * 6f)] + pos;
                        localPos = Directions[(int)(rand * 6f)] + localPos;
                    }

                    break;
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
}