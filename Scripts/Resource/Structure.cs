using Godot;

namespace Voxel.Resource;

[GlobalClass]
public partial class Structure : VoxelResource
{
	[Export]
	public float DensityPerChunk { get; set; } = 0.2f;
	[Export]
	public Vector3 SizeRangeMin { get; set; } = Vector3.One * 3;
	[Export]
	public Vector3 SizeRangeMax { get; set; } = Vector3.One * 10;
}
