using Godot;

namespace Voxel.Resource;

public partial class VoxelResource : Godot.Resource
{
    public int HashId { get; set; } = 0;
    public string FullId { get; set; } = "";

    [Export]
    public string PackageId { get; set; } = "base";
    [Export]
    public string ResourceId { get; set; } = "";

    public void BuildIds()
    {
        FullId = $"{PackageId}:{ResourceId}";
        HashId = Global.StableHash(FullId);
    }
}