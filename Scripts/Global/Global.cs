using System.Collections.Generic;
using System.Diagnostics;

namespace Voxel;

public static class Global
{
    public static int StableHash(string s)
    {
        if (string.IsNullOrEmpty(s))
            return 0;

        unchecked
        {
            int hash = 2116037303;
            foreach (var ch in s)
            {
                hash = (hash ^ ch) * 971296439;
            }
            return hash;
        }
    }

    public static int StableHash(int x, int y, int z)
    {
        unchecked
        {
            int hash = x;
            hash = hash * 2119412839 + y;
            hash = hash * 135040691 + z;
            return hash;
        }
    }

    /// <summary>
    /// Gives float between 0 and 1
    /// </summary>
    public static float GetSeededRandom(BlockVec3 position, int seedOffset)
    {
        unchecked
        {
            int hash = position.GetVecHash() * ChunkManager.Seed * seedOffset;
            return System.Math.Abs(hash % 10000000 / 10000000f);
        }
    }

    static readonly Dictionary<int, Stopwatch> watches = [];
    public static void StartWatch(int watchId)
    {
        var watch = new Stopwatch();
        watches.TryAdd(watchId, watch);
        watch.Start();
    }

    public static long StopWatch(int watchId)
    {
        long time = 0;
        if (watches.TryGetValue(watchId, out Stopwatch watch))
        {
            watch.Stop();
            time = watch.ElapsedMilliseconds;
            watches.Remove(watchId);
        }
        return time;
    }
}