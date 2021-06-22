using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

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
            _em.RemoveComponent<Data.Event.ChunksEntityCreated>(GetSingletonEntity<Data.Tag.MapEventHolder>());
        }
    }
}
