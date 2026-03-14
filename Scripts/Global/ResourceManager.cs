using Godot;
using System.Collections.Generic;
using Voxel.Resource;

namespace Voxel;

public static class ResourceManager
{
	public static Dictionary<int, Block> BlockRegistry { get; private set; } = [];
	public static Dictionary<int, BlockOre> BlockOreRegistry { get; private set; } = [];

	public static Dictionary<int, Biome> BiomeRegistry { get; private set; } = [];
	public static Dictionary<int, Structure> StructureRegistry { get; private set; } = [];

	public static BlockInstance BlockInstanceAir { get; private set; }
	public static BlockInstance BlockInstanceIrrefragabiles { get; private set; }

	public static void Ready()
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
		BlockRegistry = [];
		BlockOreRegistry = [];

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
						b.BuildIds(0);
						BlockInstanceAir = b.MakeInstance();
					}
					if (b.FullId == "base:irrefragabiles") BlockInstanceIrrefragabiles = b.MakeInstance();

					BlockRegistry.Add(b.HashId, b);
					if (b is BlockOre bOre) BlockOreRegistry.Add(bOre.HashId, bOre);
				}
			}
		}
		ListDirectory(path);

		GD.Print($"BlockRegistry: {BlockRegistry.Count} Blocks");
	}

	/// <summary>
	/// Gets block by reference
	/// </summary>
	public static Block GetBlock(string blockId)
	{
		if (blockId == "base:air") return GetAir();

		if (BlockRegistry.TryGetValue(Global.StableHash(blockId), out Block val))
			return val;

		return null;
	}

	/// <summary>
	/// Gets block by reference
	/// </summary>
	public static Block GetBlock(int blockHash)
	{
		if (blockHash == 0) return GetAir();

		if (BlockRegistry.TryGetValue(blockHash, out Block val))
			return val;

		return null;
	}

	/// <summary>
	/// Creates simpler block class for use in Chunks
	/// </summary>
	public static BlockInstance GetBlockInstance(string blockId)
	{
		if (blockId == "base:air") return BlockInstanceAir;
		if (blockId == "base:irrefragabiles") return BlockInstanceIrrefragabiles;

		return GetBlock(blockId)?.MakeInstance();
	}

	public static Block GetAir()
	{
		return BlockRegistry[0];
	}
}
