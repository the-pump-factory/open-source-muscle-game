using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.Mathematics;

namespace nugen.animation
{

    [ExecuteInEditMode]
    public class SimpleTransformDriver : AnimationBasedComponent
    {

        [System.Serializable]
        public struct KeyFrame
        {

            [Range(0, 1)]
            public float position;

            public AnimationCurve transitionCurve;

            [HideInInspector]
            public Vector4 value;

            public Quaternion AsQuaternion()
            {

                return new Quaternion(value.x, value.y, value.z, value.w);

            }

            public KeyFrame Lerp(KeyFrame f2, float t)
            {

                t = f2.transitionCurve == null ? t : f2.transitionCurve.Evaluate(t);

                return new KeyFrame() { position = position + (f2.position - position) * t, transitionCurve = null, value = Vector4.Lerp(value, f2.value, t) };

            }

        }

#if UNITY_EDITOR

        public float inputFramePosition;

        public bool resetTransform = false;
        public bool addPositionKeyFrame;
        public bool addRotationKeyFrame;
#endif

        public bool initialized;

        public bool mirrorRangeIfMirrored = true;

#if UNITY_EDITOR
        public float currentAngle;
#endif

        public Transform driverTransform;

        public Vector3 localDriverAxis;

        public bool unsigned = true;

        [Range(-180, 180)]
        public float minRange = 0;

        [Range(-180, 180)]
        public float maxRange = 180;

        public KeyFrame[] localPositionFrames;

        public KeyFrame[] localRotationFrames;

        [HideInInspector]
        public KeyFrame basePositionFrame;

        [HideInInspector]
        public KeyFrame baseRotationFrame;

        [HideInInspector]
        public Quaternion initialDriverRotation;

#if UNITY_EDITOR

        public void AddKeyFrame(ref KeyFrame[] array, KeyFrame keyFrame)
        {

            KeyFrame[] newArray = new KeyFrame[array.Length + 1];

            array.CopyTo(newArray, 0);

            newArray[array.Length] = keyFrame;

            array = newArray;

        }

        public void AddPositionKeyFrame()
        {

            AddKeyFrame(ref localPositionFrames, new KeyFrame() { position = inputFramePosition, transitionCurve = AnimationCurve.Linear(0, 0, 1, 1), value = transform.localPosition });

        }

        public void AddRotationKeyFrame()
        {

            AddKeyFrame(ref localRotationFrames, new KeyFrame() { position = inputFramePosition, transitionCurve = AnimationCurve.Linear(0, 0, 1, 1), value = new Vector4(transform.localRotation.x, transform.localRotation.y, transform.localRotation.z, transform.localRotation.w) });

        }

#endif

        public void Initialize()
        {

            if (driverTransform == null) return;

            initialized = true;

            initialDriverRotation = Quaternion.Inverse(driverTransform.transform.localRotation);

            basePositionFrame = new KeyFrame() { position = 0, transitionCurve = null, value = transform.localPosition };

            baseRotationFrame = new KeyFrame() { position = 0, transitionCurve = null, value = new Vector4(transform.localRotation.x, transform.localRotation.y, transform.localRotation.z, transform.localRotation.w) };

        }

        public void Rearrange()
        {

            if (localPositionFrames != null)
            {

                List<KeyFrame> localPositionFrames_ = new List<KeyFrame>();

                for (int a = 0; a < localPositionFrames.Length; a++)
                {

                    KeyFrame kf = localPositionFrames[a];

                    bool append = true;

                    for (int b = 0; b < localPositionFrames_.Count; b++)
                    {

                        if (localPositionFrames_[b].position > kf.position)
                        {

                            append = false;

                            localPositionFrames_.Insert(b, kf);

                            break;

                        }

                    }

                    if (append) localPositionFrames_.Add(kf);

                }

                localPositionFrames = localPositionFrames_.ToArray();

            }
            else localPositionFrames = new KeyFrame[0];

            if (localRotationFrames != null)
            {

                List<KeyFrame> localRotationFrames_ = new List<KeyFrame>();

                for (int a = 0; a < localRotationFrames.Length; a++)
                {

                    KeyFrame kf = localRotationFrames[a];

                    bool append = true;

                    for (int b = 0; b < localRotationFrames_.Count; b++)
                    {

                        if (localRotationFrames_[b].position > kf.position)
                        {

                            append = false;

                            localRotationFrames_.Insert(b, kf);

                            break;

                        }

                    }

                    if (append) localRotationFrames_.Add(kf);

                }

                localRotationFrames = localRotationFrames_.ToArray();

            }
            else localRotationFrames = new KeyFrame[0];

        }

