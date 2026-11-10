using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using nugen.mesh;

namespace nugen.modding
{

    [ExecuteInEditMode]
    public class MeshBakeAndCombine : MonoBehaviour
    {

        public bool apply = false;

        public bool destroyInputs = true;

        public string newAssetPath = "";

        public bool gatherChildren = false;

        public List<MeshFilter> meshFilters = new List<MeshFilter>();

        public List<SkinnedMeshRenderer> skinnedRenderers = new List<SkinnedMeshRenderer>();

        void Update()
        {

            if (gatherChildren)
            {

                gatherChildren = false;

                MeshFilter[] filters = gameObject.GetComponentsInChildren<MeshFilter>();

                meshFilters.AddRange(filters);

                SkinnedMeshRenderer[] renderers = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();

                skinnedRenderers.AddRange(renderers);

            }

            if (apply)
            {

                apply = false;

                MeshFilter destinationFilter = gameObject.AddOrGetComponent<MeshFilter>();

                List<MeshHelper.MeshBakeInput> inputs = new List<MeshHelper.MeshBakeInput>();

                foreach (MeshFilter filter in meshFilters) inputs.Add(new MeshHelper.MeshBakeInput(filter));

                foreach (SkinnedMeshRenderer renderer in skinnedRenderers) inputs.Add(new MeshHelper.MeshBakeInput(renderer));

                MeshHelper.CombinedBake(destinationFilter, destroyInputs, inputs.ToArray());

#if UNITY_EDITOR
                destinationFilter.sharedMesh = destinationFilter.sharedMesh.SaveAsset(newAssetPath, gameObject.name);
#endif

                GameObject.DestroyImmediate(this);

            }

        }

    }

}
