using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Voxel.World;

public partial class Chunk
{
    public static readonly OrmMaterial3D BlockMaterial = GD.Load<OrmMaterial3D>("res://Materials/Block.tres");
    public static readonly OrmMaterial3D BlockTransMaterial = GD.Load<OrmMaterial3D>("res://Materials/Block_Trans.tres");
    public static readonly Texture2D MissingTexture = GD.Load<Texture2D>("res://Images/missing.png");

    public static readonly Vector3[][] FaceVertexOffsets =
    [
        [new ( 1.0f, 1.0f, 0.0f), new ( 1.0f, 1.0f, 1.0f), new ( 0.0f, 1.0f, 1.0f), new ( 0.0f, 1.0f, 0.0f)],
        [new ( 1.0f, 0.0f, 1.0f), new ( 1.0f, 0.0f, 0.0f), new ( 0.0f, 0.0f, 0.0f), new ( 0.0f, 0.0f, 1.0f)],
        [new ( 0.0f, 1.0f, 1.0f), new ( 1.0f, 1.0f, 1.0f), new ( 1.0f, 0.0f, 1.0f), new ( 0.0f, 0.0f, 1.0f)],
        [new ( 1.0f, 1.0f, 0.0f), new ( 0.0f, 1.0f, 0.0f), new ( 0.0f, 0.0f, 0.0f), new ( 1.0f, 0.0f, 0.0f)],
        [new ( 1.0f, 1.0f, 1.0f), new ( 1.0f, 1.0f, 0.0f), new ( 1.0f, 0.0f, 0.0f), new ( 1.0f, 0.0f, 1.0f)],
        [new ( 0.0f, 1.0f, 0.0f), new ( 0.0f, 1.0f, 1.0f), new ( 0.0f, 0.0f, 1.0f), new ( 0.0f, 0.0f, 0.0f)],
    ];

    public static readonly Vector2B[] FaceUVs =
    [
        new (0,0),
        new (1,0),
        new (1,1),
        new (0,1),
    ];

    private static readonly Dictionary<int, Material> blockMaterials = [];

    private Rid meshInstance;
    private ArrayMesh meshInstanceData;
    private Rid physicsMeshInstance;
    private Rid physicsMeshInstanceShape;

    private List<int> surfaceBlockIds;
    private int surfaceCount = 0;
    private List<Vector3> physicsMeshFaces;

