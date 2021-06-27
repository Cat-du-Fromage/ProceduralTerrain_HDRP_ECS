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
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public class ChunksEntitySystem : SystemBase
    {
        EntityQuery _event;
        EntityQueryDesc _eventDescription;
        EntityManager _em;
        NativeArray<Entity> _numChunksNativeArray;
        EndInitializationEntityCommandBufferSystem ecbEI;
        protected override void OnCreate()
        {
            _eventDescription = new EntityQueryDesc()
            {
                All = new ComponentType[] { typeof(Data.Event.CreationChunksEntityEvent) },
            };
            _event = GetEntityQuery(_eventDescription);
            RequireForUpdate(_event);
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            ecbEI = World.GetExistingSystem<EndInitializationEntityCommandBufferSystem>();
        }

        protected override void OnStartRunning()
        {
            #region Chunks Creation
            Entity _mapSetting = GetSingletonEntity<Data.Tag.MapSettings>();

            int _numChunk = math.mul(
                GetComponent<Data.PerlinNoise.NumChunk>(_mapSetting).Value,
                GetComponent<Data.PerlinNoise.NumChunk>(_mapSetting).Value
            );

            EntityArchetype _chunkArchetype = _em.CreateArchetype
            (
                typeof(Data.Tag.Chunk),
                typeof(RenderMesh),
                typeof(LocalToWorld),
                typeof(RenderBounds),
                typeof(ChunkData.MeshBuffer.Vertices),
                typeof(ChunkData.MeshBuffer.Triangles),
                typeof(ChunkData.MeshBuffer.Uvs),
                typeof(ChunkData.GridPosition),
                typeof(DisableRendering),
                typeof(Parent)
            );

            _numChunksNativeArray = new NativeArray<Entity>(_numChunk, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            _em.CreateEntity(_chunkArchetype, _numChunksNativeArray);
            _numChunksNativeArray.Dispose();
            #endregion Chunks Creation

            //Fill chunks holder's Dynamic Buffer with all chunks created
            Entity _chunkHolder = GetSingletonEntity<Data.Tag.ChunksHolder>();
            //Buffer : Resize
            DynamicBuffer<LinkedEntityGroup> _chunksBuffer = _em.GetBuffer<LinkedEntityGroup>(_chunkHolder);
            _chunksBuffer.ResizeUninitialized(_numChunk);
            //Buffer : Fill
            NativeArray<Entity> _chunksNativeArray = GetEntityQuery(typeof(Data.Tag.Chunk)).ToEntityArray(Allocator.TempJob);
            _chunksBuffer.Reinterpret<Entity>().CopyFrom(_chunksNativeArray);
            _chunksNativeArray.Dispose();
        }

        protected override void OnUpdate()
        {
            //Assign Chunk Holder as Parent of each chunk (Maybe not useful, but if needed it's available)
            EntityCommandBuffer ecb = ecbEI.CreateCommandBuffer();
            Entities
                .WithoutBurst()
                .WithAll<Data.Tag.Chunk>()
                .ForEach((Entity chunk) =>
                {
                    ecb.SetComponent(chunk, new Parent() { Value = GetSingletonEntity<Data.Tag.ChunksHolder>() });
                }).Run();
            ecbEI.AddJobHandleForProducer(this.Dependency);

            #region EVENT
            _em.RemoveComponent<Data.Event.CreationChunksEntityEvent>(GetSingletonEntity<Data.Tag.MapEventHolder>());
            #endregion EVENT
        }

        protected override void OnDestroy()
        {
            if (_numChunksNativeArray.IsCreated) _numChunksNativeArray.Dispose();
        }
    }
}
