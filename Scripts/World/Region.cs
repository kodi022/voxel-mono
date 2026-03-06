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

	public Chunk GetChunk(Vector3 position)
	{
		if (!ready) return null;

		position = position.ToChunkPosition();
		var posHash = HashCode.Combine(position);

		if (!Chunks.TryGetValue(posHash, out Chunk chunk))
		{
			chunk = new Chunk
			{
				WorldPosition = position,
				PositionHash = posHash
			};
			Chunks.Add(posHash, chunk);
		}

		return chunk;
	}

	public Chunk GetChunk(int posHash)
	{
		if (!ready) return null;

		if (Chunks.TryGetValue(posHash, out Chunk chunk))
		{
			return chunk;
		}

		return null;
	}

	// necessary because CallDeferred is required yet chunks are not Node's
	public async void ChunkGenerate(Chunk chunk)
	{
		chunk.Generating = true;
		ChunkManager.GeneratingChunks.Add(chunk.PositionHash);

		chunk.DestroyMeshes();
		await Task.Run(async () =>
		{
			await chunk.GenerateBlocks();
			await chunk.GenerateMesh();
		});
		CallDeferred(nameof(ChunkFinish), chunk.PositionHash);
	}

	// necessary because CallDeferred is required yet chunks are not Node's
	public async void ChunkUpdate(Chunk chunk)
	{
		chunk.DestroyMeshes();
		await Task.Run(async () =>
		{
			await chunk.GenerateMesh();
		});
		CallDeferred(nameof(ChunkFinish), chunk.PositionHash);
	}

	private void ChunkFinish(int chunkPosHash)
	{
		var chunk = GetChunk(chunkPosHash);
		chunk.FinishMesh();
		chunk.Generating = false;
		ChunkManager.GeneratingChunks.Remove(chunk.PositionHash);
	}
}