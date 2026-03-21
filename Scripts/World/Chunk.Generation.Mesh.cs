using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;
using Voxel.Resource;

namespace Voxel.World;

public partial class Chunk
{
    public static readonly OrmMaterial3D BlockMaterial = GD.Load<OrmMaterial3D>("res://Materials/Block.tres");
    public static readonly Texture2D MissingTexture = GD.Load<Texture2D>("res://Images/missing.png");
    public static readonly Texture2D DefaultOrm = GD.Load<Texture2D>("res://Images/default_orm.png");

    public static readonly Vector3[][] FaceVertexOffsets =
    [
        [new ( 1, 1, 0), new ( 1, 1, 1), new ( 0, 1, 1), new ( 0, 1, 0)],
        [new ( 1, 0, 1), new ( 1, 0, 0), new ( 0, 0, 0), new ( 0, 0, 1)],
        [new ( 0, 1, 1), new ( 1, 1, 1), new ( 1, 0, 1), new ( 0, 0, 1)],
        [new ( 1, 1, 0), new ( 0, 1, 0), new ( 0, 0, 0), new ( 1, 0, 0)],
        [new ( 1, 1, 1), new ( 1, 1, 0), new ( 1, 0, 0), new ( 1, 0, 1)],
        [new ( 0, 1, 0), new ( 0, 1, 1), new ( 0, 0, 1), new ( 0, 0, 0)],
    ];

    public static readonly Vector2B[] OneFaceUVs =
    [
        new (0,0), new (1,0), new (1,1), new (0,1),
    ];

    public static readonly Vector2[] TwoFaceUVs =
    [
        new (0.0f, 0.0f), new (0.5f, 0.0f), new (0.5f, 1.0f), new (0.0f, 1.0f), // top/bottom
        new (0.5f, 0.0f), new (1.0f, 0.0f), new (1.0f, 1.0f), new (0.5f, 1.0f), // sides
    ];

    public static readonly Vector2[] SixFaceUVs =
    [
        new (0.25f, 0.25f), new (0.5f, 0.25f), new (0.5f, 0.5f), new (0.25f, 0.5f), // up
        new (0.25f, 0.75f), new (0.5f, 0.75f), new (0.5f, 1.0f), new (0.25f, 1.0f), // down

        new (0.0f, 0.5f), new (0.25f, 0.5f), new (0.25f, 0.75f), new (0.0f, 0.75f), // left
        new (0.5f, 0.5f), new (0.75f, 0.5f), new (0.75f, 0.75f), new (0.5f, 0.75f), // right

        new (0.25f, 0.5f), new (0.5f, 0.5f), new (0.5f, 0.75f), new (0.25f, 0.75f), // forward
        new (0.75f, 0.5f), new (1.0f, 0.5f), new (1.0f, 0.75f), new (0.75f, 0.75f), // backward
    ];

    // pitch 0 = up, 1 = side, 2 = down
    // yaw 0 = normal, 1 = right, 2 = back, 3 = left

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

