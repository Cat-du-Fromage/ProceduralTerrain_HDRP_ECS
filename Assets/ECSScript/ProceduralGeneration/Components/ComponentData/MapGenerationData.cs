using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace KaizerwaldCode.ProceduralGeneration.Data
{
    public struct MapGenerationData : IComponentData { }

    namespace PerlinNoise
    {
        public struct MapSize : IComponentData
        {
            public int Value;
        }
        public struct Seed: IComponentData
        {
            public int Value;
        }
        public struct Octaves : IComponentData
        {
            public int Value;
        }
        public struct Scale : IComponentData
        {
            public float Value;
        }
        public struct Lacunarity : IComponentData
        {
            public float Value;
        }
        public struct Persistance : IComponentData
        {
            public float Value;
        }
        public struct Offset : IComponentData
        {
            public float2 Value;
        }
        public struct LevelOfDetail : IComponentData
        {
            public int Value;
        }
    }

    namespace DynamicBuffer.HeightMap
    {
        public struct NoiseMap : IBufferElementData
        {
            public float Value;
        }
    }
}