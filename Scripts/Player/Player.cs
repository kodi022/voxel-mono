using System.Collections.Generic;
using Godot;
using Voxel.World;

namespace Voxel;

public partial class Player : CharacterBody3D, IPawn
{
	public static Player Self { get; private set; }

	[Export]
	public CollisionShape3D CollisionShape3D { get; set; }
	[Export]
	public CollisionShape3D GroundedShape3D { get; set; }
	[Export]
	public Camera3D Camera3D { get; set; }

	// may be null
	public Chunk WithinChunk { get; private set; }
	public Vector3 AimHitPosition { get; private set; }
	public Vector3 AimBlockPosition { get; private set; }
	public Vector3 AimBlockFrontPosition { get; private set; }

	public Controller CurrentController { get; private set; } = new ControllerWalk();
	public Input.MouseModeEnum MouseState { get; private set; } = Input.MouseModeEnum.Captured;

	public Node3D Selector { get; private set; }
	public Godot.Collections.Dictionary FrameTraceResult { get; private set; }

	public float Health { get; set; }

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Self ??= this;

		Selector = (Node3D)GD.Load<PackedScene>("res://Scenes/block_select.tscn").Instantiate();
		GetTree().Root.AddChild(Selector);
	}

	public override void _Process(double delta)
	{
		Input.MouseMode = MouseState;

		var endPos = Camera3D.GetForwardPosition(20f);
		var query = PhysicsRayQueryParameters3D.Create(Camera3D.GlobalPosition, endPos);
		FrameTraceResult = GetWorld3D().DirectSpaceState.IntersectRay(query);
		if (FrameTraceResult.TryGetValue("position", out Variant position))
		{
			var pos = (Vector3)position;
			AimHitPosition = pos;
			AimBlockPosition = (pos - (Vector3)FrameTraceResult["normal"] * 0.5f).ToBlockGlobalPosition();
			AimBlockFrontPosition = (pos + (Vector3)FrameTraceResult["normal"] * 0.5f).ToBlockGlobalPosition();
			Selector.GlobalPosition = AimBlockPosition;
		}
		else
		{
			AimHitPosition = Vector3.Zero;
			AimBlockPosition = Vector3.Zero;
			Selector.GlobalPosition = Camera3D.GetForwardPosition(-10f);
		}

		CurrentController.ControllerProcess(delta, this);

		WithinChunk = ChunkManager.FindChunk(GlobalPosition);

		// chunk render distance
		List<(Vector3, int)> chunkToSpawn = [];
		List<Vector3> chunkToDestroy = [];

		var rDist = 12;
		var cull = rDist * Chunk.ChunkSize * rDist * Chunk.ChunkSize;
		var lod0Dist = cull * 0.25f;
		var lod1Dist = cull * 0.5;
		//var lod2Dist = cull * 0.666f;
		for (int x = -rDist - 4; x < rDist + 4; x++)
		{
			for (int y = -rDist - 4; y < rDist + 4; y++)
			{
				for (int z = -rDist - 4; z < rDist + 4; z++)
				{
					var offsetGlobalPosition = GlobalPosition - Vector3.One * (Chunk.ChunkSize * 0.5f);
					var pos = new Vector3(x * Chunk.ChunkSize, y * Chunk.ChunkSize, z * Chunk.ChunkSize);
					var chunkPos = (offsetGlobalPosition + pos).ToChunkPosition();
					var distSqr = (chunkPos - offsetGlobalPosition).LengthSquared();

					if (distSqr < cull)
					{
						var lod = 0;
						if (distSqr > lod0Dist) lod = 1;
						if (distSqr > lod1Dist) lod = 2;
						//if (distSqr > lod2Dist) lod = 3;

						// insert to spot on list based on distance (shortest to furthest)
						chunkToSpawn.Insert((int)(distSqr / cull * chunkToSpawn.Count), (chunkPos, lod));
					}
					else
					{
						chunkToDestroy.Add(chunkPos);
					}
				}
			}
		}

		foreach (var chunkPos in chunkToSpawn)
		{
			ChunkManager.SpawnChunk(chunkPos.Item1, chunkPos.Item2);
		}

		foreach (var chunkPos in chunkToDestroy)
		{
			ChunkManager.DestroyChunk(chunkPos);
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		if (WithinChunk is null) return;

		CurrentController.ControllerPhysicsProcess(delta, this);
	}

	public override void _Input(InputEvent @event)
	{
		CurrentController.ControllerInput(@event, this);

		if (@event is InputEventKey keyEvent && keyEvent.Pressed)
		{
			switch (keyEvent.Keycode)
			{
				case Key.Escape:
					if (MouseState == Input.MouseModeEnum.Visible)
					{
						MouseState = Input.MouseModeEnum.Captured;
					}
					else
					{
						MouseState = Input.MouseModeEnum.Visible;
					}
					break;
				case Key.F1:
					break;
				case Key.F2:
					if (WithinChunk is null) break;
					int nullBlock = 0, nonAirBlock = 0;
					foreach (var block in WithinChunk.Blocks)
					{
						if (block is null) { nullBlock++; continue; }
						if (block != "base:air") nonAirBlock++;
					}
					GD.Print($"Chunk_Name: {WithinChunk.Name}");
					GD.Print($"Chunk_Position: {WithinChunk.GlobalPosition}");
					GD.Print($"Chunk_NonAir: {nonAirBlock} ({nullBlock})");
					GD.Print($"Chunks: {ChunkManager.AllChunks.Count}");
					break;
				case Key.F3:
					CurrentController = new ControllerWalk();
					break;
				case Key.F4:
					CurrentController = new ControllerFly();
					break;
				case Key.F5:
					GlobalPosition = Vector3.Zero;
					Camera3D.Rotation = new Vector3(0, 0, 0);
					Rotation = new Vector3(0, 0, 0);
					break;
			}
		}
	}
}