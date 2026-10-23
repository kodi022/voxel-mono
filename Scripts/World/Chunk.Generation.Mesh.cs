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

    private static Dictionary<int, OrmMaterial3D> blockMaterials = [];

    MeshInstanceData meshInstanceData;
    private List<int> surfaceBlockIds = [];

    public Task GenerateMeshData()
    {
        if (MeshGenerating) return Task.CompletedTask;

        MeshGenerating = true;
        surfaceBlockIds.Clear();

        GenerateMeshDataMesh();

        return Task.CompletedTask;
    }

    private Task GenerateMeshDataMesh()
    {
        meshInstanceData ??= new();
        meshInstanceData.MeshData = new();
        meshInstanceData.PhysicsMeshFaces = [];

        // build surfaces
        // < blockId, list< pos, dir > >
        Dictionary<Block, List<Vector4I>> surfaces = [];

        if (Lod < 1)
        {
            for (sbyte x = 0; x < ChunkSize; x++) for (sbyte z = 0; z < ChunkSize; z++) for (sbyte y = 0; y < ChunkSize; y++)
            {
                var blockInfo = Blocks[x, y, z].BlockInfo;
                if (blockInfo.BlockCull == Block.BlockCullEnum.Translucent) continue;

                for (sbyte w = 0; w < 6; w++)
                {
                    Vector3B checkPos = new Vector3B(x, y, z) + Directions[w];

                    if (checkPos.IsInside(ChunkSize))
                    { // inner edges
                        var adjBlockInfo = Blocks[checkPos.X, checkPos.Y, checkPos.Z].BlockInfo;
                        if (adjBlockInfo.BlockCull == Block.BlockCullEnum.Opaque) continue;
                        if (blockInfo.BlockCull == Block.BlockCullEnum.Transparent && adjBlockInfo.BlockCull == Block.BlockCullEnum.Transparent) continue;

                        var pos = new Vector4I(x, y, z, w);
                        if (!surfaces.TryAdd(blockInfo, [pos]))
                        {
                            surfaces[blockInfo].Add(pos);
                        }
                    }
                    else
                    { // outer edges
                        if (!AdjacentChunks[w]) continue;

                        var chunk2 = ChunkFind(ChunkPosition + Directions[w]);
                        if (chunk2 is null) continue;
                        if (!chunk2.BlocksGenerated) continue;

                        // creates negatives if adding by 16
                        var checkPos2 = (checkPos + ChunkSize * 2) % ChunkSize;
                        var adjBlockInfo = chunk2.Blocks[checkPos2.X, checkPos2.Y, checkPos2.Z].BlockInfo;
                        if (adjBlockInfo is null) continue;
                        if (adjBlockInfo.BlockCull == Block.BlockCullEnum.Opaque) continue;
                        if (blockInfo.BlockCull == Block.BlockCullEnum.Transparent && adjBlockInfo.BlockCull == Block.BlockCullEnum.Transparent) continue;

                        var pos = new Vector4I(x, y, z, w);
                        if (!surfaces.TryAdd(blockInfo, [pos]))
                        {
                            surfaces[blockInfo].Add(pos);
                        }
                    }
                }
            }
        }
        else
        {
            var blockSize = (sbyte)Mathf.Pow(2, Lod);
            var lodChunkSize = (sbyte)(MathF.Max(1, blockSize) * ChunkSize);
            for (sbyte x = 0; x < lodChunkSize; x += blockSize) for (sbyte z = 0; z < lodChunkSize; z += blockSize) for (sbyte y = 0; y < lodChunkSize; y += blockSize)
            {
                var blockInfo = GenerateBlock((BlockVec3)ChunkPosition + new BlockVec3(x, y, z));
                if (blockInfo.BlockInfo.BlockCull == Block.BlockCullEnum.Translucent) continue;

                for (sbyte w = 0; w < 6; w++)
                {
                    BlockVec3 checkPos = new BlockVec3(x, y, z) + Directions[w] * blockSize;

                    var adjBlockInfo = GenerateBlock((BlockVec3)ChunkPosition + new BlockVec3(checkPos.X, checkPos.Y, checkPos.Z));
                    if (adjBlockInfo.BlockInfo.BlockCull == Block.BlockCullEnum.Opaque) continue;
                    if (blockInfo.BlockInfo.BlockCull == Block.BlockCullEnum.Transparent && adjBlockInfo.BlockInfo.BlockCull == Block.BlockCullEnum.Transparent) continue;

                    var pos = new Vector4I(x, y, z, w);
                    if (!surfaces.TryAdd(blockInfo.BlockInfo, [pos]))
                    {
                        surfaces[blockInfo.BlockInfo].Add(pos);
                    }
                }
            }
        }

        if (surfaces.Count == 0)
        {
            MeshGenerating = false;
            return Task.CompletedTask;
        }

        // build geometry
        foreach (var blockPosKVP in surfaces)
        {
            int faces = 0;
            var meshVerts = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var indices = new List<int>();
            foreach (var pos in blockPosKVP.Value)
            {
                BlockInstance blockInstance = null;
                if (Lod < 1) blockInstance = Blocks[pos.X, pos.Y, pos.Z];
                else blockInstance = ResourceManager.GetBlockInstance(blockPosKVP.Key.FullId);
                var blockInfo = blockInstance.BlockInfo;

                var genData = new MeshGenerationData()
                {
                    BlockInstance = blockInstance,
                    PosDir = pos,
                    Lod = Lod,
                    MeshVerts = [],
                    Normals = [],
                    Uvs = [],
                    Indices = [],
                    FaceCount = faces,
                };

                blockInfo.GenerateProceduralMesh(ref genData);

                meshVerts.AddRange(genData.MeshVerts);
                normals.AddRange(genData.Normals);
                uvs.AddRange(genData.Uvs);
                indices.AddRange(genData.Indices);
                faces = genData.FaceCount;

                var physGenData = new MeshPhysicsGenerationData()
                {
                    BlockInstance = blockInstance,
                    PosDir = pos,
                    PhysMeshVerts = meshInstanceData.PhysicsMeshFaces,
                };

                blockInfo.GenerateProceduralPhysicsMesh(ref physGenData);
            }

            if (faces == 0) continue;

            var arrays = new Godot.Collections.Array();
            arrays.Resize((int)Mesh.ArrayType.Max);
            arrays[(int)Mesh.ArrayType.Vertex] = meshVerts.ToArray();
            arrays[(int)Mesh.ArrayType.Normal] = normals.ToArray();
            arrays[(int)Mesh.ArrayType.TexUV] = uvs.ToArray();
            arrays[(int)Mesh.ArrayType.Index] = indices.ToArray();

            meshInstanceData.MeshData.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
            surfaceBlockIds.Add(blockPosKVP.Key.HashId);
        }

        MeshGenerating = false;
        return Task.CompletedTask;
    }

    public class MeshInstanceData
    {
        public Rid MeshRid;
        public ArrayMesh MeshData;
        public Rid PhysicsMeshRid;
        public Rid PhysicsMeshShape;
        public List<Vector3> PhysicsMeshFaces;
        public Godot.Collections.Dictionary<string, Variant> PhysicsMeshData;
    }

    public struct MeshGenerationData
    {
        public BlockInstance BlockInstance;
        public Vector4I PosDir;
        public int Lod;
        public List<Vector3> MeshVerts;
        public List<Vector3> Normals;
        public List<Vector2> Uvs;
        public List<int> Indices;
        public int FaceCount;
    }

    public struct MeshPhysicsGenerationData
    {
        public BlockInstance BlockInstance;
        public Vector4I PosDir;
        public List<Vector3> PhysMeshVerts;
    }

    public void CreateMesh(string meshName = "")
    {
        if (MeshGenerating) return;

        if (meshInstanceData.MeshRid.IsValid)
        {
            RenderingServer.FreeRid(meshInstanceData.MeshRid);
            meshInstanceData.MeshRid = default;
        }

        if (surfaceBlockIds is null || surfaceBlockIds.Count == 0) return;

        var transform = new Transform3D(Basis.Identity, ChunkPosition.ToVector3Scaled());
        meshInstanceData.MeshRid = RenderingServer.InstanceCreate();
        RenderingServer.InstanceSetBase(meshInstanceData.MeshRid, meshInstanceData.MeshData.GetRid());
        RenderingServer.InstanceSetScenario(meshInstanceData.MeshRid, world3D.Scenario);
        RenderingServer.InstanceSetTransform(meshInstanceData.MeshRid, transform);

        int surfaceIndex = 0;
        foreach (var id in surfaceBlockIds)
        {
            if (!blockMaterials.TryGetValue(id, out OrmMaterial3D mat))
            {
                var block = ResourceManager.GetBlock(id);
                GD.Print($"NEW MATERIAL {block.Name} {block.HashId}");

                switch (block.BlockMaterial)
                {
                    case Block.BlockMaterialEnum.Default:
                    case Block.BlockMaterialEnum.Transparent:
                        mat = (OrmMaterial3D)BlockMaterial.Duplicate(true);
                        mat.Transparency = block.BlockMaterial == Block.BlockMaterialEnum.Transparent ?
                            BaseMaterial3D.TransparencyEnum.Alpha :
                            BaseMaterial3D.TransparencyEnum.Disabled;
                        mat.AlbedoColor = block.ColorTint;
                        mat.AlbedoTexture = SetTextureFromBlock(block.AlbedoTexture, MissingTexture);
                        mat.NormalTexture = SetTextureFromBlock(block.NormalTexture, MissingTexture);
                        mat.OrmTexture = SetTextureFromBlock(block.OrmTexture, DefaultOrm);
                        break;
                    case Block.BlockMaterialEnum.Custom:
                        mat = (OrmMaterial3D)block.CustomMaterial.Duplicate(true);
                        break;
                }

                blockMaterials.Add(id, mat);
            }

            RenderingServer.InstanceSetSurfaceOverrideMaterial(meshInstanceData.MeshRid, surfaceIndex, mat.GetRid());
            surfaceIndex++;
        }
    }

    public void CreatePhysics()
    {
        if (MeshGenerating) return;

        if (meshInstanceData.PhysicsMeshRid.IsValid)
        {
            PhysicsServer3D.FreeRid(meshInstanceData.PhysicsMeshRid);
            meshInstanceData.PhysicsMeshRid = default;
        }
        if (meshInstanceData.PhysicsMeshShape.IsValid)
        {
            PhysicsServer3D.FreeRid(meshInstanceData.PhysicsMeshShape);
            meshInstanceData.PhysicsMeshShape = default;
        }

        if (surfaceBlockIds is null || surfaceBlockIds.Count == 0) return;
        if (meshInstanceData.PhysicsMeshFaces.Count % 3 != 0)
        {
            GD.PushError("Physics shape does not have vertex count divisible by 3.");
            return;
        }

        var transform = new Transform3D(Basis.Identity, ChunkPosition.ToVector3Scaled());
        meshInstanceData.PhysicsMeshRid = PhysicsServer3D.BodyCreate();
        meshInstanceData.PhysicsMeshShape = PhysicsServer3D.ConcavePolygonShapeCreate();
        meshInstanceData.PhysicsMeshData = new Godot.Collections.Dictionary<string, Variant>() { { "faces", meshInstanceData.PhysicsMeshFaces.ToArray() }, { "backface_collision", false } };
        PhysicsServer3D.ShapeSetData(meshInstanceData.PhysicsMeshShape, meshInstanceData.PhysicsMeshData);
        PhysicsServer3D.BodyAddShape(meshInstanceData.PhysicsMeshRid, meshInstanceData.PhysicsMeshShape);
        PhysicsServer3D.BodySetMode(meshInstanceData.PhysicsMeshRid, PhysicsServer3D.BodyMode.Static);
        PhysicsServer3D.BodySetState(meshInstanceData.PhysicsMeshRid, PhysicsServer3D.BodyState.Transform, transform);
        PhysicsServer3D.BodySetSpace(meshInstanceData.PhysicsMeshRid, world3D.Space);
    }

    private static Texture2D SetTextureFromBlock(Texture2D resourceTexture, Texture2D errTexture)
    {
        if (GodotObject.IsInstanceValid(resourceTexture)) return resourceTexture;
        return errTexture;
    }
}