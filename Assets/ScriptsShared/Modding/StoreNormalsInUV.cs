using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

using Swole;

using Swole.DataStructures;


[ExecuteInEditMode]
public class StoreNormalsInUV : MonoBehaviour
{

    public bool apply;

    public bool tangentSpace = true;

    public bool mergeNormals = true;

    public bool recalculateNormals = true;

    new public SkinnedMeshRenderer renderer;
    public MeshFilter filter;

    public UVChannelURP targetChannel = UVChannelURP.UV3;

    public bool packXY = true;
    public bool storeInZW = true;

    protected void Update()
    {

        if (apply)
        {

            apply = false;

            if (tangentSpace) StoreInTangentSpace(targetChannel, mergeNormals, recalculateNormals, packXY, storeInZW); else Store(targetChannel, mergeNormals, recalculateNormals, packXY, storeInZW);

        }

    }

    public void Store(UVChannelURP targetChannel, bool mergeNormals = false, bool recalculateNormals = false, bool packXY = false, bool storeInZW = false)
    {

        if (renderer == null) 
        { 
            
            if (filter != null)
            {

                StoreFilter(targetChannel, mergeNormals, recalculateNormals, packXY, storeInZW);

                return;

            }

            renderer = gameObject.GetComponent<SkinnedMeshRenderer>(); 
        
        }

        if (renderer == null) 
        {

            StoreFilter(targetChannel, mergeNormals, recalculateNormals, packXY, storeInZW);

            return;

        }

        Store(renderer, targetChannel, mergeNormals, recalculateNormals, packXY, storeInZW);

    }

    public void StoreFilter(UVChannelURP targetChannel, bool mergeNormals = false, bool recalculateNormals = false, bool packXY = false, bool storeInZW = false)
    {

        if (filter == null) filter = gameObject.GetComponent<MeshFilter>();

        if (filter == null) return;

        Store(filter, targetChannel, mergeNormals, recalculateNormals, packXY, storeInZW);

    }

    public static void Store(SkinnedMeshRenderer renderer, UVChannelURP targetChannel, bool mergeNormals = false, bool recalculateNormals = false, bool packXY = false, bool storeInZW = false)
    {

        renderer.sharedMesh = Store(renderer.sharedMesh, targetChannel, mergeNormals, recalculateNormals, packXY, storeInZW);

    }

    public static void Store(MeshFilter filter, UVChannelURP targetChannel, bool mergeNormals = false, bool recalculateNormals = false, bool packXY = false, bool storeInZW = false)
    {

        filter.sharedMesh = Store(filter.sharedMesh, targetChannel, mergeNormals, recalculateNormals, packXY, storeInZW);

    }

    public static Mesh Store(Mesh inputMesh, UVChannelURP targetChannel, bool mergeNormals = false, bool recalculateNormals = false, bool packXY = false, bool storeInZW = false)
    {

        if (inputMesh == null) return null;

        Mesh mesh = MeshUtils.DuplicateMesh(inputMesh);

        Vector3[] normals = mesh.normals;

        if (recalculateNormals)
        {

            Mesh temp = MeshUtils.DuplicateMesh(mesh);
            temp.RecalculateNormals();

            normals = temp.normals;

        }

        if (recalculateNormals || mergeNormals)
        {

            var clones = MeshUtils.CalculateClones(mesh.vertices);

            for (int a = 0; a < mesh.vertexCount; a++) 
            {

                if (clones[a].first != a) continue;

                Vector3 normal = Vector3.zero;

                VertexClone clone = clones[a];

                for (int b = 0; b < clone.indices.Length; b++) normal = normal + normals[clone.indices[b]];

                normal = normal.normalized;

                for (int b = 0; b < clone.indices.Length; b++) normals[clone.indices[b]] = normal;

            }

        }

        List<Vector4> uvs = new List<Vector4>();

        mesh.GetUVs((int)targetChannel, uvs);
        if (uvs.Count < mesh.vertexCount)
        {
            for (int a = uvs.Count; a < mesh.vertexCount; a++) uvs.Add(Vector4.zero);
        }

        for (int a = 0; a < mesh.vertexCount; a++)
        {

            Vector4 uv = uvs[a];

            if (packXY)
            {

                if (storeInZW)
                {

                    uv.z = Data.FloatPack(math.remap(-1, 1, 0, 1, normals[a].x), math.remap(-1, 1, 0, 1, normals[a].y));
                    uv.w = normals[a].z;

                }
                else
                {

                    uv.x = Data.FloatPack(math.remap(-1, 1, 0, 1, normals[a].x), math.remap(-1, 1, 0, 1, normals[a].y));
                    uv.y = normals[a].z;

                }

            }
            else
            {

                uv.x = normals[a].x;
                uv.y = normals[a].y;
                uv.z = normals[a].z;

            }

            uvs[a] = uv;

        } 

        mesh.SetUVs((int)targetChannel, uvs); 

        return mesh;

    }

    public void StoreInTangentSpace(UVChannelURP targetChannel, bool mergeNormals = false, bool recalculateNormals = false, bool packXY = false, bool storeInZW = false)
    {

        if (renderer == null)
        {

            if (filter != null)
            {

                StoreInTangentSpaceFilter(targetChannel, mergeNormals, recalculateNormals, packXY, storeInZW);

                return;

            }

            renderer = gameObject.GetComponent<SkinnedMeshRenderer>();

        }

        if (renderer == null)
        {

            StoreInTangentSpaceFilter(targetChannel, mergeNormals, recalculateNormals, packXY, storeInZW);

            return;

        }

        if (recalculateNormals) 
        { 
            
            StoreRecalculatedNormalsInTangentSpace(renderer, targetChannel, mergeNormals, packXY, storeInZW);

            return;
        
        }

        StoreInTangentSpace(renderer, null, targetChannel, mergeNormals, packXY, storeInZW);

    }

