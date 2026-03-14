using Godot;

namespace Voxel.Resource;

// only necessary info for chunks
public class BlockInstance
{
	public int HashId { get; set; }

	public string Name { get; set; }
	public Block.BlockDirections DirectionSupport { get; set; }
	public bool IsAir { get; set; }
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
	public enum BlockDirections
	{
		None,
		Horizontal,
		Vertical,
		HorizonalAndVertical,
	}

	public static bool operator ==(Block self, int hashId) => self.HashId == hashId;
	public static bool operator !=(Block self, int hashId) => self.HashId != hashId;
	public static bool operator ==(Block self, string blockId) => self.HashId == ResourceManager.GetBlock(blockId).HashId;
	public static bool operator !=(Block self, string blockId) => self.HashId != ResourceManager.GetBlock(blockId).HashId;
	public static bool operator ==(Block left, Block right) => left.HashId == right.HashId;
	public static bool operator !=(Block left, Block right) => left.HashId != right.HashId;
	public static bool operator ==(Block left, BlockInstance right) => left.HashId == right.HashId;
	public static bool operator !=(Block left, BlockInstance right) => left.HashId != right.HashId;

	[Export]
	public string Name { get; set; } = "";
	[Export]
	public BlockDirections DirectionSupport { get; set; } = BlockDirections.None;
	[Export]
	public bool IsAir { get; set; } = false;
	[Export]
	public bool Unbreakable { get; set; }
	[Export]
	public bool InvulnerableLava { get; set; }
	[Export]
	public float BombResistance { get; set; }

	[Export]
	public Color ColorTint { get; set; } = Color.Color8(255, 255, 255);
	[Export]
	public Texture2D AlbedoTexture { get; set; }
	[Export]
	public Texture2D NormalTexture { get; set; }
	[Export]
	public Texture2D EmissionTexture { get; set; }

	// try if above is null (possible useful for modding)
	[Export]
	public string AlbedoTexturePath { get; set; }
	[Export]
	public string NormalTexturePath { get; set; }
	[Export]
	public string EmissionTexturePath { get; set; }

	[Export]
	public Vector2 HpRange { get; set; } = new(2, 4);

	// OnBreak (add export with choice on what block to replace, default air 0)
	// OnLavaConsume (lava tries to consume)

	public static void OnHit(DamageInfo info)
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

	public static void OnBreak(DamageInfo info)
	{
		// send different block back to chunk? (allow other than air)

		// particle
	}

	public BlockInstance MakeInstance()
	{
		return new BlockInstance()
		{
			HashId = HashId,
			Name = Name,
			DirectionSupport = DirectionSupport,
			IsAir = IsAir,
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
	public Vector3 HitPosition;
	public Vector3 FaceNormal;
	public BlockInstance BlockInstance;
}