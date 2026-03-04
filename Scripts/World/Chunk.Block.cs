using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Voxel.World;

public partial class Chunk : MeshInstance3D
{
    public Task GenerateBlocks()
    {
        string[,,] tempBlocks = new string[ChunkSize, ChunkSize, ChunkSize];
        Dictionary<int, List<Vector3I>> surfaces = [];

        FastNoiseLite BiomeNoise = new()
        {
            Seed = ChunkManager.Seed + 1,
            NoiseType = FastNoiseLite.NoiseTypeEnum.Cellular,
            Frequency = 0.001f
        };


        FastNoiseLite RandomMain1 = new()
        {
            Seed = ChunkManager.Seed,
            NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin,
            Frequency = 0.003f
        };
        FastNoiseLite RandomMain2 = new()
        {
            Seed = ChunkManager.Seed,
            NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex,
            Frequency = 0.013f
        };

        FastNoiseLite Random2 = new()
        {
            Seed = ChunkManager.Seed + 2,
            NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex,
            Frequency = 5678f
        };

        IterateChunk((x, y, z) =>
        {
            var pos = new Vector3(x + WorldPosition.X, y + WorldPosition.Y, z + WorldPosition.Z);

            string blockId = "base:air";

            var rand = RandomMain1.GetNoise3D(pos.X, pos.Y, pos.Z) + RandomMain2.GetNoise3D(pos.X, pos.Y, pos.Z) * 0.5f;
            if (rand > 0f)
            {
                if (Random2.GetNoise3D(pos.X, pos.Y, pos.Z) > -0.6f)
                {
                    blockId = "base:stone";
                }
                else
                {
                    blockId = "base:copper";
                }
            }

            if (pos.DistanceSquaredTo(new Vector3(-0.5f, -0.5f, -0.5f)) < 40f)
            {
                blockId = "base:air";
            }

            tempBlocks[x, y, z] = blockId;
        });

        Dictionary<int, int> blockCount = [];
        IterateChunk((x, y, z) =>
        {
            var block = ResourceManager.GetBlock(tempBlocks[x, y, z]);
            var blockHpSize = block.HpRange.Y - block.HpRange.X;
            block.Hp = Random2.GetNoise3D(x, y, z) * blockHpSize + block.HpRange.X;
            Blocks[x, y, z] = block;

            if (!blockCount.TryAdd(block.HashId, 1))
            {
                blockCount[block.HashId] += 1;
            }
        });

        return Task.CompletedTask;
    }
}