using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;


using Swole.Morphing;

using Unity.Mathematics;

namespace Swole.API.Unity
{

    public class GenitalController : MonoBehaviour
    {

#if UNITY_EDITOR

        private float prevBustSize;
        [SerializeField, Range(0, 1)]
        private float bustSizeVal;

        private float prevPenisLength;
        [SerializeField, Range(0, 1)]
        private float penisLengthVal;

        private float prevPenisGirth;
        [SerializeField, Range(0, 1)]
        private float penisGirthVal;

        private float prevPenisErection;
        [SerializeField, Range(0, 1)]
        private float penisErectionVal;

        private float prevTesticleHeight;
        [SerializeField, Range(0, 1)]
        private float testicleHeightVal;

        private float prevTesticleSize;
        [SerializeField, Range(0, 1)]
        private float testicleSizeVal;

        private float prevClitLength;
        [SerializeField, Range(0, 1)]
        private float clitLengthVal;

        private float prevClitGirth;
        [SerializeField, Range(0, 1)]
        private float clitGirthVal;

        private float prevClitErection;
        [SerializeField, Range(0, 1)]
        private float clitErectionVal;

        protected void OnValidate()
        {
            if (prevBustSize != bustSizeVal)
            {
                prevBustSize = bustSizeVal;

                SetBustSize(bustSizeVal);
            }

            if (prevPenisLength != penisLengthVal || prevPenisGirth != penisGirthVal)
            {
                prevPenisLength = penisLengthVal;
                prevPenisGirth = penisGirthVal;

                SetPenisLengthGirth(penisLengthVal, penisGirthVal, true, true, true, true); 
            }

            if (prevPenisErection != penisErectionVal)
            {
                prevPenisErection = penisErectionVal;

                SetPenisErectionFactor(penisErectionVal, true); 
            }

            if (prevTesticleHeight != testicleHeightVal || prevTesticleSize != testicleSizeVal)
            {
                prevTesticleHeight = testicleHeightVal;
                prevTesticleSize = testicleSizeVal;

                SetTestesHeightAndSize(testicleHeightVal, testicleSizeVal, true, true);  
            }

            if (prevClitLength != clitLengthVal || prevClitGirth != clitGirthVal)
            {
                prevClitLength = clitLengthVal;
                prevClitGirth = clitGirthVal;

                SetClitLengthGirth(clitLengthVal, clitGirthVal, true, true, true);
            }

            if (prevClitErection != clitErectionVal)
            {
                prevClitErection = clitErectionVal;

                SetClitErectionFactor(clitErectionVal, true);
            }
        }
#endif

        private readonly List<TransformStateDelta> tempTransformStates = new List<TransformStateDelta>();

        [Serializable]
        public struct PenisPoint
        {
            public Transform bone;
            public float extensionRatio;
            public float girthRatio;
            public float pitchRatio;
        }

        [Serializable]
        public struct DynamicBonePhysicsSettings
        {
            public float damping;

            public float elasticity;

            public float stiffness;

            public float inertia;

            public float friction;

            public float radius;

            public float gravity;

            [Range(0, 1)]
            public float blendWeight;

            public DynamicBonePhysicsSettings Mix(DynamicBonePhysicsSettings otherSettings, float t)
            {
                DynamicBonePhysicsSettings val = this;

                float clampedT = Mathf.Clamp01(t);
                val.damping = Mathf.LerpUnclamped(damping, otherSettings.damping, t);
                val.elasticity = Mathf.LerpUnclamped(elasticity, otherSettings.elasticity, t);
                val.stiffness = Mathf.LerpUnclamped(stiffness, otherSettings.stiffness, t);
                val.inertia = Mathf.LerpUnclamped(inertia, otherSettings.inertia, t); 
                val.friction = Mathf.LerpUnclamped(friction, otherSettings.friction, t);
                val.radius = Mathf.LerpUnclamped(radius, otherSettings.radius, t); 
                val.gravity = Mathf.LerpUnclamped(gravity, otherSettings.gravity, t);
                val.blendWeight = Mathf.LerpUnclamped(blendWeight, otherSettings.blendWeight, clampedT);

                return val;
            }
            public static DynamicBonePhysicsSettings Lerp(DynamicBonePhysicsSettings settingsA, DynamicBonePhysicsSettings settingsB, float t) => settingsA.Mix(settingsB, t);
        }
        [Serializable]
        public class DynamicBoneLargeSmallPhysicsSettings
        {

            public DynamicBonePhysicsSettings smallSettings;
            public DynamicBonePhysicsSettings largeSettings;

            public DynamicBonePhysicsSettings FromSize(float size) => DynamicBonePhysicsSettings.Lerp(smallSettings, largeSettings, size);

            public float DampingFromSize(float size) => Mathf.LerpUnclamped(smallSettings.damping, largeSettings.damping, size);
            public float ElasticityFromSize(float size) => Mathf.LerpUnclamped(smallSettings.elasticity, largeSettings.elasticity, size);
            public float StiffnessFromSize(float size) => Mathf.LerpUnclamped(smallSettings.stiffness, largeSettings.stiffness, size);
            public float InertiaFromSize(float size) => Mathf.LerpUnclamped(smallSettings.inertia, largeSettings.inertia, size);
            public float FrictionFromSize(float size) => Mathf.LerpUnclamped(smallSettings.friction, largeSettings.friction, size);
            public float RadiusFromSize(float size) => Mathf.LerpUnclamped(smallSettings.radius, largeSettings.radius, size);
            public float GravityFromSize(float size) => Mathf.LerpUnclamped(smallSettings.gravity, largeSettings.gravity, size);

        }
        [Serializable]
        public class DynamicBoneLargeSmallPhysicsSettingsWithCurves : DynamicBoneLargeSmallPhysicsSettings
        {

