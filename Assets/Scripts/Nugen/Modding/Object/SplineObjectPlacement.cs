using System.Collections.Generic;
using UnityEngine;

using nugen.random;

namespace nugen.modding
{

    [ExecuteInEditMode]
    public class SplineObjectPlacement : MonoBehaviour
    {

        [System.Serializable]
        public enum InputAxis
        {

            X, Y, Z

        }

        [System.Serializable]
        public struct ObjectInput
        {

            public InputAxis axis;

            public GameObject gameObject;

            public ObjectInput(InputAxis axis, GameObject gameObject)
            {
                this.axis = axis;
                this.gameObject = gameObject;
            }

        }

        public bool refresh = false;

        public int seed = -1;

        public int repetitions = 0;

        public bool randomize = false;

        public bool randomizePlacement = false;

        public ObjectInput[] objects = new ObjectInput[0];

        public float twist = 0;

        public float angle = 0;

        public float scale = 1;

        public Vector3 angleJitter = Vector3.zero;

        public Vector3 scaleJitter = Vector3.zero;

        public AnimationCurve angleCurve = AnimationCurve.Linear(0, 1, 1, 1);

        public AnimationCurve twistCurve = AnimationCurve.Linear(0, 0, 1, 1);

        public AnimationCurve distributionCurve = AnimationCurve.Linear(0, 0, 1, 1);

        public AnimationCurve scaleCurve = AnimationCurve.Linear(0, 1, 1, 1);

        public AnimationCurve angleJitterCurve = AnimationCurve.Linear(0, 1, 1, 1);

        public AnimationCurve scaleJitterCurve = AnimationCurve.Linear(0, 1, 1, 1);

        public BezierPath path;

        public BezierVisualizer visualizer;

        public List<TransformWidget> widgets = new List<TransformWidget>();

        public int randomPlacementIndexOffset = 1;

        public int angleJitterXIndexOffset1 = 2;
        public int angleJitterXIndexOffset2 = 4;

        public int angleJitterYIndexOffset1 = 6;
        public int angleJitterYIndexOffset2 = 8;

        public int angleJitterZIndexOffset1 = 10;
        public int angleJitterZIndexOffset2 = 12;

        public int scaleJitterIndexOffset1 = 234;
        public int scaleJitterIndexOffset2 = 567;

        private List<GameObject> instances = new List<GameObject>();

        private GeneratedSequence sequence;

        private float refreshT = 0;

        void Start()
        {

            if (visualizer == null) visualizer = gameObject.AddComponent<BezierVisualizer>();

            visualizer.resizeWithCameraDistance = true;

            if (path == null) path = new CubicBezierPath();

            visualizer.path = path;

        }

        private void Update()
        {

            if (refresh)
            {

                refreshT += Time.deltaTime;

                if (refreshT > 0.5)
                {

                    refreshT = 0;

                    Refresh();

                }

            }

            if (path == null) path = new CubicBezierPath();

            if (widgets.Count != path.path.Length) RefreshWidgets();

            UpdatePath();

            if (visualizer == null) visualizer = gameObject.AddOrGetComponent<BezierVisualizer>();

            visualizer.resizeWithCameraDistance = true;

            visualizer.path = path;

            visualizer.RefreshLine();

        }

