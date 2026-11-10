using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using nugen.mesh;

namespace nugen.modding
{

    public class SplineFromVertices : MonoBehaviour
    {

        public bool refresh = false;

        public bool useSelectionOrder = false;

        public bool convertToObjectPlacement = false;

        public SplineObjectPlacement ConvertToObjectPlacement()
        {

            if (selector != null) GameObject.DestroyImmediate(selector);

            if (visualizer != null) GameObject.DestroyImmediate(visualizer);

            SplineObjectPlacement sop = gameObject.AddOrGetComponent<SplineObjectPlacement>();

            sop.path = path;

            GameObject.DestroyImmediate(this);

            return sop;

        }

        public float distanceEXP = 1;

        public float dotInfluence = 0.25f;

        public MeshFilter target_filter;

        public SkinnedMeshRenderer target;

        public VertexSelector selector;

        public QuadraticBezierPath path;

        public BezierVisualizer visualizer;

        private float refreshT = 0;

        private void Start()
        {

            if (selector != null)
            {

                if (target_filter == null) target_filter = selector.filter_target;

                if (target == null) target = selector.target;

            }

        }

        private void Update()
        {

            if (path == null) path = new QuadraticBezierPath();

            if (convertToObjectPlacement)
            {

                convertToObjectPlacement = false;

                ConvertToObjectPlacement();

                return;

            }

            if (visualizer == null) visualizer = gameObject.AddOrGetComponent<BezierVisualizer>();

            visualizer.path = path;

            if (selector != null)
            {

                if (target_filter == null) target_filter = selector.filter_target;

                if (target == null) target = selector.target;

            }

            if (selector == null) selector = gameObject.AddOrGetComponent<VertexSelector>();

            selector.target = target;

            selector.filter_target = target_filter;

            if (refresh)
            {

                refreshT += Time.deltaTime;

                if (refreshT > 0.5)
                {

                    refreshT = 0;

                }
                else return;

            }
            else return;

            UpdatePath();

            UpdateVisualizer();

        }

        public void UpdateVisualizer()
        {

            if (visualizer == null) visualizer = gameObject.AddOrGetComponent<BezierVisualizer>();

            visualizer.path = path;

            visualizer.precision = path.path.Length * 5;

            visualizer.RefreshLine();

        }

        public void UpdatePath()
        {

            int[] selection = selector.GetSelection();

            UpdatePath(selection);

        }

        public void UpdatePath(int[] selection)
        {

            if (target == null && target_filter == null) return;

            bool isFilter = target == null;

            if (path == null) path = new QuadraticBezierPath();

            path.path = new Vector3[Mathf.Max(3, selection.Length)];

            if (selection.Length == 0) return;

            Vector3[] vertices;

            if (isFilter)
            {

                vertices = target_filter.sharedMesh.vertices;

                for (int a = 0; a < vertices.Length; a++) vertices[a] = target_filter.transform.TransformPoint(vertices[a]);

            } 
            else
            {

                vertices = RealtimeMesh.GetTransformedVertices(target);

            }

            if (useSelectionOrder) {

                for(int a = 0; a < selection.Length; a++)
                {

                    int i = selection[a];

                    if (i < 0 || i >= vertices.Length) Debug.Log(i + " / " + vertices.Length);

                    path.path[a] = transform.InverseTransformPoint(vertices[selection[a]]);

                }

            } 
            else
            {

                Vector3 center = Vector3.zero;

                for (int a = 0; a < selection.Length; a++) center += vertices[selection[a]];

                center = center / selection.Length;

                float md = -1;

                int start = -1;

                for(int a = 0; a < selection.Length; a++)
                {

                    float d = (vertices[selection[a]] - center).sqrMagnitude;

                    if (start < 0 || d > md)
                    {

                        start = a;

                        md = d;

                    }

                }

                List<int> newOrder = new List<int>();

                newOrder.Add(start);

                Vector3 prevDir = Vector3.zero;

                int current = start;

                while (current >= 0)
                {

                    float mc = -1;

                    int n = -1;

                    Vector3 ndir = Vector3.zero;

                    for (int a = 0; a < selection.Length; a++)
                    {

                        if (newOrder.Contains(a)) continue;

                        Vector3 offset = vertices[selection[a]] - vertices[selection[current]];

                        float mag = offset.magnitude;

                        if (mag == 0) continue;

                        Vector3 dir = offset / mag;

                        float dot = current == start ? 1 : Vector3.Dot(prevDir, dir);

                        float cost = Mathf.Pow(mag, distanceEXP);

                        cost = cost - cost * dot * dotInfluence;

                        if (n < 0 || cost < mc)
                        {

                            mc = cost;

                            n = a;

                        }

                    }

                    if (n < 0) break;

                    prevDir = ndir;

                    current = n;

                    newOrder.Add(n);

                }

                for (int a = 0; a < path.path.Length; a++)
                {

                    path.path[a] = transform.InverseTransformPoint(vertices[selection[newOrder[a >= newOrder.Count ? (newOrder.Count - 1) : a]]]);

                }

            }

        }

    }

}
