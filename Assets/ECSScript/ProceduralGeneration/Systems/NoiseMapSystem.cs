using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using MapJobs = KaizerwaldCode.ProceduralGeneration.Jobs;
using MapSett = KaizerwaldCode.ProceduralGeneration.Data.PerlinNoise;
using BufferHeightMap = KaizerwaldCode.ProceduralGeneration.Data.DynamicBuffer;
using UnityEngine;

namespace KaizerwaldCode.ProceduralGeneration.System
{
    public class NoiseMapSystem : SystemBase
    {
        NativeArray<float> _noiseMapNativeArray;
        NativeArray<float2> _octaveOffsetNativeArray;
        NativeArray<float> _minMaxHeightNativeArray;

        EntityManager _em;
        protected override void OnCreate()
        {
            RequireForUpdate(GetEntityQuery(typeof(Data.Event.MapSettingsConverted)));
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        protected override void OnUpdate()
        {
            Entity _mapSettings = GetSingletonEntity<Data.Tag.MapSettings>();
            int _mapSurface = math.mul(GetComponent<MapSett.MapSize>(_mapSettings).Value, GetComponent<MapSett.MapSize>(_mapSettings).Value);

            /*========================
             *Native Array declaration
             ========================*/
            _noiseMapNativeArray = new NativeArray<float>(_mapSurface, Allocator.TempJob);
            _octaveOffsetNativeArray = new NativeArray<float2>(GetComponent<MapSett.Octaves>(_mapSettings).Value, Allocator.TempJob);
            _minMaxHeightNativeArray = new NativeArray<float>(2, Allocator.TempJob); // 0:min; 1:max;

            /*========================
             * Random octaves Offset Job
             * return : OctOffsetArrayJob
             ========================*/
            MapJobs.NoiseRandomJob _noiseRandomJob = new MapJobs.NoiseRandomJob()
            {
                RandomJob = new Unity.Mathematics.Random((uint)GetComponent<MapSett.Seed>(_mapSettings).Value),
                OffsetJob = GetComponent<MapSett.Offset>(_mapSettings).Value,
                OctOffsetArrayJob = _octaveOffsetNativeArray,
            };
            JobHandle _noiseRandomJobHandle = _noiseRandomJob.Schedule(_octaveOffsetNativeArray.Length, 32);

            /*========================
             * Perlin Noise Job
             * return : NoiseMap
             ========================*/
            MapJobs.NoiseHeightMapJob _noiseHeightMapJob = new MapJobs.NoiseHeightMapJob()
            {
                MapSizeJob = GetComponent<MapSett.MapSize>(_mapSettings).Value,
                ScaleJob = GetComponent<MapSett.Scale>(_mapSettings).Value,
                OctavesJob = GetComponent<MapSett.Octaves>(_mapSettings).Value,
                PersistanceJob = GetComponent<MapSett.Persistance>(_mapSettings).Value,
                LacunarityJob = GetComponent<MapSett.Lacunarity>(_mapSettings).Value,
                NoiseMap = _noiseMapNativeArray,
                OctOffsetArray = _octaveOffsetNativeArray,
                //MinMaxHeightJob = _minMaxHeightNativeArray,
            };
            JobHandle _noiseHeightMapJobHandle = _noiseHeightMapJob.Schedule(_noiseMapNativeArray.Length, 250, _noiseRandomJobHandle);
            _noiseHeightMapJobHandle.Complete();
            /*========================
             * Inverse Lerp Perlin Noise Job
             * return : inverse lerp NoiseMap
             ========================*/
            
            MapJobs.UnLerpNoiseHeightMapJob _unLerpNoiseHeightMapJob = new MapJobs.UnLerpNoiseHeightMapJob()
            {
                MapSizeJob = GetComponent<MapSett.MapSize>(_mapSettings).Value,
                NoiseMap = _noiseMapNativeArray,
                //MinMaxHeightJob = _minMaxHeightNativeArray,
                MinJob = _noiseMapNativeArray.Min(),
                MaxJob = _noiseMapNativeArray.Max(),
            };
            JobHandle _unLerpNoiseHeightMapJobHandle = _unLerpNoiseHeightMapJob.Schedule(_noiseMapNativeArray.Length, 250, _noiseHeightMapJobHandle);
            _unLerpNoiseHeightMapJobHandle.Complete(); // doc is lying : .Run() do not force previous job to complete
            
            /*========================
             * Pass Noise Map to DynamicBuffer
             * Process : NoiseMap stored
             * END Event : MapSettingsConverted
             * START Event : NoiseMapCalculated
             ========================*/
            Entities
                .WithName("NoiseMapToBuffer")
                .WithoutBurst()
                .WithStructuralChanges()
                .ForEach((Entity Map, ref DynamicBuffer<BufferHeightMap.HeightMap> noiseMap) =>
                {
                    noiseMap.Reinterpret<float>().CopyFrom(_noiseMapNativeArray);
                    noiseMap.Reinterpret<BufferHeightMap.HeightMap>();
                    _em.RemoveComponent<Data.Event.MapSettingsConverted>(GetSingletonEntity<Data.Event.MapSettingsConverted>());
                    _em.AddComponent<Data.Event.NoiseMapCalculated>(GetSingletonEntity<Data.Tag.MapEventHolder>());
                }).Run();
            
            _octaveOffsetNativeArray.Dispose();
            _noiseMapNativeArray.Dispose();
            _minMaxHeightNativeArray.Dispose();
        }
        protected override void OnDestroy()
        {
            if (_noiseMapNativeArray.IsCreated) _noiseMapNativeArray.Dispose();
            if (_octaveOffsetNativeArray.IsCreated) _octaveOffsetNativeArray.Dispose();
            if (_minMaxHeightNativeArray.IsCreated) _minMaxHeightNativeArray.Dispose();
        }
    }
}
