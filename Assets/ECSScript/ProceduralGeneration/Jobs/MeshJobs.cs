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
