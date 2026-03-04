using System.Collections.Generic;
using Godot;
using Voxel;

public class BlockAction
{
    public Vector3 Position { get; set; } = Vector3.Zero;
    public ActionType Action { get; set; } = ActionType.Mine;
    public Vector3 FaceNormal { get; set; } = Vector3.Zero;

    public enum ActionType
    {
        Mine,
        Place
    }

    public virtual void Apply()
    {
        var chunk = ChunkManager.FindChunk(Position);

        if (Action == ActionType.Mine)
        {
            chunk?.SetBlock(Position, "base:air");
        }
        else
        {
            chunk?.SetBlock(Position, "base:brick");
        }

    }
}

public class BlockActionArea : BlockAction
{
    public enum ActionShape
    {
        Square,
        Circle,
        Cube,
        Sphere,
        Explosive
    }

    public float Radius { get; set; } = 5f;
    public ActionShape Shape { get; set; } = ActionShape.Sphere;

    // used with square/circle
    public float Depth { get; set; }

    public override void Apply()
    {
        Dictionary<Vector3, List<Vector3>> changed = [];

        bool CountBlock(Vector3 pos, FastNoiseLite explosiveNoise = null) => Shape switch
        {
            ActionShape.Square => true,
            ActionShape.Circle => true,
            ActionShape.Cube => true,
            ActionShape.Sphere => Position.DistanceSquaredTo(pos) < Radius * Radius,
            ActionShape.Explosive => Position.DistanceSquaredTo(pos) < Radius * Radius - (explosiveNoise.GetNoise3D(pos.X, pos.Y, pos.Z) * Radius * 5),
            _ => true,
        };

        FastNoiseLite noise = Shape == ActionShape.Explosive ? new FastNoiseLite()
        {
            Seed = 987654321,
            NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex,
            Frequency = 3.31526f
        } : null;

        for (int x = (int)-Radius; x < Radius; x++)
        {
            for (int y = (int)-Radius; y < Radius; y++)
            {
                for (int z = (int)-Radius; z < Radius; z++)
                {
                    var currentPos = Position + new Vector3(x, y, z);
                    var chunkPos = currentPos.ToChunkPosition();

                    if (CountBlock(currentPos, noise))
                    {
                        if (!changed.TryAdd(chunkPos, [currentPos]))
                        {
                            changed[chunkPos].Add(currentPos);
                        }
                    }
                }
            }
        }

        foreach (var value in changed)
        {
            var chunk = ChunkManager.FindChunk(value.Key);

            if (Action == ActionType.Mine)
            {
                chunk?.SetBlocks([.. value.Value], "base:air");
            }
            else
            {
                chunk?.SetBlocks([.. value.Value], "base:brick");
            }
        }
    }
}