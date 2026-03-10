using Godot;
using System.Collections.Generic;
using Voxel.Resource;

namespace Voxel;

public partial class ResourceManager : Node
{
	public static Dictionary<int, Block> BlockRegistry { get; private set; } = [];
	public static Dictionary<int, BlockOre> BlockOreRegistry { get; private set; } = [];

	public static Dictionary<int, Biome> BiomeRegistry { get; private set; } = [];
	public static Dictionary<int, Structure> StructureRegistry { get; private set; } = [];

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		RegisterBlocks("res://Resources/Blocks/");

		BiomeRegistry = RegisterGeneric<Biome>("res://Resources/Biomes/");
		StructureRegistry = RegisterGeneric<Structure>("res://Resources/Structures/");
	}

	private static Dictionary<int, T> RegisterGeneric<T>(string path) where T : VoxelResource
	{
		Dictionary<int, T> registry = [];

		void ListDirectory(string path)
		{
			foreach (var file in ResourceLoader.ListDirectory(path))
			{
				if (file == "") continue;
				if (file.EndsWith('/'))
				{
					ListDirectory(path + file);
					continue;
				}

				var res = ResourceLoader.Load(path + file);
				if (res is T resource)
				{
					resource.BuildIds();
					registry.Add(resource.HashId, resource);
				}
			}
		}
		ListDirectory(path);

		GD.Print($"{typeof(T).Name}Registry: {registry.Count} {typeof(T).Name}s");
		return registry;
	}

	private static void RegisterBlocks(string path)
	{
		static void ListDirectory(string path)
		{
			foreach (var file in ResourceLoader.ListDirectory(path))
			{
				if (file == "") continue;
				if (file.EndsWith('/'))
				{
					ListDirectory(path + file);
					continue;
				}

				var resource = ResourceLoader.Load(path + file);
				if (resource is Block b)
				{
					b.BuildIds();

					if (b.FullId == "base:air")
					{
						b.HashId = 0;
					}

					BlockRegistry.Add(b.HashId, b);
					if (b is BlockOre bOre) BlockOreRegistry.Add(bOre.HashId, bOre);
				}
			}
		}
		ListDirectory(path);

		GD.Print($"BlockRegistry: {BlockRegistry.Count} Blocks");
	}

	public static Block GetBlock(string blockId)
	{
		if (blockId == "base:air") return GetAir();

		if (BlockRegistry.TryGetValue(Global.StableHash(blockId), out Block val))
			return val;

		return null;
	}

	public static Block GetBlockCopy(string blockId)
	{
		if (blockId == "base:air") return GetAir();

		var block = GetBlock(blockId);
		if (block == "base:air") return block;

		var newBlock = block.Duplicate() as Block;
		newBlock.FullId = block.FullId;
		newBlock.HashId = block.HashId;
		newBlock.Hp = block.Hp;
		return newBlock;
	}

	public static Block GetAir()
	{
		return BlockRegistry[0];
	}
}
