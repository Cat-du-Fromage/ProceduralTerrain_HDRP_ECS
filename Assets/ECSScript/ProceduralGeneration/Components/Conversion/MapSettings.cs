using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Animation;
using Unity.Animation.Hybrid;
using AnimationCurve = Unity.Animation.AnimationCurve;
using MapSett = KaizerwaldCode.ProceduralGeneration.Data.PerlinNoise;
namespace KaizerwaldCode.ProceduralGeneration.Data.Conversion
{
    [DisallowMultipleComponent]
    public class MapSettings : MonoBehaviour, IConvertGameObjectToEntity
    {
        [SerializeField] int _mapSize;
        [SerializeField] int _chunkSize;
        [SerializeField] int _seed;
        [SerializeField] int _octaves;
        [SerializeField] float _scale;
        [SerializeField] float _lacunarity;
        [Range(0,1)]
        [SerializeField] float _persistance;
        [SerializeField] float2 _offset;
        [SerializeField] float _heightMultiplier;
        [Range(0, 6)]
        [SerializeField] int _levelOfDetail;
        [SerializeField] UnityEngine.AnimationCurve _animationCurve;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            #region Check Values
            math.max(1, _mapSize);
            _chunkSize = _chunkSize > _mapSize || _chunkSize == 0 ? _mapSize : _chunkSize;
            math.max(1, _seed);
            math.max(1, _octaves);
            math.max(0.0001f, _scale);
            math.max(1f, _lacunarity);
            math.max(1f, _heightMultiplier);
            #endregion Check Values
            dstManager.AddComponent<Tag.MapSettings>(entity);
            dstManager.AddComponent<DynamicBuffer.HeightMap>(entity);

            dstManager.AddComponentData(entity, new MapSett.MapSize {Value = _mapSize});
            dstManager.AddComponentData(entity, new MapSett.ChunkSize { Value = _chunkSize });
            dstManager.AddComponentData(entity, new MapSett.Octaves { Value = _octaves });
            dstManager.AddComponentData(entity, new MapSett.Scale { Value = _scale });
            dstManager.AddComponentData(entity, new MapSett.Seed { Value = _seed });
            dstManager.AddComponentData(entity, new MapSett.Lacunarity { Value = _lacunarity });
            dstManager.AddComponentData(entity, new MapSett.Persistance { Value = _persistance });
            dstManager.AddComponentData(entity, new MapSett.Offset { Value = _offset });
            dstManager.AddComponentData(entity, new MapSett.HeightMultiplier { Value = _heightMultiplier });
            dstManager.AddComponentData(entity, new MapSett.LevelOfDetail { Value = _levelOfDetail });
            dstManager.AddComponentData(entity, new MapSett.HeightCurve { Value = _animationCurve.ToDotsAnimationCurve() });

            //Create Event Holder with a the Event "MapSettingsConverted"
            Entity MapEventHolder = dstManager.CreateEntity(typeof(Tag.MapEventHolder),typeof(Event.MapSettingsConverted));
            dstManager.SetName(MapEventHolder, "MapEventHolder");


            Entity ChunksHolder = dstManager.CreateEntity(typeof(Tag.ChunksHolder), typeof(LinkedEntityGroup), typeof(Chunks.MeshBuffer.Vertices), typeof(Chunks.MeshBuffer.Uvs), typeof(Chunks.MeshBuffer.Triangles));
            dstManager.SetName(ChunksHolder, "ChunksHolder");
        }
    }
}
