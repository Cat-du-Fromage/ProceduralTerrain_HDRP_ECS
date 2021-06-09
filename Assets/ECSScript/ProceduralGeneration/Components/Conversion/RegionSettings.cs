using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

[DisallowMultipleComponent]
public class RegionSettings : MonoBehaviour, IConvertGameObjectToEntity
{
    //Use of UnityEngine Color for convieniency use on the editor
    [SerializeField] Color OceanColor;
    [SerializeField] float Ocean;
    [SerializeField] Color CoastColor;
    [SerializeField] float Coast;
    [SerializeField] Color SandColor;
    [SerializeField] float Sand;
    [SerializeField] Color PlainColor;
    [SerializeField] float Plain;
    [SerializeField] Color ForestColor;
    [SerializeField] float Forest;
    [SerializeField] Color TundraColor;
    [SerializeField] float Tundra;
    [SerializeField] Color MountainColor;
    [SerializeField] float Mountain;
    [SerializeField] Color SnowColor;
    [SerializeField] float Snow;

    MaterialColor OceanMat;
    MaterialColor CoastMat;
    MaterialColor SandMat;
    MaterialColor PlainMat;
    MaterialColor ForestMat;
    MaterialColor TundraMat;
    MaterialColor MountainMat;
    MaterialColor SnowMat;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        #region conversion to MaterialColor
        OceanMat.Value = ColorConverter(OceanColor);
        CoastMat.Value = ColorConverter(CoastColor);
        SandMat.Value = ColorConverter(SandColor);
        PlainMat.Value = ColorConverter(PlainColor);
        ForestMat.Value = ColorConverter(ForestColor);
        TundraMat.Value = ColorConverter(TundraColor);
        MountainMat.Value = ColorConverter(MountainColor);
        SnowMat.Value = ColorConverter(SnowColor);
        #endregion conversion to MaterialColor

        Debug.Log(OceanMat.Value);
        Debug.Log(CoastMat.Value);
    }

    float4 ColorConverter(Color color)
    {
        Vector4 ColorV4 = color;
        float4 ColorF4 = ColorV4;
        return ColorF4;
    }
}
