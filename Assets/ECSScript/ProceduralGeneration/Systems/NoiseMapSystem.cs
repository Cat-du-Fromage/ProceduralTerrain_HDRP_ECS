using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using MapJobs = KaizerwaldCode.ProceduralGeneration.Jobs;
using MapSett = KaizerwaldCode.ProceduralGeneration.Data.PerlinNoise;
using BufferHeightMap = KaizerwaldCode.ProceduralGeneration.Data.DynamicBuffer.HeightMap;
using UnityEngine;

namespace KaizerwaldCode.ProceduralGeneration.System
{
    public class NoiseMapSystem : SystemBase
    {
        NativeArray<float> _noiseMapNativeArray;
        NativeArray<float2> _octaveOffsetNativeArray;
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
            Debug.Log(_mapSurface);
            _noiseMapNativeArray = new NativeArray<float>(_mapSurface, Allocator.TempJob);
            _octaveOffsetNativeArray = new NativeArray<float2>(GetComponent<MapSett.Octaves>(_mapSettings).Value, Allocator.TempJob);
            /*
             * Perlin Noise Job
             * return : HeightMap
             */
            MapJobs.NoiseRandomJob _noiseRandomJob = new MapJobs.NoiseRandomJob()
            {
                RandomJob = new Unity.Mathematics.Random((uint)GetComponent<MapSett.Seed>(_mapSettings).Value),
                OffsetJob = GetComponent<MapSett.Offset>(_mapSettings).Value,
                OctOffsetArrayJob = _octaveOffsetNativeArray,
            };
            JobHandle _noiseRandomJobHandle = _noiseRandomJob.Schedule(_octaveOffsetNativeArray.Length, 2, this.Dependency);
            _noiseRandomJobHandle.Complete();

            MapJobs.NoiseHeightMapJob _noiseHeightMapJob = new MapJobs.NoiseHeightMapJob()
            {
                MapSizeJob = GetComponent<MapSett.MapSize>(_mapSettings).Value,
                ScaleJob = GetComponent<MapSett.Scale>(_mapSettings).Value,
                OctavesJob = GetComponent<MapSett.Octaves>(_mapSettings).Value,
                PersistanceJob = GetComponent<MapSett.Persistance>(_mapSettings).Value,
                LacunarityJob = GetComponent<MapSett.Lacunarity>(_mapSettings).Value,
                NoiseMap = _noiseMapNativeArray,
                OctOffsetArray = _octaveOffsetNativeArray,
            };
            JobHandle _noiseHeightMapJobHandle = _noiseHeightMapJob.Schedule(_noiseMapNativeArray.Length, 64, _noiseRandomJobHandle);
            _noiseHeightMapJobHandle.Complete();
            /*
            MapJobs.UnLerpNoiseHeightMapJob _unLerpNoiseHeightMapJob = new MapJobs.UnLerpNoiseHeightMapJob()
            {
                MapSizeJob = GetComponent<MapSett.MapSize>(_mapSettings).Value,
                NoiseMap = _noiseMapNativeArray,
                MaxNoiseHeightJob = float.MaxValue,
                MinNoiseHeightJob = float.MinValue,
            };
            JobHandle _unLerpNoiseHeightMapJobHandle = _unLerpNoiseHeightMapJob.Schedule(_noiseMapNativeArray.Length, 64, _noiseHeightMapJobHandle);
            _unLerpNoiseHeightMapJobHandle.Complete();
            */
            /*
            MapJobs.PerlinNoiseJob NoiseMapHandle = new MapJobs.PerlinNoiseJob()
            {
                mapSizeJob = GetComponent<MapSett.MapSize>(MapSettings).Value,
                seedJob = GetComponent<MapSett.Seed>(MapSettings).Value,
                scaleJob = GetComponent<MapSett.Scale>(MapSettings).Value,
                octavesJob = GetComponent<MapSett.Octaves>(MapSettings).Value,
                persistanceJob = GetComponent<MapSett.Persistance>(MapSettings).Value,
                lacunarityJob = GetComponent<MapSett.Lacunarity>(MapSettings).Value,
                offsetJob = GetComponent<MapSett.Offset>(MapSettings).Value,
                noiseMap = _noiseMapNativeArray,
                octOffsetArray = _octaveOffsetNativeArray,
            };
            JobHandle jobHandle = NoiseMapHandle.Schedule(this.Dependency);
            jobHandle.Complete();
            */

            /*
             * Add each element of the NativeArray HeightMap to a solid DynamicBuffer
             */
            Entities
                .WithName("NoiseMapToBuffer")
                .WithoutBurst()
                .WithStructuralChanges()
                .ForEach((Entity Map, ref DynamicBuffer<BufferHeightMap.NoiseMap> noiseMap) =>
                {
                    for (int i = 0; i < _noiseMapNativeArray.Length; i++)
                    {
                        BufferHeightMap.NoiseMap height = new BufferHeightMap.NoiseMap();
                        height.Value = _noiseMapNativeArray[i];
                        noiseMap.Add(height);
                    }
                    _em.RemoveComponent<Data.Event.MapSettingsConverted>(GetSingletonEntity<Data.Event.MapSettingsConverted>());
                    _em.AddComponent<Data.Event.NoiseMapCalculated>(GetSingletonEntity<Data.Tag.MapEventHolder>());
                }).Run();

            _octaveOffsetNativeArray.Dispose();
            _noiseMapNativeArray.Dispose();
        }

        protected override void OnDestroy()
        {
            if (_noiseMapNativeArray.IsCreated) _noiseMapNativeArray.Dispose();
            if (_octaveOffsetNativeArray.IsCreated) _octaveOffsetNativeArray.Dispose();
        }
    }
}
