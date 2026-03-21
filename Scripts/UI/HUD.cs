using Godot;
using System;
using Voxel.World;

namespace Voxel;

public partial class HUD : Panel
{
	[Export]
	public Label PositionLabel;
	[Export]
	public Label HealthLabel;

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
			HealthLabel.Text = $"{block.Hp:0.0} {block.BlockInfo.Name}";
		}
		else
		{
			HealthLabel.Text = "";
		}
	}
}