    public Task GenerateMeshData()
    {
        MeshGenerating = true;

        meshInstanceData = new();
        surfaceBlockIds = [];
        physicsMeshFaces = [];

        // < blockId, < lodId, positions > >
        Dictionary<int, Dictionary<int, List<Vector4I>>> surfaces = [];

        for (int LOD = 0; LOD < 1; LOD++)
        {
            var blockSize = (sbyte)Mathf.Pow(2, LOD);
            for (sbyte x = 0; x < ChunkSize; x += blockSize) for (sbyte z = 0; z < ChunkSize; z += blockSize) for (sbyte y = 0; y < ChunkSize; y += blockSize)
            {
                var block = Blocks[x, y, z];
                var fullBlock = ResourceManager.GetBlock(block.HashId);
                if (fullBlock.BlockCull == Resource.Block.BlockCullEnum.Translucent) continue;

                for (sbyte w = 0; w < 6; w++)
                {
                    Vector3B checkPos = new Vector3B(x, y, z) + Directions[w] * blockSize;

                    if (checkPos.IsInside(ChunkSize))
                    { // inner edges
                        var adjBlock = ResourceManager.GetBlock(Blocks[checkPos.X, checkPos.Y, checkPos.Z].HashId);
                        if (adjBlock.BlockCull == Resource.Block.BlockCullEnum.Opaque) continue;
                        if (fullBlock.BlockCull == Resource.Block.BlockCullEnum.Transparent && adjBlock.BlockCull == Resource.Block.BlockCullEnum.Transparent) continue;

                        surfaces.TryAdd(block.HashId, []);
                        if (!surfaces[block.HashId].TryAdd(LOD, [new(x, y, z, w)]))
                        {
                            surfaces[block.HashId][LOD].Add(new(x, y, z, w));
                        }
                    }
                    else
                    { // outer edges
                        if (!AdjacentChunks[w]) continue;

                        var chunk2 = ChunkManager.FindChunk(ChunkPosition + Directions[w]);
                        if (chunk2 is null) continue;
                        if (!chunk2.BlocksGenerated) continue;

                        // creates negatives if adding by 16
                        var checkPos2 = (checkPos + ChunkSize * 2) % ChunkSize;
                        var adjBlock = ResourceManager.GetBlock(chunk2.Blocks[checkPos2.X, checkPos2.Y, checkPos2.Z].HashId);
                        if (adjBlock is null) continue;
                        if (adjBlock.BlockCull == Resource.Block.BlockCullEnum.Opaque) continue;
                        if (fullBlock.BlockCull == Resource.Block.BlockCullEnum.Transparent && adjBlock.BlockCull == Resource.Block.BlockCullEnum.Transparent) continue;

                        surfaces.TryAdd(block.HashId, []);
                        if (!surfaces[block.HashId].TryAdd(LOD, [new(x, y, z, w)]))
                        {
                            surfaces[block.HashId][LOD].Add(new(x, y, z, w));
                        }
                    }
                }
            }
        }

        surfaceCount = surfaces.Count;
        if (surfaceCount == 0)
        {
            MeshGenerating = false;
            return Task.CompletedTask;
        }

        var fullRenderDistance = Player.RenderDistance * ChunkSize * Player.RenderDistance * ChunkSize;
        foreach (var blockSurfaceKVP in surfaces)
        {
            int faces = 0;
            var meshVerts = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var indices = new List<int>();
            Godot.Collections.Dictionary lods = []; // (float, int[])   float is distance to use, int[] is indexes of the geometry

            // GD.Print($"--{WorldPosition}: {blockSurfaceKVP.Key}");

            foreach (var lodPosKVP in blockSurfaceKVP.Value)
            {
                var blockSize = (sbyte)Mathf.Pow(2, lodPosKVP.Key);
                List<int> lodIndices = [];
                foreach (var pos in lodPosKVP.Value)
                {
                    int x = pos.X, y = pos.Y, z = pos.Z, w = pos.W;

                    // mesh verts
                    for (int v = 0; v < 4; v++)
                    {
                        var off = FaceVertexOffsets[w][v] * blockSize;
                        meshVerts.Add(new Vector3(x + off.X, y + off.Y, z + off.Z));
                        normals.Add(Directions[w]);
                        uvs.Add((Vector2)FaceUVs[v] * blockSize);
                    }

                    // phys verts
                    if (lodPosKVP.Key == 0)
                    {
                        var off = FaceVertexOffsets[w][0] * blockSize;
                        physicsMeshFaces.Add(new Vector3(x + off.X, y + off.Y, z + off.Z));
                        off = FaceVertexOffsets[w][1] * blockSize;
                        physicsMeshFaces.Add(new Vector3(x + off.X, y + off.Y, z + off.Z));
                        off = FaceVertexOffsets[w][2] * blockSize;
                        physicsMeshFaces.Add(new Vector3(x + off.X, y + off.Y, z + off.Z));
                        off = FaceVertexOffsets[w][2] * blockSize;
                        physicsMeshFaces.Add(new Vector3(x + off.X, y + off.Y, z + off.Z));
                        off = FaceVertexOffsets[w][3] * blockSize;
                        physicsMeshFaces.Add(new Vector3(x + off.X, y + off.Y, z + off.Z));
                        off = FaceVertexOffsets[w][0] * blockSize;
                        physicsMeshFaces.Add(new Vector3(x + off.X, y + off.Y, z + off.Z));
                    }

                    var o = faces * 4;
                    lodIndices.AddRange([
                        o, o + 1, o + 2,
                        o + 2, o + 3, o
                    ]);
                    faces++;
                }

                indices.AddRange(lodIndices);
                var distance = fullRenderDistance / blockSize;
                lods.Add((float)distance, lodIndices.ToArray());

                // GD.Print($"-{lodPosKVP.Key}: {lodIndices.Count} ({lodIndices.First()})");
            }

            if (faces == 0) continue;

            var arrays = new Godot.Collections.Array();
            arrays.Resize((int)Mesh.ArrayType.Max);
            arrays[(int)Mesh.ArrayType.Vertex] = meshVerts.ToArray();
            arrays[(int)Mesh.ArrayType.Normal] = normals.ToArray();
            arrays[(int)Mesh.ArrayType.TexUV] = uvs.ToArray();
            arrays[(int)Mesh.ArrayType.Index] = indices.ToArray();

            // errors if no lods for given surface
            if (lods.Count < 2) meshInstanceData.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
            else meshInstanceData.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays, lods: lods);

            surfaceBlockIds.Add(blockSurfaceKVP.Key);
        }

