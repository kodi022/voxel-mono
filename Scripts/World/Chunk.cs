using Godot;
using System;
using System.Threading.Tasks;
using Voxel.Resource;

namespace Voxel.World;

public partial class Chunk : MeshInstance3D
{
	public static readonly int ChunkSize = 16;

	public static readonly OrmMaterial3D BlockMaterial = GD.Load<OrmMaterial3D>("res://Materials/Block.tres");
	public static readonly Texture2D MissingTexture = GD.Load<Texture2D>("res://Images/missing.png");

	[Export]
	public CollisionShape3D CollisionShape3D { get; set; }

	public Block[,,] Blocks { get; set; }
	public Vector3 WorldPosition { get; set; }
	public int PositionHash { get; set; }
	public int LOD { get; set; } = 0;
	public bool ClearMeshOnReady { get; set; } = true;
	public bool Initialized { get; private set; } = false;

	private Task blockGeneration;
	private Task meshGeneration;

	// Called when the node enters the scene tree for the first time. Also called when made visible.
	public override void _Ready()
	{
		ChunkManager.InitializingChunks.Add(PositionHash);
		GlobalPosition = WorldPosition;

		if (EmptyMesh is null)
		{
			var emptyMesh = new ArrayMesh();
			var emptyArrays = new Godot.Collections.Array();
			emptyArrays.Resize((int)Mesh.ArrayType.Max);
			emptyArrays[(int)Mesh.ArrayType.Vertex] = new Vector3[3] { new(0, 0, 0), new(0, 0.00001f, 0), new(0, 0, 0.00001f) };
			emptyArrays[(int)Mesh.ArrayType.Normal] = new Vector3[3] { new(0, 0, 0), new(0, 0, 0), new(0, 0, 0) };
			emptyArrays[(int)Mesh.ArrayType.TexUV] = new Vector2[3] { new(0, 0), new(0, 0), new(0, 0) };
			emptyArrays[(int)Mesh.ArrayType.Index] = new int[3] { 0, 1, 2 };
			emptyMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, emptyArrays);
			EmptyMesh = emptyMesh;
		}

		if (ClearMeshOnReady)
		{
			Mesh = EmptyMesh;
			CollisionShape3D.Shape = null;
		}

		mesh = null;
		meshShape = null;
		surfacesTemp = null;
		blockGeneration = null;
		meshGeneration = null;
		Blocks ??= new Block[ChunkSize, ChunkSize, ChunkSize];

		GenerateNewChunk();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public static void ChunkMineBlock(in Vector3 position)
	{
		var chunk = ChunkManager.FindChunk(position);
		chunk?.SetBlock(position, "base:air");
	}

	public static void ChunkPlaceBlock(in Vector3 position, in string blockId)
	{
		var chunk = ChunkManager.FindChunk(position);
		chunk?.SetBlock(position, blockId);
	}

	public static Block ChunkSelectBlock(in Vector3 position)
	{
		var chunk = ChunkManager.FindChunk(position);
		return chunk?.GetBlock(position);
	}

	// use if erroring in threaded code (VERY LAGGY)
	// public async void GenerateNewChunk()
	// {
	// 	blockGeneration = GenerateBlocks();
	// 	await blockGeneration;
	// 	blockGeneration = null;
	// 	meshGeneration = GenerateMesh();
	// 	await meshGeneration;
	// 	meshGeneration = null;
	// 	CallDeferred(nameof(ApplyMesh));
	// }

	public void GenerateNewChunk()
	{
		Task.Run(async () =>
		{
			blockGeneration = GenerateBlocks();
			await blockGeneration;
			blockGeneration = null;
			meshGeneration = GenerateMesh();
			await meshGeneration;
			meshGeneration = null;
			CallDeferred(nameof(ApplyMesh));
			CallDeferred(nameof(FinishInitializing));
		});

		// regen adjacent meshes to cull
		// ChunkManager.FindChunk(WorldPosition + new Vector3(16, 0, 0))?.UpdateMesh();
		// ChunkManager.FindChunk(WorldPosition + new Vector3(-16, 0, 0))?.UpdateMesh();
		// ChunkManager.FindChunk(WorldPosition + new Vector3(0, 16, 0))?.UpdateMesh();
		// ChunkManager.FindChunk(WorldPosition + new Vector3(0, -16, 0))?.UpdateMesh();
		// ChunkManager.FindChunk(WorldPosition + new Vector3(0, 0, 16))?.UpdateMesh();
		// ChunkManager.FindChunk(WorldPosition + new Vector3(0, 0, -16))?.UpdateMesh();
	}

	private void FinishInitializing()
	{
		Initialized = true;
		ChunkManager.InitializingChunks.Remove(PositionHash);
	}

	public void SetProcessing(bool physics)
	{
		SetProcess(false);
		SetPhysicsProcess(physics);
	}

	public void SetBlocks(in Vector3[] positions, in string blockId)
	{
		bool change = false;
		foreach (var pos in positions)
		{
			var bPos = pos.ToBlockLocalPosition(GlobalPosition);
			if (!bPos.IsInside(ChunkSize)) return;

			Blocks[(int)bPos.X, (int)bPos.Y, (int)bPos.Z] = (Block)blockId;
			change = true;
		}

		if (change) UpdateMesh();
	}

	public void SetBlock(in Vector3 position, in string blockId)
	{
		var pos = position.ToBlockLocalPosition(GlobalPosition);
		if (!pos.IsInside(ChunkSize)) return;

		bool change = Blocks[(int)pos.X, (int)pos.Y, (int)pos.Z] != blockId;
		Blocks[(int)pos.X, (int)pos.Y, (int)pos.Z] = (Block)blockId;
		if (change) UpdateMesh();
	}

	public Block GetBlock(in Vector3 position)
	{
		var pos = position.ToBlockLocalPosition(GlobalPosition);
		if (!pos.IsInside(ChunkSize)) return (Block)"block:air";

		return Blocks[(int)pos.X, (int)pos.Y, (int)pos.Z];
	}

	// no api for threading
	public Block GetBlock(in Vector3 position, in Vector3 globalPosition)
	{
		var pos = position.ToBlockLocalPosition(globalPosition);
		if (!pos.IsInside(ChunkSize)) return (Block)"block:air";

		return Blocks[(int)pos.X, (int)pos.Y, (int)pos.Z];
	}

	public static void IterateChunk(Action<sbyte, sbyte, sbyte> action)
	{
		for (sbyte x = 0; x < ChunkSize; x++)
		{
			for (sbyte z = 0; z < ChunkSize; z++)
			{
				for (sbyte y = 0; y < ChunkSize; y++)
				{
					action.Invoke(x, y, z);
				}
			}
		}
	}

	public static Texture2D LoadTextureFromBlock(Texture2D resourceTexture, string resourceTexturePath)
	{
		if (resourceTexture is not null) return resourceTexture;
		if (!string.IsNullOrEmpty(resourceTexturePath)) return GD.Load<Texture2D>(resourceTexturePath);
		return MissingTexture;
	}
}