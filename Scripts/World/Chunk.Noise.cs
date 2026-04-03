using Godot;

namespace Voxel.World;

public partial class Chunk
{
    // noise per chunk because static noises collide with chunk multithreading
    private FastNoiseLite noise = new();

    // https://auburn.github.io/FastNoiseLite/
    private struct NoiseSettings
    {
        public int SeedOffset = 0;
        public FastNoiseLite.NoiseTypeEnum NoiseType = FastNoiseLite.NoiseTypeEnum.Value;
        public float Frequency = 0.1f;

        public bool FractalEnabled = false;
        public FastNoiseLite.FractalTypeEnum FractalType;
        public int FractalOctaves;
        public float FractalLacunarity;
        public float FractalGain;

        public bool DomainWarpEnabled = false;
        public float DomainWarpAmplitude;
        public FastNoiseLite.DomainWarpTypeEnum DomainWarpType;

        public NoiseSettings() { }
    }

    FastNoiseLite SetNoiseSettings(int seedOffset, FastNoiseLite.NoiseTypeEnum noiseType, float frequency)
    {
        return SetNoiseSettings(new NoiseSettings() { SeedOffset = seedOffset, NoiseType = noiseType, Frequency = frequency });
    }

    FastNoiseLite SetNoiseSettings(NoiseSettings settings)
    {
        noise.Seed = ChunkManager.Seed + (settings.SeedOffset % 10);
        noise.NoiseType = settings.NoiseType;
        noise.Frequency = settings.Frequency;

        if (settings.FractalEnabled)
        {
            noise.FractalType = settings.FractalType;
            noise.FractalOctaves = settings.FractalOctaves;
            noise.FractalLacunarity = settings.FractalLacunarity;
            noise.FractalGain = settings.FractalGain;
        }
        else
        {
            noise.FractalType = FastNoiseLite.FractalTypeEnum.None;
        }

        if (settings.DomainWarpEnabled)
        {
            noise.DomainWarpEnabled = settings.DomainWarpEnabled;
            noise.DomainWarpAmplitude = settings.DomainWarpAmplitude;
            noise.DomainWarpType = settings.DomainWarpType;
        }
        else
        {
            noise.DomainWarpEnabled = false;
        }

        return noise;
    }
}