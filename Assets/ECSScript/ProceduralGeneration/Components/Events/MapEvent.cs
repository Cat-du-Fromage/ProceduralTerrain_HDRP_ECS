using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace KaizerwaldCode.ProceduralGeneration.Data.Event
{
    public struct CreationChunksEntityEvent : IComponentData { }
    public struct HeightMapBigMapCalculEvent : IComponentData { }
    public struct ColorMapCalculEvent : IComponentData { }
    public struct ColorMapCalculated : IComponentData { }
    public struct ChunksEntityCreated : IComponentData {}
}
