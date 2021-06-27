using System;
using System.Linq;
using System.Threading.Tasks;
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
using KaizerwaldCode.Utils;

namespace KaizerwaldCode.ProceduralGeneration.System
{
    public class ColorMapSystem : SystemBase
    {
        EntityManager _em;
        protected override void OnCreate()
        {
            RequireForUpdate(GetEntityQuery(typeof(Data.Event.NoiseMapCalculated)));
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        protected async override void OnStartRunning()
        {
            Entity _mapSettings = GetSingletonEntity<Data.Tag.MapSettings>();
            int _mapSurface = math.mul(GetComponent<MapSett.MapSize>(_mapSettings).Value, GetComponent<MapSett.MapSize>(_mapSettings).Value);

            #region ColorMap Compute Shader
            ComputeShader _colorMapComputeShader = AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/ECSScript/ProceduralGeneration/ComputeShader/ColorMapComputeShader.compute");
            float _numThreadsGPU = 32f;
            int _threadGroups = (int)math.ceil(GetComponent<MapSett.MapSize>(_mapSettings).Value / _numThreadsGPU);

            float[] _heightMapArray = GetBuffer<HeightMap>(_mapSettings).AsNativeArray().Reinterpret<float>().ToArray(); //mmmh this seems bad, need some check
            //Potential Refactor needed for Regions Data
            //Yet we can't have seperate array from a dynamic buffer with more than one data
            float4[] _regionsColorArray = new float4[GetBuffer<Regions>(_mapSettings).Length];
            float[] _regionsHeightArray = new float[GetBuffer<Regions>(_mapSettings).Length];

            await Task.Run(() =>
            {
                for (int i = 0; i < GetBuffer<Regions>(_mapSettings).Length; i++)
                {
                    _regionsColorArray[i] = GetBuffer<Regions>(_mapSettings)[i].Color.Value;
                    _regionsHeightArray[i] = GetBuffer<Regions>(_mapSettings)[i].Height;
                }
            });

            RenderTexture _renderTexture = new RenderTexture(GetComponent<MapSett.MapSize>(_mapSettings).Value, GetComponent<MapSett.MapSize>(_mapSettings).Value, 16);
            _renderTexture.enableRandomWrite = true;
            _renderTexture.Create();

            _colorMapComputeShader.SetTexture(0, "mapTextureCSH", _renderTexture);
            _colorMapComputeShader.SetInt("mapSizeCSH", GetComponent<MapSett.MapSize>(_mapSettings).Value);
            _colorMapComputeShader.SetFloat("heightMapLength", _mapSurface);
            //_regionHeightBuffer To ComputeBuffer
            ComputeBuffer _regionsHeightBuffer = new ComputeBuffer(_regionsHeightArray.Length, sizeof(float));
            UtComputeShader.CSHSetBuffer(_colorMapComputeShader, 0, "regionsHeightArrCSH", _regionsHeightBuffer, _regionsHeightArray);

            //_regionColorBuffer To ComputeBuffer
            ComputeBuffer _regionsColorBuffer = new ComputeBuffer(_regionsColorArray.Length, sizeof(float) * 4);
            UtComputeShader.CSHSetBuffer(_colorMapComputeShader, 0, "regionsColorArrCSH", _regionsColorBuffer, _regionsColorArray);

            //HeightMapArray To ComputeBuffer
            ComputeBuffer _heightMapBuffer = new ComputeBuffer(GetBuffer<HeightMap>(_mapSettings).Length, sizeof(float));
            UtComputeShader.CSHSetBuffer(_colorMapComputeShader, 0, "heightMapArrCSH", _heightMapBuffer, _heightMapArray);

            //Get RenderTexture after GPU is done working
            _renderTexture = await AsyncRenderTextureGPU(_colorMapComputeShader, 0, _threadGroups, _regionsColorBuffer, _renderTexture);

            //releaseBuffers
            UtComputeShader.CSHReleaseBuffers(_heightMapBuffer, _regionsColorBuffer, _regionsHeightBuffer);
            #endregion ColorMap Compute Shader

            //for test : May need a refactor when Mesh construction completed
            #region TEST
            _renderTexture.wrapMode = TextureWrapMode.Clamp;
            _renderTexture.filterMode = FilterMode.Point;
            Material material = _em.GetSharedComponentData<RenderMesh>(GetSingletonEntity<Data.Authoring.TerrainAuthoring>()).material;
            material.mainTexture = _renderTexture;
            //Set the correct Scale to the Mesh
            /*
            NonUniformScale localToWorldScale = new NonUniformScale
            {
                Value = new float3(GetComponent<MapSett.MapSize>(_mapSettings).Value, GetComponent<MapSett.MapSize>(_mapSettings).Value, GetComponent<MapSett.MapSize>(_mapSettings).Value)
            };

            _em.AddComponentData(GetSingletonEntity<Data.Authoring.TerrainAuthoring>(), localToWorldScale);
            */
            #endregion TEST
            #region EVENT
            _em.RemoveComponent<Data.Event.NoiseMapCalculated>(GetSingletonEntity<Data.Tag.MapEventHolder>());
            _em.AddComponent<Data.Event.ColorMapCalculated>(GetSingletonEntity<Data.Tag.MapEventHolder>());
            #endregion EVENT
        }

        protected override void OnUpdate()
        {
            
        }

        private async Task<RenderTexture> AsyncRenderTextureGPU(ComputeShader computeShader, int kernel, int threadGroups, ComputeBuffer computeBufferRenderTexture, RenderTexture renderTexture)
        {
            computeShader.Dispatch(kernel, threadGroups, threadGroups, 1);
            AsyncGPUReadbackRequest _request = AsyncGPUReadback.Request(computeBufferRenderTexture);
            
            while (!_request.done && !_request.hasError)
            {
                await Task.Yield();
            }

            return renderTexture;
        }
    }
}
