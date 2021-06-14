using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using Unity.Mathematics;
using Unity.Transforms;

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
    public struct VertexChunkJob : IJobParallelFor
    {
        [ReadOnly] public int ChunkSizeJob;

        [ReadOnly] public int MapSizeJob;
        [ReadOnly] public float TopLeftXJob;
        [ReadOnly] public float TopLeftZJob;
        [ReadOnly] public NativeArray<float> HeightMapJob;
        public NativeArray<float3> VerticesJob;
        public void Execute(int index)
        {
            int y = (int) math.floor(index / MapSizeJob);
            int x = index - math.mul(y, MapSizeJob);
            VerticesJob[index] = new float3(TopLeftXJob + x, HeightMapJob[index], TopLeftZJob - y);

            //int _chunk = (int)math.floor(y / ChunkSizeJob);
        }
    }
}
