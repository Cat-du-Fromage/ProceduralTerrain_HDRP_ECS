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

            //HeightMap
            float[] _heightMapArray = GetBuffer<HeightMap>(_mapSettings).AsNativeArray().Reinterpret<float>().ToArray(); //mmmh this seems bad, need some check
            ComputeBuffer _heightMapBuffer = new ComputeBuffer(_mapSurface, sizeof(float));
            UtComputeShader.CSHSetBuffer(_meshMapComputeShader, 0, "noiseMapCSH", _heightMapBuffer, _heightMapArray);
            
            //Vertices Mesh
            float3[] _verticesArray = new float3[_mapSurface];
            ComputeBuffer _verticesPositionBuffer = new ComputeBuffer(_mapSurface, sizeof(float) * 3);
            UtComputeShader.CSHSetBuffer(_meshMapComputeShader, 0, "verticesPositionCSH", _verticesPositionBuffer, _verticesArray);
            
            //Vertices Mesh
            float2[] _uvsArray = new float2[_mapSurface];
            ComputeBuffer _uvsBuffer = new ComputeBuffer(_mapSurface, sizeof(float) * 2);
            UtComputeShader.CSHSetBuffer(_meshMapComputeShader, 0, "uvsCSH", _uvsBuffer, _uvsArray);
            
            //Vertices Mesh
            int[] _trianglesArray = new int[_mapSurface*6];
            ComputeBuffer _trianglesBuffer = new ComputeBuffer(_mapSurface*6, sizeof(int));
            UtComputeShader.CSHSetBuffer(_meshMapComputeShader, 0, "trianglesCSH", _trianglesBuffer, _trianglesArray);

            (Vector3[], Vector2[], int[]) _requestAsyncGPUMeshData = await AsyncGPUMeshData(_meshMapComputeShader, 0, _threadGroups, _verticesPositionBuffer, _uvsBuffer, _trianglesBuffer);
            //_verticesArray = _requestAsyncGPUMeshData.Item1;
            //_uvsArray = _requestAsyncGPUMeshData.Item2;
            //_trianglesArray = _requestAsyncGPUMeshData.Item3;
            //TEST
            Mesh mesh = new Mesh();
            mesh.name = "planePROC";
            //mesh.SetVertices(_requestAsyncGPUMeshData.Item1);;
            //mesh.SetUVs(0, _requestAsyncGPUMeshData.Item2);;
            //mesh.SetIndices(_requestAsyncGPUMeshData.Item3, MeshTopology.Triangles,0,true);
            mesh.vertices = _requestAsyncGPUMeshData.Item1;
            mesh.uv = _requestAsyncGPUMeshData.Item2;
            mesh.triangles = _requestAsyncGPUMeshData.Item3;
            mesh.RecalculateNormals();
            //mesh.Optimize();

            #region TEST
            /*
            //var dataArray = Mesh.AllocateWritableMeshData(1);
            //var data = dataArray[0];

            var layout = new[]
            {
                new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 2),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2)
            };

            mesh.SetVertexBufferParams(_verticesArray.Length, layout);
            //1 is for ONE mesh
            mesh.SetVertexBufferData(_verticesArray, 0,0, 1);

            mesh.SetIndexBufferParams(_verticesArray.Length, IndexFormat.UInt32);
            mesh.SetIndexBufferData(_trianglesArray, 0,0, _verticesArray.Length);
            */
            var mat = _em.GetSharedComponentData<RenderMesh>(GetSingletonEntity<Data.Authoring.TerrainAuthoring>()).material;

            var desc = new RenderMeshDescription(
                mesh,
                mat);
            RenderMeshUtility.AddComponents(
                GetSingletonEntity<Data.Authoring.TerrainAuthoring>(),
                _em,
                desc);
            //_em.SetSharedComponentData(GetSingletonEntity<Data.Authoring.TerrainAuthoring>(), new RenderMesh() { mesh = mesh, material = mat });
            //_em.AddSharedComponentData(GetSingletonEntity<Data.Tag.ChunksHolder>(), new RenderMesh() {mesh = mesh, material = mat });
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
            //for test only
            _em.GetBuffer<Data.Chunks.MeshBuffer.Vertices>(GetSingletonEntity<Data.Tag.ChunksHolder>()).Reinterpret<float3>().CopyFrom(_verticesNativeArray);
            _em.GetBuffer<Data.Chunks.MeshBuffer.Uvs>(GetSingletonEntity<Data.Tag.ChunksHolder>()).Reinterpret<float2>().CopyFrom(_uvsNativeArray);
            _em.GetBuffer<Data.Chunks.MeshBuffer.Triangles>(GetSingletonEntity<Data.Tag.ChunksHolder>()).Reinterpret<int>().CopyFrom(_trianglesNativeArray);

            return (_verticesNativeArray.Reinterpret<Vector3>().ToArray(), _uvsNativeArray.Reinterpret<Vector2>().ToArray(), _trianglesNativeArray.ToArray());
        }
    }
}
