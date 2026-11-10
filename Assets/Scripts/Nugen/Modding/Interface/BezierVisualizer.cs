using UnityEngine;


namespace nugen.modding
{

    [ExecuteInEditMode]
    public class BezierVisualizer : Widget
    {

        public bool resizeWithCameraDistance = false;

        public bool drawControlPointLines = false;

        public float width = 0.001f;

        public int precision = 10;

        public BezierPath path;

        public LineRenderer line;

        public LineRenderer controlPointLines;

        void OnDestroy()
        {

            if (controlPointLines != null) GameObject.DestroyImmediate(controlPointLines.gameObject);

        }

        public void RefreshLine()
        {

            if (path == null || path.path == null) return;

            if (line == null) line = gameObject.AddOrGetComponent<LineRenderer>();

            line.positionCount = precision;

            for(int a = 0; a < precision; a++)
            {

                line.SetPosition(a, transform.TransformPoint(path.Evaluate(a / (precision - 1f))));

            }

            line.sharedMaterial = ModUI.GetVisualizerMaterial();

            if (controlPointLines == null)
            {

                controlPointLines = new GameObject("cpLines").AddComponent<LineRenderer>();

                controlPointLines.transform.SetParent(transform, false);
                controlPointLines.transform.localPosition = Vector3.zero;
                controlPointLines.transform.localRotation = Quaternion.identity;
                controlPointLines.transform.localScale = Vector3.one;

            }

            controlPointLines.positionCount = path.path.Length;

            for (int a = 0; a < controlPointLines.positionCount; a++)
            {

                controlPointLines.SetPosition(a, transform.TransformPoint(path.path[a]));

            }

            controlPointLines.sharedMaterial = ModUI.GetAuxiliaryMaterial();

        }

        void Update()
        {

            if (path == null) return;

            if (line == null || controlPointLines == null) RefreshLine();

            Vector3 pos = Vector3.zero;

            Vector3[] points = new Vector3[line.positionCount];

            line.GetPositions(points);

            for (int a = 0; a < points.Length; a++)
            {

                pos += points[a];

            }

            pos = pos / points.Length;

            line.startWidth = line.endWidth = width * (resizeWithCameraDistance ? (Application.isPlaying ? Mathf.Min(1, (Camera.main.transform.position - pos).magnitude) : 1) : 1);

            controlPointLines.startWidth = controlPointLines.endWidth = line.startWidth * 0.75f;

            controlPointLines.enabled = drawControlPointLines;

        }

    }

}