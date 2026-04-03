using Godot;
using System;
using Voxel.World;

namespace Voxel;

public partial class HUD : Panel
{
	[Export]
	public Label PositionLabel;
	[Export]
	public Label BlockNameLabel;
	[Export]
	public Label BlockHealthLabel;
	[Export]
	public Panel BlockHealthBar;

	private double hpLerp;
	private Vector3 lastBlock;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		PositionLabel.Text = $"{BlockVec3.FromVector3(Player.Self.GlobalPosition)}";
		var block = Chunk.ChunkSelectBlock(Player.Self.AimBlockPosition);
		if (block is not null)
		{
			if (lastBlock != Player.Self.AimBlockPosition.ToVector3()) hpLerp = block.Hp;

			lastBlock = Player.Self.AimBlockPosition.ToVector3();

			hpLerp = Mathf.Lerp(hpLerp, block.Hp, delta * 10f);
			BlockNameLabel.Text = block.BlockInfo.Name;
			BlockHealthLabel.Text = ParseHealth(hpLerp);
			BlockHealthBar.Position = new Vector2(((float)(hpLerp / block.GeneratedMaxHp) - 1f) * BlockHealthBar.Size.X, 0);
			if (!this.Visible) this.Show();
		}
		else
		{
			if (this.Visible) this.Hide();
			BlockNameLabel.Text = "";
			BlockHealthLabel.Text = "";
			BlockHealthBar.Position = new Vector2(0, 0);
		}
	}

	private string ParseHealth(double hp)
	{
		if (hp < 100) return hp.ToString("0.00");
		if (hp < 10000) return hp.ToString("0.0");

		return hp.ToString("0");
	}
}
