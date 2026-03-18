using System.Threading.Tasks;
using Godot;

namespace Voxel.Resource;

[GlobalClass]
public partial class StructureSphere : Structure
{
	public override async void GenerateBlocks(ChunkVec3 position, StructureInstance instance)
	{
		await Task.Run(async () =>
		{
			void SetBlock(Vector3I pos, string blockId)
			{
				instance.StructureBlocks[pos.X, pos.Y, pos.Z] = ResourceManager.GetBlock(blockId);
			}

			float GetRand(int offset)
			{
				return Global.GetSeededRandom((BlockVec3)position, offset);
			}

			instance.Position = position;
			var sizeScales = new Vector3(GetRand(512528), GetRand(824725), GetRand(107284));
			var sizeRand = (SizeRangeMax - SizeRangeMin) * sizeScales;
			Vector3I size = (Vector3I)(SizeRangeMin + sizeRand);
			var halfSize = size / 2;
			instance.CenterPosition = ((BlockVec3)position) + halfSize;

			instance.StructureBlocks = new Block[size.X, size.Y, size.Z];
			instance.StructureBlocksLengths = new Vector3I(size.X, size.Y, size.Z);

			var farthestSqr = SizeRangeMax[(int)SizeRangeMax.MaxAxisIndex()] * sizeScales[(int)sizeScales.MaxAxisIndex()];
			farthestSqr *= farthestSqr;
			for (int x = 0; x < size.X; x++) for (int y = 0; y < size.Y; y++) for (int z = 0; z < size.Z; z++)
			{
				var dist = (instance.Position.ToVector3Scaled() + new Vector3(x, y, z) / sizeScales).DistanceSquaredTo(instance.CenterPosition.ToVector3());

				if (dist < farthestSqr * 0.01f)
				{
					SetBlock(new Vector3I(x, y, z), "base:gemmite");
					continue;
				}

				if (dist < farthestSqr * 0.2f)
				{
					SetBlock(new Vector3I(x, y, z), "base:air");
					continue;
				}

				if (dist < farthestSqr * 0.3f)
				{
					SetBlock(new Vector3I(x, y, z), "base:glass");
					continue;
				}

				if (dist < farthestSqr * 0.4f)
				{
					SetBlock(new Vector3I(x, y, z), "base:brick");
					continue;
				}
			}

			instance.GeneratedBlocks = true;
		});
	}
}