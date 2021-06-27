using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using MapSett = KaizerwaldCode.ProceduralGeneration.Data.PerlinNoise;
using KaizerwaldCode.ProceduralGeneration.Data.DynamicBuffer;
using KaizerwaldCode.Utils;
using System.Threading.Tasks;
using KaizerwaldCode.ProceduralGeneration.Jobs;
using Unity.Rendering;
using UnityEngine.Rendering;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;

namespace KaizerwaldCode.ProceduralGeneration.System
{
    /*
    public class MeshSystem2 : SystemBase
    {
        EntityManager _em;

        protected override void OnCreate()
        {
            RequireForUpdate(GetEntityQuery(typeof(Data.Event.ChunksEntityCreated)));
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        protected override void OnUpdate()
        {
            Entity _mapSettings = GetSingletonEntity<Data.Tag.MapSettings>();
            int _mapSurface = math.mul(GetComponent<MapSett.MapSize>(_mapSettings).Value, GetComponent<MapSett.MapSize>(_mapSettings).Value);
            //NativeArray Creation
            NativeArray<float> _heightMapNativeArray = GetBuffer<HeightMap>(_mapSettings).Reinterpret<float>().ToNativeArray(Allocator.TempJob);
            //to fill
            NativeArray<float3> _verticesNativeArray = new NativeArray<float3>(_mapSurface, Allocator.TempJob);
            NativeArray<float2> _uvsNativeArray = new NativeArray<float2>(_mapSurface, Allocator.TempJob);
            NativeArray<int> _trianglesNativeArray = new NativeArray<int>(_mapSurface * 6, Allocator.TempJob);

            MeshMapJob meshMapJob = new MeshMapJob()
            {
                MapSizeJob = GetComponent<MapSett.MapSize>(_mapSettings).Value,
                TopLeftXJob = (GetComponent<MapSett.MapSize>(_mapSettings).Value - 1)/-2f,
                TopLeftZJob = (GetComponent<MapSett.MapSize>(_mapSettings).Value - 1) / 2f,
                HeightMulJob = GetComponent<MapSett.HeightMultiplier>(_mapSettings).Value,
                HeightMapJob = _heightMapNativeArray,
                VerticesJob = _verticesNativeArray,
                UvsJob = _uvsNativeArray,
                TrianglesJob = _trianglesNativeArray,
            };
            JobHandle _meshMapJobHandle = meshMapJob.Schedule(_mapSurface, JobsUtility.JobWorkerCount - 1);
            _meshMapJobHandle.Complete();
            _heightMapNativeArray.Dispose();

            #region TEST

            Mesh meshMap = new Mesh();
            meshMap.name = "planePROC";
            meshMap.vertices = _verticesNativeArray.Reinterpret<Vector3>().ToArray();
            meshMap.uv = _uvsNativeArray.Reinterpret<Vector2>().ToArray();
            meshMap.triangles = _trianglesNativeArray.ToArray();
            meshMap.RecalculateNormals();
            RenderMesh RenderTest = new RenderMesh() { mesh = meshMap, material = _em.GetSharedComponentData<RenderMesh>(GetSingletonEntity<Data.Authoring.TerrainAuthoring>()).material };
            //_em.AddSharedComponentData(GetSingletonEntity<Data.Tag.ChunksHolder>(), RenderTest);
            _em.SetSharedComponentData(GetSingletonEntity<Data.Authoring.TerrainAuthoring>(), RenderTest);
            #endregion TEST

            _verticesNativeArray.Dispose();
            _uvsNativeArray.Dispose();
            _trianglesNativeArray.Dispose();
            _em.RemoveComponent<Data.Event.ChunksEntityCreated>(GetSingletonEntity<Data.Tag.MapEventHolder>());
        }

    }
    */
}
