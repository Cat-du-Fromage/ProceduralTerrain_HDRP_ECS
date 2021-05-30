using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using System.Diagnostics;

using CamMove = CameraECS.Data.Move;
using CamInput = CameraECS.Data.Inputs;
namespace CameraECS.InputSystem
{

    [BurstCompile]
    public class CameraSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireSingletonForUpdate<Data.Tag.CameraTag>();
        }
        protected override void OnUpdate()
        {
            Entities
                //.WithoutBurst()
                .WithAll<Data.Tag.CameraTag>()
                .ForEach((Entity camera, ref CamMove.Direction direction, in CamInput.Up up, in CamInput.Down down, in CamInput.Right right, in CamInput.Left left) => 
                {
                    float x = (Input.GetKey(right.RightKey) ? 1 : 0) + (Input.GetKey(left.LeftKey) ? -1 : 0);
                    float z = (Input.GetKey(up.UpKey) ? 1 : 0) + (Input.GetKey(down.DownKey) ? -1 : 0);

                    direction.DirectionValue.x = x;
                    direction.DirectionValue.z = z;
                }).Run();
        }
    }

    [BurstCompile]
    public class CameraMoveSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireSingletonForUpdate<Data.Tag.CameraTag>();
        }
        protected override void OnUpdate()
        {
            bool didMove = !GetComponent<CamMove.Direction>(GetSingletonEntity<Data.Tag.CameraTag>()).DirectionValue.Equals(float3.zero);
            float deltaTime = Time.DeltaTime;

            if (didMove)
            {
                Entities
                    //.WithoutBurst()
                    .ForEach((ref Translation position, ref CamMove.Speed speed, in CamMove.Direction direction, in CamInput.LeftShift leftShift) =>
                    {
                        speed.SpeedValue = Input.GetKey(leftShift.LeftShiftKey) ? math.mul(speed.SpeedValue, 2) : speed.SpeedValue;

                        #region X/Z translation
                        float3 normalizeDir = math.normalizesafe(direction.DirectionValue);
                        UnityEngine.Debug.Log($"normalizeDir : {normalizeDir}");
                        float SpeedDeltaTime = math.mul(speed.SpeedValue, deltaTime);
                        position.Value = math.mad(SpeedDeltaTime, normalizeDir, position.Value);
                        #endregion X/Z translation


                    }).Run();
            }
        }
    }

}
