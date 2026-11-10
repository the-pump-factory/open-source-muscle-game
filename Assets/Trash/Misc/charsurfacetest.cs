using nugen.mesh;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Swole.API.Unity;

public class charsurfacetest : MonoBehaviour
{

    public RenderedCharacter character;

    private CharacterSurface.SamplePointReference testReference;
    private CharacterSurface finalSurface;

    protected void Start()
    {
        
        if (character != null)
        {

            int closestSurface = -1;
            int closestVertex = -1;
            float minDist = float.MaxValue;

            Vector3 position = transform.position;

            for(int a = 0; a < character.SurfaceCount; a++)
            {

                var surface = character.GetSurface(a);

                Vector3[] worldVertices = RealtimeMesh.GetTransformedVertices(surface.Renderer);

                for(int b = 0; b < worldVertices.Length; b++)
                {

                    Vector3 worldVertex = worldVertices[b];

                    float dist = (worldVertex - position).sqrMagnitude;

                    if (dist < minDist)
                    {

                        minDist = dist;
                        closestVertex = b;
                        closestSurface = a;

                    }

                }

                if (closestVertex >= 0 && closestVertex < worldVertices.Length) Debug.DrawLine(worldVertices[closestVertex], transform.position, Color.red, 15);

            }

            if (closestSurface >= 0 && closestVertex >= 0)
            {

                finalSurface = character.GetSurface(closestSurface);

                if (finalSurface != null) testReference = finalSurface.GetSamplePointReference(closestVertex);

            }

        }

    }

    protected void Update()
    {
        
        if (testReference != null && testReference.Valid)
        {

            transform.position = finalSurface.GetSamplePointUnsafe(testReference).position;

        }

    }

    public void OnDrawGizmos()
    {

        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(transform.position, 0.05f);

    }

}
