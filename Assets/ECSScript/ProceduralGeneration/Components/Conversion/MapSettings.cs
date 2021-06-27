using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Animation;
using Unity.Animation.Hybrid;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine.UI;
using AnimationCurve = Unity.Animation.AnimationCurve;
using MapSett = KaizerwaldCode.ProceduralGeneration.Data.PerlinNoise;
namespace KaizerwaldCode.ProceduralGeneration.Data.Conversion
{
    [DisallowMultipleComponent]
    public class MapSettings : MonoBehaviour, IConvertGameObjectToEntity
    {
        [Header("Perlin Noise")]
        [SerializeField] int _mapSize;
        [SerializeField] int _chunkSize;
        [Tooltip("num chunk is calculated as a table exemple : numchunk = 100 (100 X 100)")]
        [SerializeField] int _numChunk;
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
        [Space]
        [SerializeField] UnityEngine.AnimationCurve _animationCurve;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            #region Check Values
            _chunkSize = _chunkSize <= 0 ? 241 : _chunkSize;
            _numChunk = math.max(1, _numChunk);
            _mapSize = _chunkSize * _numChunk;
            _seed = math.max(1, _seed);
            _octaves = math.max(1, _octaves);
            _scale = math.max(0.0001f, _scale);
            _lacunarity = math.max(1f, _lacunarity);
            _heightMultiplier = math.max(1f, _heightMultiplier);

            #endregion Check Values

            dstManager.AddComponent<Tag.MapSettings>(entity);

            dstManager.AddComponentData(entity, new MapSett.MapSize {Value = _mapSize});
            dstManager.AddComponentData(entity, new MapSett.ChunkSize { Value = _chunkSize });
            dstManager.AddComponentData(entity, new MapSett.NumChunk { Value = _numChunk });
            dstManager.AddComponentData(entity, new MapSett.Octaves { Value = _octaves });
            dstManager.AddComponentData(entity, new MapSett.Scale { Value = _scale });
            dstManager.AddComponentData(entity, new MapSett.Seed { Value = _seed });
            dstManager.AddComponentData(entity, new MapSett.Lacunarity { Value = _lacunarity });
            dstManager.AddComponentData(entity, new MapSett.Persistance { Value = _persistance });
            dstManager.AddComponentData(entity, new MapSett.Offset { Value = _offset });
            dstManager.AddComponentData(entity, new MapSett.HeightMultiplier { Value = _heightMultiplier });
            dstManager.AddComponentData(entity, new MapSett.LevelOfDetail { Value = _levelOfDetail });
            dstManager.AddComponentData(entity, new MapSett.HeightCurve { Value = _animationCurve.ToDotsAnimationCurve() });

            ComponentTypes _chunkHolderComponents = new ComponentTypes
            (
                typeof(LinkedEntityGroup),
                typeof(DynamicBuffer.HeightMap),
                typeof(Chunks.MeshBuffer.Vertices),
                typeof(Chunks.MeshBuffer.Uvs),
                typeof(Chunks.MeshBuffer.Triangles)
            );
            Entity ChunksHolder = dstManager.CreateEntity(typeof(Tag.ChunksHolder));
            dstManager.AddComponents(ChunksHolder, _chunkHolderComponents);
            dstManager.SetName(ChunksHolder, "ChunksHolder");

            //Create Event Holder with a the Event "MapSettingsConverted"
            Entity MapEventHolder = dstManager.CreateEntity(typeof(Tag.MapEventHolder),typeof(Event.CreationChunksEntityEvent),typeof(Event.HeightMapBigMapCalculEvent));
            dstManager.SetName(MapEventHolder, "MapEventHolder");
        }

        private void OnValidate()
        {
            //_mapSize = math.max(1, _mapSize);
            _chunkSize = _chunkSize > _mapSize || _chunkSize == 0 ? _mapSize : _chunkSize;
            _numChunk = math.max(1, _numChunk);

            _mapSize = _chunkSize * _numChunk;

            _seed = math.max(1, _seed);
            _octaves = math.max(1, _octaves);
            _scale = math.max(0.0001f, _scale);
            _lacunarity = math.max(1f, _lacunarity);
            _heightMultiplier = math.max(1f, _heightMultiplier);
        }
    }
}
