using Godot;
using Voxel.World;

namespace Voxel.Resource;

// necessary to clone Blocks inside Chunk multithreaded
public class BlockInstance(in Block self)
{
	public readonly Block BlockInfo = self;

	// network these
	public float Hp;
	public int DirYaw;
	public int DirPitch;
	public Block.BlockShapeEnum Shape;

	public static bool operator ==(BlockInstance self, int hashId) => self.BlockInfo.HashId == hashId;
	public static bool operator !=(BlockInstance self, int hashId) => self.BlockInfo.HashId != hashId;
	public static bool operator ==(BlockInstance self, string blockId) => self.BlockInfo.HashId == ResourceManager.GetBlock(blockId).HashId;
	public static bool operator !=(BlockInstance self, string blockId) => self.BlockInfo.HashId != ResourceManager.GetBlock(blockId).HashId;
	public static bool operator ==(BlockInstance left, BlockInstance right) => left.BlockInfo.HashId == right.BlockInfo.HashId;
	public static bool operator !=(BlockInstance left, BlockInstance right) => left.BlockInfo.HashId != right.BlockInfo.HashId;

	public static explicit operator BlockInstance(string blockId)
	{
		return ResourceManager.GetBlockInstance(blockId);
	}

	// required for == and =! operators
	public override bool Equals(object obj)
	{
		return false;
	}

	// required for == and =! operators
	public override int GetHashCode()
	{
		return base.GetHashCode();
	}
}

[GlobalClass]
public partial class Block : VoxelResource
{
	public enum BlockDirectionsEnum
	{
		None,
		Yaw, // block has yaw directions
		Pitch, // block has pitch directions
		YawAndPitch, // block has pitch and yaw directions
	}

	public enum BlockTextureUVEnum
	{
		OneFaceUV, // 32x32 texture
		TwoFacesUV, // 64x32 texture top/side
		SixFacesUV, // 128x128 texture block uv
		Custom, // set each face manually (tbd)
	}

	public enum BlockModelEnum
	{
		Default,
		Custom,
	}

	public enum BlockShapeEnum
	{
		Block,
		Stair,
		Slab,
	}

	public enum BlockMaterialEnum
	{
		Default,
		Transparent,
		Custom
	}

	public enum BlockCullEnum
	{
		Opaque,
		Transparent,
		Translucent,
	}

	public static bool operator ==(Block self, int hashId) => self.HashId == hashId;
	public static bool operator !=(Block self, int hashId) => self.HashId != hashId;
	public static bool operator ==(Block self, string blockId) => self.HashId == ResourceManager.GetBlock(blockId).HashId;
	public static bool operator !=(Block self, string blockId) => self.HashId != ResourceManager.GetBlock(blockId).HashId;
	public static bool operator ==(Block left, Block right) => left.HashId == right.HashId;
	public static bool operator !=(Block left, Block right) => left.HashId != right.HashId;
	public static bool operator ==(Block left, BlockInstance right) => left.HashId == right.BlockInfo.HashId;
	public static bool operator !=(Block left, BlockInstance right) => left.HashId != right.BlockInfo.HashId;

	[Export, ExportGroup("Identity")]
	public string Name { get; set; } = "";


	[Export, ExportGroup("Function")]
	public bool Unbreakable { get; set; }
	[Export]
	public bool InvulnerableLava { get; set; }
	[Export]
	public float BombResistance { get; set; }
	[Export]
	public string EntityScenePath { get; set; }
	[Export]
	public Vector2 HpRange { get; set; } = new(2, 4);


	[Export, ExportGroup("Visual")]
	public BlockDirectionsEnum BlockDirections { get; set; } = BlockDirectionsEnum.None;

	[Export, ExportGroup("Visual")]
	public BlockCullEnum BlockCull { get; set; } = BlockCullEnum.Opaque;

	[Export, ExportGroup("Visual")]
	public BlockTextureUVEnum BlockTextureUV { get; set; } = BlockTextureUVEnum.OneFaceUV;

	[Export, ExportGroup("Visual")]
	public BlockModelEnum BlockModel { get; set; } = BlockModelEnum.Default;

	[Export, ExportSubgroup("Model.Custom")]
	public Mesh Model { get; set; }

	[Export, ExportGroup("Visual")]
	public BlockMaterialEnum BlockMaterial { get; set; } = BlockMaterialEnum.Default;

	// default material
	[Export, ExportSubgroup("Material.Default Material.Transparent")]
	public Color ColorTint { get; set; } = Color.Color8(255, 255, 255);
	[Export(hintString: "Uses alpha channel if Material.Transparent")]
	public Texture2D AlbedoTexture { get; set; } // uses alpha channel if Material.Transparent
	[Export]
	public Texture2D NormalTexture { get; set; }
	[Export]
	public Texture2D OrmTexture { get; set; }

	// custom material
	[Export, ExportSubgroup("Material.Custom")]
	public Material CustomMaterial { get; set; }

	public BlockInstance MakeInstance()
	{
		return new BlockInstance(this);
	}

	public virtual void OnHit(DamageInfo info)
	{
		var block = ResourceManager.GetBlock(info.BlockInstance.BlockInfo.HashId);
		if (info.Damage * 100 < block.HpRange.Y) return;

		if (info.BlockInstance.Hp < info.Damage)
		{
			info.BlockInstance.Hp = 0;
			OnBreak(info);
			return;
		}
		// particle
		// block damage overlay?
	}

