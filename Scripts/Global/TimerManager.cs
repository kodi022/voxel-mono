using Godot;
using System;
using System.Collections.Generic;

namespace Voxel;

public partial class TimerManager : Node
{
    private static readonly List<Timer> currentTimers = [];

    public override void _Process(double delta)
    {
        List<Timer> timersToRemove = null;
        foreach (var timer in currentTimers)
        {
            if (timer.IncrementTime(delta))
            {
                timer.TargetAction.Invoke();
                timersToRemove ??= [];
                timersToRemove.Add(timer);
            }
        }

        if (timersToRemove is not null)
        {
            foreach (var timer in timersToRemove)
            {
                currentTimers.Remove(timer);
            }
        }
    }

    public static void AddTimer(double waitTime, Action action) => currentTimers.Add(new Timer(waitTime, action));

    public class Timer(double waitTime, Action action)
    {
        public double CurrentTime { get; private set; } = 0;
        public double TargetTime { get; private set; } = waitTime;
        public Action TargetAction { get; private set; } = action;

        public bool IncrementTime(double amount) => (CurrentTime += amount) > TargetTime;
    }
}