            public AnimationCurve dampingCurve;

            public AnimationCurve elasticityCurve;

            public AnimationCurve stiffnessCurve;

            public AnimationCurve inertiaCurve;

            public AnimationCurve frictionCurve;

            public AnimationCurve radiusCurve;

        }

        [Serializable]
        public struct PenisBoneData
        {
            public float length;
            public Vector3 offset;

            public Vector3 initLocalPosition;
            public Quaternion initLocalRotation;
            public Vector3 initLocalScale;
        }

        [Header("Penis")]
        public CustomizableCharacterMesh penisMesh;
        public string circumciseShape;
        [Range(0, 1)]
        public float circumciseForeskinRetractMix = 0.5f;
        public string[] foreskinRetractShapes;

        private int circumciseShapeIndex;
        private int[] foreskinRetractShapesIndices;

        public AnimationCurve foreskinRetractErectionCurve;

        private bool penisCircumcised;
        public void SetCircumcised(bool flag)
        {
            penisCircumcised = flag; 

            if (penisMesh != null)
            {
                if (foreskinRetractShapesIndices != null && foreskinRetractShapesIndices.Length > 0)
                {
                    foreach(var ind in foreskinRetractShapesIndices)
                    {
                        if (ind >= 0) penisMesh.SetStandaloneShapeWeightUnsafe(ind, 0);
                    } 

                    if (circumciseShapeIndex >= 0) penisMesh.SetStandaloneShapeWeightUnsafe(circumciseShapeIndex, 1 - circumciseForeskinRetractMix);

                    int finalIndex = foreskinRetractShapesIndices[foreskinRetractShapesIndices.Length - 1];
                    if (finalIndex >= 0) penisMesh.SetStandaloneShapeWeightUnsafe(finalIndex, circumciseForeskinRetractMix);
                } 
                else
                {
                    if (circumciseShapeIndex >= 0) penisMesh.SetStandaloneShapeWeightUnsafe(circumciseShapeIndex, 1);
                }
            }
        }

        public PenisPoint[] penisBones;
        private PenisBoneData[] penisData;

        public Vector3 penisExtensionAxis = Vector3.up;
        public Vector3 penisPitchAxis = Vector3.right;

        public DynamicBoneLargeSmallPhysicsSettingsWithCurves flaccidPenisPhysicsSettings;
        public DynamicBoneLargeSmallPhysicsSettings erectPenisPhysicsSettings;

        public float penisMinLengthRatio = 0.3f;
        public float penisMaxLengthRatio = 2f;

        public float penisMinGirthRatio = 0.8f;
        public float penisMaxGirthRatio = 1.4f;

        public float penisErectionLengthAdd = 0.05f;
        public float penisErectionGirthAdd = 0.05f;

        [NonSerialized]
        private DynamicBone penisPhysics;
        [NonSerialized]
        private DynamicBonePhysicsSettings currentFlaccidPenisPhysicsSettings;
        [NonSerialized]
        private DynamicBonePhysicsSettings currentErectPenisPhysicsSettings;
        [NonSerialized]
        private DynamicBonePhysicsSettings currentPenisPhysicsSettings;
        [NonSerialized]
        private float penisLength;
        [NonSerialized]
        private float penisGirth;
        [NonSerialized]
        private float penisErectionFactor;

        public float PenisSizeLengthBiased => Mathf.Min((penisLength * 0.7f) + (penisGirth * 0.3f), 1.25f);
        public float PenisSizeGirthBiased => Mathf.Min((penisLength * 0.3f) + (penisGirth * 0.7f), 1.25f);