	public virtual void OnBreak(DamageInfo info)
	{
		// send different block back to chunk? (allow other than air)

		// particle
	}

	public virtual void OnUpdate(DamageInfo info)
	{
	}

	public virtual void OnTouch(DamageInfo info)
	{
	}

	public virtual void OnLavaTouch(DamageInfo info)
	{
		// should consume by lava or not?
	}


	public virtual void GenerateProceduralMesh(ref Chunk.MeshGenerationData data)
	{
		if (data.Lod == 0)
		{
			switch (data.BlockInstance.Shape)
			{
				case BlockShapeEnum.Block:
					ProceduralBlockMesh(ref data);
					break;
				case BlockShapeEnum.Stair:
					ProceduralStairMesh(ref data);
					break;
				case BlockShapeEnum.Slab:
					ProceduralSlabMesh(ref data);
					break;
			}
		}
		else
		{
			ProceduralBlockMesh(ref data);
		}
	}

	private void ProceduralBlockMesh(ref Chunk.MeshGenerationData data)
	{
		var blockSize = (sbyte)Mathf.Pow(2, data.Lod);
		int x = data.PosDir.X, y = data.PosDir.Y, z = data.PosDir.Z, w = data.PosDir.W;

		// mesh verts
		switch (BlockTextureUV)
		{
			case BlockTextureUVEnum.OneFaceUV:
				for (int v = 0; v < 4; v++)
				{
					var off = Chunk.FaceVertexOffsets[w][v] * blockSize;
					data.MeshVerts.Add(new Vector3(x + off.X, y + off.Y, z + off.Z));
					data.Normals.Add(Chunk.Directions[w]);
					data.Uvs.Add((Vector2)Chunk.OneFaceUVs[v] * blockSize);
				}
				break;
			case BlockTextureUVEnum.TwoFacesUV:
				var indexOffset = w < 2 ? 0 : 4;
				//if (block.BlockDirections != )
				for (int v = 0; v < 4; v++)
				{
					var off = Chunk.FaceVertexOffsets[w][v] * blockSize;
					data.MeshVerts.Add(new Vector3(x + off.X, y + off.Y, z + off.Z));
					data.Normals.Add(Chunk.Directions[w]);
					data.Uvs.Add(Chunk.TwoFaceUVs[indexOffset + v] * blockSize);
				}
				break;
			case BlockTextureUVEnum.SixFacesUV:
				var indexOffset2 = w * 4;
				for (int v = 0; v < 4; v++)
				{
					var off = Chunk.FaceVertexOffsets[w][v] * blockSize;
					data.MeshVerts.Add(new Vector3(x + off.X, y + off.Y, z + off.Z));
					data.Normals.Add(Chunk.Directions[w]);
					data.Uvs.Add(Chunk.SixFaceUVs[indexOffset2 + v] * blockSize);
				}
				break;
		}

		var o = data.FaceCount * 4;
		data.CurrentLodIndices.AddRange([
			o, o + 1, o + 2,
			o + 2, o + 3, o
		]);
		data.FaceCount++;
	}

	private void ProceduralStairMesh(ref Chunk.MeshGenerationData data)
	{

	}

	private void ProceduralSlabMesh(ref Chunk.MeshGenerationData data)
	{

	}

	public virtual void GenerateProceduralPhysicsMesh(ref Chunk.MeshPhysicsGenerationData data)
	{
		if (data.Lod > 0) return;

		int x = data.PosDir.X, y = data.PosDir.Y, z = data.PosDir.Z, w = data.PosDir.W;

		var off = Chunk.FaceVertexOffsets[w][0];
		data.PhysMeshVerts.Add(new Vector3(x + off.X, y + off.Y, z + off.Z));
		off = Chunk.FaceVertexOffsets[w][1];
		data.PhysMeshVerts.Add(new Vector3(x + off.X, y + off.Y, z + off.Z));
		off = Chunk.FaceVertexOffsets[w][2];
		data.PhysMeshVerts.Add(new Vector3(x + off.X, y + off.Y, z + off.Z));
		off = Chunk.FaceVertexOffsets[w][2];
		data.PhysMeshVerts.Add(new Vector3(x + off.X, y + off.Y, z + off.Z));
		off = Chunk.FaceVertexOffsets[w][3];
		data.PhysMeshVerts.Add(new Vector3(x + off.X, y + off.Y, z + off.Z));
		off = Chunk.FaceVertexOffsets[w][0];
		data.PhysMeshVerts.Add(new Vector3(x + off.X, y + off.Y, z + off.Z));
	}


	// required for == and =! operators
	public override bool Equals(object obj)
	{
		if (ReferenceEquals(this, obj))
		{
			return true;
		}

		return false;
	}

	// required for == and =! operators
	public override int GetHashCode()
	{
		return base.GetHashCode();
	}
}

public struct DamageInfo
{
	// using temporary types until proper types are implemented
	public string Player;
	public float Damage;
	public string Tool;
	public BlockVec3 BlockPosition;
	public Vector3 HitPosition;
	public Vector3 FaceNormal;
	public BlockInstance BlockInstance;
}

public struct TouchInfo
{
	// using temporary player until proper types are implemented
	public string Player;
	public BlockVec3 BlockPosition;
	public Vector3 Velocity;
	public Vector3 HitPosition;
	public Vector3 FaceNormal;
	public BlockInstance BlockInstance;
}