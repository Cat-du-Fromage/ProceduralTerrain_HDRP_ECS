using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace KaizerwaldCode.ProceduralGeneration.Data.Event
{
    public struct MapSettingsConverted : IComponentData { }
    public struct CreationChunksEntityEvent : IComponentData { }
    public struct HeightMapBigMapCalculEvent : IComponentData { }
    public struct NoiseMapCalculated : IComponentData { }
    public struct ColorMapCalculated : IComponentData { }
    public struct ChunksEntityCreated : IComponentData {}
}
