using KaizerwaldCode.ProceduralGeneration.Data.DynamicBuffer;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using MapJobs = KaizerwaldCode.ProceduralGeneration.Jobs;
using MapSett = KaizerwaldCode.ProceduralGeneration.Data.PerlinNoise;

namespace KaizerwaldCode.ProceduralGeneration.System
{
    public class ColorMapSystem : SystemBase
    {
        NativeArray<MaterialColor> _colorMapNativeArray;
        EntityManager _em;
        protected override void OnCreate()
        {
            RequireForUpdate(GetEntityQuery(typeof(Data.Event.NoiseMapCalculated)));
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
        }
        protected override void OnUpdate()
        {
            Entity _mapSettings = GetSingletonEntity<Data.Tag.MapSettings>();
            int _mapSurface = math.mul(GetComponent<MapSett.MapSize>(_mapSettings).Value, GetComponent<MapSett.MapSize>(_mapSettings).Value);

            _colorMapNativeArray = new NativeArray<MaterialColor>(_mapSurface, Allocator.Persistent);
            /*========================
             * Random octaves Offset Job
             * return : OctOffsetArrayJob
             ========================*/
            MapJobs.ColorMapJob _colorMapJob = new MapJobs.ColorMapJob()
            {
                HeightMapBufferJob = GetBuffer<HeightMap>(_mapSettings),
                RegionsBufferJob = GetBuffer<Regions>(_mapSettings),
                ColorMapNativeArrayJob = _colorMapNativeArray,
            };
            JobHandle _colorMapJobHandle = _colorMapJob.Schedule(_colorMapNativeArray.Length, 250);
            _colorMapJobHandle.Complete();

            _em.GetBuffer<ColorMap>(_mapSettings).Reinterpret<MaterialColor>().CopyFrom(_colorMapNativeArray);
            _em.GetBuffer<ColorMap>(_mapSettings).Reinterpret<ColorMap>();
            //for test
            #region TEST
            Texture2D texture2D = new Texture2D(GetComponent<MapSett.MapSize>(_mapSettings).Value, GetComponent<MapSett.MapSize>(_mapSettings).Value);
            texture2D.filterMode = FilterMode.Point;
            texture2D.wrapMode = TextureWrapMode.Clamp;
            texture2D.SetPixels(_colorMapNativeArray.Reinterpret<Color>().ToArray());
            texture2D.Apply();
            var material = _em.GetSharedComponentData<RenderMesh>(GetSingletonEntity<TerrainAuthoring>()).material;
            material.mainTexture = texture2D;
            //Set the correct Scale to the Mesh
            var localToWorldScale = new NonUniformScale
            {
                Value = new float3(GetComponent<MapSett.MapSize>(_mapSettings).Value, GetComponent<MapSett.MapSize>(_mapSettings).Value, GetComponent<MapSett.MapSize>(_mapSettings).Value)
            };
            
            _em.AddComponentData(GetSingletonEntity<TerrainAuthoring>(), localToWorldScale);
            #endregion TEST

            _em.RemoveComponent<Data.Event.NoiseMapCalculated>(GetSingletonEntity<Data.Tag.MapEventHolder>());

            _colorMapNativeArray.Dispose();
        }

        protected override void OnDestroy()
        {
            if (_colorMapNativeArray.IsCreated) _colorMapNativeArray.Dispose();
        }
    }
}
