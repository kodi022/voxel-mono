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
		PositionLabel.Text = $"{Player.Self.GlobalPosition.ToBlockGlobalPosition()}";
		//HealthLabel.Text = $"{Chunk.ChunkSelectBlock(player.AimBlockPosition)?.Hp ?? 0:0.0}";
	}
}
