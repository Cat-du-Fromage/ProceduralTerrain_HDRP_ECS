using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace CameraECS.Data.Tag
{
    public readonly struct CameraTag : IComponentData { }
}

namespace CameraECS.Data.Inputs
{
    public struct Up : IComponentData
    {
        public KeyCode UpKey;
    }
    public struct Down : IComponentData
    {
        public KeyCode DownKey;
    }
    public struct Right : IComponentData
    {
        public KeyCode RightKey;
    }
    public struct Left : IComponentData
    {
        public KeyCode LeftKey;
    }
    public struct LeftShift : IComponentData
    {
        public KeyCode LeftShiftKey;
    }
    /*
    public struct MouseScrollUp : IComponentData
    {
        public KeyCode ScrollUpKey;
    }
    public struct MouseScrollDown : IComponentData
    {
        public KeyCode ScrollDownKey;
    }
    */
}

namespace CameraECS.Data.Move
{
    public struct Direction : IComponentData
    {
        public float3 DirectionValue;
    }
    public struct Speed : IComponentData
    {
        public float SpeedValue;
    }
}