        private void Start()
        {

            if (!initialized) Initialize();

            Rearrange();

        }

        private void Update()
        {

            if (!subscribed) Logic(); else if (currentAnimator == null) Unsubscribe();

        }

        [System.NonSerialized] private CustomAnimator currentAnimator;

        protected override void Subscribe()
        {

            if (subscribed) return;

            subscribed = true;

            if (currentAnimator != null) Unsubscribe();

            if (animator != null)
            {

                currentAnimator = animator;

                currentAnimator.postAnimate += Logic;

            }

        }

        protected override void Unsubscribe()
        {

            if (!subscribed) return;

            subscribed = false;
            
            if (currentAnimator != null)
            {

                currentAnimator.postAnimate -= Logic;

            }

        }

        private void Logic()
        {

            if (!initialized) return;

            Vector3 dir = new Vector3(localDriverAxis.z, localDriverAxis.x, localDriverAxis.y);

            Quaternion driverRot = driverTransform.localRotation * initialDriverRotation;

            float angle = Vector3.SignedAngle(dir, driverRot * dir, localDriverAxis);

            if (unsigned) angle = Mathf.Abs(angle);

#if UNITY_EDITOR

            currentAngle = angle;

            if (Application.isPlaying)
            {
#endif

                float t = Mathf.Clamp01((angle - minRange) / (maxRange - minRange));

                KeyFrame pos = basePositionFrame;
                KeyFrame rot = baseRotationFrame;

                for(int a = 1; a < localPositionFrames.Length; a++)
                {

                    float b = a / (localPositionFrames.Length - 1f);

                    if (t <= b)
                    {

                        KeyFrame f1 = localPositionFrames[a - 1];
                        KeyFrame f2 = localPositionFrames[a];

                        pos = f1.Lerp(f2, Mathf.Max(0, t - f1.position) / (f2.position - f1.position));

                        break;

                    }

                }

                for (int a = 1; a < localRotationFrames.Length; a++)
                {

                    float b = a / (localRotationFrames.Length - 1f);

                    if (t <= b)
                    {

                        KeyFrame f1 = localRotationFrames[a - 1];
                        KeyFrame f2 = localRotationFrames[a];

                        rot = f1.Lerp(f2, Mathf.Max(0, t - f1.position) / (f2.position - f1.position));

                        break;

                    }

                }

                transform.localPosition = pos.value;
                transform.localRotation = rot.AsQuaternion();

#if UNITY_EDITOR
            }
#endif

        }

#if UNITY_EDITOR

        private void OnValidate()
        {
            
            if (!initialized)
            {

                Initialize();

            }

            if (initialized)
            {

                if (addPositionKeyFrame)
                {

                    addPositionKeyFrame = false;

                    AddPositionKeyFrame();

                }

                if (addRotationKeyFrame)
                {

                    addRotationKeyFrame = false;

                    AddRotationKeyFrame();

                }

                if (resetTransform)
                {

                    resetTransform = false;

                    transform.localPosition = basePositionFrame.value;
                    transform.localRotation = baseRotationFrame.AsQuaternion();

                    driverTransform.localRotation = Quaternion.Inverse(initialDriverRotation);

                }

            }

        }

#endif

    }

}
