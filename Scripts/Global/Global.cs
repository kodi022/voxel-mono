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
            uint hash = 2166136261u;
            foreach (var ch in s)
            {
                hash = (hash ^ ch) * 16777619u;
            }
            return (int)hash;
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