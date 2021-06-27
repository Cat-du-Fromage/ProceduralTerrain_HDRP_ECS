using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Animation;
using AnimationCurve = Unity.Animation.AnimationCurve;

namespace KaizerwaldCode.ProceduralGeneration.Jobs
{
    /*
     #region MESH JOB
    /// <summary>
    /// Mesh Generation
    /// </summary>
    [BurstCompile]
    public struct MeshDataJob : IJob
    {
        [ReadOnly] public int widthJob;
        [ReadOnly] public int heightJob;
        [ReadOnly] public NativeArray<float> noiseMapJob;
        [ReadOnly] public float heightMulJob;
        [ReadOnly] public NativeArray<float> curveJob;

        //Terrain Complexity(increase/Decrease
        [ReadOnly] public int levelOfDetailJob;
        [ReadOnly] public int meshSimplificationIncrementJob;
        [ReadOnly] public int verticesPerLineJob;

        public NativeArray<float3> verticesJob;
        public NativeArray<int> trianglesJob;
        public NativeArray<float2> uvsJob;
        public void Execute()
        {
            int triangleIndex = 0;
            
            float topLeftX = (widthJob - 1) / -2f;
            float topLeftZ = (heightJob - 1) / 2f;

            int vertexIndex = 0;
            for (int y = 0; y < heightJob; y+= meshSimplificationIncrementJob)
            {
                for (int x = 0; x < widthJob; x+= meshSimplificationIncrementJob)
                {
                    int4 tranglesVertex = new int4(vertexIndex, vertexIndex + verticesPerLineJob + 1, vertexIndex + verticesPerLineJob, vertexIndex + 1);

                    //int linearIndex = math.mad(y, widthJob, x); // Index in a Linear Array
                    //float curveValue = animCurveJob.Evaluate(noiseMapJob[linearIndex]); //Value after evaluation in the animation Curve
                    verticesJob[vertexIndex] = new float3(topLeftX + x, math.mul(curveJob[math.mad(y, widthJob, x)], heightMulJob), topLeftZ - y);

                    uvsJob[vertexIndex] = new float2(x / (float)widthJob, y / (float)heightJob);

                    if (x < widthJob - 1 && y < heightJob - 1)
                    {
                        trianglesJob[triangleIndex] = tranglesVertex.x;
                        trianglesJob[triangleIndex + 1] = tranglesVertex.y;
                        trianglesJob[triangleIndex + 2] = tranglesVertex.z;
                        triangleIndex += 3;
                        trianglesJob[triangleIndex] = tranglesVertex.y;
                        trianglesJob[triangleIndex + 1] = tranglesVertex.x;
                        trianglesJob[triangleIndex + 2] = tranglesVertex.w;
                        triangleIndex += 3;
                    }
                    vertexIndex++;
                }
            }
        }
    }
    #endregion MESH JOB
     */

    [BurstCompile(CompileSynchronously = true)]
    public struct MeshMapJob : IJobParallelFor
    {
        [ReadOnly] public int MapSizeJob;
        [ReadOnly] public float TopLeftXJob;
        [ReadOnly] public float TopLeftZJob;
        [ReadOnly] public float HeightMulJob;
        [ReadOnly] public NativeArray<float> HeightMapJob;

        [NativeDisableParallelForRestriction] [WriteOnly]
        public NativeArray<float3> VerticesJob;

        [NativeDisableParallelForRestriction] [WriteOnly]
        public NativeArray<float2> UvsJob;

        [NativeDisableParallelForRestriction] [WriteOnly]
        public NativeArray<int> TrianglesJob;

        public void Execute(int index)
        {
            int _y = (int) math.floor(index / MapSizeJob);
            int _x = index - math.mul(_y, MapSizeJob);

            int _vertexIndex = index;
            int _triangleIndex = _vertexIndex * 6;
            int4 _tranglesVertex = new int4(_vertexIndex, _vertexIndex + MapSizeJob + 1, _vertexIndex + MapSizeJob,
                _vertexIndex + 1);

            VerticesJob[index] = new float3(TopLeftXJob + _x, math.mul(HeightMapJob[index], HeightMulJob),
                TopLeftZJob - _y);
            UvsJob[index] = new float2(_x / (float) MapSizeJob, _y / (float) MapSizeJob);
            if (_x < MapSizeJob - 1 && _y < MapSizeJob - 1)
            {
                TrianglesJob[_triangleIndex] = _tranglesVertex.x;
                TrianglesJob[_triangleIndex + 1] = _tranglesVertex.y;
                TrianglesJob[_triangleIndex + 2] = _tranglesVertex.z;
                _triangleIndex += 3;
                TrianglesJob[_triangleIndex] = _tranglesVertex.y;
                TrianglesJob[_triangleIndex + 1] = _tranglesVertex.x;
                TrianglesJob[_triangleIndex + 2] = _tranglesVertex.w;
            }
        }
    }

    [BurstCompile(CompileSynchronously = true)]
    public struct MeshCurvedMapJob : IJobParallelFor
    {
        [DeallocateOnJobCompletion]
        [ReadOnly] public NativeArray<float> HeightMapJob;
        [ReadOnly] public AnimationCurve AnimCurveJob;

        [WriteOnly] public NativeArray<float> CurvedHeightMapJob;
        public void Execute(int index)
        {
            CurvedHeightMapJob[index] = AnimationCurveEvaluator.Evaluate(HeightMapJob[index], ref AnimCurveJob);
        }
    }
}
