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

    public static Vector3 ToRegionPosition(this in Vector3 v)
    {
        var size = Chunk.ChunkSize * Chunk.ChunkSize;
        return (v / size).Floor() * size;
    }

    public static Vector3 ToChunkPosition(this in Vector3 v)
    {
        return (v / Chunk.ChunkSize).Floor() * Chunk.ChunkSize;
    }

    public static Vector3 ToBlockLocalPosition(this in Vector3 v)
    {
        return (v % Chunk.ChunkSize).Floor();
    }

    // possibly slightly more efficient option
    public static Vector3 ToBlockLocalPosition(this in Vector3 v, in Vector3 chunkPos)
    {
        return (v - chunkPos).Floor();
    }

    public static Vector3 ToBlockGlobalPosition(this in Vector3 v)
    {
        return v.Floor();
    }

    // vector3I

    public static Vector3I ToBlockLocalPosition(this in Vector3I v)
    {
        return v % Chunk.ChunkSize;
    }

    // possibly slightly more efficient option
    public static Vector3I ToBlockLocalPosition(this in Vector3I v, in Vector3I chunkPos)
    {
        return v - chunkPos;
    }

    public static bool IsInside(this in Vector3 v, in float topLimit)
    {
        return topLimit > v.X && v.X > -1 && topLimit > v.Y && v.Y > -1 && topLimit > v.Z && v.Z > -1;
    }

    public static bool IsInside(this in Vector3 v, in float bottomLimit, in float topLimit)
    {
        return topLimit > v.X && v.X > bottomLimit && topLimit > v.Y && v.Y > bottomLimit && topLimit > v.Z && v.Z > bottomLimit;
    }

    public static bool IsInside(this in Vector3I v, in int topLimit)
    {
        return topLimit > v.X && v.X > -1 && topLimit > v.Y && v.Y > -1 && topLimit > v.Z && v.Z > -1;
    }

    public static bool IsInside(this in Vector3I v, in float bottomLimit, in float topLimit)
    {
        return topLimit > v.X && v.X > bottomLimit && topLimit > v.Y && v.Y > bottomLimit && topLimit > v.Z && v.Z > bottomLimit;
    }
}