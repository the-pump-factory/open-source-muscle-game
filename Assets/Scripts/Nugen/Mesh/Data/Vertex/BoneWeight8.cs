using System.Runtime.InteropServices;
using System;

using UnityEngine;

using Unity.Mathematics;
using Unity.Collections;

namespace nugen.mesh.data.vertex
{

    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct BoneWeight8
    {

        public int4 indicesA, indicesB;

        public float4 weightsA, weightsB;

        public static BoneWeight8 Fill(int boneWeightArrayOffset, int boneWeightCount, NativeArray<BoneWeight1> boneWeights)
        {

            BoneWeight8 boneWeight = new BoneWeight8();

            for (int b = 0; b < boneWeightCount; b++)
            {

                BoneWeight1 boneWeight_ = boneWeights[boneWeightArrayOffset + b];

                switch (b)
                {

                    default:
                        break;

                    case 0:
                        boneWeight.indicesA = new int4(boneWeight_.boneIndex, boneWeight.indicesA.y, boneWeight.indicesA.z, boneWeight.indicesA.w);
                        boneWeight.weightsA = new float4(boneWeight_.weight, boneWeight.weightsA.y, boneWeight.weightsA.z, boneWeight.weightsA.w);
                        break;

                    case 1:
                        boneWeight.indicesA = new int4(boneWeight.indicesA.x, boneWeight_.boneIndex, boneWeight.indicesA.z, boneWeight.indicesA.w);
                        boneWeight.weightsA = new float4(boneWeight.weightsA.x, boneWeight_.weight, boneWeight.weightsA.z, boneWeight.weightsA.w);
                        break;

                    case 2:
                        boneWeight.indicesA = new int4(boneWeight.indicesA.x, boneWeight.indicesA.y, boneWeight_.boneIndex, boneWeight.indicesA.w);
                        boneWeight.weightsA = new float4(boneWeight.weightsA.x, boneWeight.weightsA.y, boneWeight_.weight, boneWeight.weightsA.w);
                        break;

                    case 3:
                        boneWeight.indicesA = new int4(boneWeight.indicesA.x, boneWeight.indicesA.y, boneWeight.indicesA.z, boneWeight_.boneIndex);
                        boneWeight.weightsA = new float4(boneWeight.weightsA.x, boneWeight.weightsA.y, boneWeight.weightsA.z, boneWeight_.weight);
                        break;

                    case 4:
                        boneWeight.indicesB = new int4(boneWeight_.boneIndex, boneWeight.indicesB.y, boneWeight.indicesB.z, boneWeight.indicesB.w);
                        boneWeight.weightsB = new float4(boneWeight_.weight, boneWeight.weightsB.y, boneWeight.weightsB.z, boneWeight.weightsB.w);
                        break;

                    case 5:
                        boneWeight.indicesB = new int4(boneWeight.indicesB.x, boneWeight_.boneIndex, boneWeight.indicesB.z, boneWeight.indicesB.w);
                        boneWeight.weightsB = new float4(boneWeight.weightsB.x, boneWeight_.weight, boneWeight.weightsB.z, boneWeight.weightsB.w);
                        break;

                    case 6:
                        boneWeight.indicesB = new int4(boneWeight.indicesB.x, boneWeight.indicesB.y, boneWeight_.boneIndex, boneWeight.indicesB.w);
                        boneWeight.weightsB = new float4(boneWeight.weightsB.x, boneWeight.weightsB.y, boneWeight_.weight, boneWeight.weightsB.w);
                        break;

                    case 7:
                        boneWeight.indicesB = new int4(boneWeight.indicesB.x, boneWeight.indicesB.y, boneWeight.indicesB.z, boneWeight_.boneIndex);
                        boneWeight.weightsB = new float4(boneWeight.weightsB.x, boneWeight.weightsB.y, boneWeight.weightsB.z, boneWeight_.weight);
                        break;

                }

            }

            return boneWeight;

        }

    }

}
