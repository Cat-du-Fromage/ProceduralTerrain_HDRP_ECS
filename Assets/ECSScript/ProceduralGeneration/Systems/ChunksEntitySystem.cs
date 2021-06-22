using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using ChunkData = KaizerwaldCode.ProceduralGeneration.Data.Chunks;

namespace KaizerwaldCode.ProceduralGeneration.System
{
    public class ChunksEntitySystem : SystemBase
    {
        EntityManager _em;
        NativeArray<Entity> _numChunksNativeArray;
        protected override void OnCreate()
        {
            RequireForUpdate(GetEntityQuery(typeof(Data.Event.ColorMapCalculated)));
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        protected override void OnUpdate()
        {
            #region Chunks Creation
            Entity _mapSetting = GetSingletonEntity<Data.Tag.MapSettings>();
            int _mapSurface = math.mul(GetComponent<Data.PerlinNoise.MapSize>(_mapSetting).Value, GetComponent<Data.PerlinNoise.MapSize>(_mapSetting).Value);
            int _chunkSurface = math.mul(GetComponent<Data.PerlinNoise.ChunkSize>(_mapSetting).Value, GetComponent<Data.PerlinNoise.ChunkSize>(_mapSetting).Value);
            int _numChunk = _mapSurface / _chunkSurface;

            EntityArchetype _chunkArchetype = _em.CreateArchetype
            (
                typeof(Data.Tag.Chunk),
                //typeof(RenderMesh),
                typeof(LocalToWorld),
                //typeof(RenderBounds),
                typeof(ChunkData.MeshBuffer.Vertices),
                typeof(ChunkData.MeshBuffer.Triangles),
                typeof(ChunkData.MeshBuffer.Uvs),
                typeof(ChunkData.GridPosition),
                typeof(DisableRendering),
                typeof(Parent)
            );

            _numChunksNativeArray = new NativeArray<Entity>(_numChunk, Allocator.Persistent);
            _em.CreateEntity(_chunkArchetype, _numChunksNativeArray);
            _numChunksNativeArray.Dispose();
            #endregion Chunks Creation

            //Reference Each Chunk into a singelton Entity (ChunksHolder)
            Entities
                .WithoutBurst()
                .WithAll<Data.Tag.Chunk>()
                .ForEach((Entity chunk, ref Parent parent) =>
                {
                    SetComponent(chunk, new Parent(){Value = GetSingletonEntity<Data.Tag.ChunksHolder>()});
                    DynamicBuffer<LinkedEntityGroup> _chunksBuffer = _em.GetBuffer<LinkedEntityGroup>(GetSingletonEntity<Data.Tag.ChunksHolder>());
                    _chunksBuffer.Add(chunk);
                }).Run();

            #region EVENT
            _em.RemoveComponent<Data.Event.ColorMapCalculated>(GetSingletonEntity<Data.Tag.MapEventHolder>());
            _em.AddComponent<Data.Event.ChunksEntityCreated>(GetSingletonEntity<Data.Tag.MapEventHolder>());
            #endregion EVENT
        }

        protected override void OnDestroy()
        {
            if (_numChunksNativeArray.IsCreated) _numChunksNativeArray.Dispose();
        }
    }
}
