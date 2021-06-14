using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace KaizerwaldCode.ProceduralGeneration.Data.Chunks
{
    public struct ChunksMapData : IComponentData { }

    public struct GridPosition : IComponentData
    {
        public float2 Value;
    }

    namespace MeshBuffer
    {
        public struct Vertices : IBufferElementData
        {
            public float3 Position;
        }

        public struct Triangles : IBufferElementData
        {
            public int Value;
        }

        public struct Uvs : IBufferElementData
        {
            public float2 Value;
        }
    }
}
