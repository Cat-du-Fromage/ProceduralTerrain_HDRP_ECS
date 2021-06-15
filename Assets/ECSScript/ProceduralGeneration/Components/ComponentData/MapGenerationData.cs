using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Animation;
using UnityEngine;
using AnimationCurve = Unity.Animation.AnimationCurve;

namespace KaizerwaldCode.ProceduralGeneration.Data
{
    public struct MapGenerationData : IComponentData { }

    namespace PerlinNoise
    {
        public struct MapSize : IComponentData
        {
            public int Value;
        }
        public struct ChunkSize : IComponentData
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

        public struct HeightMultiplier : IComponentData
        {
            public float Value;
        }
        public struct LevelOfDetail : IComponentData
        {
            public int Value;
        }

        public struct HeightCurve : IComponentData
        {
            public AnimationCurve Value;
        }
    }

    namespace DynamicBuffer
    {
        public struct HeightMap : IBufferElementData
        {
            public float Value;
        }

        public struct ColorMap : IBufferElementData
        {
            public MaterialColor Value;
        }

        public struct Regions : IBufferElementData
        {
            public float Height;
            public MaterialColor Color;
        }
    }
}