        public void SetPenisLengthGirth(float lengthNormalized, float girthNormalized) => SetPenisLengthGirth(lengthNormalized, girthNormalized, true, true, true, true);
        public void SetPenisLengthGirth(float lengthNormalized, float girthNormalized, bool resetTransforms, bool preservePose, bool reapplyErection, bool updateTestes)
        {
            penisLength = lengthNormalized;
            penisGirth = girthNormalized;

            if (preservePose)
            { 
                tempTransformStates.Clear();
                foreach (var bone in penisBones) tempTransformStates.Add(new TransformState(bone.bone));
            }

            if (resetTransforms) penisPhysics.InitTransforms(); // resets the transforms to original positions

            /*if (preservePose) // causes jittering when reapplied
            {
                for (int a = 0; a < penisBones.Length; a++) tempTransformStates[a] = tempTransformStates[a] - (TransformStateDelta)new TransformState(penisBones[a].bone);
            }*/

            float transformPenisLength = penisLength + (penisErectionLengthAdd * penisErectionFactor);
            float transformPenisGirth = penisGirth + (penisErectionGirthAdd * penisErectionFactor);
            float lengthRatio = Mathf.LerpUnclamped(penisMinLengthRatio, penisMaxLengthRatio, transformPenisLength) - 1;
            float girthRatio = Mathf.LerpUnclamped(penisMinGirthRatio, penisMaxGirthRatio, transformPenisGirth) - 1;    
            for (int a = 0; a < penisBones.Length; a++)
            {
                var point = penisBones[a];
                var data = penisData[a];

                if (a > 0)
                {
                    int b = a - 1;

                    var prevPoint = penisBones[b];

                    float length = data.length * lengthRatio * point.extensionRatio;
                    point.bone.position = prevPoint.bone.TransformPoint(data.offset) + point.bone.TransformDirection(penisExtensionAxis) * length;
                }
                else
                {
                    point.bone.localPosition = data.initLocalPosition;
                }

                point.bone.localScale = data.initLocalScale + Vector3.one * girthRatio * point.girthRatio;  
            }

            float sizeLengthBiased = PenisSizeLengthBiased;
            float sizeGirthBiased = PenisSizeGirthBiased;

            currentPenisPhysicsSettings = currentFlaccidPenisPhysicsSettings = flaccidPenisPhysicsSettings.FromSize(sizeLengthBiased);
            currentErectPenisPhysicsSettings = erectPenisPhysicsSettings.FromSize(sizeLengthBiased);

            penisPhysics.m_Damping = currentPenisPhysicsSettings.damping; 
            penisPhysics.m_Elasticity = currentPenisPhysicsSettings.elasticity;
            penisPhysics.m_Stiffness = currentPenisPhysicsSettings.stiffness;
            penisPhysics.m_Inert = currentPenisPhysicsSettings.inertia;
            penisPhysics.m_Friction = currentPenisPhysicsSettings.friction;
            penisPhysics.m_Radius = flaccidPenisPhysicsSettings.RadiusFromSize(penisGirth);
            penisPhysics.m_Gravity = Vector3.down * flaccidPenisPhysicsSettings.GravityFromSize(sizeGirthBiased);
            penisPhysics.m_BlendWeight = currentPenisPhysicsSettings.blendWeight;

            penisPhysics.m_DampingDistrib = flaccidPenisPhysicsSettings.dampingCurve;
            penisPhysics.m_ElasticityDistrib = flaccidPenisPhysicsSettings.elasticityCurve;
            penisPhysics.m_StiffnessDistrib = flaccidPenisPhysicsSettings.stiffnessCurve; 
            penisPhysics.m_InertDistrib = flaccidPenisPhysicsSettings.inertiaCurve;
            penisPhysics.m_FrictionDistrib = flaccidPenisPhysicsSettings.frictionCurve;
            penisPhysics.m_RadiusDistrib = flaccidPenisPhysicsSettings.radiusCurve;

            if (resetTransforms) penisPhysics.SetupParticles(); else penisPhysics.UpdateParameters();

            if (preservePose)
            {
                for (int a = 0; a < penisBones.Length; a++) ((TransformState)tempTransformStates[a]).ApplyLocalRotation(penisBones[a].bone);

                penisPhysics.ResetParticlesPosition(); 
                tempTransformStates.Clear();
            }

            if (reapplyErection) SetPenisErectionFactor(penisErectionFactor, false);

            if (updateTestes) SetTestesHeightAndSize(testesHeight, testesSize, true, true);
        }

        public void SetPenisErectionFactor(float factor) => SetPenisErectionFactor(factor, true);
        public void SetPenisErectionFactor(float factor, bool reapplyLengthAndGirth)
        {
            penisErectionFactor = Mathf.Clamp01(factor);

            if (reapplyLengthAndGirth)
            {
                SetPenisLengthGirth(penisLength, penisGirth, true, true, false, false);
            }

            currentPenisPhysicsSettings = currentFlaccidPenisPhysicsSettings.Mix(currentErectPenisPhysicsSettings, penisErectionFactor);   

            float sizeGirthBiased = PenisSizeGirthBiased;
            penisPhysics.m_Damping = currentPenisPhysicsSettings.damping;
            penisPhysics.m_Elasticity = currentPenisPhysicsSettings.elasticity;
            penisPhysics.m_Stiffness = currentPenisPhysicsSettings.stiffness;
            penisPhysics.m_Inert = currentPenisPhysicsSettings.inertia; 
            penisPhysics.m_Friction = currentPenisPhysicsSettings.friction;
            penisPhysics.m_Radius = Mathf.LerpUnclamped(flaccidPenisPhysicsSettings.RadiusFromSize(penisGirth), erectPenisPhysicsSettings.RadiusFromSize(penisGirth), penisErectionFactor);
            penisPhysics.m_Gravity = Vector3.down * Mathf.LerpUnclamped(flaccidPenisPhysicsSettings.GravityFromSize(sizeGirthBiased), erectPenisPhysicsSettings.GravityFromSize(sizeGirthBiased), penisErectionFactor);
            penisPhysics.m_BlendWeight = currentPenisPhysicsSettings.blendWeight;

            penisPhysics.UpdateParameters();

            if (!penisCircumcised && penisMesh != null && foreskinRetractShapesIndices != null && foreskinRetractShapesIndices.Length > 0)
            {

                float retractFactor = penisErectionFactor;
                if (foreskinRetractErectionCurve != null && foreskinRetractErectionCurve.length > 0) retractFactor = foreskinRetractErectionCurve.Evaluate(retractFactor);

                if (foreskinRetractShapesIndices.Length == 1)
                {
                    int ind = foreskinRetractShapesIndices[0];
                    if (ind >= 0) penisMesh.SetStandaloneShapeWeightUnsafe(ind, retractFactor);
                } 
                else
                {
                    float step = 1f / foreskinRetractShapesIndices.Length;
                    int lengthMinus1 = foreskinRetractShapesIndices.Length - 1;
                    for (int a = lengthMinus1; a >= 0; a--)
                    {
                        int ind = foreskinRetractShapesIndices[a];
                        if (ind < 0) continue;

                        int b = a + 1;
                        
                        float weightStart = (a + 1) * step;
                        float weightEnd = (b + 1) * step;
                        if (a == 0 && retractFactor < weightStart)
                        {
                            penisMesh.SetStandaloneShapeWeightUnsafe(ind, retractFactor / weightStart);  
                        }
                        else if (a < lengthMinus1 && retractFactor >= weightStart && (retractFactor < weightEnd || b >= lengthMinus1))
                        {
                            float t = (retractFactor - weightStart) / (weightEnd - weightStart);

                            penisMesh.SetStandaloneShapeWeightUnsafe(ind, 1 - t);

                            int ind2 = foreskinRetractShapesIndices[b];
                            if (ind2 >= 0) penisMesh.SetStandaloneShapeWeightUnsafe(ind2, t);
                        }
                        else
                        {
                            penisMesh.SetStandaloneShapeWeightUnsafe(ind, 0);  
                        }
                    }
                }
            }
        }

