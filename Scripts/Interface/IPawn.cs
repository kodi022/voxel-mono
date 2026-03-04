namespace Voxel;

public interface IPawn
{
	public float Health { get; set; }

	public virtual void OnDamage() { }
	public virtual void Spawn() { }
	public virtual void OnDie() { }
}
