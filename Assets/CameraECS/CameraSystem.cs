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
            /*
            if (Input.GetMouseButtonDown(2))
            {
                SetComponent(GetSingletonEntity<Data.Tag.CameraTag>(), new CamMove.MouseDragPosition {DragLength = Input.mousePosition });
                //mouseDragPos.End = Input.mousePosition;
                UnityEngine.Debug.Log($"Input.mousePosition");
            }
            */

            Entities
                //.WithBurst()
                .WithoutBurst()
                .WithAll<Data.Tag.CameraTag>()
                .ForEach((ref CamMove.MouseDragPosition mouseDragPos,
                          ref CamMove.Direction direction,
                          in CamInput.LeftShift leftShift,
                          in CamInput.Up up,
                          in CamInput.Down down,
                          in CamInput.Right right,
                          in CamInput.Left left,
                          in CamInput.MouseMiddle midMouse) =>
                        {
                            float3 x = (Input.GetKey(right.RightKey) ? math.right() : float3.zero) + (Input.GetKey(left.LeftKey) ? math.left() : float3.zero);
                            float3 z = (Input.GetKey(up.UpKey) ? math.forward() : float3.zero) + (Input.GetKey(down.DownKey) ? math.back() : float3.zero);

                            // Y axe is a bit special
                            float3 y = float3.zero;
                            if (!Input.mouseScrollDelta.Equals(float2.zero)) { y = Input.mouseScrollDelta.y > 0 ? math.up() : math.down(); }

                            direction.Value = x + y + z;

                            //Rotation
                            
                            if (Input.GetMouseButtonDown(2)) 
                            {
                                mouseDragPos.Start = Input.mousePosition;
                                //mouseDragPos.End = Input.mousePosition;
                                UnityEngine.Debug.Log($"Input.mousePosition"); 
                            }
                            
                            mouseDragPos.End = Input.GetMouseButton(2) ? (float3)Input.mousePosition : mouseDragPos.Start;
                            mouseDragPos.DragLength = mouseDragPos.End - mouseDragPos.Start;
                            //UnityEngine.Debug.Log($"Input.mousePosition : {mousePosition.End}");
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
            bool didMove = !GetComponent<CamMove.Direction>(GetSingletonEntity<Data.Tag.CameraTag>()).Value.Equals(float3.zero);
            bool isRotating = Input.GetMouseButton(2);
            float deltaTime = Time.DeltaTime;

            if (didMove || isRotating) //CARFUL WITH THIS THING
            {
                Entities
                    .WithBurst()
                    .WithAll<Data.Tag.CameraTag>()
                    .ForEach((ref Translation position,
                              ref Rotation rotation,
                              ref CamMove.Speed speed,
                              ref CamMove.SpeedZoom speedZoom,
                              ref CamMove.MouseDragPosition mouseDragPos,
                              in CamMove.Direction direction,
                              in CamInput.LeftShift leftShift,
                              //in CamInput.MouseMiddle midMouse
                              in LocalToWorld ltw
                              //in CamMove.ShiftMultiplicator shiftMul,
                              ) =>
                                {
                                    UnityEngine.Debug.Log($"Enter2");
                                    #region X/Z translation
                                    //Shift Key multiplicator
                                    float speedXZ = Input.GetKey(leftShift.LeftShiftKey) ? math.mul(speed.Value, 2/*shiftMul.Value*/) : speed.Value; //speed
                                    float speedY = Input.GetKey(leftShift.LeftShiftKey) ? math.mul(speedZoom.Value, 2/*shiftMul.Value*/) : speedZoom.Value; //speedZoom

                                    //Speed depending on Y Position (min : default speed Value)
                                    speedXZ = math.max(speedXZ, math.mul(position.Value.y, speedXZ));
                                    speedY = math.max(speedY, math.mul(math.log(position.Value.y), speedY));

                                    //Dependency with delta time
                                    float SpeedDeltaTimeXZ = math.mul(speedXZ, deltaTime);
                                    float SpeedDeltaTimeY = math.mul(speedY, deltaTime);

                                    //calculate new position (both XZ and Y)
                                    float3 HorizontalMove = new float3(math.mad(direction.Value.x, SpeedDeltaTimeXZ, position.Value.x), 0, math.mad(direction.Value.z, SpeedDeltaTimeXZ, position.Value.z));
                                    float3 VerticalMove = new float3(0, math.mad(direction.Value.y, SpeedDeltaTimeY, position.Value.y), 0);

                                    position.Value = HorizontalMove + VerticalMove;
                                    #endregion X/Z translation

                                    #region Rotation
                                    float rotationSpeed = math.mul(speed.Value, deltaTime);
                                    UnityEngine.Debug.Log($"Enter3 {rotationSpeed} ");
                                    float3 distanceRadian = new float3(math.radians(mouseDragPos.DragLength.x), math.radians(mouseDragPos.DragLength.y), math.radians(mouseDragPos.DragLength.z));

                                    float distanceX = math.mul(rotationSpeed, distanceRadian.x);
                                    float distanceY = math.mul(rotationSpeed, distanceRadian.y);

                                    UnityEngine.Debug.Log($"distanceX : {distanceX}");
                                    math.transform(math.inverse(ltw.Value), position.Value);
                                    //rotation.Value = math.mul(rotation.Value, quaternion.AxisAngle(math.left(), -distanceX));
                                    //rotation.Value = math.mul(rotation.Value, quaternion.AxisAngle(math.up(), -distanceY));
                                    rotation.Value = math.mul(rotation.Value, quaternion.RotateY(distanceX));
                                    #endregion Rotation

                                }).Run();
            }
        }
    }

    /*
    [BurstCompile]
    public class CameraRotationSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireSingletonForUpdate<Data.Tag.CameraTag>();
        }
        protected override void OnUpdate()
        {
            float deltaTime = Time.DeltaTime;

            bool firstSpawn = !GetComponent<CamMove.MouseStartPosition>(GetSingletonEntity<Data.Tag.CameraTag>()).Value.Equals(float3.zero);
            if (firstSpawn)
            {
                Entities
                    .WithBurst()
                    .WithChangeFilter<CamMove.MouseStartPosition>()
                    .ForEach((ref Rotation rotation, in CamMove.MouseStartPosition startPosition) =>
                    {
                        UnityEngine.Debug.Log($"New position : {startPosition.Value}");
                        #region Rotation

                        //rotation.Value = quaternion.EulerZXY(new float3(1,1,1));
                        #endregion Rotation
                    }).Run();
            }
        }
    }
    */
}
