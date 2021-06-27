using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;
using MapSett = KaizerwaldCode.ProceduralGeneration.Data.PerlinNoise;
using KaizerwaldCode.ProceduralGeneration.Data.DynamicBuffer;
using KaizerwaldCode.Utils;
using System.Threading.Tasks;
using KaizerwaldCode.ProceduralGeneration.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Rendering;
using UnityEngine.Rendering;

namespace KaizerwaldCode.ProceduralGeneration.System
{
    public class MeshSystem : SystemBase
    {
        EntityManager _em;
        protected override void OnCreate()
        {
            RequireForUpdate(GetEntityQuery(typeof(Data.Event.ChunksEntityCreated)));
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        protected async override void OnStartRunning()
        {
            //CAREFUL !! NEVER SIZE > 241!!!!!
            Entity _mapSettings = GetSingletonEntity<Data.Tag.MapSettings>();
            int _mapSurface = math.mul(GetComponent<MapSett.MapSize>(_mapSettings).Value, GetComponent<MapSett.MapSize>(_mapSettings).Value);



            ComputeShader _meshMapComputeShader = AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/ECSScript/ProceduralGeneration/ComputeShader/MeshMapComputeShader.compute");
            float _numThreadsGPU = 32f;
            int _threadGroups = (int)math.ceil(GetComponent<MapSett.MapSize>(_mapSettings).Value / _numThreadsGPU);

            //Set Fields in Compute Shader
            _meshMapComputeShader.SetInt("mapSizeCSH", GetComponent<MapSett.MapSize>(_mapSettings).Value);

            _meshMapComputeShader.SetFloat("topLeftXCSH", (GetComponent<MapSett.MapSize>(_mapSettings).Value - 1) / -2f);
            _meshMapComputeShader.SetFloat("topLeftZCSH", (GetComponent<MapSett.MapSize>(_mapSettings).Value - 1) / 2f);
            _meshMapComputeShader.SetFloat("heightMulCSH", GetComponent<MapSett.HeightMultiplier>(_mapSettings).Value);

            NativeArray<float> _curvedNativeArray = new NativeArray<float>(_mapSurface, Allocator.TempJob);
            MeshCurvedMapJob meshCurvedMapJob = new MeshCurvedMapJob()
            {
                AnimCurveJob = GetComponent<MapSett.HeightCurve>(_mapSettings).Value,
                HeightMapJob = GetBuffer<HeightMap>(_mapSettings).Reinterpret<float>().ToNativeArray(Allocator.TempJob),
                CurvedHeightMapJob = _curvedNativeArray,
            };
            JobHandle _curveJobHandle = meshCurvedMapJob.Schedule(_mapSurface, JobsUtility.JobWorkerCount - 1);
            _curveJobHandle.Complete();

            float[] _curvedHeightMapArray = _curvedNativeArray.ToArray();
            _curvedNativeArray.Dispose();

            //HeightMap (WE actually use the curved map now)
            //float[] _heightMapArray = GetBuffer<HeightMap>(_mapSettings).AsNativeArray().Reinterpret<float>().ToArray(); //mmmh this seems bad, need some check
            ComputeBuffer _heightMapBuffer = new ComputeBuffer(_mapSurface, sizeof(float));
            UtComputeShader.CSHSetBuffer(_meshMapComputeShader, 0, "noiseMapCSH", _heightMapBuffer, _curvedHeightMapArray);

            //Vertices Mesh
            float3[] _verticesArray = new float3[_mapSurface];
            ComputeBuffer _verticesPositionBuffer = new ComputeBuffer(_mapSurface, sizeof(float) * 3);
            UtComputeShader.CSHSetBuffer(_meshMapComputeShader, 0, "verticesPositionCSH", _verticesPositionBuffer, _verticesArray);

            //Vertices Mesh
            float2[] _uvsArray = new float2[_mapSurface];
            ComputeBuffer _uvsBuffer = new ComputeBuffer(_mapSurface, sizeof(float) * 2);
            UtComputeShader.CSHSetBuffer(_meshMapComputeShader, 0, "uvsCSH", _uvsBuffer, _uvsArray);

            //Vertices Mesh
            int[] _trianglesArray = new int[_mapSurface * 6];
            ComputeBuffer _trianglesBuffer = new ComputeBuffer(_mapSurface * 6, sizeof(int));
            UtComputeShader.CSHSetBuffer(_meshMapComputeShader, 0, "trianglesCSH", _trianglesBuffer, _trianglesArray);

            (Vector3[], Vector2[], int[]) _requestAsyncGPUMeshData = await AsyncGPUMeshData(_meshMapComputeShader, 0, _threadGroups, _verticesPositionBuffer, _uvsBuffer, _trianglesBuffer);
            //_verticesArray = _requestAsyncGPUMeshData.Item1;
            //_uvsArray = _requestAsyncGPUMeshData.Item2;
            //_trianglesArray = _requestAsyncGPUMeshData.Item3;
            //TEST
            Mesh meshMade = new Mesh();
            meshMade.name = "planePROC";
            meshMade.vertices = _requestAsyncGPUMeshData.Item1;
            meshMade.uv = _requestAsyncGPUMeshData.Item2;
            meshMade.triangles = _requestAsyncGPUMeshData.Item3;
            meshMade.RecalculateNormals();
            meshMade.RecalculateBounds();
            meshMade.Optimize();
            Entity terrain = GetSingletonEntity<Data.Authoring.TerrainAuthoring>();
            SetComponent(terrain, new RenderBounds() {Value = meshMade.bounds.ToAABB()}); //Center is 0 otherwise

            #region TEST
            RenderMesh renderMesh = new RenderMesh()
            {
                material = _em.GetSharedComponentData<RenderMesh>(GetSingletonEntity<Data.Authoring.TerrainAuthoring>()).material,
                mesh = meshMade
            };
            _em.SetSharedComponentData(GetSingletonEntity<Data.Authoring.TerrainAuthoring>(), renderMesh);
            #endregion TEST

            //Release Buffer
            UtComputeShader.CSHReleaseBuffers(_heightMapBuffer, _verticesPositionBuffer, _uvsBuffer, _trianglesBuffer);

            _em.RemoveComponent<Data.Event.ChunksEntityCreated>(GetSingletonEntity<Data.Tag.MapEventHolder>());
        }

        protected override void OnUpdate()
        {
            /*
            // MapSize / Chunks Size
            int NumChunksRow = 0;
            // Chunks Size
            int ChunkSize = 0;
            float[] chunksHeight = new float[10];
            NativeArray<float> heightMap = new NativeArray<float>(10, Allocator.Persistent);
            Debug.Log("MeshSystem");
            for (int Ym = 0; Ym < NumChunksRow; Ym++)
            {
                for (int Xm = 0; Xm < NumChunksRow; Xm++)
                {
                    //=============================================================
                    //Do this in Compute Shader
                    int currentChunk = (Ym * NumChunksRow) + Xm;
                    for (int Yc = 0; Yc < ChunkSize; Yc++)
                    {
                        //since we only need start /end we don't need to go through X
                        int start = (Ym * ChunkSize) + Yc;

                        heightMap.GetSubArray(start, ChunkSize);
                        heightMap.ToArray().CopyTo(chunksHeight, currentChunk);
                        //Need a consistante way get chunks entity ORDERED for every operation
                        //maybe a parent that hold them?
                    }
                    //=============================================================
                }
            }
            heightMap.Dispose();
            */
        }

        private async Task<(Vector3[], Vector2[], int[])> AsyncGPUMeshData(ComputeShader computeShader, int kernel, int threadGroups, ComputeBuffer computeBufferVertices, ComputeBuffer computeBufferUvs, ComputeBuffer computeBufferTriangles)
        {
            computeShader.Dispatch(kernel, threadGroups, threadGroups, 1);
            AsyncGPUReadbackRequest _requestVertices = AsyncGPUReadback.Request(computeBufferVertices);
            AsyncGPUReadbackRequest _requestUvs = AsyncGPUReadback.Request(computeBufferUvs);
            AsyncGPUReadbackRequest _requestTriangles = AsyncGPUReadback.Request(computeBufferTriangles);
            while ((!_requestVertices.done && !_requestVertices.hasError) && (!_requestUvs.done && !_requestUvs.hasError) && (!_requestTriangles.done && !_requestTriangles.hasError))
            {
                await Task.Yield();
            }
            NativeArray<float3> _verticesNativeArray = _requestVertices.GetData<float3>();
            NativeArray<float2> _uvsNativeArray = _requestUvs.GetData<float2>();
            NativeArray<int> _trianglesNativeArray = _requestTriangles.GetData<int>();
            //Since Native Array are disposed it might be the easier solution to stopre buffers
            _em.GetBuffer<Data.Chunks.MeshBuffer.Vertices>(GetSingletonEntity<Data.Tag.ChunksHolder>()).Reinterpret<float3>().CopyFrom(_verticesNativeArray);
            _em.GetBuffer<Data.Chunks.MeshBuffer.Uvs>(GetSingletonEntity<Data.Tag.ChunksHolder>()).Reinterpret<float2>().CopyFrom(_uvsNativeArray);
            _em.GetBuffer<Data.Chunks.MeshBuffer.Triangles>(GetSingletonEntity<Data.Tag.ChunksHolder>()).Reinterpret<int>().CopyFrom(_trianglesNativeArray);

            return (_verticesNativeArray.Reinterpret<Vector3>().ToArray(), _uvsNativeArray.Reinterpret<Vector2>().ToArray(), _trianglesNativeArray.ToArray());
        }
    }
}
