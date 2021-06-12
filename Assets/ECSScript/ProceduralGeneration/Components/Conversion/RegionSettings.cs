using System;
using KaizerwaldCode.ProceduralGeneration.Data.Tag;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;
using KaizerwaldCode.ProceduralGeneration.Data.DynamicBuffer;

[UpdateAfter(typeof(MapSettings))]
[DisallowMultipleComponent]
public class RegionSettings : MonoBehaviour, IConvertGameObjectToEntity
{
    [SerializeField] TerrainType[] _terrains;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponent<Regions>(entity);
        dstManager.AddComponent<ColorMap>(entity);

        DynamicBuffer<Regions> _regionsBuffer = dstManager.GetBuffer<Regions>(entity);

        for (int i = 0; i < _terrains.Length; i++)
        {
            Regions _regionData = new Regions();
            _regionData.Height = _terrains[i].height;
            _regionData.Color.Value = ColorConverter(_terrains[i].colour);
            _regionsBuffer.Add(_regionData);
        }
    }

    float4 ColorConverter(Color color)
    {
        Vector4 ColorV4 = color;
        float4 ColorF4 = ColorV4;
        return ColorF4;
    }
}

[System.Serializable]
public struct TerrainType
{
    public float height;
    public Color colour;
}