using System.Linq;
using System.Threading.Tasks;
using Godot;

namespace Voxel.Resource;

[GlobalClass]
public partial class StructureSphere : Structure
{
	[Export]
	public Block CenterBlock;
	[Export]
	public Godot.Collections.Dictionary<float, Block> BlockLayers;

	public override async void GenerateBlocks(StructureInstance instance, ChunkVec3 position)
	{
		await Task.Run(async () =>
		{
			SetValuesSingleScale(instance, position, 512528);

			var sortBlocks = BlockLayers.OrderBy(b => b.Key);
			var size = instance.StructureBlocksLengths.X;

			var farthestSqr = size;
			for (int x = 0; x < size; x++) for (int y = 0; y < size; y++) for (int z = 0; z < size; z++)
			{
				var currentPos = instance.Position.ToVector3Scaled() + new Vector3(x, y, z);
				var dist = currentPos.DistanceSquaredTo(instance.CenterPosition.ToVector3());

				foreach (var kvp in sortBlocks)
				{
					if (dist < farthestSqr * kvp.Key)
					{
						instance.StructureBlocks[x, y, z] = kvp.Value;
						break;
					}
				}
			}

			if (CenterBlock is not null)
			{
				var localCenter = instance.CenterPosition - instance.Position.ToVector3Scaled();
				instance.StructureBlocks[localCenter.X, localCenter.Y, localCenter.Z] = CenterBlock;
			}

			instance.GeneratedBlocks = true;
		});
	}
}