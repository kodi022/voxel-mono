namespace Voxel.Ui;

public partial class DebugUI : Node
{
    public static int DebugUIMode { get; set; } = 0;

    private readonly List<Label> labels = [];

    private int maxFps = 60;
    private bool vsync = true;

    public override void _Ready()
    {
        maxFps = Engine.MaxFps;
        vsync = (bool)ProjectSettings.GetSetting("display/window/vsync/vsync_mode");

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
            var blockPos = BlockVec3.FromVector3(Player.Self.GlobalPosition);
            var chunkPos = ChunkVec3.FromVector3(Player.Self.GlobalPosition);
            var regionPos = RegionVec3.FromVector3(Player.Self.GlobalPosition);
            labels[1].Text = $"bPOS: {blockPos.X,4} {blockPos.Y,4} {blockPos.Z,4}";
            labels[2].Text = $"cPOS: {chunkPos.X,4} {chunkPos.Y,4} {chunkPos.Z,4}";
            labels[3].Text = $"rPOS: {regionPos.X,4} {regionPos.Y,4} {regionPos.Z,4}";

            if (Player.Self.WithinChunk is not null)
            {
                int nonAirBlock = 0, nullBlock = 0;
                foreach (var block in Player.Self.WithinChunk.Blocks)
                {
                    if (block is null) { nullBlock++; continue; }
                    if (block == 0) nonAirBlock++;
                }
                labels[4].Text = $"CHUNK: a{nonAirBlock} n{nullBlock}";
            }
            else
            {
                labels[4].Text = $"CHUNK: null";
            }

            //labels[5].Text = Player.Self.WithinChunk is null ? $"CHUNK: null" : $"CHUNK: mesh:{Player.Self.WithinChunk.GeneratedMesh} pMesh:{Player.Self.WithinChunk.GeneratedPhysicsMesh}";

            Dictionary<int, int> chunks = [];
            foreach (var region in ChunkManager.Regions)
            {
                foreach (var chunk in region.Value.Chunks)
                {
                    if (!chunks.TryAdd(chunk.Value.Lod, 1))
                    {
                        chunks[chunk.Value.Lod] += 1;
                    }
                }
            }

            string lodCount = "";
            foreach (var kvp in chunks)
            {
                lodCount += kvp.Value + ",";
            }
            lodCount = lodCount == "" ? "" : lodCount[..^1];

            labels[5].Text = $"CHUNKS: ({lodCount}) g{ChunkManager.GeneratingChunks.Count}";
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed)
        {
            switch (keyEvent.Keycode)
            {
                case Key.F7:
                    if (Engine.MaxFps == 0)
                    {
                        Engine.MaxFps = maxFps;
                        ProjectSettings.SetSetting("display/window/vsync/vsync_mode", true);
                    }
                    else
                    {
                        Engine.MaxFps = 0;
                        ProjectSettings.SetSetting("display/window/vsync/vsync_mode", false);
                    }
                    break;
                case Key.F8:
                    var viewport = GetViewport();
                    if (viewport.DebugDraw == Viewport.DebugDrawEnum.Disabled) viewport.DebugDraw = Viewport.DebugDrawEnum.Wireframe;
                    else if (viewport.DebugDraw == Viewport.DebugDrawEnum.Wireframe) viewport.DebugDraw = Viewport.DebugDrawEnum.Overdraw;
                    else viewport.DebugDraw = Viewport.DebugDrawEnum.Disabled;
                    GD.Print(viewport.DebugDraw.ToString());
                    break;
            }
        }
    }
}
