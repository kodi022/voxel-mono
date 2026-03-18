using Godot;

namespace Voxel.Resource;

// necessary to clone Structure inside Region multithreaded
public class StructureInstance
{
	public int HashId { get; set; } = 0;

	public int Priority { get; set; } = 1;
	public ChunkVec3 Position { get; set; }
	public BlockVec3 CenterPosition { get; set; }

	public Block[,,] StructureBlocks { get; set; }
	public Vector3I StructureBlocksLengths { get; set; }
	public bool GeneratedBlocks { get; set; } = false;
}

[GlobalClass]
public partial class Structure : VoxelResource
{
	[Export]
	public float ChancePerChunk { get; set; } = 0.01f;
	[Export]
	public Vector3 SizeRangeMin { get; set; } = Vector3.One * 3;
	[Export]
	public Vector3 SizeRangeMax { get; set; } = Vector3.One * 10;
	[Export]
	public int Priority { get; set; } = 1;

	public virtual async void GenerateBlocks(ChunkVec3 center, StructureInstance instance) { }
}

[GlobalClass]
public partial class StructureMaze : Structure
{
	public override async void GenerateBlocks(ChunkVec3 center, StructureInstance instance) { }
}