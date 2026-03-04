namespace Voxel;

public static class Global
{
    public static int StableHash(string s)
    {
        if (string.IsNullOrEmpty(s))
            return 0;

        unchecked
        {
            uint hash = 2166136261u;
            foreach (var ch in s)
            {
                hash = (hash ^ ch) * 16777619u;
            }
            return (int)hash;
        }
    }
}