        // build surfaces
        // < blockId, < lodId, positions > >
        Dictionary<int, Dictionary<int, List<Vector4I>>> surfaces = [];
        for (int LOD = 0; LOD < 1; LOD++)
        {
            var blockSize = (sbyte)Mathf.Pow(2, LOD);
            for (sbyte x = 0; x < ChunkSize; x += blockSize) for (sbyte z = 0; z < ChunkSize; z += blockSize) for (sbyte y = 0; y < ChunkSize; y += blockSize)
            {
                var blockInfo = Blocks[x, y, z].BlockInfo;
                if (blockInfo.BlockCull == Resource.Block.BlockCullEnum.Translucent) continue;

                for (sbyte w = 0; w < 6; w++)
                {
                    Vector3B checkPos = new Vector3B(x, y, z) + Directions[w] * blockSize;

                    if (checkPos.IsInside(ChunkSize))
                    { // inner edges
                        var adjBlockInfo = Blocks[checkPos.X, checkPos.Y, checkPos.Z].BlockInfo;
                        if (adjBlockInfo.BlockCull == Resource.Block.BlockCullEnum.Opaque) continue;
                        if (blockInfo.BlockCull == Resource.Block.BlockCullEnum.Transparent && adjBlockInfo.BlockCull == Resource.Block.BlockCullEnum.Transparent) continue;

                        surfaces.TryAdd(blockInfo.HashId, []);
                        if (!surfaces[blockInfo.HashId].TryAdd(LOD, [new(x, y, z, w)]))
                        {
                            surfaces[blockInfo.HashId][LOD].Add(new(x, y, z, w));
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
                        var adjBlockInfo = chunk2.Blocks[checkPos2.X, checkPos2.Y, checkPos2.Z].BlockInfo;
                        if (adjBlockInfo is null) continue;
                        if (adjBlockInfo.BlockCull == Resource.Block.BlockCullEnum.Opaque) continue;
                        if (blockInfo.BlockCull == Resource.Block.BlockCullEnum.Transparent && adjBlockInfo.BlockCull == Resource.Block.BlockCullEnum.Transparent) continue;

                        surfaces.TryAdd(blockInfo.HashId, []);
                        if (!surfaces[blockInfo.HashId].TryAdd(LOD, [new(x, y, z, w)]))
                        {
                            surfaces[blockInfo.HashId][LOD].Add(new(x, y, z, w));
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

        // build geometry
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
                List<int> lodIndices = [];
                foreach (var pos in lodPosKVP.Value)
                {
                    var blockInstance = Blocks[pos.X, pos.Y, pos.Z];
                    var blockInfo = blockInstance.BlockInfo;

                    var genData = new MeshGenerationData()
                    {
                        BlockInstance = blockInstance,
                        Lod = lodPosKVP.Key,
                        PosDir = pos,
                        MeshVerts = [],
                        Normals = [],
                        Uvs = [],
                        Indices = [],
                        FaceCount = faces,
                        CurrentLodIndices = [],
                    };

                    blockInfo.GenerateProceduralMesh(ref genData);
                    meshVerts.AddRange(genData.MeshVerts);
                    normals.AddRange(genData.Normals);
                    uvs.AddRange(genData.Uvs);
                    indices.AddRange(genData.Indices);
                    faces = genData.FaceCount;
                    lodIndices.AddRange(genData.CurrentLodIndices);

                    var physGenData = new MeshPhysicsGenerationData()
                    {
                        BlockInstance = blockInstance,
                        Lod = lodPosKVP.Key,
                        PosDir = pos,
                        PhysMeshVerts = physicsMeshFaces,
                    };

                    blockInfo.GenerateProceduralPhysicsMesh(ref physGenData);
                }

                var blockSize = (sbyte)Mathf.Pow(2, lodPosKVP.Key);
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

    public struct MeshGenerationData
    {
        public BlockInstance BlockInstance;
        public int Lod;
        public Vector4I PosDir;
        public List<Vector3> MeshVerts;
        public List<Vector3> Normals;
        public List<Vector2> Uvs;
        public List<int> Indices;
        public int FaceCount;
        public List<int> CurrentLodIndices;
    }

    public struct MeshPhysicsGenerationData
    {
        public BlockInstance BlockInstance;
        public int Lod;
        public Vector4I PosDir;
        public List<Vector3> PhysMeshVerts;
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
                switch (block.BlockMaterial)
                {
                    case Resource.Block.BlockMaterialEnum.Default:
                        mat = (OrmMaterial3D)BlockMaterial.Duplicate();
                        ((OrmMaterial3D)mat).Transparency = BaseMaterial3D.TransparencyEnum.Disabled;
                        ((OrmMaterial3D)mat).AlbedoColor = block.ColorTint;
                        ((OrmMaterial3D)mat).AlbedoTexture = SetTextureFromBlock(block.AlbedoTexture, MissingTexture);
                        ((OrmMaterial3D)mat).NormalTexture = SetTextureFromBlock(block.NormalTexture, MissingTexture);
                        ((OrmMaterial3D)mat).OrmTexture = SetTextureFromBlock(block.OrmTexture, DefaultOrm);
                        break;
                    case Resource.Block.BlockMaterialEnum.Transparent:
                        mat = (OrmMaterial3D)BlockMaterial.Duplicate();
                        ((OrmMaterial3D)mat).Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
                        ((OrmMaterial3D)mat).AlbedoColor = block.ColorTint;
                        ((OrmMaterial3D)mat).AlbedoTexture = SetTextureFromBlock(block.AlbedoTexture, MissingTexture);
                        ((OrmMaterial3D)mat).NormalTexture = SetTextureFromBlock(block.NormalTexture, MissingTexture);
                        ((OrmMaterial3D)mat).OrmTexture = SetTextureFromBlock(block.OrmTexture, DefaultOrm);
                        break;
                    case Resource.Block.BlockMaterialEnum.Custom:
                        mat = (Material)block.CustomMaterial.Duplicate();
                        break;
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

    private static Texture2D SetTextureFromBlock(Texture2D resourceTexture, Texture2D errTexture)
    {
        if (resourceTexture is not null) return resourceTexture;
        return errTexture;
    }
}