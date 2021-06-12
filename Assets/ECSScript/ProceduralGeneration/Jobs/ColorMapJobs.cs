using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using KaizerwaldCode.ProceduralGeneration.Data.DynamicBuffer;
using UnityEngine;

namespace KaizerwaldCode.ProceduralGeneration.Jobs
{
    [BurstCompile (CompileSynchronously = true)]
    public struct ColorMapJob : IJobParallelFor
    {
        [ReadOnly] public DynamicBuffer<Regions> RegionsBufferJob;
        [ReadOnly] public DynamicBuffer<HeightMap> HeightMapBufferJob;
        [WriteOnly] public NativeArray<MaterialColor> ColorMapNativeArrayJob;
        public void Execute(int index)
        {
            float _currentHeight = HeightMapBufferJob[index].Value;
            for (int i = 0; i< RegionsBufferJob.Length; i++)
            {
                if (_currentHeight <= RegionsBufferJob[i].Height)
                {
                    ColorMapNativeArrayJob[index] = RegionsBufferJob[i].Color;
                    break;
                }
            }
        }
    }
}
