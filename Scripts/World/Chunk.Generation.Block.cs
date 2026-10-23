using Voxel.Resource;

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

    public Task GenerateBlockData()
    {
        if (Lod > 0) return Task.CompletedTask;

        for (sbyte x = 0; x < ChunkSize; x++)
            for (sbyte y = 0; y < ChunkSize; y++)
                for (sbyte z = 0; z < ChunkSize; z++)
                    Blocks[x, y, z] = ResourceManager.GetBlockInstance("base:air");

        List<BlockGenLayer> layers = [
            new BlockGenLayerBase(),
            new BlockGenLayerOre(),
            new BlockGenLayerStructures(),
            new BlockGenLayerEdge()
        ];

        for (sbyte x = 0; x < ChunkSize; x++)
        {
            for (sbyte z = 0; z < ChunkSize; z++)
            {
                for (sbyte y = 0; y < ChunkSize; y++)
                {
                    var pos = new BlockVec3(x, y, z) + ChunkPosition.ToVector3Scaled();
                    Blocks[x, y, z] = GenerateBlock(pos, layers);

                    if (Blocks[x, y, z].BlockInfo.UsesTick)
                    {
                        TickedBlocks.Add(pos);
                    }
                }
            }
        }

        UsingTick = TickedBlocks.Count != 0;

        var noise = SetNoiseSettings(0, FastNoiseLite.NoiseTypeEnum.Value, 1f);
        for (sbyte x = 0; x < ChunkSize; x++)
        {
            for (sbyte z = 0; z < ChunkSize; z++)
            {
                for (sbyte y = 0; y < ChunkSize; y++)
                {
                    var block = Blocks[x, y, z];
                    if (block != 0 && !block.BlockInfo.Unbreakable)
                    {
                        var blockHpSize = block.BlockInfo.HpRange.Y - block.BlockInfo.HpRange.X;
                        var halfBlockHpSize = blockHpSize * 0.5f;
                        var hp = (noise.GetNoise3D(x, y, z) * halfBlockHpSize) + block.BlockInfo.HpRange.X + halfBlockHpSize;
                        Blocks[x, y, z].Hp = hp;
                        Blocks[x, y, z].GeneratedMaxHp = hp;
                    }
                }
            }
        }

        BlocksGenerated = true;
        return Task.CompletedTask;
    }

    // ! layers are handled after iteration, causing overwrites
    private BlockInstance GenerateBlock(BlockVec3 pos, List<BlockGenLayer> layerOverride = null)
    {
        layerOverride ??= [
            new BlockGenLayerBase(),
            //new BlockGenLayerOre(),
            //new BlockGenLayerStructures(),
            //new BlockGenLayerEdge()
        ];

        var localPos = pos.ToLocal(ChunkPosition);
        var blockGenInput = new BlockGenInput() { Chunk = this, CurrentBlock = Blocks[localPos.X, localPos.Y, localPos.Z], Position = pos, IndexPosition = localPos };
        BlockInstance block = null;
        foreach (var layer in layerOverride)
        {
            block = layer.Generate(ref blockGenInput);
            blockGenInput.CurrentBlock = block;
        }

        return block;
    }

    public struct BlockGenInput
    {
        public Chunk Chunk;
        public BlockInstance CurrentBlock;
        public BlockVec3 Position;
        public BlockVec3 IndexPosition;
    }

    public class BlockGenLayer
    {
        public virtual BlockInstance Generate(ref BlockGenInput input)
        {
            return ResourceManager.BlockInstanceAir;
        }
    }

    public class BlockGenLayerBase : BlockGenLayer
    {
        public string baseBlock = "base:stone";

        public override BlockInstance Generate(ref BlockGenInput input)
        {
            var rand = 0f;
            var noise = input.Chunk.SetNoiseSettings(0, FastNoiseLite.NoiseTypeEnum.Perlin, 1f / 607f);
            rand += noise.GetNoise3Dv(input.Position.ToVector3());
            var noise2 = input.Chunk.SetNoiseSettings(0, FastNoiseLite.NoiseTypeEnum.Perlin, 1f / 127f);
            rand += noise2.GetNoise3Dv(input.Position.ToVector3()) * 0.2f;
            var noise3 = input.Chunk.SetNoiseSettings(0, FastNoiseLite.NoiseTypeEnum.Value, 1f / 7f);
            rand += noise3.GetNoise3Dv(input.Position.ToVector3()) * 0.01f;

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

            return ResourceManager.GetBlockInstance(block);
        }
    }

    public class BlockGenLayerOre : BlockGenLayer
    {
        public override BlockInstance Generate(ref BlockGenInput input)
        {
            if (input.CurrentBlock == "base:air") return input.CurrentBlock;

            var ores = ResourceManager.BlockOreRegistry.OrderBy((a) => a.Value.AmountPerChunk);
            foreach (var ore in ores)
            {
                var threshold = ore.Value.AmountPerChunk / (ChunkSize * ChunkSize * ChunkSize);
                if (Global.GetSeededRandom(input.Position, ore.Value.HashId) < threshold)
                {
                    input.CurrentBlock = ResourceManager.GetBlockInstance(ore.Value.FullId);

                    var gSize = ore.Value.GroupSize;
                    if (gSize < 2) break;

                    var chance = 1f;
                    var reduct = 1f / gSize;
                    var worldPos = Directions[0] + input.Position;
                    for (int i = 0; i < 100; i++)
                    {
                        var randDir = Global.GetSeededRandom(worldPos, ore.Value.HashId) * 6f;
                        worldPos += Directions[(int)randDir];

                        var localPos = worldPos.ToLocal(input.Chunk.ChunkPosition);
                        if (!localPos.IsInside(ChunkSize)) break;
                        if (input.Chunk.Blocks[localPos.X, localPos.Y, localPos.Z] == "base:air") break;
                        if (Global.GetSeededRandom(worldPos + worldPos, ore.Value.HashId) > chance) break;

                        chance -= reduct;
                        input.Chunk.Blocks[localPos.X, localPos.Y, localPos.Z] = ResourceManager.GetBlockInstance(ore.Value.FullId);
                    }
                    break;
                }
            }

            return input.CurrentBlock;
        }
    }
    public class BlockGenLayerStructures : BlockGenLayer
    {
        public override BlockInstance Generate(ref BlockGenInput input)
        {
            var structures = input.Chunk.region.Structures.OrderBy(s => s.Priority);
            foreach (var structure in structures)
            {
                if (!structure.GeneratedBlocks) continue;

                var relativePos = structure.CenterPosition - input.Position;
                var half = structure.StructureBlocksLengths / 2;
                if (relativePos.IsInside(-half, half))
                {
                    var index = relativePos + half;
                    if (structure.StructureBlocks[index.X, index.Y, index.Z] is not null)
                    {
                        return ResourceManager.GetBlockInstance(structure.StructureBlocks[index.X, index.Y, index.Z].FullId);
                    }
                }
            }

            return input.CurrentBlock;
        }
    }

    public class BlockGenLayerEdge : BlockGenLayer
    {
        public override BlockInstance Generate(ref BlockGenInput input)
        {
            if (!input.Position.IsInside(-1000, 1000))
            {
                return ResourceManager.GetBlockInstance("base:irrefragabiles");
            }
            return input.CurrentBlock;
        }
    }
}