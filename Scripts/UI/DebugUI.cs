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
        labels[0].Text = $"FPS: {Engine.GetFramesPerSecond(),6:0.0}";

        if (DebugUIMode > 0)
        {
            Engine.MaxFps = 0;
            ProjectSettings.SetSetting("display/window/vsync/vsync_mode", false);

            var blockPos = BlockVec3.FromVector3(Player.Self.GlobalPosition);
            var chunkPos = ChunkVec3.FromVector3(Player.Self.GlobalPosition);
            var regionPos = RegionVec3.FromVector3(Player.Self.GlobalPosition);
            labels[1].Text = $"bPOS: {blockPos.X,4} {blockPos.Y,4} {blockPos.Z,4}";
            labels[2].Text = $"cPOS: {chunkPos.X,4} {chunkPos.Y,4} {chunkPos.Z,4}";
            labels[3].Text = $"rPOS: {regionPos.X,4} {regionPos.Y,4} {regionPos.Z,4}";

            int nonAirBlock = 0, nullBlock = 0;
            int chunks = 0;
            if (Player.Self.WithinChunk is not null && !Player.Self.WithinChunk.Generating)
            {
                foreach (var block in Player.Self.WithinChunk.Blocks)
                {
                    if (block is null) { nullBlock++; continue; }
                    if (block == 0) nonAirBlock++;
                }

                foreach (var region in ChunkManager.Regions)
                {
                    chunks += region.Value.Chunks.Count;
                }

                labels[4].Text = $"CHUNK: a{nonAirBlock} n{nullBlock}";
                labels[5].Text = $"CHUNK: mesh:{Player.Self.WithinChunk.GeneratedMesh} pMesh:{Player.Self.WithinChunk.GeneratedPhysicsMesh}";
                labels[6].Text = $"CHUNKS: {chunks} g{ChunkManager.GeneratingChunks.Count}";
            }
        }
    }
}
