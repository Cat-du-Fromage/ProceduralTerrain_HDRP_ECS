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
            // 1 Process octavesOffset and fill it; => separate process(so we can use it as a read only)
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
                    //Not needed in parallel! it's a layering of noise so it must be done contigiously
                    for (int i = 0; i < octavesJob; i++)
                    {
                        float sampleX = math.mul((x - halfMapSize) / scaleJob, frequency) + octOffsetArray[i].x;
                        float sampleY = math.mul((y - halfMapSize) / scaleJob, frequency) + octOffsetArray[i].y;
                        float2 sampleXY = new float2(sampleX, sampleY);

                        float pNoiseValue = snoise(sampleXY);
                        noiseHeight = math.mad(pNoiseValue, amplitude, noiseHeight);
                        //lacunarity: controls increase in frequency of octaves
                        //Persistance : controls decrease in amplitude of octaves
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
    /*
     List<jobhandles> JobList = new List<JobHandles>()
    noiseMap Nativearray
        for (int y = 0; y < mapSizeJob; y++)
            {
                for (int x = 0; x < mapSizeJob; x++)
                {
                    if(x== 0 && y == 0)
                    {
                        var new PerlinJob
                        {
                            x,
                            y,
                        }
                        JobList.add(job.schedule(noiseMap.Length, 250))
                    }
                    list<Job>.add(job.schedule(noiseMap.Length, 250,jobhandles[i-1]))
                }
            }
    */
    /// <summary>
    /// Process RandomJob
    /// </summary>
    [BurstCompile(CompileSynchronously = true)]
    public struct NoiseRandomJob : IJobParallelFor
    {
        [ReadOnly] public Unity.Mathematics.Random RandomJob;
        [ReadOnly] public float2 OffsetJob;
        [WriteOnly] public NativeArray<float2> OctOffsetArrayJob;

        public void Execute(int index)
        {
            float _offsetX = RandomJob.NextInt(-100000, 100000) + OffsetJob.x;
            float _offsetY = RandomJob.NextInt(-100000, 100000) + OffsetJob.y;
            OctOffsetArrayJob[index] = new float2(_offsetX, _offsetY);
        }
    }

    [BurstCompile(CompileSynchronously = true)]
    public struct NoiseHeightMapJob : IJobParallelFor
    {
        [ReadOnly] public int MapSizeJob;
        [ReadOnly] public int OctavesJob;
        [ReadOnly] public float LacunarityJob;
        [ReadOnly] public float PersistanceJob;
        [ReadOnly] public float ScaleJob;
        float _maxNoiseHeight;
        float _minNoiseHeight;

        [ReadOnly] public NativeArray<float2> OctOffsetArray;
        [WriteOnly] public NativeArray<float> NoiseMap;
        public void Execute(int index)
        {
            //FOR LOOP
            _maxNoiseHeight = float.MinValue;
            _minNoiseHeight = float.MaxValue;

            float _halfMapSize = MapSizeJob / 2f;

            int y = (int)math.floor(index / MapSizeJob);
            int x = index - math.mul(y, MapSizeJob);

            float _amplitude = 1;
            float _frequency = 1;
            float _noiseHeight = 0;
            //Not needed in parallel! it's a layering of noise so it must be done contigiously
            for (int i = 0; i < OctavesJob; i++)
            {
                float sampleX = math.mul((x - _halfMapSize) / ScaleJob, _frequency) + OctOffsetArray[i].x;
                float sampleY = math.mul((y - _halfMapSize) / ScaleJob, _frequency) + OctOffsetArray[i].y;
                float2 sampleXY = new float2(sampleX, sampleY);

                float pNoiseValue = snoise(sampleXY);
                _noiseHeight = math.mad(pNoiseValue, _amplitude, _noiseHeight);
                //lacunarity: controls increase in frequency of octaves
                //Persistance : controls decrease in amplitude of octaves
                _amplitude = math.mul(_amplitude, PersistanceJob);
                _frequency = math.mul(_frequency, LacunarityJob);
            }

            if (_noiseHeight > _maxNoiseHeight)
            {
                _maxNoiseHeight = _noiseHeight;
            }
            else if (_noiseHeight < _minNoiseHeight)
            {
                _minNoiseHeight = _noiseHeight;
            }
            NoiseMap[index] = _noiseHeight;
            //noiseMap[math.mad(y, MapSizeJob, x)] = noiseHeight;
        }
    }

    /// <summary>
    /// Process RandomJob
    /// </summary>
    [BurstCompile(CompileSynchronously = true)]
    public struct UnLerpNoiseHeightMapJob : IJobParallelFor
    {
        [ReadOnly] public int MapSizeJob;
        [ReadOnly] public float MaxNoiseHeightJob; //taken from NoiseHeightMapJob
        [ReadOnly] public float MinNoiseHeightJob; //taken from NoiseHeightMapJob
        public NativeArray<float> NoiseMap;

        public void Execute(int index)
        {
            //int y = (int)math.floor(index / MapSizeJob);
            //int x = index - math.mul(y, MapSizeJob);
            NoiseMap[index] = math.unlerp(MinNoiseHeightJob, MaxNoiseHeightJob, NoiseMap[index]);
        }
    }
    //.Schedule(Data.Length, DEFAULT_BATCH_COUNT, dependency)

    //.Schedule(OctavesOffset.Length, DEFAULT_BATCH_COUNT, dependency)
    //.Schedule(NoiseMap.Length, DEFAULT_BATCH_COUNT, dependency)
}
