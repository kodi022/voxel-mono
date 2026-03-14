using System;
using Godot;
using Voxel.World;

namespace Voxel;

// support is added as needed

public struct BlockVec3
{
    public static readonly BlockVec3 Zero = new();

    public int X;
    public int Y;
    public int Z;

    public readonly LocalityEnum Locality = LocalityEnum.Global;

    public enum LocalityEnum
    {
        Global,
        Local
    }

    public BlockVec3() { }

    public BlockVec3(BlockVec3 v, LocalityEnum locality = LocalityEnum.Global)
    {
        X = v.X;
        Y = v.Y;
        Z = v.Z;
        Locality = locality;
    }

    public BlockVec3(int x, int y, int z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public static BlockVec3 operator %(BlockVec3 v, BlockVec3 o) => new(v.X % o.X, v.Y % o.Y, v.Z % o.Z);
    public static BlockVec3 operator *(BlockVec3 v, BlockVec3 o) => new(v.X * o.X, v.Y * o.Y, v.Z * o.Z);
    public static BlockVec3 operator /(BlockVec3 v, BlockVec3 o) => new(v.X / o.X, v.Y / o.Y, v.Z / o.Z);
    public static BlockVec3 operator +(BlockVec3 v, BlockVec3 o) => new(v.X + o.X, v.Y + o.Y, v.Z + o.Z);
    public static BlockVec3 operator -(BlockVec3 v, BlockVec3 o) => new(v.X - o.X, v.Y - o.Y, v.Z - o.Z);

    public static BlockVec3 operator %(BlockVec3 v, int o) => new(v.X % o, v.Y % o, v.Z % o);
    public static BlockVec3 operator *(BlockVec3 v, int o) => new(v.X * o, v.Y * o, v.Z * o);
    public static BlockVec3 operator /(BlockVec3 v, int o) => new(v.X / o, v.Y / o, v.Z / o);
    public static BlockVec3 operator +(BlockVec3 v, int o) => new(v.X + o, v.Y + o, v.Z + o);
    public static BlockVec3 operator -(BlockVec3 v, int o) => new(v.X - o, v.Y - o, v.Z - o);

    public static BlockVec3 operator %(BlockVec3 v, Vector3 o) => new(v.X % o.X.FToI(), v.Y % o.Y.FToI(), v.Z % o.Z.FToI());
    public static BlockVec3 operator *(BlockVec3 v, Vector3 o) => new(v.X * o.X.FToI(), v.Y * o.Y.FToI(), v.Z * o.Z.FToI());
    public static BlockVec3 operator /(BlockVec3 v, Vector3 o) => new(v.X / o.X.FToI(), v.Y / o.Y.FToI(), v.Z / o.Z.FToI());
    public static BlockVec3 operator +(BlockVec3 v, Vector3 o) => new(v.X + o.X.FToI(), v.Y + o.Y.FToI(), v.Z + o.Z.FToI());
    public static BlockVec3 operator -(BlockVec3 v, Vector3 o) => new(v.X - o.X.FToI(), v.Y - o.Y.FToI(), v.Z - o.Z.FToI());
    public static BlockVec3 operator %(Vector3 v, BlockVec3 o) => new(v.X.FToI() % o.X, v.Y.FToI() % o.Y, v.Z.FToI() % o.Z);
    public static BlockVec3 operator *(Vector3 v, BlockVec3 o) => new(v.X.FToI() * o.X, v.Y.FToI() * o.Y, v.Z.FToI() * o.Z);
    public static BlockVec3 operator /(Vector3 v, BlockVec3 o) => new(v.X.FToI() / o.X, v.Y.FToI() / o.Y, v.Z.FToI() / o.Z);
    public static BlockVec3 operator +(Vector3 v, BlockVec3 o) => new(v.X.FToI() + o.X, v.Y.FToI() + o.Y, v.Z.FToI() + o.Z);
    public static BlockVec3 operator -(Vector3 v, BlockVec3 o) => new(v.X.FToI() - o.X, v.Y.FToI() - o.Y, v.Z.FToI() - o.Z);

    public static explicit operator BlockVec3(ChunkVec3 v) => new(v.X * Chunk.ChunkSize, v.Y * Chunk.ChunkSize, v.Z * Chunk.ChunkSize);
    public static explicit operator BlockVec3(RegionVec3 v) => new(v.X * Chunk.RegionSize, v.Y * Chunk.RegionSize, v.Z * Chunk.RegionSize);

    public static BlockVec3 FromVector3(Vector3 v) => new(v.X.FToI(), v.Y.FToI(), v.Z.FToI());
    public readonly Vector3 ToVector3() => new(X, Y, Z);

    public readonly int GetVecHash() => Global.StableHash(X, Y, Z);

    /// <summary>
    /// Convert global block position to local to chunk.
    /// </summary>
    /// <returns>New BlockVec3 with localized values</returns>
    public readonly BlockVec3 ToLocal()
    {
        var newVec = new BlockVec3(this, LocalityEnum.Local);
        if (Locality == LocalityEnum.Local) return newVec;

        return newVec % Chunk.ChunkSize;
    }

    /// <summary>
    /// Convert global block position to local to chunk. More efficient overload, but result may be outside of Chunk indexes.
    /// </summary>
    /// <returns>New BlockVec3 with localized values</returns>
    public readonly BlockVec3 ToLocal(in ChunkVec3 chunkPos)
    {
        var newVec = new BlockVec3(this, LocalityEnum.Local);
        if (Locality == LocalityEnum.Local) return newVec;

        return newVec - (BlockVec3)chunkPos;
    }

    /// <summary>
    /// Returns if values are within 0 and topLimit (exclusive).
    /// </summary>
    public readonly bool IsInside(in float topLimit)
    {
        return topLimit > X && X > -1 && topLimit > Y && Y > -1 && topLimit > Z && Z > -1;
    }

    /// <summary>
    /// Returns if values are within bottomLimit and topLimit (exclusive).
    /// </summary>
    public readonly bool IsInside(in float bottomLimit, in float topLimit)
    {
        return topLimit > X && X > bottomLimit && topLimit > Y && Y > bottomLimit && topLimit > Z && Z > bottomLimit;
    }

    public override readonly string ToString() => $"{X}, {Y}, {Z}";
}

/// <summary>
/// Position for Chunks. (1, 0, 0) == (16, 0, 0), or Chunk.ChunkSize
/// </summary>
public struct ChunkVec3
{
    public int X;
    public int Y;
    public int Z;

