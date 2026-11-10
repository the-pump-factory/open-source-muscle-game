using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Swole;

using Swole.DataStructures;

[ExecuteInEditMode]
public class UVConsolidator : MonoBehaviour
{

    public bool apply;

    new public SkinnedMeshRenderer renderer;

    public UVChannelURP startChannel = UVChannelURP.UV1;

    public UVChannelURP endChannel = UVChannelURP.UV2;

    protected void Update()
    {

        if (apply)
        {

            apply = false;

            Consolidate(startChannel, endChannel);

        }

    }

    public void Consolidate(UVChannelURP startChannel, UVChannelURP endChannel, bool removeExcessChannels = true)
    {

        if (renderer == null) renderer = gameObject.GetComponent<SkinnedMeshRenderer>();

        if (renderer == null) return;

        Consolidate(renderer, startChannel, endChannel, removeExcessChannels);

    }

    public static void Consolidate(SkinnedMeshRenderer renderer, UVChannelURP startChannel, UVChannelURP endChannel, bool removeExcessChannels = true)
    {

        renderer.sharedMesh = Consolidate(renderer.sharedMesh, startChannel, endChannel, removeExcessChannels);

    }

    public static Mesh Consolidate(Mesh inputMesh, UVChannelURP startChannel, UVChannelURP endChannel, bool removeExcessChannels = true)
    {

        if (inputMesh == null) return null;

        Mesh mesh = MeshUtils.DuplicateMesh(inputMesh);

        for(int a = (int)startChannel; a <= (int)endChannel; a++)
        {

            int i = a * 2;

            List<Vector2> uvA = mesh.GetUVsByChannelAsList(i);
            List<Vector2> uvB = mesh.GetUVsByChannelAsList(i + 1);

            Vector4[] uvC = new Vector4[mesh.vertexCount];

            for(int b = 0; b < mesh.vertexCount; b++)
            {

                Vector4 uv = new Vector4();

                if (b < uvA.Count)
                {

                    uv.x = uvA[b].x;
                    uv.y = uvA[b].y;

                }

                if (b < uvB.Count)
                {

                    uv.z = uvB[b].x;
                    uv.w = uvB[b].y;

                }

                uvC[b] = uv;

            }

            mesh.SetUVs(a, uvC);

        }

        if (removeExcessChannels)
        {

            for (int a = (int)endChannel + 1; a < 8; a++) mesh.SetUVs(a, new List<Vector2>());

        }

        return mesh;

    }

}
