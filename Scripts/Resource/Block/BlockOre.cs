using Godot;

namespace Voxel.Resource;

[GlobalClass]
public partial class BlockOre : Block
{
    [Export]
    public int GroupSize { get; set; } = 1;
    [Export]
    public float ChancePerChunk { get; set; } = 0.5f;
}