    public ChunkVec3(int all)
    {
        X = all;
        Y = all;
        Z = all;
    }

    public ChunkVec3(int x, int y, int z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public static ChunkVec3 operator %(ChunkVec3 v, int o) => new(v.X % o, v.Y % o, v.Z % o);
    public static ChunkVec3 operator *(ChunkVec3 v, int o) => new(v.X * o, v.Y * o, v.Z * o);
    public static ChunkVec3 operator /(ChunkVec3 v, int o) => new(v.X / o, v.Y / o, v.Z / o);
    public static ChunkVec3 operator +(ChunkVec3 v, int o) => new(v.X + o, v.Y + o, v.Z + o);
    public static ChunkVec3 operator -(ChunkVec3 v, int o) => new(v.X - o, v.Y - o, v.Z - o);

    public static explicit operator ChunkVec3(BlockVec3 v) => new(v.X.FIntDiv(Chunk.ChunkSize), v.Y.FIntDiv(Chunk.ChunkSize), v.Z.FIntDiv(Chunk.ChunkSize));
    public static explicit operator ChunkVec3(RegionVec3 v) => new(v.X * Chunk.ChunkSize, v.Y * Chunk.ChunkSize, v.Z * Chunk.ChunkSize);

    public static ChunkVec3 FromVector3(Vector3 v) => new((v.X / Chunk.ChunkSize).FToI(), (v.Y / Chunk.ChunkSize).FToI(), (v.Z / Chunk.ChunkSize).FToI());
    public readonly Vector3 ToVector3() => new(X, Y, Z);
    public readonly Vector3 ToVector3Scaled() => new(X * Chunk.ChunkSize, Y * Chunk.ChunkSize, Z * Chunk.ChunkSize);

    public readonly int GetVecHash() => Global.StableHash(X, Y, Z);

    public override readonly string ToString() => $"{X}, {Y}, {Z}";
}

/// <summary>
/// Position for Regions. (1, 0, 0) == (256, 0, 0), or Chunk.RegionSize
/// </summary>
public struct RegionVec3
{
    public int X;
    public int Y;
    public int Z;

