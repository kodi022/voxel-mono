using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Voxel.World;

namespace Voxel;

public partial class Player : CharacterBody3D, IPawn
{
	public static Player Self { get; private set; }
	public static int RenderDistance { get; private set; } = 10;
	// physics generation (future npc / updates?)
	public static int SimulationDistance { get; private set; } = 8;

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

	private bool processingRenderDistance = false;
	private Node debugUI;

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

		if (!processingRenderDistance) ProcessRenderDistance(GlobalPosition);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		// ! if (WithinChunk is null) return;

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
					if (debugUI is null)
					{
						DebugUI.DebugUIMode = 0;
						debugUI = GD.Load<PackedScene>("res://Scenes/UI/DebugUI.tscn").Instantiate();
						AddChild(debugUI);
					}
					else
					{
						if (DebugUI.DebugUIMode < 1)
						{
							DebugUI.DebugUIMode++;
						}
						else
						{
							debugUI.Free();
							debugUI = null;
						}
					}
					break;
				case Key.F2:
					if (CurrentController is ControllerWalk) CurrentController = new ControllerFly();
					else CurrentController = new ControllerWalk();
					break;
				case Key.F3:

					break;
				case Key.F4:

					break;
				case Key.F5:
					GlobalPosition = Vector3.Zero;
					Camera3D.Rotation = new Vector3(0, 0, 0);
					Rotation = new Vector3(0, 0, 0);
					break;
				case Key.F6:
					var viewport = GetViewport();
					if (viewport.DebugDraw == Viewport.DebugDrawEnum.Disabled) viewport.DebugDraw = Viewport.DebugDrawEnum.Wireframe;
					else if (viewport.DebugDraw == Viewport.DebugDrawEnum.Wireframe) viewport.DebugDraw = Viewport.DebugDrawEnum.DisableLod;
					else viewport.DebugDraw = Viewport.DebugDrawEnum.Disabled;
					break;
			}
		}
	}

	private async void ProcessRenderDistance(Vector3 globalPosition)
	{
		processingRenderDistance = true;

		await Task.Run(async () =>
		{
			//chunkToSpawn.Add((-Vector3.One * 16, 0));
			List<(Vector3 pos, float distSqr)> chunkToSpawn = [];
			List<Vector3> chunkToDestroy = [];

			var cullDistance = RenderDistance * Chunk.ChunkSize * RenderDistance * Chunk.ChunkSize;
			var simDistance = SimulationDistance * Chunk.ChunkSize * SimulationDistance * Chunk.ChunkSize;
			var offsetGlobalPosition = globalPosition - Vector3.One * (Chunk.ChunkSize * 0.5f);
			for (int x = -RenderDistance - 6; x < RenderDistance + 6; x++)
			{
				for (int y = -RenderDistance - 6; y < RenderDistance + 6; y++)
				{
					for (int z = -RenderDistance - 6; z < RenderDistance + 6; z++)
					{
						var pos = new Vector3(x * Chunk.ChunkSize, y * Chunk.ChunkSize, z * Chunk.ChunkSize);
						var chunkPos = (offsetGlobalPosition + pos).ToChunkPosition();
						var distSqr = (chunkPos - offsetGlobalPosition).LengthSquared();

						if (distSqr < cullDistance)
						{
							var regionHash = HashCode.Combine(chunkPos.ToRegionPosition());
							if (ChunkManager.Regions.TryGetValue(regionHash, out Region region))
							{
								var chunkPosHash = HashCode.Combine(chunkPos);
								if (!region.Chunks.TryGetValue(chunkPosHash, out Chunk chunk))
								{
									chunkToSpawn.Add((chunkPos, distSqr));
								}
								else
								{
									if (distSqr < simDistance)
									{
										if (!chunk.Simulating)
											chunk.EnableSimulation();
									}
									else
									{
										if (chunk.Simulating)
											chunk.DisableSimulation();
									}
								}
							}
							else
							{
								chunkToSpawn.Add((chunkPos, distSqr));
							}
						}
						else
						{
							var regionHash = HashCode.Combine(chunkPos.ToRegionPosition());
							if (ChunkManager.Regions.TryGetValue(regionHash, out Region region))
							{
								if (region.Chunks.ContainsKey(HashCode.Combine(chunkPos)))
								{
									chunkToDestroy.Add(chunkPos);
								}
							}
						}
					}
				}
			}

			// sort closest to furthest
			chunkToSpawn.Sort((a, b) => a.distSqr.CompareTo(b.distSqr));

			List<Vector3> a = [];
			foreach (var (pos, distSqr) in chunkToSpawn)
			{
				a.Add(pos);
			}
			ChunkManager.SpawnChunksOverride(a);
			ChunkManager.DestroyChunks(chunkToDestroy);

			await Task.Delay(500);
		});

		processingRenderDistance = false;
	}
}