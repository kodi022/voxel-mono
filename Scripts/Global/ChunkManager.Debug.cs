namespace Voxel;

public partial class ChunkManager : Node3D
{
    private static readonly Dictionary<int, DebugRender> debugChunkRenders = [];
    private static ArrayMesh debugChunkMesh;
    private World3D world3D;

    public class DebugRender(Rid mesh)
    {
        public float time = 0;
        public Rid mesh = mesh;
    }

    private static void ProcessDebug(double delta)
    {
        if (debugChunkRenders.Count > 0)
        {
            var delt = (float)delta;
            for (int i = 0; i < 512; i++)
            {
                if (debugChunkRenders.Count == 0) break;
                var debug = debugChunkRenders.First();

                if (debug.Value is null)
                {
                    debugChunkRenders.Remove(debug.Key);
                    continue;
                }

                debug.Value.time += delt;
                if (debug.Value.time > 3f)
                {
                    RenderingServer.FreeRid(debug.Value.mesh);
                    debugChunkRenders.Remove(debug.Key);
                    continue;
                }
            }
        }
    }


    public static void DebugRenderChunk(ChunkVec3 pos, Color tint, int scale = 1)
    {
        DebugRenderChunk(pos.ToVector3Scaled(), tint);
    }
    public static void DebugRenderChunk(Vector3 pos, Color tint, int scale = 1)
    {
        if (debugChunkRenders.ContainsKey(pos.GetHashCode())) return;

        if (debugChunkMesh is null)
        {
            var scene = GD.Load<PackedScene>("res://Models/debugchunk.blend");
            var instance = scene.Instantiate();
            var mesh = instance.GetNode<MeshInstance3D>("Cube");
            debugChunkMesh = mesh.Mesh as ArrayMesh;
            scene.Dispose();
        }

        var transform = new Transform3D(Basis.Identity, pos + Vector3.One * (World.Chunk.ChunkSize / 2));
        var meshInstance = RenderingServer.InstanceCreate();
        RenderingServer.InstanceSetBase(meshInstance, debugChunkMesh.GetRid());
        RenderingServer.InstanceSetScenario(meshInstance, Current.world3D.Scenario);
        RenderingServer.InstanceSetTransform(meshInstance, transform.Scaled(Vector3.One * scale));
        debugChunkRenders.Add(pos.GetHashCode(), new DebugRender(meshInstance));
    }
}