    public RegionVec3(int all)
    {
        X = all;
        Y = all;
        Z = all;
    }

    public RegionVec3(int x, int y, int z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public static explicit operator RegionVec3(BlockVec3 v) => new(v.X.FIntDiv(Chunk.RegionSize), v.Y.FIntDiv(Chunk.RegionSize), v.Z.FIntDiv(Chunk.RegionSize));
    public static explicit operator RegionVec3(ChunkVec3 v) => new(v.X.FIntDiv(Chunk.ChunkSize), v.Y.FIntDiv(Chunk.ChunkSize), v.Z.FIntDiv(Chunk.ChunkSize));

    public static RegionVec3 FromVector3(Vector3 v) => new((v.X / Chunk.RegionSize).FToI(), (v.Y / Chunk.RegionSize).FToI(), (v.Z / Chunk.RegionSize).FToI());
    public readonly Vector3 ToVector3() => new(X, Y, Z);
    public readonly Vector3 ToVector3Scaled() => new(X * Chunk.RegionSize, Y * Chunk.RegionSize, Z * Chunk.RegionSize);

    public readonly int GetVecHash() => Global.StableHash(X, Y, Z);

    public override readonly string ToString() => $"{X}, {Y}, {Z}";
}

public struct Vector3B
{
    public sbyte X;
    public sbyte Y;
    public sbyte Z;

    public Vector3B(sbyte all)
    {
        X = all;
        Y = all;
        Z = all;
    }

    public Vector3B(int x, int y, int z)
    {
        X = (sbyte)x;
        Y = (sbyte)y;
        Z = (sbyte)z;
    }

    public static Vector3B operator %(Vector3B v, int o) => new(v.X % o, v.Y % o, v.Z % o);
    public static Vector3B operator *(Vector3B v, int o) => new(v.X * o, v.Y * o, v.Z * o);
    public static Vector3B operator /(Vector3B v, int o) => new(v.X / o, v.Y / o, v.Z / o);
    public static Vector3B operator +(Vector3B v, int o) => new(v.X + o, v.Y + o, v.Z + o);
    public static Vector3B operator -(Vector3B v, int o) => new(v.X - o, v.Y - o, v.Z - o);

    public static Vector3B operator %(Vector3B v, Vector3B o) => new(v.X % o.X, v.Y % o.Y, v.Z % o.Z);
    public static Vector3B operator *(Vector3B v, Vector3B o) => new(v.X * o.X, v.Y * o.Y, v.Z * o.Z);
    public static Vector3B operator /(Vector3B v, Vector3B o) => new(v.X / o.X, v.Y / o.Y, v.Z / o.Z);
    public static Vector3B operator +(Vector3B v, Vector3B o) => new(v.X + o.X, v.Y + o.Y, v.Z + o.Z);
    public static Vector3B operator -(Vector3B v, Vector3B o) => new(v.X - o.X, v.Y - o.Y, v.Z - o.Z);

    public static BlockVec3 operator +(Vector3B v, BlockVec3 o) => new(v.X + o.X, v.Y + o.Y, v.Z + o.Z);

    public static implicit operator Vector3(Vector3B v) => new(v.X, v.Y, v.Z);


    public readonly bool IsInside(sbyte topLimit)
    {
        return topLimit > X && X > -1 && topLimit > Y && Y > -1 && topLimit > Z && Z > -1;
    }
}

public struct Vector2B
{
    public sbyte X;
    public sbyte Y;

    public Vector2B(sbyte all)
    {
        X = all;
        Y = all;
    }

    public Vector2B(sbyte x, sbyte y)
    {
        X = x;
        Y = y;
    }

    public static Vector2B operator +(Vector2B v, Vector2B o)
    {
        return new((sbyte)(v.X + o.X), (sbyte)(v.Y + o.Y));
    }

    public static explicit operator Vector2(Vector2B v)
    {
        return new(v.X, v.Y);
    }
}