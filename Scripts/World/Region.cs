using System.Collections.Generic;
using Godot;
using Voxel.Resource;

namespace Voxel;

public class Region
{
	public Vector3 Position;
	public List<Structure> Structures;
	public Dictionary<int, int> ModifiedBlocks;


}