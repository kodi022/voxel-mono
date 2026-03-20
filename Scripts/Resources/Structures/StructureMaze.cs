using System.Threading.Tasks;
using Godot;

namespace Voxel.Resource;

[GlobalClass]
public partial class StructureMaze : Structure
{
    public override async void GenerateBlocks(StructureInstance instance, ChunkVec3 position)
    {
        await Task.Run(async () =>
        {
            SetValuesManual(instance, position, new Vector3I(99, 5, 99));

            for (int x = 0; x < 99; x++) for (int y = 0; y < 5; y++) for (int z = 0; z < 99; z++)
                instance.StructureBlocks[x, y, z] = ResourceManager.GetAir();

            for (int x = 0; x < 33; x++) for (int z = 0; z < 33; z++)
            {
                var xScale = x * 3;
                var zScale = z * 3;

                // floor
                instance.StructureBlocks[xScale, 0, zScale] = ResourceManager.GetBlock("base:brick");
                instance.StructureBlocks[xScale + 1, 0, zScale] = ResourceManager.GetBlock("base:brick");
                instance.StructureBlocks[xScale + 2, 0, zScale] = ResourceManager.GetBlock("base:brick");
                instance.StructureBlocks[xScale, 0, zScale + 1] = ResourceManager.GetBlock("base:brick");
                instance.StructureBlocks[xScale + 1, 0, zScale + 1] = ResourceManager.GetBlock("base:brick");
                instance.StructureBlocks[xScale + 2, 0, zScale + 1] = ResourceManager.GetBlock("base:brick");
                instance.StructureBlocks[xScale, 0, zScale + 2] = ResourceManager.GetBlock("base:brick");
                instance.StructureBlocks[xScale + 1, 0, zScale + 2] = ResourceManager.GetBlock("base:brick");
                instance.StructureBlocks[xScale + 2, 0, zScale + 2] = ResourceManager.GetBlock("base:brick");

                // corner
                instance.StructureBlocks[xScale, 1, zScale] = ResourceManager.GetBlock("base:brick");
                instance.StructureBlocks[xScale, 2, zScale] = ResourceManager.GetBlock("base:brick");
                instance.StructureBlocks[xScale, 3, zScale] = ResourceManager.GetBlock("base:brick");

                var pos1 = (BlockVec3)position;
                var pos2 = new BlockVec3(x, 0, z);

                if (GetRand(pos1 + pos2, 8927245) > 0.5f)
                {
                    instance.StructureBlocks[xScale + 1, 1, zScale] = ResourceManager.GetBlock("base:brick");
                    instance.StructureBlocks[xScale + 1, 2, zScale] = ResourceManager.GetBlock("base:brick");
                    instance.StructureBlocks[xScale + 1, 3, zScale] = ResourceManager.GetBlock("base:brick");
                    instance.StructureBlocks[xScale + 2, 1, zScale] = ResourceManager.GetBlock("base:brick");
                    instance.StructureBlocks[xScale + 2, 2, zScale] = ResourceManager.GetBlock("base:brick");
                    instance.StructureBlocks[xScale + 2, 3, zScale] = ResourceManager.GetBlock("base:brick");
                }

                if (GetRand(pos1 + pos2, 5838934) > 0.5f)
                {
                    instance.StructureBlocks[xScale, 1, zScale + 1] = ResourceManager.GetBlock("base:brick");
                    instance.StructureBlocks[xScale, 2, zScale + 1] = ResourceManager.GetBlock("base:brick");
                    instance.StructureBlocks[xScale, 3, zScale + 1] = ResourceManager.GetBlock("base:brick");
                    instance.StructureBlocks[xScale, 1, zScale + 2] = ResourceManager.GetBlock("base:brick");
                    instance.StructureBlocks[xScale, 2, zScale + 2] = ResourceManager.GetBlock("base:brick");
                    instance.StructureBlocks[xScale, 3, zScale + 2] = ResourceManager.GetBlock("base:brick");
                }
            }

            instance.GeneratedBlocks = true;
        });
    }
}