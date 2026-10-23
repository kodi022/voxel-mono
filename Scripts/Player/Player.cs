using Voxel.Resource;
using Voxel.World;

namespace Voxel;

public partial class Player : CharacterBody3D, IPawn
{
	public static Player Self { get; private set; }
	public static int RenderDistance { get; private set; } = 4;
	// physics generation (future npc / updates?)
	public static int SimulationDistance { get; private set; } = 4;

	[Export]
	public CollisionShape3D CollisionShape3D { get; set; }
	[Export]
	public CollisionShape3D GroundedShape3D { get; set; }
	[Export]
	public Camera3D Camera3D { get; set; }

	// may be null
	public Chunk WithinChunk { get; private set; }
	public bool AimHitting { get; private set; }
	public Vector3 AimHitPosition { get; private set; }
	public Vector3 AimHitNormal { get; private set; }
	public BlockVec3 AimHitBlockPosition { get; private set; }
	public BlockVec3 AimHitBlockFrontPosition { get; private set; }
	public BlockInstance AimHitBlock { get; private set; }

	public Controller CurrentController { get; private set; } = new ControllerWalk();
	public Input.MouseModeEnum MouseState { get; private set; } = Input.MouseModeEnum.Captured;

	public Node3D Selector { get; private set; }
	public Godot.Collections.Dictionary FrameTraceResult { get; private set; }

	public float Health { get; set; }
	public int DrillLevel { get; set; } = 1;

	private Task rendDisTask = null;
	private Dictionary<int, List<ChunkVec3>> chunkToSpawn = [];
	private HashSet<ChunkVec3> chunkToDestroy = [];
	private Node debugUI;
	private Node menuUI;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Self ??= this;