    public void StoreInTangentSpaceFilter(UVChannelURP targetChannel, bool mergeNormals = false, bool recalculateNormals = false, bool packXY = false, bool storeInZW = false)
    {

        if (filter == null) filter = gameObject.GetComponent<MeshFilter>();

        if (filter == null) return;

        if (recalculateNormals)
        {

            StoreRecalculatedNormalsInTangentSpace(filter, targetChannel, mergeNormals, packXY, storeInZW);

            return;

        }

        StoreInTangentSpace(filter, null, targetChannel, mergeNormals, packXY, storeInZW); 

    }

    public static Mesh StoreRecalculatedNormalsInTangentSpace(Mesh inputMesh, UVChannelURP targetChannel, bool mergeNormals = false, bool packXY = false, bool storeInZW = false)
    {

        Mesh temp = MeshUtils.DuplicateMesh(inputMesh);
        temp.RecalculateNormals();

        return StoreInTangentSpace(inputMesh, temp.normals, targetChannel, mergeNormals, packXY, storeInZW);

    }

    public static void StoreRecalculatedNormalsInTangentSpace(SkinnedMeshRenderer renderer, UVChannelURP targetChannel, bool mergeNormals = false, bool packXY = false, bool storeInZW = false)
    {

        renderer.sharedMesh = StoreRecalculatedNormalsInTangentSpace(renderer.sharedMesh, targetChannel, mergeNormals, packXY, storeInZW);

    }

    public static void StoreRecalculatedNormalsInTangentSpace(MeshFilter filter, UVChannelURP targetChannel, bool mergeNormals = false, bool packXY = false, bool storeInZW = false)
    {

        filter.sharedMesh = StoreRecalculatedNormalsInTangentSpace(filter.sharedMesh, targetChannel, mergeNormals, packXY, storeInZW);

    }

    public static void StoreInTangentSpace(SkinnedMeshRenderer renderer, Vector3[] normalsToStore, UVChannelURP targetChannel, bool mergeNormals = false, bool packXY = false, bool storeInZW = false)
    {

        renderer.sharedMesh = StoreInTangentSpace(renderer.sharedMesh, normalsToStore, targetChannel, mergeNormals, packXY, storeInZW);

    }

    public static void StoreInTangentSpace(MeshFilter filter, Vector3[] normalsToStore, UVChannelURP targetChannel, bool mergeNormals = false, bool packXY = false, bool storeInZW = false)
    {

        filter.sharedMesh = StoreInTangentSpace(filter.sharedMesh, normalsToStore, targetChannel, mergeNormals, packXY, storeInZW);

    }

    public static Mesh StoreInTangentSpace(Mesh inputMesh, Vector3[] normalsToStore, UVChannelURP targetChannel, bool mergeNormals = false, bool packXY = false, bool storeInZW = false)
    {

        if (inputMesh == null) return null;

        Mesh mesh = MeshUtils.DuplicateMesh(inputMesh);

        Vector3[] normals = mesh.normals;
        Vector4[] tangents = mesh.tangents;

        if (normalsToStore == null) normalsToStore = (Vector3[])normals.Clone();

        if (mergeNormals)
        {

            var clones = MeshUtils.CalculateClones(mesh.vertices);

            for (int a = 0; a < mesh.vertexCount; a++)
            {

                if (clones[a].first != a) continue;

                Vector3 normal = Vector3.zero;

                VertexClone clone = clones[a];

                for (int b = 0; b < clone.indices.Length; b++) normal = normal + normalsToStore[clone.indices[b]];

                normal = normal.normalized;

                for (int b = 0; b < clone.indices.Length; b++) normalsToStore[clone.indices[b]] = normal;

            }

        }

        List<Vector4> uvs = new List<Vector4>();

        mesh.GetUVs((int)targetChannel, uvs);
        if (uvs.Count < mesh.vertexCount)
        {
            for (int a = uvs.Count; a < mesh.vertexCount; a++) uvs.Add(Vector4.zero);
        }

        if (uvs.Count < mesh.vertexCount)
        {

            uvs.Clear();

            for (int a = 0; a < mesh.vertexCount; a++) uvs.Add(Vector4.zero);

        }

        for (int a = 0; a < mesh.vertexCount; a++)
        {

            Vector3 normal = normalsToStore[a];

            normal = Maths.ObjectSpaceToTangentSpaceOffset(normal, normals[a], tangents[a]);

            Vector4 uv = uvs[a];

            if (packXY)
            {

                if (storeInZW)
                {

                    uv.z = Data.FloatPack(math.remap(-1, 1, 0, 1, normal.x), math.remap(-1, 1, 0, 1, normal.y));
                    uv.w = normal.z;

                }
                else
                {

                    uv.x = Data.FloatPack(math.remap(-1, 1, 0, 1, normal.x), math.remap(-1, 1, 0, 1, normal.y));
                    uv.y = normal.z;

                }

            }
            else
            {

                uv.x = normal.x;
                uv.y = normal.y;
                uv.z = normal.z;

            }

            uvs[a] = uv;

        }

        mesh.SetUVs((int)targetChannel, uvs);

        return mesh;

    }

}
