using System.Linq;
using System.Threading.Tasks;
using Godot;

namespace Voxel.Resource;

[GlobalClass]
public partial class StructureSphere : Structure
{
	[Export]
	public Godot.Collections.Dictionary<float, Block> Blocks;

	public override async void GenerateBlocks(ChunkVec3 position, StructureInstance instance)
	{
		await Task.Run(async () =>
		{
			void SetBlock(Vector3I pos, string blockId)
			{
				instance.StructureBlocks[pos.X, pos.Y, pos.Z] = ResourceManager.GetBlock(blockId);
			}

			instance.Position = position;
			var scale = GetRand((BlockVec3)position, 512528);
			int sizeRangeRand = (int)((SizeRangeMax[0] - SizeRangeMin[0]) * scale);
			int size = (int)SizeRangeMin[0] + sizeRangeRand;
			var halfSize = size / 2;

			instance.CenterPosition = ((BlockVec3)position) + halfSize;
			instance.StructureBlocks = new Block[size, size, size];
			instance.StructureBlocksLengths = new Vector3I(size, size, size);

			var sortBlocks = Blocks.OrderBy(b => b.Key);

			var farthestSqr = size * scale;
			farthestSqr *= farthestSqr;
			for (int x = 0; x < size; x++) for (int y = 0; y < size; y++) for (int z = 0; z < size; z++)
			{
				var dist = (instance.Position.ToVector3Scaled() + new Vector3(x, y, z) / scale).DistanceSquaredTo(instance.CenterPosition.ToVector3());

				foreach (var kvp in sortBlocks)
				{
					if (dist < farthestSqr * kvp.Key)
					{
						SetBlock(new Vector3I(x, y, z), kvp.Value.FullId);
						break;
					}
				}
			}

			instance.GeneratedBlocks = true;
		});
	}
}