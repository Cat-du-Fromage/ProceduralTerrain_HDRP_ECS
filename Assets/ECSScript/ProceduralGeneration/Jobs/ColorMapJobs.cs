using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace KaizerwaldCode.ProceduralGeneration.Jobs
{
    /*
    MapData GenerateMapData()
    {
    float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize, mapChunkSize, seed, noiseScale, octaves, persistance, lacunarity, offset);

    //Apply color depending of the map's height value and value height's value assign to each regions
    Color[] colourMap = new Color[math.mul(mapChunkSize, mapChunkSize)];
        for (int y = 0; y < mapChunkSize; y++)
    {
        for (int x = 0; x < mapChunkSize; x++)
        {
            float currentHeight = noiseMap[x, y];
            for (int i = 0; i < regions.Length; i++)
            {
                if (currentHeight <= regions[i].height)
                {
                    colourMap[math.mad(y, mapChunkSize, x)] = regions[i].colour;
                    break;
                }
            }
        }
    }

    return new MapData(noiseMap, colourMap);
    }
    */
    [BurstCompile (CompileSynchronously = true)]
    public struct ColorMapJob : IJobParallelFor
    {
        public void Execute(int index)
        {
            throw new global::System.NotImplementedException();
        }
    }
}