        [Header("Testicles")]
        public Transform testesRoot;
        public Transform testesL;
        public Transform testesR;

        private Vector3 testesOffsetDefault;
        private Vector3 testesScaleDefault;

        public bool addTestesPhysicsToRootParent = true;
        public bool createTestesPhysicsRootParent = true;
        public bool excludeTestesLR = true;

        public float testesHeightRange = 0.075f;
        public float testesSizeAddHeightRatio = 0.4f;
        public Vector3 testesHeightAxis = Vector3.up;

        public Vector3 testesColliderOffset;
        public float testesColliderRadiusMin = 0.02f;
        public float testesColliderRadiusMax = 0.03f;
        public float testesColliderRadiusNerfShortPenis = 0.8f;

        public DynamicBoneLargeSmallPhysicsSettingsWithCurves testesPhysicsSettings;
        [NonSerialized]
        private DynamicBoneCollider testesCollider;
        public DynamicBoneCollider TestesCollider => testesCollider;

        [NonSerialized]
        private DynamicBone testesPhysics;

        [NonSerialized]
        private DynamicBonePhysicsSettings currentTestesPhysicsSettings;

        public float testesMinHeightRatio = 0.9f;
        public float testesMaxHeightRatio = 1.6f;

        public float testesMinSizeRatio = 0.9f;
        public float testesMaxSizeRatio = 1.4f;

        private float testesHeight;
        private float testesSize;

        public float TestesPhysicsWeightHeightBiased => Mathf.Min((testesHeight * 0.7f) + (testesSize * 0.3f), 1.25f); 
        public float TestesPhysicsWeightSizeBiased => Mathf.Min((testesHeight * 0.3f) + (testesSize * 0.7f), 1.25f);  

        public void SetTestesHeightAndSize(float heightNormalized, float sizeNormalized) => SetTestesHeightAndSize(heightNormalized, sizeNormalized, true, true);
        public void SetTestesHeightAndSize(float heightNormalized, float sizeNormalized, bool resetTransforms, bool preservePose)
        {
            testesHeight = heightNormalized;
            testesSize = sizeNormalized;

            if (preservePose)
            {
                tempTransformStates.Clear();
                if (testesRoot != null) 
                {
                    tempTransformStates.Add(new TransformState(testesRoot.parent));
                    tempTransformStates.Add(new TransformState(testesRoot));
                }
                if (testesL != null) tempTransformStates.Add(new TransformState(testesL));
                if (testesR != null) tempTransformStates.Add(new TransformState(testesR));
            }

            if (resetTransforms) testesPhysics.InitTransforms(); // resets the transforms

            float heightRatio = Mathf.LerpUnclamped(testesMinHeightRatio, testesMaxHeightRatio, testesHeight) - 1;
            float sizeRatio = Mathf.LerpUnclamped(testesMinSizeRatio, testesMaxSizeRatio, testesSize) - 1;
            float height = (testesHeightRange * heightRatio) + (testesHeightRange * sizeRatio * testesSizeAddHeightRatio);
            testesRoot.position = testesRoot.parent.TransformPoint(testesOffsetDefault) + testesRoot.TransformDirection(testesHeightAxis) * height;
            testesRoot.localScale = testesScaleDefault + Vector3.one * sizeRatio;

            testesCollider.m_Center = testesColliderOffset;
            testesCollider.m_Radius = Mathf.LerpUnclamped(testesColliderRadiusMin, testesColliderRadiusMax, sizeRatio) * Mathf.LerpUnclamped(1, testesColliderRadiusNerfShortPenis, Mathf.Max(0, 1 - penisLength));

            float weightHeightBiased = TestesPhysicsWeightHeightBiased;
            float weightSizeBiased = TestesPhysicsWeightSizeBiased;

            currentTestesPhysicsSettings = testesPhysicsSettings.FromSize(weightHeightBiased);

            testesPhysics.m_Damping = currentTestesPhysicsSettings.damping;
            testesPhysics.m_Elasticity = currentTestesPhysicsSettings.elasticity;
            testesPhysics.m_Stiffness = currentTestesPhysicsSettings.stiffness;
            testesPhysics.m_Inert = currentTestesPhysicsSettings.inertia;
            testesPhysics.m_Friction = currentTestesPhysicsSettings.friction;
            testesPhysics.m_Radius = testesPhysicsSettings.RadiusFromSize(testesSize);
            testesPhysics.m_Gravity = Vector3.down * testesPhysicsSettings.GravityFromSize(weightSizeBiased);
            testesPhysics.m_BlendWeight = currentTestesPhysicsSettings.blendWeight;

            testesPhysics.m_DampingDistrib = testesPhysicsSettings.dampingCurve;
            testesPhysics.m_ElasticityDistrib = testesPhysicsSettings.elasticityCurve;
            testesPhysics.m_StiffnessDistrib = testesPhysicsSettings.stiffnessCurve;
            testesPhysics.m_InertDistrib = testesPhysicsSettings.inertiaCurve;
            testesPhysics.m_FrictionDistrib = testesPhysicsSettings.frictionCurve;
            testesPhysics.m_RadiusDistrib = testesPhysicsSettings.radiusCurve;

            if (resetTransforms) testesPhysics.SetupParticles(); else testesPhysics.UpdateParameters();

            if (preservePose)
            {
                if (testesRoot != null)
                {
                    ((TransformState)tempTransformStates[0]).ApplyLocalRotation(testesRoot.parent);
                    ((TransformState)tempTransformStates[1]).ApplyLocalRotation(testesRoot);
                }
                if (testesL != null) ((TransformState)tempTransformStates[2]).ApplyLocalRotation(testesL);
                if (testesR != null) ((TransformState)tempTransformStates[3]).ApplyLocalRotation(testesR);

                testesPhysics.ResetParticlesPosition();
                tempTransformStates.Clear();
            }
        }

