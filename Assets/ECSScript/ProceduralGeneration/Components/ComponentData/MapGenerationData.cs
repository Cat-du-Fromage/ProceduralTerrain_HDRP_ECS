using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace KaizerwaldCode.ProceduralGeneration.Data
{
    [Serializable]
    public struct MapGenerationData : IComponentData { }
    // PerlinNoise Data
    /**
     * int Seed
     * int Octaves
     * float Lacunarity
     * float Persistance
     * float Scale
     * float2 Offset
    */
    namespace PerlinNoise
    {
        public struct Seed: IComponentData
        {
            public int value;
        }
        public struct Octaves : IComponentData
        {
            public int value;
        }
        public struct Scale : IComponentData
        {
            public float value;
        }
        public struct Lacunarity : IComponentData
        {
            public float value;
        }
        public struct Persistance : IComponentData
        {
            public float value;
        }
        public struct Offset : IComponentData
        {
            public float2 value;
        }
    }
}