        public void Refresh()
        {

            if (visualizer == null || objects == null || objects.Length == 0) return;

            if (path == null) path = new CubicBezierPath();

            visualizer.path = path;

            if (seed <= 0) seed = Random.Range(1, int.MaxValue);

            if (sequence == null || sequence.seed != seed)
            {

                sequence = new GeneratedSequence(seed, 56562, 3274, 100000, 10000);

            }

            if (instances != null)
            {

                for (int a = 0; a < instances.Count; a++)
                {

                    GameObject go = instances[a];

                    if (go == null) continue;

                    GameObject.DestroyImmediate(go);

                }

                instances.Clear();

            }
            else instances = new List<GameObject>();

            List<ObjectInput> objs = new List<ObjectInput>();

            for(int a = 0; a < repetitions + 1; a++)
            {

                ObjectInput[] inputs = (ObjectInput[])objects.Clone();

                if (randomize)
                {

                    for(int b = 0; b < inputs.Length; b++)
                    {

                        int c = Mathf.RoundToInt(sequence.GetRangeAt(b + inputs.Length * a) * (inputs.Length - 1));

                        ObjectInput oi = inputs[c];

                        inputs[c] = inputs[b];

                        inputs[b] = oi;

                    }

                }

                objs.AddRange(inputs);

            }

            float nobj = Mathf.Max(1, objs.Count - 1);

            float increment = 1 / nobj;

            for (int a = 0; a < objs.Count; a++)
            {

                float t = randomizePlacement ? sequence.GetRangeAt(randomPlacementIndexOffset + a) : (a / nobj);

                float pt = distributionCurve.Evaluate(t);

                float dpt = a == 0 ? distributionCurve.Evaluate(t + increment) : distributionCurve.Evaluate(t - increment);

                Vector3 pathPosition = path.Evaluate(pt);

                Vector3 pathDirection = (a == 0 ? (path.Evaluate(dpt) - pathPosition) : (pathPosition - path.Evaluate(dpt))).normalized;

                //Vector3 pathTangent = new Vector3(pathDirection.z, pathDirection.x, pathDirection.y);

                //Vector3 pathTangent2 = Vector3.Cross(pathDirection, pathTangent).normalized;

                float ang = (angle + twistCurve.Evaluate(t) * twist) * angleCurve.Evaluate(t);

                float sca = scaleCurve.Evaluate(t) * scale;

                ObjectInput input = objs[a];

                GameObject obj = Instantiate(input.gameObject);

                GameObject container = new GameObject("obj_" + a);

                container.transform.SetParent(transform, false);

                container.transform.position = (transform.rotation * pathPosition) + transform.position;

                obj.transform.SetParent(container.transform, false);

                obj.transform.localPosition = Vector3.zero;
                obj.transform.localRotation = Quaternion.identity;

                obj.transform.localScale = (Vector3.one * sca) + (scaleJitter * scaleJitterCurve.Evaluate(t) * (sequence.GetRangeAt(a + scaleJitterIndexOffset1) - sequence.GetRangeAt(a + scaleJitterIndexOffset2)));

                Quaternion toLocal = Quaternion.identity;

                Vector3 jitter;

                switch (input.axis)
                {

                    case InputAxis.X:

                        jitter = new Vector3(
                            angleJitter.x * (sequence.GetRangeAt(a + angleJitterXIndexOffset1) - sequence.GetRangeAt(a + angleJitterXIndexOffset2)), 
                            angleJitter.y * (sequence.GetRangeAt(a + angleJitterYIndexOffset1) - sequence.GetRangeAt(a + angleJitterYIndexOffset2)), 
                            angleJitter.z * (sequence.GetRangeAt(a + angleJitterZIndexOffset1) - sequence.GetRangeAt(a + angleJitterZIndexOffset2)));

                        toLocal = Quaternion.FromToRotation(Vector3.right, pathDirection) * Quaternion.Euler(new Vector3(ang, 0, 0) + jitter * angleJitterCurve.Evaluate(t));

                        break;

                    case InputAxis.Y:

                        jitter = new Vector3(
                            angleJitter.x * (sequence.GetRangeAt(a + angleJitterXIndexOffset1) - sequence.GetRangeAt(a + angleJitterXIndexOffset2)),
                            angleJitter.y * (sequence.GetRangeAt(a + angleJitterYIndexOffset1) - sequence.GetRangeAt(a + angleJitterYIndexOffset2)),
                            angleJitter.z * (sequence.GetRangeAt(a + angleJitterZIndexOffset1) - sequence.GetRangeAt(a + angleJitterZIndexOffset2)));

                        toLocal = Quaternion.FromToRotation(Vector3.up, pathDirection) * Quaternion.Euler(new Vector3(0, ang, 0) + jitter * angleJitterCurve.Evaluate(t));

                        break;

                    case InputAxis.Z:

                        jitter = new Vector3(
                            angleJitter.x * (sequence.GetRangeAt(a + angleJitterXIndexOffset1) - sequence.GetRangeAt(a + angleJitterXIndexOffset2)),
                            angleJitter.y * (sequence.GetRangeAt(a + angleJitterYIndexOffset1) - sequence.GetRangeAt(a + angleJitterYIndexOffset2)),
                            angleJitter.z * (sequence.GetRangeAt(a + angleJitterZIndexOffset1) - sequence.GetRangeAt(a + angleJitterZIndexOffset2)));

                        toLocal = Quaternion.FromToRotation(Vector3.forward, pathDirection) * Quaternion.Euler(new Vector3(0, 0, ang) + jitter * angleJitterCurve.Evaluate(t));

                        break;

                }

                obj.transform.localRotation = toLocal;

                instances.Add(container);

            }

        }

        public void RefreshWidgets()
        {

            if (widgets != null) for (int a = 0; a < widgets.Count; a++) if (widgets[a] != null) GameObject.DestroyImmediate(widgets[a].gameObject);

            widgets.Clear();

            for (int a = 0; a < path.path.Length; a++)
            {

                TransformWidget handle = new GameObject("widget_" + a).AddComponent<TransformWidget>();

                handle.transform.SetParent(gameObject.transform, false);

                handle.transform.position = transform.TransformPoint(path.path[a]);

                widgets.Add(handle);

                handle.Initialize();

                handle.primitive.transform.localScale = Vector3.one * 0.005f;

            }

        }

        public void UpdatePath()
        {

            for (int a = 0; a < path.path.Length; a++)
            {

                TransformWidget widget = widgets[a];

                if (widget == null) { 
                    
                    RefreshWidgets();

                    widget = widgets[a];
                
                }

                path.path[a] = transform.InverseTransformPoint(widget.transform.position);

                widget.transform.position = transform.TransformPoint(path.path[a]);

            }

        }

    }

}
