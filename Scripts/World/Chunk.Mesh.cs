using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Voxel.World;

public partial class Chunk : MeshInstance3D
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

    public static ArrayMesh EmptyMesh { get; private set; }

    private ArrayMesh mesh;
    private ConcavePolygonShape3D meshShape;

    private Dictionary<int, List<Vector4I>> surfacesTemp;

    public void UpdateMesh(bool async = true)
    {
        if (async)
        {
            Task.Run(async () =>
            {
                meshGeneration = GenerateMesh();
                await meshGeneration;
                meshGeneration = null;
                CallDeferred(nameof(ApplyMesh));
            });
        }
        else
        {
            meshGeneration = GenerateMesh();
            meshGeneration = null;
            CallDeferred(nameof(ApplyMesh));
        }
    }

    private void ApplyMesh()
    {
        if (surfacesTemp is null || surfacesTemp.Count == 0 || mesh == EmptyMesh)
        {
            Mesh = EmptyMesh;
            CollisionShape3D.Shape = null;
            return;
        }

        int surfaceIndex = 0;
        foreach (var surface in surfacesTemp)
        {
            var mat = (OrmMaterial3D)BlockMaterial.Duplicate();
            var block = ResourceManager.BlockRegistry[surface.Key];
            mat.AlbedoTexture = LoadTextureFromBlock(block.AlbedoTexture, block.AlbedoTexturePath);
            mat.NormalTexture = LoadTextureFromBlock(block.NormalTexture, block.NormalTexturePath);
            mat.EmissionTexture = LoadTextureFromBlock(block.EmissionTexture, block.EmissionTexturePath);
            mesh.SurfaceSetMaterial(surfaceIndex, mat);
            surfaceIndex++;
        }

        Mesh = mesh;
        if (LOD < 1) CollisionShape3D.Shape = meshShape;

        mesh = null;
        meshShape = null;
        surfacesTemp = null;
    }

    public Task GenerateMesh()
    {
        surfacesTemp = [];
        mesh = new();

        // if (Blocks.Length < ChunkSize * ChunkSize)
        // {
        //     mesh = EmptyMesh;
        //     return Task.CompletedTask;
        // }

        var blockSize = (sbyte)Mathf.Pow(2, LOD);
        for (sbyte x = 0; x < ChunkSize; x += blockSize)
        {
            for (sbyte z = 0; z < ChunkSize; z += blockSize)
            {
                for (sbyte y = 0; y < ChunkSize; y += blockSize)
                {
                    var block = Blocks[x, y, z];
                    if (block.HashId == 0) continue;

                    for (sbyte w = 0; w < 6; w++)
                    {
                        Vector3B checkPos = new Vector3B(x, y, z) + Directions[w] * blockSize;
                        if (checkPos.IsInside((sbyte)ChunkSize))
                        {
                            if (Blocks[checkPos.X, checkPos.Y, checkPos.Z].HashId == 0)
                            {
                                if (!surfacesTemp.TryAdd(block.HashId, [new(x, y, z, w)]))
                                {
                                    surfacesTemp[block.HashId].Add(new(x, y, z, w));
                                }
                            }
                        }
                        else
                        {
                            // ! this is outer edge faces, fix by checking adjacent chunks
                            if (!surfacesTemp.TryAdd(block.HashId, [new(x, y, z, w)]))
                            {
                                surfacesTemp[block.HashId].Add(new(x, y, z, w));
                            }
                        }
                    }
                }
            }
        }

        Dictionary<int, List<Vector4I>> usedSurfaces = [];
        foreach (var surface in surfacesTemp)
        {
            int faces = 0;
            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var indices = new List<int>();
            foreach (var pos in surface.Value)
            {
                int x = pos.X, y = pos.Y, z = pos.Z, w = pos.W;

                // each vert
                for (int v = 0; v < 4; v++)
                {
                    var off = FaceVertexOffsets[w][v] * blockSize;
                    verts.Add(new Vector3(x + off.X, y + off.Y, z + off.Z));
                    normals.Add((Vector3)Directions[w]);
                    uvs.Add((Vector2)FaceUVs[v] * blockSize);
                }

                var o = faces * 4;
                indices.AddRange([
                    o, o + 1, o + 2,
                    o + 2, o + 3, o
                ]);
                faces++;
            }

            if (faces > 0)
            {
                usedSurfaces.Add(surface.Key, surface.Value);
                var arrays = new Godot.Collections.Array();
                arrays.Resize((int)Mesh.ArrayType.Max);
                arrays[(int)Mesh.ArrayType.Vertex] = verts.ToArray();
                arrays[(int)Mesh.ArrayType.Normal] = normals.ToArray();
                arrays[(int)Mesh.ArrayType.TexUV] = uvs.ToArray();
                arrays[(int)Mesh.ArrayType.Index] = indices.ToArray();
                mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
            }
        }

        surfacesTemp = usedSurfaces;

        if (surfacesTemp.Count == 0)
        {
            mesh = EmptyMesh;
            return Task.CompletedTask;
        }

        if (LOD < 1) meshShape = mesh.CreateTrimeshShape();
        return Task.CompletedTask;
    }
}