using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static Unity.Mathematics.noise;

namespace KaizerwaldCode.ProceduralGeneration.Jobs
{
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
            float _offsetY = RandomJob.NextInt(-100000, 100000) - OffsetJob.y;
            OctOffsetArrayJob[index] = new float2(_offsetX, _offsetY);
        }
    }
    /// <summary>
    /// Noise Height
    /// </summary>
    [BurstCompile(CompileSynchronously = true)]
    public struct NoiseHeightMapJob : IJobParallelFor
    {
        [ReadOnly] public int MapSizeJob;
        [ReadOnly] public int OctavesJob;
        [ReadOnly] public float LacunarityJob;
        [ReadOnly] public float PersistanceJob;
        [ReadOnly] public float ScaleJob;
        [ReadOnly] public NativeArray<float2> OctOffsetArray;


        [NativeDisableParallelForRestriction]
        [WriteOnly] public NativeArray<float> NoiseMap;

        public void Execute(int index)
        {
            float _halfMapSize = MapSizeJob / 2f;
            
            int y = (int)math.floor(index / MapSizeJob);
            int x = index - math.mul(y, MapSizeJob);

            float _amplitude = 1;
            float _frequency = 1;
            float _noiseHeight = 0;
            //Not needed in parallel! it's a layering of noise so it must be done contigiously
            for (int i = 0; i < OctavesJob; i++)
            {
                float sampleX = math.mul((x - _halfMapSize + OctOffsetArray[i].x) / ScaleJob, _frequency);
                float sampleY = math.mul((y - _halfMapSize + OctOffsetArray[i].y) / ScaleJob, _frequency);
                float2 sampleXY = new float2(sampleX, sampleY);

                float pNoiseValue = snoise(sampleXY);
                _noiseHeight = math.mad(pNoiseValue, _amplitude, _noiseHeight);
                _amplitude = math.mul(_amplitude, PersistanceJob);
                _frequency = math.mul(_frequency, LacunarityJob);
            }
            NoiseMap[index] = _noiseHeight;
        }


    }

    /// <summary>
    /// Process RandomJob
    /// </summary>
    [BurstCompile(CompileSynchronously = true)]
    public struct UnLerpNoiseHeightMapJob : IJobParallelFor
    {
        [ReadOnly] public int MapSizeJob;
        //[ReadOnly] public NativeArray<float> MinMaxHeightJob; // min = 0 : max = 1;
        [ReadOnly] public float MinJob;
        [ReadOnly] public float MaxJob;

        public NativeArray<float> NoiseMap;

        public void Execute(int index)
        {
            NoiseMap[index] = math.unlerp(MinJob, MaxJob, NoiseMap[index]);
        }
    }
}
