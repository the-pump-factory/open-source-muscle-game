using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;

namespace nugen.animation
{

    public class Poser : MonoBehaviour
    {

        [Serializable]
        public class Pose
        {

            public string name;

            [Serializable]
            public struct TransformData
            {

                public string[] path;

                public Vector3 localPosition;

                public Quaternion localRotation;

                public Vector3 localScale;

                public Transform GetTransform(Transform root)
                {

                    for (int a = 0; a < path.Length; a++) root = root.Find(path[a]);

                    return root;

                }

            }

            public TransformData[] data;

            public Pose(string name)
            {

                this.name = name;

            }

            public void Apply(Transform root, float t)
            {

                foreach (TransformData td in data)
                {

                    Transform trn = root;

                    for (int a = 0; a < td.path.Length; a++) trn = trn.Find(td.path[a]);

                    trn.localPosition = trn.localPosition + td.localPosition * t;

                    trn.localRotation = Quaternion.Lerp(trn.localRotation, td.localRotation * trn.localRotation, t);

                    trn.localScale = trn.localScale + td.localScale * t;

                }

            }

            public void Revert(Transform root, float t)
            {

                foreach (TransformData td in data)
                {

                    Transform trn = root;

                    for (int a = 0; a < td.path.Length; a++) trn = trn.Find(td.path[a]);

                    trn.localPosition = trn.localPosition - td.localPosition * t;

                    trn.localRotation = Quaternion.Lerp(trn.localRotation, Quaternion.Inverse(td.localRotation) * trn.localRotation, t);

                    trn.localScale = trn.localScale - td.localScale * t;

                }

            }

        }

        [HideInInspector]
        public Pose defaultPose;

        [HideInInspector]
        public List<Pose> poses;

        [System.Serializable]
        public class PoseWeight
        {

            public string name;

            [Range(0, 1.5f)]
            public float weight;

            [HideInInspector]
            public float prevWeight;

        }

        public List<PoseWeight> poseWeights;

        public void Initialize()
        {

            defaultPose = new Pose("default");

            List<Pose.TransformData> defaultData = new List<Pose.TransformData>();

            void AddDefaultTransformData(Transform t, List<string> path)
            {

                defaultData.Add(new Pose.TransformData() { path = path.ToArray(), localPosition = t.localPosition, localRotation = t.localRotation, localScale = t.localScale });

                for (int a = 0; a < t.childCount; a++)
                {

                    Transform child = t.GetChild(a);

                    List<string> path_ = new List<string>();

                    path_.AddRange(path);

                    path_.Add(child.name);

                    AddDefaultTransformData(child, path_);

                }

            }

            AddDefaultTransformData(transform, new List<string>());

            defaultPose.data = defaultData.ToArray();

        }

        void Update()
        {

            if (poses == null) return;

            if (poseWeights == null || poseWeights.Count != poses.Count)
            {

                poseWeights = new List<PoseWeight>();

                for(int a = 0; a < poses.Count; a++)
                {

                    poseWeights.Add(new PoseWeight() { name = poses[a].name, weight = 0, prevWeight = 0 });

                }

            }

            for (int a = 0; a < poseWeights.Count; a++)
            {

                PoseWeight weight = poseWeights[a];

                if (weight.prevWeight == weight.weight) continue;

                Pose pose = poses[a];

                pose.Revert(transform, weight.prevWeight);

                pose.Apply(transform, weight.weight);

                weight.prevWeight = weight.weight;

                poseWeights[a] = weight;

            }

        }

        public void ResetTransforms()
        {

            if (defaultPose == null) return;

            foreach (Pose.TransformData data in defaultPose.data)
            {

                Transform trn = transform;

                if (data.path != null) for (int a = 0; a < data.path.Length; a++) trn = trn.Find(data.path[a]);

                trn.localPosition = data.localPosition;
                trn.localRotation = data.localRotation;
                trn.localScale = data.localScale;

            }

            if (poseWeights != null)
            {

                for (int a = 0; a < poseWeights.Count; a++)
                {

                    PoseWeight weight = poseWeights[a];

                    weight.weight = 0;
                    weight.prevWeight = 0;

                }

            }

        }

    }
}
