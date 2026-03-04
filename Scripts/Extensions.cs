using Godot;
using Voxel.World;

namespace Voxel;

public static class Extensions
{
    // theres an easier way if using raycasts
    public static Vector3 GetForwardPosition(this Node3D node3D, float distance)
    {
        return node3D.GlobalPosition - node3D.GlobalTransform.Basis.Z * distance;
    }

    public static Vector3 ToRegionPosition(this Vector3 v)
    {
        var size = Chunk.ChunkSize * Chunk.ChunkSize;
        v /= size;
        return v.Floor() * size;
    }

    public static Vector3 ToChunkPosition(this Vector3 v)
    {
        v /= Chunk.ChunkSize;
        return v.Floor() * Chunk.ChunkSize;
    }

    public static Vector3 ToBlockLocalPosition(this Vector3 v)
    {
        v %= Chunk.ChunkSize;
        return v.Floor();
    }

    // possibly slightly more efficient option
    public static Vector3 ToBlockLocalPosition(this Vector3 v, in Vector3 chunkPos)
    {
        v -= chunkPos;
        return v.Floor();
    }

    public static Vector3 ToBlockGlobalPosition(this Vector3 v)
    {
        return v.Floor();
    }

    public static bool IsInside(this Vector3 v, float bottomLimit, float topLimit)
    {
        return topLimit > v.X && v.X > bottomLimit && topLimit > v.Y && v.Y > bottomLimit && topLimit > v.Z && v.Z > bottomLimit;
    }

    public static bool IsInside(this Vector3 v, float topLimit)
    {
        return topLimit > v.X && v.X > -1 && topLimit > v.Y && v.Y > -1 && topLimit > v.Z && v.Z > -1;
    }

    public static bool IsInside(this Vector3I v, int topLimit)
    {
        return topLimit > v.X && v.X > -1 && topLimit > v.Y && v.Y > -1 && topLimit > v.Z && v.Z > -1;
    }
}