        [Header("Breasts")]
        public List<CustomizableCharacterMesh> breastMeshes;

        public Transform breastL;
        public Transform breastR;

        public Transform breastEndL;
        public Transform breastEndR;
         
        public DynamicBoneLargeSmallPhysicsSettingsWithCurves breastPhysicsSettings; 

        public void SetBustSize(float size)
        {
            if (breastMeshes != null)
            {
                foreach(var bm in breastMeshes) bm.SetBustSize(size);
            }
        }

        [Header("Vulva")]
        public CustomizableCharacterMesh vulvaMesh;

        public string clitUngirthShape;
        [NonSerialized]
        private int clitUngirthShapeIndex;

        public ClitPoint[] clitBones;
        public ClitHoodPoint[] clitHoodBones;

        [NonSerialized]
        private ProxyBone clitHoodProxy;

        [NonSerialized]
        private PenisBoneData[] clitData;
        [NonSerialized]
        private PenisBoneData[] clitHoodData;

        [Serializable]
        public struct ClitPoint
        {
            public Transform bone;

            public float extensionRatio;
            public float girthRatio;
            public float pitchRatio;

            public Vector3 minLengthOffset;
            public Vector3 minLengthRotation;

            public Vector3 maxLengthOffset;
            public Vector3 maxLengthRotation;

            public AnimationCurve lengthRemapCurve;
            [Range(0, 1)]
            public float lengthMixGirth;

            public Vector3 minGirthOffset;
            public Vector3 minGirthRotation;

            public Vector3 maxGirthOffset;
            public Vector3 maxGirthRotation;

            public AnimationCurve girthRemapCurve;
            [Range(0, 1)]
            public float girthMixLength;
        }

        [Serializable]
        public struct ClitHoodPoint
        {
            public Transform bone;

            public float proxyWeight;
            public float proxyPositionWeight;
            public float proxyRotationWeight;
            public float proxyScaleWeight;

            public Vector3 minLengthOffset;
            public Vector3 minLengthRotation;

            public Vector3 maxLengthOffset;
            public Vector3 maxLengthRotation;

            public AnimationCurve lengthRemapCurve;
            [Range(0, 1)]
            public float lengthMixGirth;

            public Vector3 minGirthOffset;
            public Vector3 minGirthRotation;

            public Vector3 maxGirthOffset;
            public Vector3 maxGirthRotation;

            public AnimationCurve girthRemapCurve;
            [Range(0, 1)]
            public float girthMixLength;
        }

        public Vector3 clitExtensionAxis = Vector3.up;
        public Vector3 clitPitchAxis = Vector3.right;

        public DynamicBoneLargeSmallPhysicsSettingsWithCurves flaccidClitPhysicsSettings;
        public DynamicBoneLargeSmallPhysicsSettings erectClitPhysicsSettings;

        public float clitMinLengthRatio = 0.3f;
        public float clitMaxLengthRatio = 2f;

        public float clitMinGirthRatio = 0.8f;
        public float clitMaxGirthRatio = 1.4f;

        public float clitErectionLengthAdd = 0.05f;
        public float clitErectionGirthAdd = 0.05f;

        [NonSerialized]
        private DynamicBone clitPhysics;
        [NonSerialized]
        private DynamicBonePhysicsSettings currentFlaccidClitPhysicsSettings;
        [NonSerialized]
        private DynamicBonePhysicsSettings currentErectClitPhysicsSettings;
        [NonSerialized]
        private DynamicBonePhysicsSettings currentClitPhysicsSettings;
        [NonSerialized]
        private float clitLength;
        [NonSerialized]
        private float clitGirth;
        [NonSerialized]
        private float clitErectionFactor;

        public float ClitSizeLengthBiased => Mathf.Min((clitLength * 0.7f) + (clitGirth * 0.3f), 1.25f);
        public float ClitSizeGirthBiased => Mathf.Min((clitLength * 0.3f) + (clitGirth * 0.7f), 1.25f);

