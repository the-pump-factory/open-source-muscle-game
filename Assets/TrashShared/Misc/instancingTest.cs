using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Swole.InstancedRendering;

public class instancingTest : MonoBehaviour
{

    [Serializable]
    public struct FloatProperty
    {
        public string propertyName;
        public float valueMin;
        public float valueMax;
    }

    public bool useInstancing = true;

    public bool useMaterialPropertyBlockForNonInstancedRendering = true;

    public int count = 1023;

    public Vector3 positionVariance = new Vector3(10, 10, 0);

    public bool randomColorEveryInstance = false;

    public Material material;
    public Mesh mesh;

    public int subMeshIndex;

    public string colorPropertyName = "_Color";
     
    public FloatProperty[] floatProperties;

    protected void Start()
    {

        if (useInstancing)
        {

            var renderGroup = Swole.InstancedRendering.GetRenderGroup<InstanceDataMatrixOnly>(mesh, subMeshIndex);

            Color col = Color.clear;

            for (int a = 0; a < count; a++)
            {

                if (randomColorEveryInstance || Mathf.FloorToInt((a - 1) / 1023f) != Mathf.FloorToInt(a / 1023f)) col = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value, 1);

                Swole.InstancedRendering.InstanceDataMatrixOnly data = new Swole.InstancedRendering.InstanceDataMatrixOnly()
                {

                    objectToWorld = Matrix4x4.TRS(transform.position + new Vector3(positionVariance.x * (0.5f - UnityEngine.Random.value) * 2, positionVariance.y * (0.5f - UnityEngine.Random.value) * 2, positionVariance.z * (0.5f - UnityEngine.Random.value) * 2), Quaternion.identity, Vector3.one),

                };

                MaterialPropertyInstanceOverride<float>[] floatProps = floatProperties == null ? null : new MaterialPropertyInstanceOverride<float>[floatProperties.Length];
                for (int b = 0; b < floatProperties.Length; b++) 
                {
                    floatProps[b] = new MaterialPropertyInstanceOverride<float>(floatProperties[b].propertyName, Mathf.RoundToInt(UnityEngine.Random.Range(floatProperties[b].valueMin, floatProperties[b].valueMax))); 
                }
                 
                renderGroup.AddNewInstance(material, data, true, false, default, floatProps, new MaterialPropertyInstanceOverride<Color>[] { new MaterialPropertyInstanceOverride<Color>(colorPropertyName, col) });

            }

        } 
        else
        {

            Color col = Color.clear;
            MaterialPropertyBlock matProps = new MaterialPropertyBlock();

            for (int a = 0; a < count; a++)
            {

                if (randomColorEveryInstance || Mathf.FloorToInt((a - 1) / 1023f) != Mathf.FloorToInt(a / 1023f)) 
                { 
                    
                    col = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value, 1);
                    matProps.SetColor(colorPropertyName, col);

                }

                GameObject obj = new GameObject("instanceTest_" + a);
                obj.transform.position = transform.position + new Vector3(positionVariance.x * (0.5f - UnityEngine.Random.value) * 2, positionVariance.y * (0.5f - UnityEngine.Random.value) * 2, positionVariance.z * (0.5f - UnityEngine.Random.value) * 2);
                obj.transform.rotation = Quaternion.identity;
                MeshFilter filter = obj.AddComponent<MeshFilter>();
                filter.sharedMesh = mesh;
                MeshRenderer renderer = obj.AddComponent<MeshRenderer>();

                if (!useMaterialPropertyBlockForNonInstancedRendering)
                {

                    Material mat = Instantiate(material);
                    mat.SetColor(colorPropertyName, col);
                    renderer.sharedMaterial = mat;

                }
                else
                {

                    renderer.sharedMaterial = material;
                    renderer.SetPropertyBlock(matProps);

                }

            }

        }

    }

}
