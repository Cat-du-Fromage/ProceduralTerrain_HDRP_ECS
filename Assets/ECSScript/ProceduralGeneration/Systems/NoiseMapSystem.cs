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
using System.Collections.Generic;
using MapJobs = KaizerwaldCode.ProceduralGeneration.Jobs;
using MapSett = KaizerwaldCode.ProceduralGeneration.Data.PerlinNoise;
using BufferHeightMap = KaizerwaldCode.ProceduralGeneration.Data.DynamicBuffer;
using KaizerwaldCode.Utils;
using Unity.Jobs.LowLevel.Unsafe;
//REMOVE comments  Ctrl + k + u (c to put in comment)
namespace KaizerwaldCode.ProceduralGeneration.System
{
    public class NoiseMapSystem : SystemBase
    {
        EntityQuery _event;
        EntityQueryDesc _eventDescription;
        EntityManager _em;
        NativeArray<float2> _octaveOffsetNativeArray;

        protected override void OnCreate()
        {
            _eventDescription = new EntityQueryDesc()
            {
                All = new ComponentType[] { typeof(Data.Event.HeightMapBigMapCalculEvent) },
            };
            _event = GetEntityQuery(_eventDescription);
            RequireForUpdate(_event);
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        protected async override void OnStartRunning()
        {
            Entity _mapSettings = GetSingletonEntity<Data.Tag.MapSettings>();
            int _mapSurface = math.mul(GetComponent<MapSett.MapSize>(_mapSettings).Value, GetComponent<MapSett.MapSize>(_mapSettings).Value);

            float _numThreadsGPU = 32f;
            int _threadGroups = (int)math.ceil(GetComponent<MapSett.MapSize>(_mapSettings).Value / _numThreadsGPU);

            float[] _heightMapArr = new float[_mapSurface];
            int[] _minMaxHeight = new int[2]; // Int beacause of interlockMin/max don't accept float
            int _floatToIntMultiplier = 1000; // needed for interlock function that can only take int (so we make float*1000 inside compute shader function)

            //Load compute shader
            ComputeShader _heightMapComputeShader = AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/ECSScript/ProceduralGeneration/ComputeShader/HeightMapComputeShader.compute");
            int _heightMapKernel = _heightMapComputeShader.FindKernel("CSHeightMap");
            int _heightMapInverseKernel = _heightMapComputeShader.FindKernel("CSHeightMapInverseLerp");

            /*========================
             *Native Array declaration
             ========================*/
            _octaveOffsetNativeArray = new NativeArray<float2>(GetComponent<MapSett.Octaves>(_mapSettings).Value, Allocator.Persistent);

//==========================================================================================================================================================

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
            JobHandle _noiseRandomJobHandle = _noiseRandomJob.Schedule(_octaveOffsetNativeArray.Length, JobsUtility.JobWorkerCount-1);
            //needed for compute shader => can't use dependency in this case
            _noiseRandomJobHandle.Complete();
            float2[] _octaveOffsetArr = _octaveOffsetNativeArray.ToArray();
            _octaveOffsetNativeArray.Dispose();

//==========================================================================================================================================================

            #region Noise Map
            /*========================
             * Perlin Noise Compute Shader
             * return : NoiseMap
             ========================*/

            //OffsetArray To ComputeBuffer
            ComputeBuffer _offsetsBuffer = new ComputeBuffer(_octaveOffsetArr.Length, sizeof(float) * 2);
            UtComputeShader.CSHSetBuffer(_heightMapComputeShader, _heightMapKernel, "offsetsArrCSH", _offsetsBuffer, _octaveOffsetArr);

            //HeightMapArray To ComputeBuffer
            ComputeBuffer _heightMapBuffer = new ComputeBuffer(_heightMapArr.Length, sizeof(float));
            UtComputeShader.CSHSetBuffer(_heightMapComputeShader, _heightMapKernel, "heightMapsArrCSH", _heightMapBuffer, _heightMapArr);

            //MinMaxArray To ComputeBuffer
            ComputeBuffer _minMaxBuffer = new ComputeBuffer(_minMaxHeight.Length, sizeof(int));
            UtComputeShader.CSHSetBuffer(_heightMapComputeShader, _heightMapKernel, "minMaxArrCSH", _minMaxBuffer, _minMaxHeight);

            //Set Parameters in ComputeShader
            _heightMapComputeShader.SetInt("floatToIntMultiplierCSH", _floatToIntMultiplier);
            _heightMapComputeShader.SetInt("mapSizeCSH", GetComponent<MapSett.MapSize>(_mapSettings).Value);
            _heightMapComputeShader.SetInt("octavesCSH", GetComponent<MapSett.Octaves>(_mapSettings).Value);
            _heightMapComputeShader.SetFloat("lacunarityCSH", GetComponent<MapSett.Lacunarity>(_mapSettings).Value);
            _heightMapComputeShader.SetFloat("persistenceCSH", GetComponent<MapSett.Persistance>(_mapSettings).Value);
            _heightMapComputeShader.SetFloat("scaleCSH", GetComponent<MapSett.Scale>(_mapSettings).Value);

            //Dispatch ThreadGroup
            (float[], int[]) _requestAsyncGPUHeightMap = await AsyncGPUHeightMap(_heightMapComputeShader, _heightMapKernel, _threadGroups, _heightMapBuffer, _minMaxBuffer);
            _heightMapArr = _requestAsyncGPUHeightMap.Item1;
            _minMaxHeight = _requestAsyncGPUHeightMap.Item2;

            UtComputeShader.CSHReleaseBuffers(_heightMapBuffer, _offsetsBuffer, _minMaxBuffer);
            #endregion Noise Map

//==========================================================================================================================================================

            #region Inverse Lerp Noise Map

            /*========================
             * Inverse Lerp Perlin Noise Compute Shader
             * return : inverse lerp NoiseMap
             ========================*/

            //HeightMapArray To ComputeBuffer
            ComputeBuffer _heightMapInverseBuffer = new ComputeBuffer(_heightMapArr.Length, sizeof(float));
            UtComputeShader.CSHSetBuffer(_heightMapComputeShader, _heightMapInverseKernel, "heightMapsInverseArrCSH", _heightMapInverseBuffer, _heightMapArr);

            float _min = (float)_minMaxHeight[0] / (float)_floatToIntMultiplier;
            float _max = (float)_minMaxHeight[1] / (float)_floatToIntMultiplier;

            _heightMapComputeShader.SetFloat("minHeightCSH", _min);
            _heightMapComputeShader.SetFloat("maxHeightCSH", _max);
            _heightMapArr = await AsyncGPUHeightMapUnLerp(_heightMapComputeShader, _heightMapInverseKernel, _threadGroups, _heightMapInverseBuffer);

            _heightMapInverseBuffer.Release();

            #endregion Inverse Lerp Noise Map

            //==========================================================================================================================================================

            #region FallOffMap

            NativeArray<float> fallOffNativeArray = new NativeArray<float>(_mapSurface, Allocator.TempJob);
            fallOffNativeArray.CopyFrom(_heightMapArr);
            MapJobs.FallOffJob fallOffJob = new MapJobs.FallOffJob()
            {
                MapSizeJob = GetComponent<MapSett.MapSize>(_mapSettings).Value,
                HeightMapJob = fallOffNativeArray,
            };
            JobHandle _fallOffJobHandle = fallOffJob.Schedule(_mapSurface, JobsUtility.JobWorkerCount - 1);
            _fallOffJobHandle.Complete();
            fallOffNativeArray.CopyTo(_heightMapArr);
            fallOffNativeArray.Dispose();
            #endregion FallOffMap
            /*========================
             * Pass Noise Map to DynamicBuffer
             * Process : NoiseMap stored
             * END Event : MapSettingsConverted
             * START Event : NoiseMapCalculated
             ========================*/
            _em.GetBuffer<BufferHeightMap.HeightMap>(GetSingletonEntity<Data.Tag.ChunksHolder>()).Reinterpret<float>().CopyFrom(_heightMapArr);

            _em.RemoveComponent<Data.Event.HeightMapBigMapCalculEvent>(GetSingletonEntity<Data.Tag.MapEventHolder>());
            _em.AddComponent<Data.Event.ColorMapCalculEvent>(GetSingletonEntity<Data.Tag.MapEventHolder>());
        }

        protected override void OnUpdate()
        {

        }

        /// <summary>
        /// Start Compute shader "Unlerp Height Map"
        /// Retrieve both HeightMap and MinMax arrays when calculation is done(using AsyncGPUReadbackRequest)
        /// </summary>
        /// <param name="computeShader"></param>
        /// <param name="kernel"></param>
        /// <param name="threadGroups"></param>
        /// <param name="computeBufferHeightMap"></param>
        /// <param name="computeBufferMineMax"></param>
        /// <returns></returns>
        private async Task<(float[], int[])> AsyncGPUHeightMap(ComputeShader computeShader, int kernel, int threadGroups, ComputeBuffer computeBufferHeightMap, ComputeBuffer computeBufferMineMax)
        {
            computeShader.Dispatch(kernel, threadGroups, threadGroups, 1);
            AsyncGPUReadbackRequest requestHeightMap = AsyncGPUReadback.Request(computeBufferHeightMap);
            AsyncGPUReadbackRequest requestMinMax = AsyncGPUReadback.Request(computeBufferMineMax);
            while ((!requestHeightMap.done && !requestHeightMap.hasError) && (!requestMinMax.done && !requestMinMax.hasError))
            {
                await Task.Yield();
            }
            NativeArray<float> _heightMapNativeArray = requestHeightMap.GetData<float>(0);
            NativeArray<int> _minMaxNativeArray = requestMinMax.GetData<int>();
            return (_heightMapNativeArray.ToArray(), _minMaxNativeArray.ToArray());
        }
        /// <summary>
        /// Start Compute shader "Unlerp Height Map"
        /// Retrieve Unlerped HeightMap when calculation is done(using AsyncGPUReadbackRequest)
        /// </summary>
        /// <param name="computeShader"></param>
        /// <param name="kernel"></param>
        /// <param name="threadGroups"></param>
        /// <param name="computeBufferHeightMap"></param>
        /// <returns></returns>
        private async Task<float[]> AsyncGPUHeightMapUnLerp(ComputeShader computeShader, int kernel, int threadGroups, ComputeBuffer computeBufferHeightMap)
        {
            computeShader.Dispatch(kernel, threadGroups, threadGroups, 1);
            AsyncGPUReadbackRequest requestHeightMap = AsyncGPUReadback.Request(computeBufferHeightMap);
            while ((!requestHeightMap.done && !requestHeightMap.hasError))
            {
                await Task.Yield();
            }
            NativeArray<float> _heightMapNativeArray = requestHeightMap.GetData<float>(0);
            return _heightMapNativeArray.ToArray();
        }



        protected override void OnDestroy()
        {
            if (_octaveOffsetNativeArray.IsCreated) _octaveOffsetNativeArray.Dispose();
        }
    }
}