        public void SetClitLengthGirth(float lengthNormalized, float girthNormalized) => SetClitLengthGirth(lengthNormalized, girthNormalized, true, true, true);
        public void SetClitLengthGirth(float lengthNormalized, float girthNormalized, bool resetTransforms, bool preservePose, bool reapplyErection)
        {
            clitLength = lengthNormalized;
            clitGirth = girthNormalized;

            if (preservePose)
            {
                tempTransformStates.Clear();
                foreach (var bone in clitBones) tempTransformStates.Add(new TransformState(bone.bone));
                foreach (var bone in clitHoodBones) tempTransformStates.Add(new TransformState(bone.bone));
            }

            if (resetTransforms)
            {
                clitPhysics.InitTransforms(); // resets the transforms to original positions
                for(int a = 0; a < clitHoodBones.Length; a++)
                {
                    var bone = clitHoodBones[a];
                    var data = clitHoodData[a];

                    bone.bone.SetLocalPositionAndRotation(data.initLocalPosition, data.initLocalRotation);
                    bone.bone.localScale = data.initLocalScale; 
                }
            }

            if (preservePose)
            {
                int count = clitBones.Length;
                //for (int a = 0; a < count; a++) tempTransformStates[a] = tempTransformStates[a] - (TransformStateDelta)new TransformState(clitBones[a].bone); // causes jittering when reapplied
                for (int a = 0; a < clitHoodBones.Length; a++) tempTransformStates[a + count] = tempTransformStates[a + count] - (TransformStateDelta)new TransformState(clitHoodBones[a].bone);
            }

            float transformClitLength = clitLength + (clitErectionLengthAdd * clitErectionFactor);
            float transformClitGirth = clitGirth + (clitErectionGirthAdd * clitErectionFactor);
            float lengthRatio = Mathf.LerpUnclamped(clitMinLengthRatio, clitMaxLengthRatio, transformClitLength) - 1;
            float girthRatio = Mathf.LerpUnclamped(clitMinGirthRatio, clitMaxGirthRatio, transformClitGirth) - 1; 
            for (int a = 0; a < clitBones.Length; a++)
            {
                var point = clitBones[a];
                var data = clitData[a];

                if (a > 0)
                {
                    int b = a - 1;

                    var prevPoint = clitBones[b];

                    float length = data.length * lengthRatio * point.extensionRatio;
                    point.bone.position = prevPoint.bone.TransformPoint(data.offset) + point.bone.TransformDirection(clitExtensionAxis) * length;
                }
                else
                {
                    point.bone.localPosition = data.initLocalPosition;
                }

                point.bone.localScale = data.initLocalScale + Vector3.one * girthRatio * point.girthRatio;

                point.bone.GetLocalPositionAndRotation(out var localPosition, out var localRotation);

                float lengthMix = point.lengthRemapCurve != null && point.lengthRemapCurve.length > 0 ? point.lengthRemapCurve.Evaluate(clitLength) : clitLength;
                float girthMix = point.girthRemapCurve != null && point.girthRemapCurve.length > 0 ? point.girthRemapCurve.Evaluate(clitGirth) : clitGirth;

                lengthMix = Mathf.LerpUnclamped(lengthMix, Mathf.Max(lengthMix, girthMix), point.lengthMixGirth);
                girthMix = Mathf.LerpUnclamped(girthMix, Mathf.Max(lengthMix, girthMix), point.girthMixLength);

                localPosition = localPosition + Vector3.LerpUnclamped(point.minLengthOffset, point.maxLengthOffset, lengthMix) + Vector3.LerpUnclamped(point.minGirthOffset, point.maxGirthOffset, girthMix);
                localRotation = Quaternion.SlerpUnclamped(Quaternion.Euler(point.minLengthRotation), Quaternion.Euler(point.maxLengthRotation), lengthMix) * Quaternion.SlerpUnclamped(Quaternion.Euler(point.minGirthRotation), Quaternion.Euler(point.maxGirthRotation), girthMix) * localRotation;

                point.bone.SetLocalPositionAndRotation(localPosition, localRotation);  
            }

            for (int a = 0; a < clitHoodBones.Length; a++)
            {
                var point = clitHoodBones[a];
                var data = clitHoodData[a];

                point.bone.GetLocalPositionAndRotation(out var localPosition, out var localRotation);

                float lengthMix = point.lengthRemapCurve != null && point.lengthRemapCurve.length > 0 ? point.lengthRemapCurve.Evaluate(clitLength) : clitLength;
                float girthMix = point.girthRemapCurve != null && point.girthRemapCurve.length > 0 ? point.girthRemapCurve.Evaluate(clitGirth) : clitGirth;

                lengthMix = Mathf.LerpUnclamped(lengthMix, Mathf.Max(lengthMix, girthMix), point.lengthMixGirth);
                girthMix = Mathf.LerpUnclamped(girthMix, Mathf.Max(lengthMix, girthMix), point.girthMixLength);
                  
                localPosition = localPosition + Vector3.LerpUnclamped(point.minLengthOffset, point.maxLengthOffset, lengthMix) + Vector3.LerpUnclamped(point.minGirthOffset, point.maxGirthOffset, girthMix);
                localRotation = Quaternion.SlerpUnclamped(Quaternion.Euler(point.minLengthRotation), Quaternion.Euler(point.maxLengthRotation), lengthMix) * Quaternion.SlerpUnclamped(Quaternion.Euler(point.minGirthRotation), Quaternion.Euler(point.maxGirthRotation), girthMix) * localRotation;

                point.bone.SetLocalPositionAndRotation(localPosition, localRotation);
            }

            //float sizeLengthBiased = ClitSizeLengthBiased;
            float sizeGirthBiased = ClitSizeGirthBiased;

            currentClitPhysicsSettings = currentFlaccidClitPhysicsSettings = flaccidClitPhysicsSettings.FromSize(sizeGirthBiased);
            currentErectClitPhysicsSettings = erectClitPhysicsSettings.FromSize(sizeGirthBiased);

            clitPhysics.m_Damping = currentClitPhysicsSettings.damping;
            clitPhysics.m_Elasticity = currentClitPhysicsSettings.elasticity;
            clitPhysics.m_Stiffness = currentClitPhysicsSettings.stiffness;
            clitPhysics.m_Inert = currentClitPhysicsSettings.inertia;
            clitPhysics.m_Friction = currentClitPhysicsSettings.friction;
            clitPhysics.m_Radius = flaccidClitPhysicsSettings.RadiusFromSize(clitGirth);
            clitPhysics.m_Gravity = Vector3.down * flaccidClitPhysicsSettings.GravityFromSize(sizeGirthBiased);
            clitPhysics.m_BlendWeight = currentClitPhysicsSettings.blendWeight;

            clitPhysics.m_DampingDistrib = flaccidClitPhysicsSettings.dampingCurve;
            clitPhysics.m_ElasticityDistrib = flaccidClitPhysicsSettings.elasticityCurve;
            clitPhysics.m_StiffnessDistrib = flaccidClitPhysicsSettings.stiffnessCurve;
            clitPhysics.m_InertDistrib = flaccidClitPhysicsSettings.inertiaCurve;
            clitPhysics.m_FrictionDistrib = flaccidClitPhysicsSettings.frictionCurve;
            clitPhysics.m_RadiusDistrib = flaccidClitPhysicsSettings.radiusCurve;

            if (resetTransforms)
            {
                if (clitHoodProxy != null) clitHoodProxy.ReinitializeBindingLocalStartPositions();

                clitPhysics.SetupParticles();
            }
            else 
            { 
                clitPhysics.UpdateParameters();  
            }
             
            if (preservePose)
            { 
                int count = clitBones.Length; 
                for (int a = 0; a < count; a++) ((TransformState)tempTransformStates[a]).ApplyLocalRotation(clitBones[a].bone); 
                for (int a = 0; a < clitHoodBones.Length; a++) tempTransformStates[a + count].ApplyLocal(clitHoodBones[a].bone); 

                clitPhysics.ResetParticlesPosition(); 
                tempTransformStates.Clear();
            }

            if (reapplyErection) SetClitErectionFactor(clitErectionFactor, false);

            if (vulvaMesh != null && vulvaMesh.Instance != null)
            {
                if (clitUngirthShapeIndex >= 0) vulvaMesh.SetStandaloneShapeWeightUnsafe(clitUngirthShapeIndex, Mathf.Max(0, 1 - clitGirth)); 
            }
        }

