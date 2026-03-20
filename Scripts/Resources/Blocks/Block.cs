using Godot;

namespace Voxel.Resource;

// necessary to clone Blocks inside Chunk multithreaded
public class BlockInstance
{
	public int HashId { get; set; }

	public string Name { get; set; }
	public Block.BlockDirectionsEnum BlockDirections { get; set; }
	public bool Unbreakable { get; set; }
	public bool InvulnerableLava { get; set; }
	public float BombResistance { get; set; }

	public Vector2 HpRange { get; set; }

	public float Hp;

	public static bool operator ==(BlockInstance self, int hashId) => self.HashId == hashId;
	public static bool operator !=(BlockInstance self, int hashId) => self.HashId != hashId;
	public static bool operator ==(BlockInstance self, string blockId) => self.HashId == ResourceManager.GetBlock(blockId).HashId;
	public static bool operator !=(BlockInstance self, string blockId) => self.HashId != ResourceManager.GetBlock(blockId).HashId;
	public static bool operator ==(BlockInstance left, BlockInstance right) => left.HashId == right.HashId;
	public static bool operator !=(BlockInstance left, BlockInstance right) => left.HashId != right.HashId;

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
		Custom, // set each face manually
	}

	public enum BlockModelEnum
	{
		Default,
		Custom,
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
	public static bool operator ==(Block left, BlockInstance right) => left.HashId == right.HashId;
	public static bool operator !=(Block left, BlockInstance right) => left.HashId != right.HashId;

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

	// OnBreak (add export with choice on what block to replace, default air 0)
	// OnLavaConsume (lava tries to consume)

	public virtual void OnHit(DamageInfo info)
	{
		if (info.Damage * 100 < info.BlockInstance.HpRange.Y) return;

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

	public BlockInstance MakeInstance()
	{
		return new BlockInstance()
		{
			HashId = HashId,
			Name = Name,
			BlockDirections = BlockDirections,
			Unbreakable = Unbreakable,
			InvulnerableLava = InvulnerableLava,
			BombResistance = BombResistance,
			HpRange = HpRange
		};
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