		Selector = (Node3D)GD.Load<PackedScene>("res://Scenes/selector.tscn").Instantiate();
		GetTree().Root.AddChild(Selector);
	}

	public override void _Process(double delta)
	{
		Input.MouseMode = MouseState;

		var endPos = Camera3D.GetForwardPosition(CurrentController.BlockReachDistance);
		var query = PhysicsRayQueryParameters3D.Create(Camera3D.GlobalPosition, endPos, exclude: [GetRid()]);
		FrameTraceResult = GetWorld3D().DirectSpaceState.IntersectRay(query);
		if (FrameTraceResult.TryGetValue("position", out Variant pos))
		{
			AimHitting = true;
			AimHitPosition = (Vector3)pos;
			AimHitNormal = (Vector3)FrameTraceResult["normal"];
			AimHitBlockPosition = BlockVec3.FromVector3(AimHitPosition - (Vector3)FrameTraceResult["normal"] * 0.5f);
			AimHitBlockFrontPosition = BlockVec3.FromVector3(AimHitPosition + (Vector3)FrameTraceResult["normal"] * 0.5f);

			var block = Chunk.ChunkGetBlock(AimHitBlockPosition);
			if (block is not null && block != 0)
			{
				AimHitBlock = block;
				Selector.GlobalPosition = AimHitBlockPosition.ToVector3();
			}
			else
			{
				AimHitBlock = null;
				Selector.GlobalPosition = Camera3D.GetForwardPosition(-10000f);
			}
		}
		else
		{
			AimHitting = false;
			AimHitPosition = Vector3.Zero;
			AimHitNormal = Vector3.Zero;
			AimHitBlockPosition = BlockVec3.Zero;
			AimHitBlockFrontPosition = BlockVec3.Zero;
			AimHitBlock = null;
			Selector.GlobalPosition = Camera3D.GetForwardPosition(-10000f);
		}

		CurrentController.ControllerProcess(delta, this);

		WithinChunk = Chunk.ChunkFind(ChunkVec3.FromVector3(GlobalPosition));

		if (rendDisTask is null)
		{
			rendDisTask = ProcessRenderDistance(GlobalPosition);
		}
		else if (rendDisTask.Status == TaskStatus.RanToCompletion || rendDisTask.Status == TaskStatus.Faulted)
		{
			ChunkManager.SpawnChunksOverride(chunkToSpawn);
			ChunkManager.DestroyChunks(chunkToDestroy);
			chunkToSpawn = [];
			chunkToDestroy = [];
			rendDisTask = ProcessRenderDistance(GlobalPosition);
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
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
					if (MouseState == Input.MouseModeEnum.Captured)
					{
						MouseState = Input.MouseModeEnum.Visible;
						if (!IsInstanceValid(menuUI))
						{
							menuUI = GD.Load<PackedScene>("res://Scenes/UI/MenuUI.tscn").Instantiate();
							AddChild(menuUI);
						}
					}
					else
					{
						MouseState = Input.MouseModeEnum.Captured;
						menuUI?.Free();
						menuUI = null;
					}
					break;
				case Key.F1:
					if (!IsInstanceValid(debugUI))
					{
						Ui.DebugUI.DebugUIMode = 0;
						debugUI = GD.Load<PackedScene>("res://Scenes/UI/DebugUI.tscn").Instantiate();
						AddChild(debugUI);
					}
					else
					{
						if (Ui.DebugUI.DebugUIMode < 1)
						{
							Ui.DebugUI.DebugUIMode++;
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
					GlobalPosition = Vector3.Zero;
					Camera3D.Rotation = new Vector3(0, 0, 0);
					Rotation = new Vector3(0, 0, 0);
					break;
				case Key.F10:
					Ui.Menu.CloseGame();
					break;
			}
		}
	}


	private async Task ProcessRenderDistance(Vector3 globalPosition)
	{
		await Task.Run(async () =>
		{
			chunkToSpawn = [];
			chunkToDestroy = [];
			List<(ChunkVec3 pos, float distSqr)> chunkToSpawnList = [];
			var cullDistance = RenderDistance * Chunk.ChunkSize * RenderDistance * Chunk.ChunkSize;
			var simDistance = SimulationDistance * Chunk.ChunkSize * SimulationDistance * Chunk.ChunkSize;
			var offsetGlobalPosition = globalPosition - (Vector3.One * Chunk.ChunkSize * 0.5f);
			for (int x = -RenderDistance - 4; x < RenderDistance + 4; x++)
			{
				for (int y = -RenderDistance - 4; y < RenderDistance + 4; y++)
				{
					for (int z = -RenderDistance - 4; z < RenderDistance + 4; z++)
					{
						var checkPos = new ChunkVec3(x, y, z).ToVector3Scaled();
						var distSqr = checkPos.LengthSquared();
						checkPos += offsetGlobalPosition;
						var regionPos = RegionVec3.FromVector3(checkPos);
						var regionHash = regionPos.GetVecHash();
						var chunkPos = ChunkVec3.FromVector3(checkPos);
						var chunkHash = chunkPos.GetVecHash();

						if (distSqr < cullDistance)
						{
							if (ChunkManager.Regions.TryGetValue(regionHash, out Region region))
							{
								if (!region.Chunks.TryGetValue(chunkHash, out Chunk chunk))
								{
									chunkToSpawnList.Add((chunkPos, distSqr));
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
								chunkToSpawnList.Add((chunkPos, distSqr));
							}
						}
						else
						{
							if (ChunkManager.Regions.TryGetValue(regionHash, out Region region))
							{
								if (region.Chunks.ContainsKey(chunkHash))
								{
									chunkToDestroy.Add(chunkPos);
								}
							}
						}
					}
				}
			}

			chunkToSpawn = [];
			chunkToSpawn.Add(0, []);
			chunkToDestroy = [];
			chunkToSpawnList.Sort((a, b) => a.distSqr.CompareTo(b.distSqr));

			// sort closest to furthest
			foreach (var (pos, distSqr) in chunkToSpawnList)
			{
				chunkToSpawn[0].Add(pos);
			}

			await Task.Delay(500);
		});
	}

	// generate lod 0, then generate on 1, etc
	// ! the higher lod, the less it needs processed, try to separate
	// ! rewrite so starts at highest lod and distance, then if under half distance, run at lower lod, like octree
	// private async Task ProcessRenderDistance(Vector3 globalPosition)
	// {
	// 	await Task.Run(async () =>
	// 	{
	// 		var chunks = new Dictionary<int, List<(ChunkVec3 Pos, float DistSqr)>>();

	// 		var simDistance = SimulationDistance * Chunk.ChunkSize * SimulationDistance * Chunk.ChunkSize;

	// 		int LodChunkScale(int lod) => (int)MathF.Max(1f, Mathf.Pow(2f, lod));
	// 		Vector3 CheckOffset(int lodChunkScale) => globalPosition + (Vector3.One * lodChunkScale);

	// 		var maxLod = 1;
	// 		var lodChunkScale = LodChunkScale(maxLod);
	// 		var checkOffset = CheckOffset(lodChunkScale);
	// 		var cullDistance = Mathf.Pow(RenderDistance * Chunk.ChunkSize * lodChunkScale, 2f);
	// 		for (int x = -RenderDistance; x < RenderDistance + 1; x++)
	// 		{
	// 			for (int y = -RenderDistance; y < RenderDistance + 1; y++)
	// 			{
	// 				for (int z = -RenderDistance; z < RenderDistance + 1; z++)
	// 				{
	// 					var checkPos = (new ChunkVec3(x, y, z) * lodChunkScale).ToVector3Scaled();
	// 					var distSqr = checkPos.LengthSquared();
	// 					if (distSqr > cullDistance) continue;

	// 					checkPos += checkOffset;
	// 					var chunkPos = ChunkVec3.FromVector3(checkPos);

	// 					// 1/2 of 10sqr = 7.071sqr
	// 					if (distSqr < cullDistance)
	// 					{
	// 						IterateSection(maxLod - 1, chunkPos);
	// 						continue;
	// 					}

	// 					GenerateLodAtSection(maxLod, chunkPos, distSqr);
	// 				}
	// 			}
	// 		}

	// 		void IterateSection(int lod, ChunkVec3 lodPos)
	// 		{
	// 			if (lod <= 0)
	// 			{
	// 				GenerateLodAtSection(lod, lodPos, 0);
	// 				return;
	// 			}

	// 			var lodChunkScale = LodChunkScale(lod);
	// 			var checkOffset = CheckOffset(lodChunkScale);
	// 			var cullDistance = Mathf.Pow(RenderDistance * Chunk.ChunkSize * lodChunkScale, 2f);
	// 			for (int x = 0; x < 2; x++)
	// 			{
	// 				for (int y = 0; y < 2; y++)
	// 				{
	// 					for (int z = 0; z < 2; z++)
	// 					{
	// 						var checkPos = (lodPos - (new ChunkVec3(x, y, z) * lodChunkScale)).ToVector3Scaled();
	// 						var distSqr = checkPos.LengthSquared();

	// 						checkPos += checkOffset;
	// 						var newChunkPos = ChunkVec3.FromVector3(checkPos);

	// 						if (distSqr < cullDistance)
	// 						{
	// 							IterateSection(lod - 1, newChunkPos);
	// 							continue;
	// 						}

	// 						GenerateLodAtSection(lod, newChunkPos, distSqr);
	// 					}
	// 				}
	// 			}
	// 		}

	// 		void GenerateLodAtSection(int lod, ChunkVec3 chunkPos, float distSqr)
	// 		{
	// 			var lodChunkScale = LodChunkScale(lod);
	// 			var checkOffset = CheckOffset(lodChunkScale);

	// 			for (int x = 0; x < 2; x++)
	// 			{
	// 				for (int y = 0; y < 2; y++)
	// 				{
	// 					for (int z = 0; z < 2; z++)
	// 					{
	// 						var checkPos = (chunkPos + (new ChunkVec3(x, y, z) * lodChunkScale)).ToVector3Scaled();
	// 						//checkPos += checkOffset;
	// 						var newChunkPos = ChunkVec3.FromVector3(checkPos);
	// 						var chunkHash = newChunkPos.GetVecHash();
	// 						ChunkManager.DebugRenderChunk(newChunkPos.ToVector3Scaled(), new Color(1, 1, 1), lodChunkScale);
	// 						var region = ChunkManager.FindRegion((RegionVec3)newChunkPos);

	// 						if (region?.Chunks.TryGetValue(chunkHash, out Chunk chunk) ?? false)
	// 						{
	// 							if (chunk.Lod != lod)
	// 							{
	// 								// ! bruh
	// 								//ChunkManager.UpdateChunk(chunk.ChunkPosition, lod);
	// 								//ChunkManager.DestroyChunkImmediate(chunk.ChunkPosition);
	// 								//if (!chunks.TryAdd(lod, [(newChunkPos, distSqr)]))
	// 								//{
	// 								//chunks[lod].Add((newChunkPos, distSqr));
	// 								//if (lod == 0) ChunkRender.RenderChunk(GetTree().Root, newChunkPos, new Color(1, 1, 1));
	// 								//}
	// 							}

	// 							if (lod == 0)
	// 							{
	// 								if (distSqr < simDistance)
	// 								{
	// 									if (!chunk.Simulating)
	// 										chunk.EnableSimulation();
	// 								}
	// 								else
	// 								{
	// 									if (chunk.Simulating)
	// 										chunk.DisableSimulation();
	// 								}
	// 							}
	// 						}
	// 						else
	// 						{
	// 							//if (!chunks.TryAdd(lod, [(newChunkPos, distSqr)]))
	// 							//{
	// 							//chunks[lod].Add((newChunkPos, distSqr));
	// 							//if (lod == 0) ChunkManager.DebugRenderChunk(newChunkPos, new Color(1, 1, 1));
	// 							//}
	// 						}
	// 					}
	// 				}
	// 			}
	// 		}

	// 		chunkToSpawn = [];
	// 		chunkToDestroy = [];

	// 		// sort closest to furthest
	// 		foreach (var lodKVP in chunks.Reverse())
	// 		{
	// 			lodKVP.Value.Sort((a, b) => a.DistSqr.CompareTo(b.DistSqr));
	// 			foreach (var val in lodKVP.Value)
	// 			{
	// 				if (!chunkToSpawn.TryAdd(lodKVP.Key, [val.Pos]))
	// 					chunkToSpawn[lodKVP.Key].Add(val.Pos);
	// 			}
	// 		}
	// 	});
	// }
}