        public void SetClitErectionFactor(float factor) => SetClitErectionFactor(factor, true);
        public void SetClitErectionFactor(float factor, bool reapplyLengthAndGirth)
        {
            clitErectionFactor = Mathf.Clamp01(factor);

            if (reapplyLengthAndGirth)
            {
                SetClitLengthGirth(clitLength, clitGirth, true, true, false);
            }

            currentClitPhysicsSettings = currentFlaccidClitPhysicsSettings.Mix(currentErectClitPhysicsSettings, clitErectionFactor);

            float sizeGirthBiased = ClitSizeGirthBiased;
            clitPhysics.m_Damping = currentClitPhysicsSettings.damping;
            clitPhysics.m_Elasticity = currentClitPhysicsSettings.elasticity;
            clitPhysics.m_Stiffness = currentClitPhysicsSettings.stiffness;
            clitPhysics.m_Inert = currentClitPhysicsSettings.inertia;
            clitPhysics.m_Friction = currentClitPhysicsSettings.friction;
            clitPhysics.m_Radius = Mathf.LerpUnclamped(flaccidClitPhysicsSettings.RadiusFromSize(clitGirth), erectClitPhysicsSettings.RadiusFromSize(clitGirth), clitErectionFactor);
            clitPhysics.m_Gravity = Vector3.down * Mathf.LerpUnclamped(flaccidClitPhysicsSettings.GravityFromSize(sizeGirthBiased), erectClitPhysicsSettings.GravityFromSize(sizeGirthBiased), clitErectionFactor);
            clitPhysics.m_BlendWeight = currentClitPhysicsSettings.blendWeight;

            clitPhysics.UpdateParameters();
        }

