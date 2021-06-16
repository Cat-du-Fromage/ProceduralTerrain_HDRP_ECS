using System;
using System.Linq;
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

            #region ColorMap Compute Shader
            ComputeShader _colorMapComputeShader = AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/ECSScript/ProceduralGeneration/ComputeShader/ColorMapComputeShader.compute");
            Texture2D texture2D = new Texture2D(GetComponent<MapSett.MapSize>(_mapSettings).Value, GetComponent<MapSett.MapSize>(_mapSettings).Value);
            RenderTexture _renderTexture = new RenderTexture(GetComponent<MapSett.MapSize>(_mapSettings).Value, GetComponent<MapSett.MapSize>(_mapSettings).Value, 16);
            float[] _heightMapArray = GetBuffer<HeightMap>(_mapSettings).AsNativeArray().Reinterpret<float>().ToArray(); //mmmh this seems bad, need some check

            //Potential Refactor needed for Regions Data
            //Yet we can't have seperate array from a dynamic buffer with more than one data
            float4[] _regionsColorArray = new float4[GetBuffer<Regions>(_mapSettings).Length];
            float[] _regionsHeightArray = new float[GetBuffer<Regions>(_mapSettings).Length];
            Debug.Log($"buffer region length = {GetBuffer<Regions>(_mapSettings).Length}");
            for (int i = 0; i < GetBuffer<Regions>(_mapSettings).Length; i++)
            {
                _regionsColorArray[i] = GetBuffer<Regions>(_mapSettings)[i].Color.Value;
                _regionsHeightArray[i] = GetBuffer<Regions>(_mapSettings)[i].Height;
            }

            _renderTexture.enableRandomWrite = true;
            _renderTexture.Create();
            _colorMapComputeShader.SetTexture(0, "_mapTextureCSH", _renderTexture);
            _colorMapComputeShader.SetInt("_mapSizeCSH", GetComponent<MapSett.MapSize>(_mapSettings).Value);
            _colorMapComputeShader.SetFloat("_heightMapLength", _mapSurface);
            //_regionHeightBuffer To ComputeBuffer
            ComputeBuffer _regionsHeightBuffer = new ComputeBuffer(_regionsHeightArray.Length, sizeof(float));
            CSSetBuffer(_colorMapComputeShader, 0, "_regionsHeightArrCSH", _regionsHeightArray, _regionsHeightBuffer);

            //_regionColorBuffer To ComputeBuffer
            ComputeBuffer _regionsColorBuffer = new ComputeBuffer(_regionsColorArray.Length, sizeof(float) * 4);
            CSSetBuffer(_colorMapComputeShader, 0, "_regionsColorArrCSH", _regionsColorArray, _regionsColorBuffer);

            //HeightMapArray To ComputeBuffer
            ComputeBuffer _heightMapBuffer = new ComputeBuffer(GetBuffer<HeightMap>(_mapSettings).Length, sizeof(float));
            CSSetBuffer(_colorMapComputeShader, 0, "_heightMapArrCSH", _heightMapArray, _heightMapBuffer);

            float _numThreadsGPU = 32f;
            int _threadGroups = (int)math.ceil(GetComponent<MapSett.MapSize>(_mapSettings).Value / _numThreadsGPU);
            _colorMapComputeShader.Dispatch(0, _threadGroups, _threadGroups,1);

            _heightMapBuffer.Release();
            _regionsColorBuffer.Release();
            _regionsHeightBuffer.Release();
            #endregion ColorMap Compute Shader

            //for test
            #region TEST

            //Texture2D texture2D = new Texture2D(GetComponent<MapSett.MapSize>(_mapSettings).Value, GetComponent<MapSett.MapSize>(_mapSettings).Value);
            texture2D.filterMode = FilterMode.Point;
            texture2D.wrapMode = TextureWrapMode.Clamp;
            //texture2D.SetPixels(_colorMapNativeArray.Reinterpret<Color>().ToArray());
            RenderTexture.active = _renderTexture;
            texture2D.ReadPixels(new Rect(0, 0, _renderTexture.width, _renderTexture.height), 0, 0);
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

        private void CSSetBuffer(ComputeShader computeShader, int kernel, string CSdata, Array array, ComputeBuffer computeBuffer)
        {
            computeBuffer.SetData(array);
            computeShader.SetBuffer(kernel, CSdata, computeBuffer);
        }
    }
}
