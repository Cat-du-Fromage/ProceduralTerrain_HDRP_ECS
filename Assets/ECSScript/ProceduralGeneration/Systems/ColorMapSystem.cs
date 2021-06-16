using KaizerwaldCode.ProceduralGeneration.Data.DynamicBuffer;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
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

            #region ColorMap Compute Shader
            ComputeShader _colorMapComputeShader = AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/ECSScript/ProceduralGeneration/ComputeShader/ColorMapComputeShader.compute");
            Texture2D texture2D = new Texture2D(GetComponent<MapSett.MapSize>(_mapSettings).Value, GetComponent<MapSett.MapSize>(_mapSettings).Value);
            float[] _heightMapArray = GetBuffer<HeightMap>(_mapSettings).AsNativeArray().Reinterpret<float>().ToArray(); //mmmh this seems bad, need some check

            //OffsetArray To ComputeBuffer
            ComputeBuffer _colorsBuffer = new ComputeBuffer(_colorMapNativeArray.Length, sizeof(float) * 4);
            _colorsBuffer.SetData(_colorMapNativeArray);
            _colorMapComputeShader.SetBuffer(0, "_mapTextureCSH", _colorsBuffer);

            //HeightMapArray To ComputeBuffer
            ComputeBuffer _heightMapBuffer = new ComputeBuffer(GetBuffer<HeightMap>(_mapSettings).Length, sizeof(float));
            _heightMapBuffer.SetData(_heightMapArray);
            _colorMapComputeShader.SetBuffer(0, "_heightMapArrCSH", _heightMapBuffer);

            _colorsBuffer.Release();
            _heightMapBuffer.Release();
            #endregion ColorMap Compute Shader
                        //for test
            #region TEST

            //Texture2D texture2D = new Texture2D(GetComponent<MapSett.MapSize>(_mapSettings).Value, GetComponent<MapSett.MapSize>(_mapSettings).Value);
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

            //NEW MESH API
            // Vertex buffer
            /*
            Mesh mesh = new Mesh();
            var vertexCount = 40;
            var bulletCount = 10;

            var iarray = new NativeArray<uint>(vertexCount, Allocator.TempJob,
                NativeArrayOptions.UninitializedMemory);
            var varray = new NativeArray<float4>(bulletCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < varray.Length; i++)
            {
                varray[i]= new float4(i, i+3, i+2, i+1);
            }

            for (int i = 0; i < iarray.Length; i++)
            {
                iarray[i] = (uint)i;
            }

            mesh.SetVertexBufferParams
            (vertexCount,
                new VertexAttributeDescriptor(VertexAttribute.Position,
                    VertexAttributeFormat.Float32, 3),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord0,
                    VertexAttributeFormat.Float32, 3));
            mesh.SetVertexBufferData(varray, 0, 0, bulletCount);
            // Index buffer
            mesh.SetIndexBufferParams(vertexCount, IndexFormat.UInt32);
            mesh.SetIndexBufferData(iarray, 0, 0, vertexCount);

            // Submesh definition
            var meshDesc = new SubMeshDescriptor(0, vertexCount, MeshTopology.Quads);
            mesh.SetSubMesh(0, meshDesc, MeshUpdateFlags.DontRecalculateBounds);

            varray.Dispose();
            iarray.Dispose();
            */
            #endregion TEST
            _colorMapNativeArray.Dispose();
            #region EVENT
            _em.RemoveComponent<Data.Event.NoiseMapCalculated>(GetSingletonEntity<Data.Tag.MapEventHolder>());
            _em.AddComponent<Data.Event.ColorMapCalculated>(GetSingletonEntity<Data.Tag.MapEventHolder>());
            #endregion EVENT
        }

        protected override void OnDestroy()
        {
            if (_colorMapNativeArray.IsCreated) _colorMapNativeArray.Dispose();
        }
    }
}
