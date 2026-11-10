using System.Collections;
using System.Collections.Generic;

using UnityEngine;


namespace nugen.modding
{

    public class VertexSelector : MonoBehaviour
    {

        public SkinnedMeshRenderer target;

        public MeshFilter filter_target;

        new public BakedMeshCollider collider;

        public float UIsize = 0.01f;

        public float maxUIsize = 0.035f;

        public float minUIsize = 0.001f;

        public float maxDistance = 0.5f;

        public float updateRate = 0.5f;

        private GameObject UIcontainer;

        private GameObject cubeUI;

        private MeshRenderer[] cubes;

        private List<int> selected_vertices = new List<int>();

        private float tick = 0;

        private Vector3 lastCameraPosition;

        private Vector3[] vertices;

        private int[] triangles;

        private bool isSubscribed = false;

        public void Subscribe(bool subscribe = true)
        {

            try
            {

                if (isSubscribed)
                {

                    ModdingInterfaceBehaviour ui = ModUI.behaviour;

                    if (ui == null || subscribe) return;

                    ui.OnRaycastClick -= OnClick;

                    isSubscribed = false;

                }
                else
                {

                    ModdingInterfaceBehaviour ui = ModUI.GetBehaviour();

                    if (ui == null || !subscribe) return;

                    ui.OnRaycastClick += OnClick;

                    isSubscribed = true;

                }

            }
            catch { }

        }

        void Awake()
        {

            if (target == null && filter_target == null) target = gameObject.GetComponent<SkinnedMeshRenderer>();

            if (target == null && filter_target == null) filter_target = gameObject.GetComponent<MeshFilter>(); 

            cubeUI = GameObject.CreatePrimitive(PrimitiveType.Cube);

            Collider collider = cubeUI.GetComponent<Collider>();

            if (collider != null) GameObject.DestroyImmediate(collider);

            UIcontainer = new GameObject("UI");

            UIcontainer.transform.SetParent(gameObject.transform, false);

            UIcontainer.transform.localPosition = Vector3.zero;

        }

        public bool IsFilter()
        {

            return target == null;

        }

        public void CheckUI()
        {

            Mesh mesh = IsFilter() ? filter_target.sharedMesh : target.sharedMesh;

            if (cubes == null || cubes.Length == 0)
            {

                cubes = new MeshRenderer[mesh.vertexCount];

                selected_vertices = new List<int>();

                for (int a = 0; a < mesh.vertexCount; a++) { 
                    
                    GameObject obj = GameObject.Instantiate(cubeUI);

                    obj.transform.SetParent(UIcontainer.transform, false);

                    MeshRenderer renderer = obj.GetComponent<MeshRenderer>();

                    renderer.sharedMaterial = ModUI.GetDefaultMaterial();

                    cubes[a] = renderer;
                
                }

            }

        }

        public void UpdateUI()
        {

            CheckUI();

            collider.UpdateMesh();

            vertices = collider.GetVertices();

            triangles = collider.GetTriangles();

            Vector3 pos = collider.transform.position;

            for (int a = 0; a < cubes.Length; a++)
            {

                MeshRenderer cube = cubes[a];

                cube.transform.position = collider.transform.TransformPoint(vertices[a]);

            }

        }

        void Update()
        {

            if ((target == null && filter_target == null) || (target != null && target.sharedMesh == null) || (filter_target != null && filter_target.sharedMesh == null))
            {

                GameObject.Destroy(this);

                return;

            }

            Subscribe(true);

            if (tick < updateRate)
            {

                tick += Time.deltaTime;

                return;

            }

            tick = 0;

            if (collider == null) {

                if (IsFilter())
                {

                    collider = filter_target.gameObject.AddOrGetComponent<BakedMeshCollider>();

                    collider.referenceFilter = filter_target;

                } 
                else
                {

                    collider = target.gameObject.AddOrGetComponent<BakedMeshCollider>();

                    collider.referenceRenderer = target;

                }

                collider.UpdateMesh();

                UpdateUI();
            
            }

            if (lastCameraPosition == Camera.main.transform.position) return;

            lastCameraPosition = Camera.main.transform.position;

            CheckUI();

            Mesh mesh = IsFilter() ? filter_target.sharedMesh : target.sharedMesh;

            float mD = maxDistance * maxDistance;

            for(int a = 0; a < cubes.Length; a++)
            {

                bool isSelected = selected_vertices.Contains(a);

                MeshRenderer cube = cubes[a];

                float distance = (cube.transform.position - Camera.main.transform.position).sqrMagnitude;

                if (!isSelected && distance > mD)
                {

                    cube.enabled = false;

                    continue;

                }

                cube.enabled = true;

                distance = Mathf.Clamp(distance * UIsize, minUIsize, maxUIsize);

                cube.transform.localScale = new Vector3(distance, distance, distance);

            }

        }

        public void Select(int index)
        {

            if (index < 0 || index >= cubes.Length || selected_vertices.Contains(index)) return;

            selected_vertices.Add(index);

            cubes[index].sharedMaterial = ModUI.GetSelectionMaterial();

        }

        public void Deselect(int index)
        {

            if (index < 0 || index >= cubes.Length) return;

            selected_vertices.RemoveAll(i => i == index);

            cubes[index].sharedMaterial = ModUI.GetDefaultMaterial();

        }

        public void SelectAll()
        {

            for (int a = 0; a < cubes.Length; a++)
            {

                if (selected_vertices.Contains(a)) continue;

                selected_vertices.Add(a);

                cubes[a].sharedMaterial = ModUI.GetSelectionMaterial();

            }

        }

        public void DeselectAll()
        {

            selected_vertices.Clear();

            for(int a = 0; a < cubes.Length; a++)
            {

                cubes[a].sharedMaterial = ModUI.GetDefaultMaterial();

            }

        }

        public int[] GetSelection()
        {

            int[] array = new int[selected_vertices.Count];

            selected_vertices.CopyTo(array);

            return array;

        }

        public void OnClick(int button, ModUI.ClickType clickType, bool clickedObject, RaycastHit hit)
        {

            if (!clickedObject) return;

            if (button < 2 && hit.collider == collider.GetMeshCollider())
            {

                if (vertices == null) vertices = collider.GetVertices();

                if (triangles == null) triangles = collider.GetTriangles();

                int i1 = hit.triangleIndex * 3;
                int i2 = i1 + 1;
                int i3 = i1 + 2;

                i1 = triangles[i1];
                i2 = triangles[i2];
                i3 = triangles[i3];

                int select = i1;

                Vector3 v1 = collider.transform.TransformPoint(vertices[i1]);
                Vector3 v2 = collider.transform.TransformPoint(vertices[i2]);
                Vector3 v3 = collider.transform.TransformPoint(vertices[i3]);

                float d1 = (v1 - hit.point).sqrMagnitude;
                float d2 = (v2 - hit.point).sqrMagnitude;
                float d3 = (v3 - hit.point).sqrMagnitude;

                if (d1 < d2 && d1 < d3)
                {

                    select = i1;

                }
                else if (d2 < d1 && d2 < d3)
                {

                    select = i2;

                }
                else
                {

                    select = i3;

                }

                if (button ==  0) Select(select); else if (button == 1) Deselect(select);

            }

        }

        private void OnDestroy()
        {

            Subscribe(false);

            if (cubes != null)
            {

                for(int a = 0; a < cubes.Length; a++)
                {

                    MeshRenderer renderer = cubes[a];

                    if (renderer == null) continue;

                    GameObject.DestroyImmediate(renderer.gameObject);

                }

            }

        }

    }

}