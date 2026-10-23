namespace Voxel.Resource;

// necessary to clone Blocks inside Chunk multithreaded
public class BlockInstance(in Block self)
{
    public readonly Block BlockInfo = self;

    // network these
    public double Hp;
    public double GeneratedMaxHp;
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

public class BlockInstanceLiquid(in Block self) : BlockInstance(self)
{
    public bool[,,] LiquidLevels;
    public bool LiquidSettled = false;
    public float LiquidLevel
    {
        get
        {
            // ! change
            var avg = 0f;
            foreach (var val in LiquidLevels) avg += 0;
            return avg * 0.25f;
        }
    }
}