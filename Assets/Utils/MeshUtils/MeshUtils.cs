using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Transforms;
using UnityEngine.Rendering;
using Unity.Mathematics;

namespace KaizerwaldCode.Utils
{
    public static class MeshUtils
    {
        // This applies the game object's transform to the local bounds
        // Code by benblo from https://answers.unity.com/questions/361275/cant-convert-bounds-from-world-coordinates-to-loca.html
        public static Bounds TransformBounds(Bounds boundsOS, LocalToWorld ltw) 
        {
            Matrix4x4 matrixTrans = ltw.Value;
            //var center = transform.TransformPoint(boundsOS.center);
            float3 center = math.transform(ltw.Value, boundsOS.center);
            // transform the local extents' axes
            float3 extents = boundsOS.extents;
            //float3 axisX = transform.TransformVector(extents.x, 0, 0);
            float3 axisX = matrixTrans.MultiplyVector(new float3(extents.x, 0, 0));
            //float3 axisY = transform.TransformVector(0, extents.y, 0);
            float3 axisY = matrixTrans.MultiplyVector(new float3(0, extents.y, 0));
            //float3 axisZ = transform.TransformVector(0, 0, extents.z);
            float3 axisZ = matrixTrans.MultiplyVector(new float3(0, 0, extents.z));

            // sum their absolute value to get the world extents
            extents.x = math.abs(axisX.x) + math.abs(axisY.x) + math.abs(axisZ.x);
            extents.y = math.abs(axisX.y) + math.abs(axisY.y) + math.abs(axisZ.y);
            extents.z = math.abs(axisX.z) + math.abs(axisY.z) + math.abs(axisZ.z);

            return new Bounds { center = center, extents = extents };
        }
        /*
        public static void FromMatrix(this Transform transform, Matrix4x4 matrix)
        {
            transform.localScale = matrix.ExtractScale();
            transform.rotation = matrix.ExtractRotation();
            transform.position = matrix.ExtractPosition();
        }

        public static Quaternion ExtractRotation(this Matrix4x4 matrix)
        {
        Vector3 forward;
        forward.x = matrix.m02;
        forward.y = matrix.m12;
        forward.z = matrix.m22;

        Vector3 upwards;
        upwards.x = matrix.m01;
        upwards.y = matrix.m11;
        upwards.z = matrix.m21;

        return Quaternion.LookRotation(forward, upwards);
        }

        public static Vector3 ExtractPosition(this Matrix4x4 matrix)
        {
            Vector3 position;
            position.x = matrix.m03;
            position.y = matrix.m13;
            position.z = matrix.m23;
            return position;
        }

        public static Vector3 ExtractScale(this Matrix4x4 matrix)
        {
            Vector3 scale;
            scale.x = new Vector4(matrix.m00, matrix.m10, matrix.m20, matrix.m30).magnitude;
            scale.y = new Vector4(matrix.m01, matrix.m11, matrix.m21, matrix.m31).magnitude;
            scale.z = new Vector4(matrix.m02, matrix.m12, matrix.m22, matrix.m32).magnitude;
            return scale;
        }
        */
    }
}
