using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HideableObstructionOriginSync : MonoBehaviour
{

    public Transform origin;

    public string propertyName = "_Origin";
    public List<Material> materials;

    void Awake()
    {
        if (origin == null) origin = transform; 
    }

    void LateUpdate()
    {
        
        if (materials != null)
        {
            var pos = origin.position;
            foreach (Material material in materials) material.SetVector(propertyName, pos);
        }

    }

}
