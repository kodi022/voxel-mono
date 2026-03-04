using Godot;
using System;
using Voxel.World;

namespace Voxel;

public partial class DebugPanel : Panel
{
	[Export]
	public Label PositionLabel;
	[Export]
	public Label HealthLabel;

	private Player player;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		player = (Player)GetParent();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		PositionLabel.Text = $"{player.GlobalPosition.ToBlockGlobalPosition()}";
		HealthLabel.Text = $"{Chunk.ChunkSelectBlock(player.AimBlockPosition)?.Hp ?? 0:0.0}";
	}
}
