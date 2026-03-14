using Godot;

namespace Voxel;

public static class Extensions
{
    // theres an easier way if using raycasts
    public static Vector3 GetForwardPosition(this Node3D node3D, float distance)
    {
        return node3D.GlobalPosition - node3D.GlobalTransform.Basis.Z * distance;
    }

    /// <summary>
    /// Convenience function for Godot.Mathf.FloorToInt.
    /// </summary>
    public static int FToI(this in float f) => Mathf.FloorToInt(f);

    /// <summary>
    /// Floored Integer Division. src: https://stackoverflow.com/questions/28059655/floored-integer-division
    /// </summary>
    public static int FIntDiv(this in int i, int div) => i / div - System.Convert.ToInt32(((i < 0) ^ (div < 0)) && (i % div != 0));
}