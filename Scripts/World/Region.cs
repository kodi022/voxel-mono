using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Voxel.Resource;

namespace Voxel.World;

public partial class Region : Node3D
{
	public Vector3 WorldPosition;
	public List<Structure> Structures;
	public Dictionary<int, Chunk> Chunks { get; private set; } = [];
	//public Dictionary<int, int> ModifiedBlocks;

	private bool ready = false;

	// Called when the node enters the scene tree for the first time. Also called when made visible.
	public override void _Ready()
	{
		ready = true;
	}

	public void ChunkCreate(Vector3 position)
	{
		if (!ready) return;

		position = position.ToChunkPosition();
		var posHash = HashCode.Combine(position);

		if (!Chunks.ContainsKey(posHash))
		{
			var chunk = new Chunk(this, position);
			Chunks.Add(posHash, chunk);
		}
	}

	public Chunk ChunkGet(int posHash)
	{
		if (!ready) return null;

		if (Chunks.TryGetValue(posHash, out Chunk chunk))
		{
			return chunk;
		}

		return null;
	}

	public Chunk ChunkGet(Vector3 position)
	{
		if (!ready) return null;

		position = position.ToChunkPosition();

		if (Chunks.TryGetValue(HashCode.Combine(position), out Chunk chunk))
		{
			return chunk;
		}

		return null;
	}

	public void ChunkDestroy(int posHash)
	{
		if (Chunks.TryGetValue(posHash, out Chunk chunk))
		{
			if (chunk is not null)
			{
				ChunkManager.GeneratingChunks.Remove(chunk.PositionHash);
				Chunks[posHash] = null;
				Chunks.Remove(posHash);
			}
		}
	}

	// necessary because CallDeferred is required yet chunks are not Node's
	public async void ChunkGenerate(Chunk chunk)
	{
		chunk.Generating = true;
		ChunkManager.GeneratingChunks.Add(chunk.PositionHash);

		await Task.Run(async () =>
		{
			await chunk.GenerateBlockData();
			await chunk.GenerateMeshData();
			CallDeferred(nameof(ChunkFinish), chunk.PositionHash);
		});
	}

	// necessary because CallDeferred is required yet chunks are not Node's
	public async void ChunkUpdate(Chunk chunk)
	{
		chunk.Generating = true;
		await Task.Run(async () =>
		{
			await chunk.GenerateMeshData();
			CallDeferred(nameof(ChunkFinish), chunk.PositionHash);
		});
	}

	// necessary because CallDeferred cannot call functions on other objects. arg has to be Godot.Variant
	private void ChunkFinish(int chunkPosHash)
	{
		var chunk = ChunkGet(chunkPosHash);
		if (chunk is null) return;

		chunk.CreateMesh();
		if (chunk.Simulating) chunk.CreatePhysics();

		chunk.Generating = false;

		ChunkManager.GeneratingChunks.Remove(chunk.PositionHash);
	}
}