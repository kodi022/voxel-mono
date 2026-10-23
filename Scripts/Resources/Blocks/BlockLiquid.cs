using Voxel.World;

namespace Voxel.Resource;

[GlobalClass]
public partial class BlockLiquid : Block
{
    // ! rewrite so water is 4x4x8 or so grid to flow around and shit

    [Export]
    private float Flowrate = 0.01f;

    public override void OnStart(GenericInfo info)
    {

    }

    public override void OnTick(GenericInfo info)
    {
        // Water spreading logic
        SpreadLiquid(info);
    }

    public void AddLiquid(BlockInstanceLiquid instance, float amount)
    {
        var half = (Half)(amount * 0.25f);
        // instance.LiquidLevels[0] += half;
        // instance.LiquidLevels[1] += half;
        // instance.LiquidLevels[2] += half;
        // instance.LiquidLevels[3] += half;
    }

    public void RemoveLiquid(BlockInstanceLiquid instance, float amount)
    {
        var half = (Half)(amount * 0.25f);
        // instance.LiquidLevels[0] -= half;
        // instance.LiquidLevels[1] -= half;
        // instance.LiquidLevels[2] -= half;
        // instance.LiquidLevels[3] -= half;
    }

    private void SpreadLiquid(GenericInfo info)
    {
        //     if (info.BlockInstance.LiquidSettled) return;

        //     GD.Print(info.BlockLocalPosition);
        //     // delete if too low
        //     if (info.BlockInstance.LiquidAvgLevel <= 0.1f)
        //     {
        //         info.Chunk.SetBlock(info.BlockLocalPosition, FullId);
        //         return;
        //     }

        //     BlockVec3[] directions = [
        //         info.BlockLocalPosition + new BlockVec3(0, -1, 0),
        //         info.BlockLocalPosition + new BlockVec3(1, 0, 0),
        //         info.BlockLocalPosition + new BlockVec3(-1, 0, 0),
        //         info.BlockLocalPosition + new BlockVec3(0, 0, 1),
        //         info.BlockLocalPosition + new BlockVec3(0, 0, -1)
        //     ];
        //     // go down
        //     BlockInstance downBlock = Chunk.ChunkGetBlock((BlockVec3)info.Chunk.ChunkPosition + directions[0]);
        //     if (downBlock == 0)
        //     {
        //         Chunk.ChunkPlaceBlock((BlockVec3)info.Chunk.ChunkPosition + directions[0], FullId);
        //         downBlock = Chunk.ChunkGetBlock((BlockVec3)info.Chunk.ChunkPosition + directions[0]);

        //         if (info.BlockInstance.LiquidAvgLevel < Flowrate)
        //         {
        //             AddWater(downBlock, Flowrate);
        //             RemoveWater(info.BlockInstance, Flowrate);
        //         }
        //         else
        //         {
        //             AddWater(downBlock, info.BlockInstance.LiquidAvgLevel);
        //             info.Chunk.SetBlock(info.BlockLocalPosition, "base:air");
        //         }
        //         return;
        //     }
        //     if (downBlock == HashId)
        //     {
        //         if (info.BlockInstance.LiquidAvgLevel < Flowrate)
        //         {
        //             AddWater(downBlock, Flowrate);
        //             RemoveWater(info.BlockInstance, Flowrate);
        //         }
        //         else
        //         {
        //             AddWater(downBlock, info.BlockInstance.LiquidAvgLevel);
        //             info.Chunk.SetBlock(info.BlockLocalPosition, "base:air");
        //         }
        //         return;
        //     }



        // Then flow horizontally to adjacent blocks with lower water levels
        // for (int i = 1; i < directions.Length; i++)
        // {
        //     BlockInstance adjacentBlock = Chunk.ChunkSelectBlock(directions[i]);
        //     if (CanFlowInto(adjacentBlock))
        //     {
        //         int adjacentLevel = GetWaterLevel(adjacentBlock);
        //         if (currentLevel > adjacentLevel)
        //         {
        //             int flowAmount = Mathf.Min(currentLevel - adjacentLevel, MaxWaterLevel - adjacentLevel) / 2;
        //             if (flowAmount > 0)
        //             {
        //                 instance.WaterLevel -= flowAmount;
        //                 SetWaterLevel(adjacentBlock, adjacentLevel + flowAmount, directions[i]);
        //             }
        //         }
        //     }
        // }

        // settle self
        // var avg = (Half)info.BlockInstance.LiquidAvgLevel;
        // var changes = new List<Half>();
        // foreach (var level in info.BlockInstance.LiquidLevels)
        // {
        //     changes.Add(Half.Min((Half)Flowrate, avg - level));
        // }

        // for (int i = 0; i < 3; i++)
        // {
        //     info.BlockInstance.LiquidLevels[i] += changes[i];
        // }

        // If water level is very low, remove the block
        // if (instance.WaterLevel <= 0)
        // {
        //     var chunk = ChunkManager.FindChunk((ChunkVec3)position);
        //     chunk?.SetBlock(position, "base:air");
        // }
    }

    public override void GenerateProceduralMesh(ref Chunk.MeshGenerationData data)
    {
        // if (data.Lod > 0) return;

        // int x = data.PosDir.X, y = data.PosDir.Y, z = data.PosDir.Z, w = data.PosDir.W;
        // // mesh verts
        // if (w == 0)
        // {
        //     for (int i = 0; i < 9; i++)
        //     {
        //         var tileX = i % 3 * 0.33333f;
        //         var tileZ = i / 3 * 0.33333f;

        //         for (int v = 0; v < 4; v++)
        //         {
        //             var faceOffsets = Chunk.FaceVertexOffsets[0][v] * 0.33333f;

        //             var fluidIndex = -1;
        //             var vertexPos = new Vector3(tileX + faceOffsets.X, 0, tileZ + faceOffsets.Z);
        //             if (vertexPos.X == 0.33333f && vertexPos.Z == 0.33333f) fluidIndex = 0;
        //             if (vertexPos.X == 0.66666f && vertexPos.Z == 0.33333f) fluidIndex = 1;
        //             if (vertexPos.X == 0.33333f && vertexPos.Z == 0.66666f) fluidIndex = 2;
        //             if (vertexPos.X == 0.66666f && vertexPos.Z == 0.66666f) fluidIndex = 3;

        //             data.Uvs.Add(new Vector2(vertexPos.X, vertexPos.Z));

        //             var level = fluidIndex == -1 ? 0 : (float)data.BlockInstance.LiquidLevels[fluidIndex];
        //             // max height 30 / 32 pixels
        //             vertexPos += new Vector3(x, y + (level / 0.9375f), z);
        //             data.MeshVerts.Add(vertexPos);
        //             data.Normals.Add(Chunk.Directions[w]); // ! normal wrong
        //         }

        //         var o = data.FaceCount * 4;
        //         data.Indices.AddRange([
        //             o, o + 1, o + 2,
        //             o + 2, o + 3, o
        //         ]);
        //         data.FaceCount++;
        //     }
    }
}