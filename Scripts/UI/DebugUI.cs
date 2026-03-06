using Godot;
using System;
using System.Collections.Generic;
using Voxel;

public partial class DebugUI : Node
{
    public static int DebugUIMode { get; set; } = 0;

    private List<Label> labels = [];

    private double avgFps;
    private double[] avgFpsValues = new double[100];
    private int avgFpsValuesIndex;

    public override void _Ready()
    {
        var panel = GetChild(0);
        foreach (var label in panel.GetChildren())
        {
            labels.Add((Label)label);
        }
    }

    public override void _Process(double delta)
    {
        var fps = 1d / delta;
        avgFpsValues[avgFpsValuesIndex] = fps;
        avgFpsValuesIndex = (avgFpsValuesIndex + 1) % 100;

        var avg = 0d;
        var count = 0;
        foreach (var val in avgFpsValues)
        {
            avg += val;
            count++;
        }
        avg /= count;

        labels[0].Text = $"FPS:{avg,6:0.0}";

        if (DebugUIMode > 0)
        {
            var blockPos = Player.Self.GlobalPosition.ToBlockGlobalPosition();
            labels[1].Text = $"POS: X:{blockPos.X,5} Y:{blockPos.Y,5} Z:{blockPos.Z,5}";

            int nonAirBlock = 0, nullBlock = 0;
            int chunks = 0;
            if (Player.Self.WithinChunk is not null)
            {
                foreach (var block in Player.Self.WithinChunk.Blocks)
                {
                    if (block is null) { nullBlock++; continue; }
                    if (block != 0) nonAirBlock++;
                }
                foreach (var region in ChunkManager.AllRegions)
                {
                    chunks += region.Value.Chunks.Count;
                }
            }

            labels[2].Text = $"CHUNK: !a{nonAirBlock} n{nullBlock}";
            labels[3].Text = $"CHUNKS: {chunks}";
        }
    }
}
