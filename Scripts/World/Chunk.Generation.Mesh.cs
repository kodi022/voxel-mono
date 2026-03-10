using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Voxel.World;

public partial class Chunk
{
    public static readonly OrmMaterial3D BlockMaterial = GD.Load<OrmMaterial3D>("res://Materials/Block.tres");
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

    private static readonly Dictionary<int, OrmMaterial3D> blockMaterials = [];

    private Rid meshInstance;
    private ArrayMesh meshInstanceData;
    private Rid physicsMesh;
    private Rid physicsMeshShape;

    private List<int> surfaceBlockIds;
    private int surfaceCount = 0;
    private List<Vector3> physicsMeshFaces;

    public Task GenerateMeshData()
    {
        // < blockId, < lodId, positions > >
        Dictionary<int, Dictionary<int, List<Vector4I>>> surfaces = [];

        for (int LOD = 0; LOD < 1; LOD++)
        {
            var blockSize = (sbyte)Mathf.Pow(2, LOD);
            for (sbyte x = 0; x < ChunkSize; x += blockSize) for (sbyte z = 0; z < ChunkSize; z += blockSize) for (sbyte y = 0; y < ChunkSize; y += blockSize)
            {
                var block = Blocks[x, y, z];
                if (block.HashId == 0) continue;

                for (sbyte w = 0; w < 6; w++)
                {
                    Vector3B checkPos = new Vector3B(x, y, z) + Directions[w] * blockSize;
                    if (checkPos.IsInside(ChunkSize))
                    {
                        if (Blocks[checkPos.X, checkPos.Y, checkPos.Z].HashId == 0)
                        {
                            surfaces.TryAdd(block.HashId, []);
                            if (!surfaces[block.HashId].TryAdd(LOD, [new(x, y, z, w)]))
                            {
                                surfaces[block.HashId][LOD].Add(new(x, y, z, w));
                            }
                        }
                    }
                    else
                    {
                        // ! this is outer edge faces, fix by checking adjacent chunks
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
        if (surfaceCount == 0) return Task.CompletedTask;

        var fullRenderDistance = Player.RenderDistance * ChunkSize * Player.RenderDistance * ChunkSize;
        meshInstanceData = new();
        surfaceBlockIds = [];
        physicsMeshFaces = [];
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
                        normals.Add((Vector3)Directions[w]);
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

        return Task.CompletedTask;
    }

    public void CreateMesh()
    {
        if (meshInstance.IsValid)
            RenderingServer.FreeRid(meshInstance);

        if (surfaceCount == 0) return;

        var transform = new Transform3D(Basis.Identity, WorldPosition);
        meshInstance = RenderingServer.InstanceCreate();
        RenderingServer.InstanceSetBase(meshInstance, meshInstanceData.GetRid());
        RenderingServer.InstanceSetScenario(meshInstance, world3D.Scenario);
        RenderingServer.InstanceSetTransform(meshInstance, transform);
        RenderingServer.InstanceGeometrySetLodBias(meshInstance, -1f);

        int surfaceIndex = 0;
        foreach (var id in surfaceBlockIds)
        {
            if (!blockMaterials.TryGetValue(id, out OrmMaterial3D mat))
            {
                mat = (OrmMaterial3D)BlockMaterial.Duplicate();
                var block = ResourceManager.BlockRegistry[id];
                mat.AlbedoColor = block.ColorTint;
                mat.AlbedoTexture = LoadTextureFromBlock(block.AlbedoTexture, block.AlbedoTexturePath);
                mat.NormalTexture = LoadTextureFromBlock(block.NormalTexture, block.NormalTexturePath);
                mat.EmissionTexture = LoadTextureFromBlock(block.EmissionTexture, block.EmissionTexturePath);
                blockMaterials.Add(id, mat);
            }

            RenderingServer.InstanceSetSurfaceOverrideMaterial(meshInstance, surfaceIndex, mat.GetRid());
            surfaceIndex++;
        }

        surfaceBlockIds = null;

        Visible = true;
    }

    public void CreatePhysics()
    {
        if (physicsMesh.IsValid)
            PhysicsServer3D.FreeRid(physicsMesh);

        if (surfaceCount == 0) return;
        if (physicsMeshFaces is null || physicsMeshFaces.Count == 0) return;

        var transform = new Transform3D(Basis.Identity, WorldPosition);
        physicsMesh = PhysicsServer3D.BodyCreate();
        physicsMeshShape = PhysicsServer3D.ConcavePolygonShapeCreate();
        var physicsMeshData = new Godot.Collections.Dictionary<string, Variant>() { { "faces", physicsMeshFaces.ToArray() }, { "backface_collision", false } };
        PhysicsServer3D.ShapeSetData(physicsMeshShape, physicsMeshData);
        PhysicsServer3D.BodyAddShape(physicsMesh, physicsMeshShape);
        PhysicsServer3D.BodySetMode(physicsMesh, PhysicsServer3D.BodyMode.Static);
        PhysicsServer3D.BodySetState(physicsMesh, PhysicsServer3D.BodyState.Transform, transform);
        PhysicsServer3D.BodySetSpace(physicsMesh, world3D.Space);
    }
}