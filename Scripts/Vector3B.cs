using Godot;

public class Vector3B
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

    public Vector3B(sbyte x, sbyte y, sbyte z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public Vector3B(int x, int y, int z)
    {
        X = (sbyte)x;
        Y = (sbyte)y;
        Z = (sbyte)z;
    }

    public static Vector3B operator +(Vector3B v, Vector3B other)
    {
        return new((sbyte)(v.X + other.X), (sbyte)(v.Y + other.Y), (sbyte)(v.Z + other.Z));
    }

    public static Vector3B operator *(Vector3B v, int other)
    {
        return new((sbyte)(v.X * other), (sbyte)(v.Y * other), (sbyte)(v.Z * other));
    }

    public static Vector3B operator *(Vector3B v, sbyte other)
    {
        return new((sbyte)(v.X * other), (sbyte)(v.Y * other), (sbyte)(v.Z * other));
    }

    public static explicit operator Vector3(Vector3B v)
    {
        return new(v.X, v.Y, v.Z);
    }

    public bool IsInside(sbyte topLimit)
    {
        return topLimit > X && X > -1 && topLimit > Y && Y > -1 && topLimit > Z && Z > -1;
    }
}

public class Vector2B
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

    public static Vector2B operator +(Vector2B v, Vector2B other)
    {
        return new((sbyte)(v.X + other.X), (sbyte)(v.Y + other.Y));
    }

    public static explicit operator Vector2(Vector2B v)
    {
        return new(v.X, v.Y);
    }
}