using Godot;

namespace Voxel.Resource;

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

	public static bool operator ==(Block self, int hashId)
	{
		return self.HashId == hashId;
	}

	public static bool operator !=(Block self, int hashId)
	{
		return self.HashId != hashId;
	}

	public static bool operator ==(Block self, string blockId)
	{
		return self.HashId == ResourceManager.GetBlock(blockId).HashId;
	}

	public static bool operator !=(Block self, string blockId)
	{
		return self.HashId != ResourceManager.GetBlock(blockId).HashId;
	}

	public static bool operator ==(Block left, Block right)
	{
		return left.HashId == right.HashId;
	}

	public static bool operator !=(Block left, Block right)
	{
		return left.HashId != right.HashId;
	}

	public static explicit operator Block(string blockId)
	{
		return ResourceManager.GetBlock(blockId);
	}

	[Export]
	public string Name { get; set; } = "";
	[Export]
	public bool IsAir { get; set; } = false;
	[Export]
	public bool IsOre { get; set; } = false;
	[Export]
	public BlockDirections DirectionSupport { get; set; } = BlockDirections.None;
	[Export]
	public bool Unbreakable { get; set; }

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
	public Vector2 HpRange { get; set; } = new(10, 100);

	public float Hp = 0;

	public void OnHit(DamageInfo info)
	{
		if (Hp < info.Damage)
		{
			Hp = 0;
			OnBreak();
			return;
		}
		// particle
		// block damage overlay?
	}

	public void OnBreak()
	{
		// send different block back to chunk? (allow other than air)

		// particle
	}

	// OnBreak (add export with choice on what block to replace, default air 0)
	// OnLavaConsume (lava tries to consume)

	// required for operators
	public override bool Equals(object obj)
	{
		if (ReferenceEquals(this, obj))
		{
			return true;
		}

		return false;
	}

	// required for operators
	public override int GetHashCode()
	{
		return HashId;
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
}