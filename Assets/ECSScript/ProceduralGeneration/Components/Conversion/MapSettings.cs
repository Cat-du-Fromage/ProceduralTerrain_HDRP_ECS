using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using MapSett = KaizerwaldCode.ProceduralGeneration.Data.PerlinNoise;
namespace KaizerwaldCode.ProceduralGeneration.Data.Conversion
{
    [DisallowMultipleComponent]
    public class MapSettings : MonoBehaviour, IConvertGameObjectToEntity
    {
        [SerializeField] int _mapSize;
        [SerializeField] int _seed;
        [SerializeField] int _octaves;
        [SerializeField] float _scale;
        [SerializeField] float _lacunarity;
        [Range(0,1)]
        [SerializeField] float _persistance;
        [SerializeField] float2 _offset;
        [Range(0, 6)]
        [SerializeField] int _levelOfDetail;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            #region Check Values
            _mapSize = _mapSize < 1 ? 1 : _mapSize;
            _seed = _seed <= 0 ? 1 : _seed;
            _octaves = _octaves <= 0 ? 1 : _octaves;
            _scale = _scale <= 0 ? 0.0001f : _scale;
            _lacunarity = _lacunarity < 1f ? 1f : _lacunarity;
            #endregion Check Values
            dstManager.AddComponent<Tag.MapSettings>(entity);

            dstManager.AddComponent<DynamicBuffer.HeightMap.NoiseMap>(entity);

            dstManager.AddComponentData(entity, new MapSett.MapSize {Value = _mapSize});
            dstManager.AddComponentData(entity, new MapSett.Octaves { Value = _octaves });
            dstManager.AddComponentData(entity, new MapSett.Scale { Value = _scale });
            dstManager.AddComponentData(entity, new MapSett.Seed { Value = _seed });
            dstManager.AddComponentData(entity, new MapSett.Lacunarity { Value = _lacunarity });
            dstManager.AddComponentData(entity, new MapSett.Persistance { Value = _persistance });
            dstManager.AddComponentData(entity, new MapSett.Offset { Value = _offset });
            dstManager.AddComponentData(entity, new MapSett.LevelOfDetail { Value = _levelOfDetail });

            //Create Event Holder with a the Event "MapSettingsConverted"
            Entity MapEventHolder = dstManager.CreateEntity(typeof(Tag.MapEventHolder),typeof(Event.MapSettingsConverted));
            dstManager.SetName(MapEventHolder, "MapEventHolder");
        }
    }
}