        protected void Awake()
        {
            if (penisBones != null)
            {
                penisData = new PenisBoneData[penisBones.Length];
                for (int a = 0; a < penisBones.Length; a++)
                {
                    int b = a - 1;

                    var point = penisBones[a];

                    var worldPos = point.bone.position;
                    if (a > 0)
                    {
                        var prevPoint = penisBones[b];
                        penisData[a] = new PenisBoneData() { length = (worldPos - prevPoint.bone.position).magnitude, offset = prevPoint.bone.InverseTransformPoint(worldPos), initLocalPosition = point.bone.localPosition, initLocalRotation = point.bone.localRotation, initLocalScale = point.bone.localScale };
                    }
                    else
                    {
                        penisData[a] = new PenisBoneData() { length = 0, offset = Vector3.zero, initLocalPosition = point.bone.localPosition, initLocalRotation = point.bone.localRotation, initLocalScale = point.bone.localScale };
                    }
                }

                if (penisMesh != null)
                {
                    if (!string.IsNullOrWhiteSpace(circumciseShape)) circumciseShapeIndex = penisMesh.CharacterMeshData.IndexOfStandaloneShape(circumciseShape);
                    if (foreskinRetractShapes != null)
                    {
                        foreskinRetractShapesIndices = new int[foreskinRetractShapes.Length];
                        for (int a = 0; a < foreskinRetractShapes.Length; a++) foreskinRetractShapesIndices[a] = penisMesh.CharacterMeshData.IndexOfStandaloneShape(foreskinRetractShapes[a]);
                    }
                }

                penisPhysics = penisBones[0].bone.gameObject.AddComponent<DynamicBone>();
                penisPhysics.SetUseSwoleUpdater(true);
                penisPhysics.m_Root = penisBones[0].bone;

                penisErectionFactor = 0;
                SetPenisLengthGirth((1 - penisMinLengthRatio) / (penisMaxLengthRatio - penisMinLengthRatio), (1 - penisMinGirthRatio) / (penisMaxGirthRatio - penisMinGirthRatio), false, false, false, false);
            }

            if (testesRoot != null)
            {
                testesOffsetDefault = testesRoot.localPosition;
                testesScaleDefault = testesRoot.localScale;
                //testesHeightDefault = Mathf.Abs(Vector3.Dot(testesHeightAxis, testesOffsetDefault));

                if (addTestesPhysicsToRootParent)
                {
                    if (createTestesPhysicsRootParent)
                    {
                        var parent = new GameObject(testesRoot.name + "_ROOT").transform;
                        parent.SetParent(testesRoot.parent, false); 
                        testesRoot.SetParent(parent, true); 
                    }

                    testesPhysics = testesRoot.parent.gameObject.AddComponent<DynamicBone>();
                } 
                else
                {
                    testesPhysics = testesRoot.gameObject.AddComponent<DynamicBone>(); 
                }

                testesCollider = testesRoot.gameObject.AddComponent<DynamicBoneCollider>();
                testesCollider.m_Center = testesColliderOffset;
                testesCollider.m_Radius = testesColliderRadiusMax;

                if (penisPhysics != null)
                {
                    if (penisPhysics.m_Colliders == null) penisPhysics.m_Colliders = new List<DynamicBoneColliderBase>();
                    penisPhysics.m_Colliders.Add(testesCollider);
                }

                testesPhysics.SetUseSwoleUpdater(true);
                testesPhysics.m_Root = testesPhysics.transform;

                if (addTestesPhysicsToRootParent)
                {
                    if (testesPhysics.m_Exclusions == null) testesPhysics.m_Exclusions = new List<Transform>();
                    for(int a = 0; a < testesRoot.parent.childCount; a++)
                    {
                        var child = testesRoot.parent.GetChild(a);
                        if (ReferenceEquals(child, testesRoot) || ReferenceEquals(child, testesL) || ReferenceEquals(child, testesR) || child == null) continue;

                        testesPhysics.m_Exclusions.Add(child); 
                    }

                    if (excludeTestesLR)
                    {
                        if (testesPhysics.m_Exclusions == null) testesPhysics.m_Exclusions = new List<Transform>(); 

                        testesPhysics.m_Exclusions.Add(testesL);
                        testesPhysics.m_Exclusions.Add(testesR);
                    }
                }

                SetTestesHeightAndSize((1 - testesMinHeightRatio) / (testesMaxHeightRatio - testesMinHeightRatio), (1 - testesMinSizeRatio) / (testesMaxSizeRatio - testesMinSizeRatio), false, false);
            }

            if (clitBones != null && clitHoodBones != null)
            {
                clitData = new PenisBoneData[clitBones.Length];
                for (int a = 0; a < clitBones.Length; a++)
                {
                    int b = a - 1;

                    var point = clitBones[a];

                    var worldPos = point.bone.position;
                    if (a > 0)
                    {
                        var prevPoint = clitBones[b];
                        clitData[a] = new PenisBoneData() { length = (worldPos - prevPoint.bone.position).magnitude, offset = prevPoint.bone.InverseTransformPoint(worldPos), initLocalPosition = point.bone.localPosition, initLocalRotation = point.bone.localRotation, initLocalScale = point.bone.localScale };
                    }
                    else
                    {
                        clitData[a] = new PenisBoneData() { length = 0, offset = Vector3.zero, initLocalPosition = point.bone.localPosition, initLocalRotation = point.bone.localRotation, initLocalScale = point.bone.localScale };
                    }
                }

                clitHoodProxy = null;
                clitHoodData = new PenisBoneData[clitHoodBones.Length];
                List<ProxyBone.BoneBinding> proxyBoneBindings = null;
                for (int a = 0; a < clitHoodBones.Length; a++)
                {
                    int b = a - 1;

                    var point = clitHoodBones[a];

                    if (point.proxyWeight > 0)
                    {
                        if (clitHoodProxy == null)
                        {
                            clitHoodProxy = clitBones[0].bone.gameObject.AddOrGetComponent<ProxyBone>();
                        }

                        if (proxyBoneBindings == null) proxyBoneBindings = new List<ProxyBone.BoneBinding>();
                        proxyBoneBindings.Add(new ProxyBone.BoneBinding()
                        {
                            bone = point.bone,
                            binding = new ProxyBoneJobs.Binding()
                            {
                                applyScale = true,
                                baseWeight = point.proxyWeight,
                                positionWeights = new float3(point.proxyRotationWeight, point.proxyRotationWeight, point.proxyRotationWeight),
                                rotationWeights = new float3(point.proxyPositionWeight, point.proxyPositionWeight, point.proxyPositionWeight),
                                scaleWeights = new float3(point.proxyScaleWeight, point.proxyScaleWeight, point.proxyScaleWeight)
                            }
                        });
                    }
                     
                    var worldPos = point.bone.position;
                    if (a > 0)
                    {
                        var prevPoint = clitHoodBones[b];
                        clitHoodData[a] = new PenisBoneData() { length = (worldPos - prevPoint.bone.position).magnitude, offset = prevPoint.bone.InverseTransformPoint(worldPos), initLocalPosition = point.bone.localPosition, initLocalRotation = point.bone.localRotation, initLocalScale = point.bone.localScale };
                    }
                    else
                    {
                        clitHoodData[a] = new PenisBoneData() { length = 0, offset = Vector3.zero, initLocalPosition = point.bone.localPosition, initLocalRotation = point.bone.localRotation, initLocalScale = point.bone.localScale };
                    }
                }

                if (clitHoodProxy != null) 
                { 
                    clitHoodProxy.skipAutoRegister = true;
                    clitHoodProxy.bindings = proxyBoneBindings.ToArray(); 
                    clitHoodProxy.Register(); 
                }

                if (vulvaMesh != null)
                {
                    if (!string.IsNullOrWhiteSpace(clitUngirthShape)) clitUngirthShapeIndex = vulvaMesh.CharacterMeshData.IndexOfStandaloneShape(clitUngirthShape);  
                }

                clitPhysics = clitBones[0].bone.gameObject.AddComponent<DynamicBone>(); 
                clitPhysics.SetUseSwoleUpdater(true);
                clitPhysics.m_Root = clitBones[0].bone; 

                clitErectionFactor = 0;
                SetClitLengthGirth((1 - clitMinLengthRatio) / (clitMaxLengthRatio - clitMinLengthRatio), (1 - clitMinGirthRatio) / (clitMaxGirthRatio - clitMinGirthRatio), false, false, false);
            }
        }

    }

}