using Godot;
using System.Collections.Generic;
using Voxel;

public partial class DebugUI : Node
{
    public static int DebugUIMode { get; set; } = 0;

    private readonly List<Label> labels = [];

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
        labels[0].Text = $"FPS:{Engine.GetFramesPerSecond(),6:0.0}";

        if (DebugUIMode > 0)
        {
            Engine.MaxFps = 0;
            ProjectSettings.SetSetting("display/window/vsync/vsync_mode", false);

            var blockPos = Player.Self.GlobalPosition.ToBlockGlobalPosition();
            labels[1].Text = $"POS: X:{blockPos.X,5} Y:{blockPos.Y,5} Z:{blockPos.Z,5}";

            int nonAirBlock = 0, nullBlock = 0;
            int chunks = 0;
            if (Player.Self.WithinChunk is not null && !Player.Self.WithinChunk.Generating)
            {
                foreach (var block in Player.Self.WithinChunk.Blocks)
                {
                    if (block is null) { nullBlock++; continue; }
                    if (block != 0) nonAirBlock++;
                }
            }

            foreach (var region in ChunkManager.Regions)
            {
                chunks += region.Value.Chunks.Count;
            }

            labels[2].Text = $"CHUNK: !a{nonAirBlock} n{nullBlock}";
            labels[3].Text = $"CHUNKS: {chunks} g{ChunkManager.GeneratingChunks.Count}";
        }
    }
}
