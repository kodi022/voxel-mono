using Godot;

namespace Voxel.Resource;

public partial class VoxelResource : Godot.Resource
{
    public int HashId { get; private set; } = 0;
    public string FullId { get; private set; } = "";

    [Export]
    public string PackageId { get; set; } = "base";
    [Export]
    public string ResourceId { get; set; } = "";

    public void BuildIds()
    {
        FullId = $"{PackageId}:{ResourceId}";
        HashId = Global.StableHash(FullId);
    }

    public void BuildIds(int hashId)
    {
        FullId = $"{PackageId}:{ResourceId}";
        HashId = hashId;
    }
}