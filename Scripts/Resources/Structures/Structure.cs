using Godot;

namespace Voxel.Resource;

// necessary to clone Structure inside Region multithreaded
public class StructureInstance
{
	// set at creation
	public int HashId { get; private set; }
	public string FullId { get; private set; }
	public int Priority { get; private set; }
	public ChunkVec3 Position { get; private set; }

	// set by generation
	public BlockVec3 CenterPosition { get; set; }
	public Block[,,] StructureBlocks { get; set; }
	public Vector3I StructureBlocksLengths { get; set; }
	public bool GeneratedBlocks { get; set; }

	public StructureInstance(int hashId, string fullId, int priority, ChunkVec3 position)
	{
		HashId = hashId;
		FullId = fullId;
		Priority = priority;
		Position = position;
	}
}

[GlobalClass]
public partial class Structure : VoxelResource
{
	[Export]
	public int Priority { get; set; } = 0;
	[Export]
	public float ChancePerChunk { get; set; } = 0.01f;
	[Export]
	public Vector3 SizeRangeMin { get; set; } = Vector3.One * 10;
	[Export]
	public Vector3 SizeRangeMax { get; set; } = Vector3.One * 12;

	protected void SetValuesManual(StructureInstance instance, ChunkVec3 position, Vector3I size)
	{
		instance.StructureBlocks = new Block[size.X, size.Y, size.Z];
		instance.StructureBlocksLengths = new Vector3I(size.X, size.Y, size.Z);
		instance.CenterPosition = (BlockVec3)position + new BlockVec3(size.X / 2, size.Y / 2, size.Z / 2);
	}

	protected void SetValuesSingleScale(StructureInstance instance, ChunkVec3 position, int randOffset)
	{
		var scale = GetRand((BlockVec3)position, randOffset);
		int sizeRangeRand = (int)((SizeRangeMax[0] - SizeRangeMin[0]) * scale);
		int size = (int)SizeRangeMin[0] + sizeRangeRand;
		var halfSize = size / 2;

		instance.CenterPosition = ((BlockVec3)position) + halfSize;
		instance.StructureBlocks = new Block[size, size, size];
		instance.StructureBlocksLengths = new Vector3I(size, size, size);
	}

	protected void SetValuesMultiScale(StructureInstance instance, ChunkVec3 position, Vector3I randOffsets)
	{
		var bPos = (BlockVec3)position;
		var scales = new Vector3(GetRand(bPos, randOffsets.X), GetRand(bPos, randOffsets.Y), GetRand(bPos, randOffsets.Z));
		var sizeRangesRand = (SizeRangeMax - SizeRangeMin) * scales;
		var sizes = SizeRangeMin + sizeRangesRand;
		var halfSizes = sizes / 2;

		instance.CenterPosition = bPos + halfSizes;
		instance.StructureBlocks = new Block[(int)sizes.X, (int)sizes.Y, (int)sizes.Z];
		instance.StructureBlocksLengths = new Vector3I((int)sizes.X, (int)sizes.Y, (int)sizes.Z);
	}

	protected static float GetRand(BlockVec3 block, int offset)
	{
		return Global.GetSeededRandom(block, offset);
	}

	public virtual async void GenerateBlocks(StructureInstance instance, ChunkVec3 position) { }
}