        MeshGenerating = false;
        return Task.CompletedTask;
    }

    public void CreateMesh()
    {
        if (meshInstance.IsValid)
            RenderingServer.FreeRid(meshInstance);

        if (MeshGenerating) return;
        var transform = new Transform3D(Basis.Identity, ChunkPosition.ToVector3Scaled());
        meshInstance = RenderingServer.InstanceCreate();
        RenderingServer.InstanceSetBase(meshInstance, meshInstanceData.GetRid());
        RenderingServer.InstanceSetScenario(meshInstance, world3D.Scenario);
        RenderingServer.InstanceSetTransform(meshInstance, transform);
        RenderingServer.InstanceGeometrySetLodBias(meshInstance, -1f);

        if (MeshGenerating) return;
        int surfaceIndex = 0;
        foreach (var id in surfaceBlockIds)
        {
            if (!blockMaterials.TryGetValue(id, out Material mat))
            {
                var block = ResourceManager.BlockRegistry[id];
                if (block.BlockMaterial == Resource.Block.BlockMaterialEnum.Default)
                {
                    mat = (OrmMaterial3D)BlockMaterial.Duplicate();
                    ((OrmMaterial3D)mat).AlbedoColor = block.ColorTint;
                    ((OrmMaterial3D)mat).AlbedoTexture = SetTextureFromBlock(block.AlbedoTexture);
                    ((OrmMaterial3D)mat).NormalTexture = SetTextureFromBlock(block.NormalTexture);
                    ((OrmMaterial3D)mat).EmissionTexture = SetTextureFromBlock(block.EmissionTexture);
                }
                else if (block.BlockMaterial == Resource.Block.BlockMaterialEnum.Transparent)
                {
                    mat = (OrmMaterial3D)BlockTransMaterial.Duplicate();
                    ((OrmMaterial3D)mat).AlbedoColor = block.ColorTint;
                    ((OrmMaterial3D)mat).AlbedoTexture = SetTextureFromBlock(block.AlbedoTexture);
                    ((OrmMaterial3D)mat).NormalTexture = SetTextureFromBlock(block.NormalTexture);
                    ((OrmMaterial3D)mat).EmissionTexture = SetTextureFromBlock(block.EmissionTexture);
                }
                else
                {
                    mat = (Material)block.CustomMaterial.Duplicate();
                }

                blockMaterials.Add(id, mat);
            }

            RenderingServer.InstanceSetSurfaceOverrideMaterial(meshInstance, surfaceIndex, mat.GetRid());
            surfaceIndex++;
        }

        surfaceBlockIds = null;
    }

    public void CreatePhysics()
    {
        if (physicsMeshInstance.IsValid)
            PhysicsServer3D.FreeRid(physicsMeshInstance);

        if (MeshGenerating) return;
        var transform = new Transform3D(Basis.Identity, ChunkPosition.ToVector3Scaled());
        physicsMeshInstance = PhysicsServer3D.BodyCreate();
        physicsMeshInstanceShape = PhysicsServer3D.ConcavePolygonShapeCreate();
        var physicsMeshData = new Godot.Collections.Dictionary<string, Variant>() { { "faces", physicsMeshFaces.ToArray() }, { "backface_collision", false } };
        PhysicsServer3D.ShapeSetData(physicsMeshInstanceShape, physicsMeshData);
        PhysicsServer3D.BodyAddShape(physicsMeshInstance, physicsMeshInstanceShape);
        PhysicsServer3D.BodySetMode(physicsMeshInstance, PhysicsServer3D.BodyMode.Static);
        PhysicsServer3D.BodySetState(physicsMeshInstance, PhysicsServer3D.BodyState.Transform, transform);
        PhysicsServer3D.BodySetSpace(physicsMeshInstance, world3D.Space);
    }

    private static Texture2D SetTextureFromBlock(Texture2D resourceTexture)
    {
        if (resourceTexture is not null) return resourceTexture;
        return MissingTexture;
    }
}