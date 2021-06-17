using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using System.Threading.Tasks;

using MapJobs = KaizerwaldCode.ProceduralGeneration.Jobs;
using MapSett = KaizerwaldCode.ProceduralGeneration.Data.PerlinNoise;
using BufferHeightMap = KaizerwaldCode.ProceduralGeneration.Data.DynamicBuffer;
using System.Collections.Generic;

namespace KaizerwaldCode.ProceduralGeneration.System
{
    public class NoiseMapSystemTest : SystemBase
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

            /*========================
             *Native Array declaration
             ========================*/
            _noiseMapNativeArray = new NativeArray<float>(_mapSurface, Allocator.TempJob);
            _octaveOffsetNativeArray = new NativeArray<float2>(GetComponent<MapSett.Octaves>(_mapSettings).Value, Allocator.Persistent);
            /*========================
             * Random octaves Offset Job
             * return : OctOffsetArrayJob
             ========================*/
            //~10 Iteration MAX NO NEED FOR COMPUTE SHADER
            MapJobs.NoiseRandomJob _noiseRandomJob = new MapJobs.NoiseRandomJob()
            {
                RandomJob = new Unity.Mathematics.Random((uint)GetComponent<MapSett.Seed>(_mapSettings).Value),
                OffsetJob = GetComponent<MapSett.Offset>(_mapSettings).Value,
                OctOffsetArrayJob = _octaveOffsetNativeArray,
            };
            JobHandle _noiseRandomJobHandle = _noiseRandomJob.Schedule(_octaveOffsetNativeArray.Length, 32);
            //needed for compute shader => can't use dependency in this case
            _noiseRandomJobHandle.Complete(); 
//==========================================================================================================================================================

            #region Noise Map
            /*========================
             * Perlin Noise Compute Shader
             * return : NoiseMap
             ========================*/
            ComputeShader _heightMapComputeShader = AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/ECSScript/ProceduralGeneration/ComputeShader/HeightMapComputeShader.compute");
            int _heightMapKernel = _heightMapComputeShader.FindKernel("CSHeightMap");
            int _heightMapInverseKernel = _heightMapComputeShader.FindKernel("CSHeightMapInverseLerp");
            float _numThreadsGPU = 32f;
            int _threadGroups = (int)math.ceil(GetComponent<MapSett.MapSize>(_mapSettings).Value / _numThreadsGPU);
            //AsyncGPUReadback Dispose Native array on Call and buffer.GetData don't accept NativeArray...
            //So we have to construct array to retrieve our data
            float[] _heightMapArr = new float[_mapSurface];
            int[] _minMaxHeight = new int[2];
            int _floatToIntMultiplier = 1000; // needed for interlock function that can only take int (so we make float*1000)

            //OffsetArray To ComputeBuffer
            ComputeBuffer _offsetsBuffer = new ComputeBuffer(_octaveOffsetNativeArray.Length, sizeof(float) * 2);
            _offsetsBuffer.SetData(_octaveOffsetNativeArray);
            _heightMapComputeShader.SetBuffer(_heightMapKernel, "_offsetsArrCSH", _offsetsBuffer);

            //HeightMapArray To ComputeBuffer
            ComputeBuffer _heightMapBuffer = new ComputeBuffer(_noiseMapNativeArray.Length, sizeof(float));
            _heightMapBuffer.SetData(_noiseMapNativeArray);
            _heightMapComputeShader.SetBuffer(_heightMapKernel, "_heightMapsArrCSH", _heightMapBuffer);

            //MinMaxArray To ComputeBuffer
            ComputeBuffer _minMaxBuffer = new ComputeBuffer(_minMaxHeight.Length, sizeof(int));
            _minMaxBuffer.SetData(_minMaxHeight);
            _heightMapComputeShader.SetBuffer(_heightMapKernel, "_minMaxArrCSH", _minMaxBuffer);

            //Set Parameters in ComputeShader
            _heightMapComputeShader.SetInt("_floatToIntMultiplierCSH", _floatToIntMultiplier);
            _heightMapComputeShader.SetInt("_mapSizeCSH", GetComponent<MapSett.MapSize>(_mapSettings).Value);
            _heightMapComputeShader.SetInt("_octavesCSH", GetComponent<MapSett.Octaves>(_mapSettings).Value);
            _heightMapComputeShader.SetFloat("_lacunarityCSH", GetComponent<MapSett.Lacunarity>(_mapSettings).Value);
            _heightMapComputeShader.SetFloat("_persistenceCSH", GetComponent<MapSett.Persistance>(_mapSettings).Value);
            _heightMapComputeShader.SetFloat("_scaleCSH", GetComponent<MapSett.Scale>(_mapSettings).Value);

            //Dispatch ThreadGroup
            Debug.Log($"thread dispatch {_threadGroups}");
            _heightMapComputeShader.Dispatch(_heightMapKernel, _threadGroups, _threadGroups, 1);

            _heightMapBuffer.GetData(_heightMapArr);
            _minMaxBuffer.GetData(_minMaxHeight);

            _heightMapBuffer.Release();
            _offsetsBuffer.Release();
            _minMaxBuffer.Release();

            #endregion Noise Map

            #region Inverse Lerp Noise Map

            /*========================
             * Inverse Lerp Perlin Noise Compute Shader
             * return : inverse lerp NoiseMap
             ========================*/

            //HeightMapArray To ComputeBuffer
            ComputeBuffer _heightMapInverseBuffer = new ComputeBuffer(_heightMapArr.Length, sizeof(float));
            _heightMapInverseBuffer.SetData(_heightMapArr);
            _heightMapComputeShader.SetBuffer(_heightMapInverseKernel, "_heightMapsInverseArrCSH", _heightMapInverseBuffer);

            float _min = (float)_minMaxHeight[0] / (float)_floatToIntMultiplier;
            float _max = (float)_minMaxHeight[1] / (float)_floatToIntMultiplier;

            Debug.Log($"min = {_min} ; max = {_max}");
            _heightMapComputeShader.SetFloat("_minHeightCSH", _min);
            _heightMapComputeShader.SetFloat("_maxHeightCSH", _max);

            _heightMapComputeShader.Dispatch(_heightMapInverseKernel, _threadGroups, _threadGroups, 1);

            _heightMapInverseBuffer.GetData(_heightMapArr);
            _heightMapInverseBuffer.Release();

            //Conversion to NativeArray
            _noiseMapNativeArray.CopyFrom(_heightMapArr);
            //Debug.Log(_noiseMapNativeArray.Max());

            #endregion Inverse Lerp Noise Map

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
        }
        /*
        private async IAsyncEnumerable<NativeArray<float>> AsyncGPUHeightMapMap(ComputeBuffer computeBuffer, AsyncGPUReadbackRequest request)
        {
            while (true)
            {
                // extract
                var request = AsyncGPUReadback.Request(buffer);

                yield return new WaitUntil(() => request.done);

                NativeArray<float> _heightMapNativeArray = request.GetData<float>();
            }
        }
        */



        protected override void OnDestroy()
        {
            if (_noiseMapNativeArray.IsCreated) _noiseMapNativeArray.Dispose();
            if (_octaveOffsetNativeArray.IsCreated) _octaveOffsetNativeArray.Dispose();
        }
    }
}
