using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Godot;
using Voxel.World;

namespace Voxel;

public partial class Player : CharacterBody3D, IPawn
{
	public static Player Self { get; private set; }
	public static int RenderDistance { get; private set; } = 1;

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

	private readonly List<(Vector3 pos, float distSqr)> chunkToSpawn = [];
	private readonly HashSet<Vector3> chunkToDestroy = [];
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

		if (!processingRenderDistance)
		{
			foreach (var (pos, distSqr) in chunkToSpawn)
			{
				ChunkManager.SpawnChunk(pos);
			}
			chunkToSpawn.Clear();
			foreach (var chunkPos in chunkToDestroy)
			{
				ChunkManager.DestroyChunk(chunkPos);
			}
			chunkToDestroy.Clear();
			ProcessRenderDistance(GlobalPosition);
		}
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
				case Key.F6:
					if (GetViewport().DebugDraw == Viewport.DebugDrawEnum.Wireframe) GetViewport().DebugDraw = Viewport.DebugDrawEnum.Disabled;
					else GetViewport().DebugDraw = Viewport.DebugDrawEnum.Wireframe;
					break;
			}
		}
	}

	// ! change so chunkmanager will churn through full output, and replace every few seconds instead of as fast as possible
	private async void ProcessRenderDistance(Vector3 globalPosition)
	{
		processingRenderDistance = true;

		await Task.Run(async () =>
		{
			chunkToSpawn.Add((-Vector3.One * 16, 0));
			// ! var cull = RenderDistance * Chunk.ChunkSize * RenderDistance * Chunk.ChunkSize;
			// var offsetGlobalPosition = globalPosition - Vector3.One * (Chunk.ChunkSize * 0.5f);

			// for (int x = -RenderDistance - 8; x < RenderDistance + 8; x++)
			// {
			// 	for (int y = -RenderDistance - 8; y < RenderDistance + 8; y++)
			// 	{
			// 		for (int z = -RenderDistance - 8; z < RenderDistance + 8; z++)
			// 		{
			// 			var pos = new Vector3(x * Chunk.ChunkSize, y * Chunk.ChunkSize, z * Chunk.ChunkSize);
			// 			var chunkPos = (offsetGlobalPosition + pos).ToChunkPosition();
			// 			var distSqr = (chunkPos - offsetGlobalPosition).LengthSquared();

			// 			if (distSqr < cull)
			// 			{
			// 				chunkToSpawn.Add((chunkPos, distSqr));
			// 			}
			// 			else
			// 			{
			// 				chunkToDestroy.Add(chunkPos);
			// 			}
			// 		}
			// 	}
			// }

			// // sort closest to furthest
			// chunkToSpawn.Sort((a, b) => a.distSqr.CompareTo(b.distSqr));
		});

		processingRenderDistance = false;
	}
}