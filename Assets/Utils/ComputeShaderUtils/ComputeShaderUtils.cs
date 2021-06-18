using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KaizerwaldCode.Utils
{
    public static class UtComputeShader
    {
        /// <summary>
        /// concat "computeBuffer.SetData(Array[])" and "computeShader.SetBuffer(int, string, ComputeBuffer)" into one function
        /// </summary>
        /// <param name="computeShader"></param>
        /// <param name="kernel"></param>
        /// <param name="CSdata"></param>
        /// <param name="computeBuffer"></param>
        /// <param name="array"></param>
        public static void CSHSetBuffer(ComputeShader computeShader, int kernel, string CSdata, ComputeBuffer computeBuffer, Array array)
        {
            computeBuffer.SetData(array);
            computeShader.SetBuffer(kernel, CSdata, computeBuffer);
        }
    }
}
