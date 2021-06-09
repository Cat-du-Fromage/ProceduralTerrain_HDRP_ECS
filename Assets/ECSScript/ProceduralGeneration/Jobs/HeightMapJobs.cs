using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.noise;

namespace KaizerwaldCode.ProceduralGeneration.Jobs
{
    [BurstCompile]
    public struct PerlinNoiseJob : IJob
    {
        [ReadOnly] public int mapSizeJob;
        [ReadOnly] public int seedJob;
        [ReadOnly] public float scaleJob;
        [ReadOnly] public int octavesJob;
        [ReadOnly] public float persistanceJob;
        [ReadOnly] public float lacunarityJob;
        [ReadOnly] public float2 offsetJob;

        //returned Value
        public NativeArray<float> noiseMap;
        public NativeArray<float2> octOffsetArray;

        public void Execute()
        {
            #region Random
            Unity.Mathematics.Random pRNG = new Unity.Mathematics.Random((uint) seedJob);

            for (int i = 0; i < octOffsetArray.Length; i++)
            {
                float offsetX = pRNG.NextInt(-100000, 100000) + offsetJob.x;
                float offsetY = pRNG.NextInt(-100000, 100000) + offsetJob.y;
                octOffsetArray[i] = new float2(offsetX, offsetY);
            }

            #endregion Random

            float maxNoiseHeight = float.MinValue;
            float minNoiseHeight = float.MaxValue;

            float halfMapSize = mapSizeJob / 2f;

            for (int y = 0; y < mapSizeJob; y++)
            {
                for (int x = 0; x < mapSizeJob; x++)
                {
                    float amplitude = 1;
                    float frequency = 1;
                    float noiseHeight = 0;
                    for (int i = 0; i < octavesJob; i++)
                    {
                        float sampleX = math.mul((x - halfMapSize) / scaleJob, frequency) + octOffsetArray[i].x;
                        float sampleY = math.mul((y - halfMapSize) / scaleJob, frequency) + octOffsetArray[i].y;
                        float2 sampleXY = new float2(sampleX, sampleY);

                        float pNoiseValue = snoise(sampleXY);
                        noiseHeight = math.mad(pNoiseValue, amplitude, noiseHeight);
                        //amplitude : decrease each octaves; frequency : increase each octaves
                        amplitude = math.mul(amplitude, persistanceJob);
                        frequency = math.mul(frequency, lacunarityJob);
                    }

                    //First we check max and min Height for the terrain
                    if (noiseHeight > maxNoiseHeight)
                    {
                        maxNoiseHeight = noiseHeight;
                    }
                    else if (noiseHeight < minNoiseHeight)
                    {
                        minNoiseHeight = noiseHeight;
                    }

                    //then we apply thoses value to the terrain
                    noiseMap[math.mad(y, mapSizeJob, x)] = noiseHeight; // to find index of a 2D array in a 1D array (y*width)+1
                }
            }

            for (int y = 0; y < mapSizeJob; y++)
            {
                for (int x = 0; x < mapSizeJob; x++)
                {
                    noiseMap[math.mad(y, mapSizeJob, x)] = math.unlerp(minNoiseHeight, maxNoiseHeight, noiseMap[math.mad(y, mapSizeJob, x)]); //unlerp = InverseLerp so we want a %
                }
            }
        }
    }
}
