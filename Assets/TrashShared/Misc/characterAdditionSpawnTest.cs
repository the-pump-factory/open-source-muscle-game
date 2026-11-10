using Swole;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class characterAdditionSpawnTest : MonoBehaviour
{
    public Transform root;

    public CharacterAddition[] additions;

    protected void Awake()
    {
        if (root == null) root = transform; 
    }

    void Start()
    {
        if (additions != null)
        {
            foreach(var addition in additions)
            {
                switch(addition.meshType)
                {
                    case CharacterAddition.MeshType.UnitySkinnedMesh:
                        addition.SpawnAsSkinnedMeshRenderer(root);
                        break;

                    case CharacterAddition.MeshType.CustomizableCharacterMesh:
                        addition.SpawnAsCustomizableCharacterMesh(root);
                        break;
                }
            }
        }

        Destroy(this);
    }
}
