using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Unity.Mathematics;

using PurrNet;

using Swole;
using Swole.API.Unity;
using Swole.Morphing;
using Swole.API.Unity.Animation;

namespace BulkOut
{

    public class GrowthChamberStation : NetworkBehaviour, IExecutableBehaviour, INetworkedWorkoutProvider
    {

        private static readonly List<float3> tempGroups = new List<float3>();

        [Serializable]
        public class InjectionTube
        {
            [NonSerialized]
            public int index;
            [NonSerialized]
            public GrowthChamberStation station;

            public SimpleCablePhysics physics;

            public Vector3 Origin => physics.transform.position;

            private float[] idleSpringForces;

            private float pulseTimeOffset;

            public void Init(int index, GrowthChamberStation station)
            {
                this.index = index;
                this.station = station;

                if (physics != null)
                {
                    idleSpringForces = new float[physics.ChainLength];
                    for (int i = 0; i < physics.ChainLength; i++)
                    {
                        var body = physics.GetPhysicsBody(i);
                        if (body != null)
                        {
                            var joint = body.GetComponent<SpringJoint>();
                            if (joint != null)
                            {
                                idleSpringForces[i] = joint.spring;
                            }
                        }
                    }
                }

                pulseTimeOffset = UnityEngine.Random.value;
            }

            public bool IsInjecting => character != null && injectionIndex0 >= 0 && injectionIndex1 >= 0 && injectionIndex2 >= 0;

            [NonSerialized]
            public float scaleOffset;

            [NonSerialized]
            public float pulse;
            [NonSerialized]
            public bool pulseDir;

            [NonSerialized]
            public int injectionIndex0;
            [NonSerialized]
            public int injectionIndex1;
            [NonSerialized]
            public int injectionIndex2;

            [NonSerialized]
            public float injectionWeight0;
            [NonSerialized]
            public float injectionWeight1;
            [NonSerialized]
            public float injectionWeight2;

            [NonSerialized]
            public ICustomizableCharacter character;

            public readonly List<float3> affectedMuscleGroups = new List<float3>(); 
            public void ConsolidateMuscleGroups()
            {
                tempGroups.Clear();

                foreach (var group in affectedMuscleGroups)
                {
                    bool flag = true;
                    for (int i = 0; i < tempGroups.Count; i++)
                    {
                        var g = tempGroups[i];
                        if ((int)g.x == (int)group.x)
                        {
                            g.y += group.y;
                            g.z += group.z;
                            tempGroups[i] = g;
                            flag = false;
                            break;
                        }
                    }

                    if (flag) tempGroups.Add(group);
                }

                affectedMuscleGroups.Clear();
                affectedMuscleGroups.AddRange(tempGroups);
            }

            private float interp;
            private Vector3 endStartPos;
            private Vector3 preEndStartPos;

            public float lastInjectionTime;

            public void StartInjecting(ICustomizableCharacter character, int injIndex0, int injIndex1, int injIndex2, float injectionWeight0, float injectionWeight1, float injectionWeight2)
            {
                this.character = character;

                this.injectionIndex0 = injIndex0;
                this.injectionIndex1 = injIndex1;
                this.injectionIndex2 = injIndex2;

                this.injectionWeight0 = injectionWeight0;
                this.injectionWeight1 = injectionWeight1;
                this.injectionWeight2 = injectionWeight2;

                StartInjecting();
            }
            public void StartInjecting()
            {
                lastInjectionTime = Time.time;

                if (physics != null)
                {
                    int endIndex = physics.ChainLength - 1;
                    int preEndIndex = physics.ChainLength - 2;

                    var b0 = physics.GetPhysicsBody(endIndex);
                    var b1 = physics.GetPhysicsBody(preEndIndex);

                    b0.isKinematic = true;
                    b1.isKinematic = true;

                    endStartPos = b0.position;
                    preEndStartPos = b1.position;
                }

                interp = 0f;

                foreach (var group in affectedMuscleGroups)
                {
                    int groupIndex = (int)group.x;
                    var muscleData = character.GetMuscleDataUnsafe(groupIndex);

                    muscleData.valuesLeft.varicose = muscleData.valuesLeft.varicose + group.y;
                    muscleData.valuesRight.varicose = muscleData.valuesRight.varicose + group.z;

                    character.SetMuscleDataUnsafe(groupIndex, muscleData);
                }

                if (physics != null)
                {
                    for (int i = 0; i < physics.ChainLength; i++)
                    {
                        var body = physics.GetPhysicsBody(i);
                        if (body != null)
                        {
                            var joint = body.GetComponent<SpringJoint>();
                            if (joint != null)
                            {
                                joint.spring = station.injectingJointSpringForce;
                            }
                        }
                    }
                }
            }

            public void StopInjecting()
            {
                if (physics != null)
                {
                    int endIndex = physics.ChainLength - 1;
                    int preEndIndex = physics.ChainLength - 2;

                    physics.GetPhysicsBody(endIndex).isKinematic = false;
                    physics.GetPhysicsBody(preEndIndex).isKinematic = false;
                }

                foreach (var group in affectedMuscleGroups)
                {
                    int groupIndex = (int)group.x;
                    var muscleData = character.GetMuscleDataUnsafe(groupIndex);

                    muscleData.valuesLeft.varicose = muscleData.valuesLeft.varicose - group.y;
                    muscleData.valuesRight.varicose = muscleData.valuesRight.varicose - group.z;

                    character.SetMuscleDataUnsafe(groupIndex, muscleData);
                }

                if (physics != null)
                {
                    for (int i = 0; i < physics.ChainLength; i++)
                    {
                        var body = physics.GetPhysicsBody(i);
                        if (body != null)
                        {
                            var joint = body.GetComponent<SpringJoint>();
                            if (joint != null)
                            {
                                joint.spring = idleSpringForces[i];
                            }
                        }
                    }
                }

                character = null;
                injectionIndex0 = -1;
                injectionIndex1 = -1;
                injectionIndex2 = -1;
            }

            public void Update(float deltaTime)
            {
                if (physics != null)
                {
                    if (IsInjecting)
                    {
                        int endIndex = physics.ChainLength - 1;
                        int preEndIndex = physics.ChainLength - 2;

                        var b0 = physics.GetPhysicsBody(endIndex);
                        var b1 = physics.GetPhysicsBody(preEndIndex);

                        character.GetVertexInWorld(0, injectionIndex0, out var p0, out var n0, out var t0);
                        character.GetVertexInWorld(0, injectionIndex1, out var p1, out var n1, out var t1);
                        character.GetVertexInWorld(0, injectionIndex2, out var p2, out var n2, out var t2);

                        p0 = p0 * injectionWeight0;
                        p1 = p1 * injectionWeight1;
                        p2 = p2 * injectionWeight2;

                        n0 = n0 * injectionWeight0;
                        n1 = n1 * injectionWeight1;
                        n2 = n2 * injectionWeight2;

                        var center = p0 + p1 + p2;
                        var normal = math.normalize(n0 + n1 + n2);
                        //var originDir = math.normalize((float3)Origin - center);

                        interp += deltaTime * 3f;
                        float3 offsetDir = (b1.position - (Vector3)center).normalized;
                        offsetDir = Vector3.RotateTowards(offsetDir, normal, Vector3.Angle(normal, offsetDir) * Mathf.Pow(Mathf.Clamp01(interp), 1.5f) * Mathf.Deg2Rad, 0f);

                        b0.MovePosition(Vector3.Lerp(endStartPos, center + (normal * station.injectionSurfaceOffset), interp));
                        //b1.MovePosition(Vector3.Lerp(preEndStartPos, center + (float3)(Vector3.RotateTowards(normal, originDir, 35f * Mathf.Deg2Rad, 0f) * (0.05f + physics.AverageSpacing)), interp));   
                        b1.MovePosition(Vector3.Lerp(preEndStartPos, center + offsetDir * (station.injectionSurfaceOffset + physics.AverageSpacing * 0.5f), interp));

                        pulse = Mathf.Min(1f, pulse + deltaTime);
                    }
                    else
                    {
                        if (pulse > 0f) pulse = Mathf.Max(0f, pulse - deltaTime);
                    }

                    if (pulse > 0f)
                    {
                        for (int i = 0; i < physics.ChainLength; i++)
                        {
                            float t = i / (physics.ChainLength - 1f);
                            var visualT = physics.GetVisualBone(i);
                            if (visualT != null)
                            {
                                float scale = 1f + Mathf.Sin((Time.time * station.injectionPulseRate) + pulseTimeOffset + t * station.injectionPulseSpacing) * pulse * station.injectionPulseScale * station.injectionPulseScaleCurve.Evaluate(t);
                                visualT.localScale = new Vector3(scale, scale, scale);
                            }
                        }
                    }
                }
            }

            public void Grow()
            {
                if (!IsInjecting) return;

                foreach (var group in affectedMuscleGroups)
                {
                    int groupIndex = (int)group.x;
                    var muscleData = character.GetMuscleDataUnsafe(groupIndex);

                    muscleData.valuesLeft.mass = muscleData.valuesLeft.mass + ((station.growthRate * group.y) / Mathf.Pow(muscleData.valuesLeft.mass + 1f, station.growthFalloff));
                    muscleData.valuesRight.mass = muscleData.valuesRight.mass + ((station.growthRate * group.z) / Mathf.Pow(muscleData.valuesLeft.mass + 1f, station.growthFalloff));

                    character.SetMuscleDataUnsafe(groupIndex, muscleData);

                    int basicGroupIndexL = groupIndex * 2;
                    int basicGroupIndexR = basicGroupIndexL + 1;
                    float pumpL = station.pumpController.GetPumpUnsafe(basicGroupIndexL);
                    float pumpR = station.pumpController.GetPumpUnsafe(basicGroupIndexR);
                    station.pumpController.SetPumpUnsafe(basicGroupIndexL, Mathf.Min(5f, pumpL + muscleData.valuesLeft.mass * station.pumpGainRate * group.y));
                    station.pumpController.SetPumpUnsafe(basicGroupIndexR, Mathf.Min(5f, pumpR + muscleData.valuesRight.mass * station.pumpGainRate * group.z));
                }
            }
        }

        public float injectingJointSpringForce = 30f;
        public float injectionSurfaceOffset = 0.1f;
        public float injectionPulseRate = -5f;
        public float injectionPulseSpacing = 5f;
        public float injectionPulseScale = 0.2f;
        public AnimationCurve injectionPulseScaleCurve;

        public float growthRate = 0.001f;
        public float growthFalloff = 1.5f;
        public float pumpGainRate = 0.06f;
        public float pumpDecayRate = 0.01f;
        public float pulseRate = 15f;
        public float pulseMassThreshold = 0.2f;
        public float pulseMassAdd = 0.005f;
        public float pulseMassAddMax = 0.05f;

        public string pulseRatePropertyName = "_PulseRate";
        public string pulseMassThresholdName = "_PulseMassThreshold";
        public string pulseMassAddPropertyName = "_PulseMassAdd";
        public string pulseMassAddMaxPropertyName = "_PulseMassAddMax";

        public InjectionTube[] injectionTubes;

        public GameObject characterObject;
        public ICustomizableCharacter character;
        public TieredPumpController pumpController;
        public void SetCharacter(GameObject characterObj)
        {
            this.characterObject = characterObj;
            if (characterObject != null)
            {
                var collection = characterObject.GetComponent<CustomizableCharacterMeshCollection>();
                if (collection != null) character = collection.First; else character = null;

                pumpController = characterObject.GetComponent<TieredPumpController>();
                if (pumpController == null && character is MonoBehaviour behaviour)
                {
                    pumpController = behaviour.GetComponent<TieredPumpController>();
                }
                if (pumpController != null)
                {
                    pumpController.pumpDecayRate = pumpDecayRate;
                }

                var objs = characterObject.GetComponentsInChildren<ICustomizableCharacter>();
                if (objs != null)
                {
                    foreach (var obj in objs)
                    {
                        obj.SetFloatOverride(pulseRatePropertyName, pulseRate, false);
                        obj.SetFloatOverride(pulseMassThresholdName, pulseMassThreshold, false);
                        obj.SetFloatOverride(pulseMassAddPropertyName, pulseMassAdd, false);
                        obj.SetFloatOverride(pulseMassAddMaxPropertyName, pulseMassAddMax, true);
                    }
                }
            }
        }

        [SerializeField]
        protected GameObject reserveObject;
        public GameObject ReserveObject => reserveObject;
        public bool IsReserved => reserveObject != null;
        public void SetReserveObject(GameObject reserveObject)
        {
            this.reserveObject = reserveObject;
        }

        private readonly List<SimplePlayer> charactersInRange = new List<SimplePlayer>();
        private float charactersInRangeClearTimer;

        public Transform cameraTransform;
        new private Camera camera;

        public Vector3 enterBoxCenter;
        public Vector3 enterBoxSize = Vector3.one;

        protected void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(enterBoxCenter, enterBoxSize);

            Gizmos.matrix = Matrix4x4.identity;
        }

        protected void Awake()
        {
            if (injectionPulseScaleCurve == null || injectionPulseScaleCurve.length <= 0)
            {
                injectionPulseScaleCurve = AnimationCurve.Constant(0f, 1f, 1f);
            }

            SetCharacter(characterObject);

            if (injectionTubes != null)
            {
                for (int i = 0; i < injectionTubes.Length; i++)
                {
                    var tube = injectionTubes[i];
                    tube.Init(i, this);
                }
            }

            if (cameraTransform != null)
            {
                camera = cameraTransform.GetComponentInChildren<Camera>();
            }

            var enterObj = new GameObject("EnterBox");
            enterObj.transform.SetParent(transform, false);
            enterObj.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            enterObj.transform.localScale = Vector3.one;

            var enterCollider = enterObj.AddComponent<BoxCollider>();
            enterCollider.center = enterBoxCenter;
            enterCollider.size = enterBoxSize;
            enterCollider.isTrigger = true;

            var enterEvents = enterObj.AddComponent<TriggerEventBroadcaster>();
            enterEvents.target = gameObject;

            this.AddToCallstack();
        }

        protected override void OnDestroy()
        {
            try
            {
                this.RemoveFromCallstack();
            }
            finally
            {
                base.OnDestroy();
            }
        }

        public bool TryInjectFromScreen(int injectorIndex = -1)
        {
            return TryInjectFromScreen(Camera.main, InputProxy.CursorScreenPosition, injectorIndex);
        }
        public bool TryInjectFromScreen(Camera camera, Vector2 cursorPos, int injectorIndex = -1)
        {
            var ray = camera.ScreenPointToRay(new Vector3(cursorPos.x, cursorPos.y, camera.nearClipPlane));
            return TryInjectFromRay(ray.origin, ray.direction, 100f, injectorIndex);
        }
        public bool TryInjectFromRay(Vector3 rayOrigin, Vector3 rayDir, float maxDistance, int injectorIndex = -1)
        {
            if (character != null && character.TryGetTriangles(0, out var tris))
            {
                if (character.RaycastAgainst(0, rayOrigin, rayDir * maxDistance, out var hit))
                {
                    int triIndex = hit.triangleIndex * 3;

                    int injIndex0 = tris[triIndex];
                    int injIndex1 = tris[triIndex + 1];
                    int injIndex2 = tris[triIndex + 2];

                    float injWeight0 = hit.barycentricCoordinate.x;
                    float injWeight1 = hit.barycentricCoordinate.y;
                    float injWeight2 = hit.barycentricCoordinate.z;

                    if (injectorIndex < 0)
                    {
                        var worldPos0 = character.GetVertexInWorld(0, injIndex0);
                        var worldPos1 = character.GetVertexInWorld(0, injIndex1);
                        var worldPos2 = character.GetVertexInWorld(0, injIndex2);

                        var worldCenter = (Vector3)(worldPos0 + worldPos1 + worldPos2) / 3f;

                        float minDist = float.MaxValue;
                        for (int i = 0; i < injectionTubes.Length; i++)
                        {
                            var injector = injectionTubes[i];
                            if (injector.physics != null && !injector.IsInjecting)
                            {
                                float dist = (worldCenter - injector.Origin).sqrMagnitude;

                                if (dist < minDist)
                                {
                                    minDist = dist;
                                    injectorIndex = i;
                                }
                            }
                        }
                    }

                    if (injectorIndex < 0) return false;

                    var injector_ = injectionTubes[injectorIndex];

                    injector_.affectedMuscleGroups.Clear();

                    tempGroups.Clear();
                    character.GetMuscleGroupsAffecting(0, injIndex0, tempGroups);
                    for (int i = 0; i < tempGroups.Count; i++)
                    {
                        var g = tempGroups[i];
                        g.y = g.y * injWeight0;
                        g.z = g.z * injWeight0;
                        tempGroups[i] = g;
                    }
                    injector_.affectedMuscleGroups.AddRange(tempGroups);

                    tempGroups.Clear();
                    character.GetMuscleGroupsAffecting(0, injIndex1, tempGroups);
                    for (int i = 0; i < tempGroups.Count; i++)
                    {
                        var g = tempGroups[i];
                        g.y = g.y * injWeight1;
                        g.z = g.z * injWeight1;
                        tempGroups[i] = g;
                    }
                    injector_.affectedMuscleGroups.AddRange(tempGroups);

                    character.GetMuscleGroupsAffecting(0, injIndex2, tempGroups);
                    for (int i = 0; i < tempGroups.Count; i++)
                    {
                        var g = tempGroups[i];
                        g.y = g.y * injWeight2;
                        g.z = g.z * injWeight2;
                        tempGroups[i] = g;
                    }
                    injector_.affectedMuscleGroups.AddRange(tempGroups);

                    injector_.ConsolidateMuscleGroups();

                    injector_.StartInjecting(character, injIndex0, injIndex1, injIndex2, injWeight0, injWeight1, injWeight2);
                }
            }

            return false;
        }

        [Serializable]
        public enum UnhookRequirement
        {
            Nearest, Furthest, Oldest, Newest, Random
        }
        public void UnhookInjector(UnhookRequirement requirement)
        {
            var t = transform;
            if (cameraTransform != null) t = cameraTransform;

            Vector3 worldPoint = transform.position;
            if (requirement == UnhookRequirement.Nearest || requirement == UnhookRequirement.Furthest)
            {
                if (character != null)
                {
                    if (character.RaycastAgainst(character.DefaultRaycastLOD, worldPoint, cameraTransform.forward * 1000f, out var hit))
                    {
                        worldPoint = hit.point;
                    }
                }
            }

            switch (requirement)
            {
                case UnhookRequirement.Nearest:
                    UnhookNearestInjector(worldPoint);
                    break;
                case UnhookRequirement.Furthest:
                    UnhookFurthestInjector(worldPoint);
                    break;
                case UnhookRequirement.Oldest:
                    UnhookOldestInjector();
                    break;
                case UnhookRequirement.Newest:
                    UnhookNearestInjector();
                    break;
                case UnhookRequirement.Random:
                    UnhookRandomInjector();
                    break;
            }
        }
        public void UnhookNearestInjector()
        {
            UnhookInjector(UnhookRequirement.Nearest);
        }
        public void UnhookNearestInjector(Vector3 worldPoint)
        {
            if (injectionTubes != null)
            {
                float minDist = float.MaxValue;
                InjectionTube closestTube = null;
                foreach (var tube in injectionTubes)
                {
                    if (tube.physics == null || !tube.IsInjecting) continue;

                    int endIndex = tube.physics.ChainLength - 1;

                    var endTransform = tube.physics.GetPhysicsTransform(endIndex);
                    if (endTransform == null) continue;

                    float dist = Vector3.Distance(worldPoint, endTransform.position);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        closestTube = tube;
                    }
                }

                if (closestTube != null)
                {
                    closestTube.StopInjecting();
                }
            }
        }

        public void UnhookFurthestInjector()
        {
            UnhookInjector(UnhookRequirement.Furthest);
        }
        public void UnhookFurthestInjector(Vector3 worldPoint)
        {
            if (injectionTubes != null)
            {
                float maxDist = float.MinValue;
                InjectionTube closestTube = null;
                foreach (var tube in injectionTubes)
                {
                    if (tube.physics == null || !tube.IsInjecting) continue;

                    int endIndex = tube.physics.ChainLength - 1;

                    var endTransform = tube.physics.GetPhysicsTransform(endIndex);
                    if (endTransform == null) continue;

                    float dist = Vector3.Distance(worldPoint, endTransform.position);
                    if (dist > maxDist)
                    {
                        maxDist = dist;
                        closestTube = tube;
                    }
                }

                if (closestTube != null)
                {
                    closestTube.StopInjecting();
                }
            }
        }

        public void UnhookOldestInjector()
        {
            if (injectionTubes != null)
            {
                float minInjectTime = float.MaxValue;
                InjectionTube oldestTube = null;
                foreach (var tube in injectionTubes)
                {
                    if (tube.physics == null || !tube.IsInjecting) continue;

                    if (tube.lastInjectionTime < minInjectTime)
                    {
                        minInjectTime = tube.lastInjectionTime;
                        oldestTube = tube;
                    }
                }

                if (oldestTube != null)
                {
                    oldestTube.StopInjecting();
                }
            }
        }

        public void UnhookNewestInjector()
        {
            if (injectionTubes != null)
            {
                float maxInjectTime = float.MinValue;
                InjectionTube oldestTube = null;
                foreach (var tube in injectionTubes)
                {
                    if (tube.physics == null || !tube.IsInjecting) continue;

                    if (tube.lastInjectionTime > maxInjectTime)
                    {
                        maxInjectTime = tube.lastInjectionTime;
                        oldestTube = tube;
                    }
                }

                if (oldestTube != null)
                {
                    oldestTube.StopInjecting();
                }
            }
        }

        private readonly List<InjectionTube> _tempTubes = new List<InjectionTube>();
        public void UnhookRandomInjector()
        {
            if (injectionTubes != null)
            {
                _tempTubes.Clear();
                foreach (var tube in injectionTubes)
                {
                    if (tube.physics == null || !tube.IsInjecting) continue;

                    _tempTubes.Add(tube);
                }

                if (_tempTubes.Count > 1)
                {
                    _tempTubes[UnityEngine.Random.Range(0, _tempTubes.Count)].StopInjecting();
                }
                else
                {
                    _tempTubes[0].StopInjecting();
                }
            }
        }


        public int Priority => CustomAnimatorUpdater.FinalAnimationBehaviourPriority + 1;

        public int UpdatePriority => Priority;

        public int LateUpdatePriority => Priority;

        public int FixedUpdatePriority => Priority;

        protected void OnTriggerStay(Collider other)
        {
            if (characterObject != null || other.attachedRigidbody == null) return;

            var rootObj = other.attachedRigidbody.gameObject;
            var rootPlayer = rootObj.GetComponent<SimplePlayer>();
            if (rootPlayer != null && !charactersInRange.Contains(rootPlayer))
            {
                charactersInRange.Add(rootPlayer);
            }
        }
        protected void OnTriggerExit(Collider other)
        {
            if (other.attachedRigidbody == null) return;

            var rootObj = other.attachedRigidbody.gameObject;
            var rootPlayer = rootObj.GetComponent<SimplePlayer>();
            if (rootPlayer != null)
            {
                charactersInRange.Remove(rootPlayer);
            }
        }

        public void OnUpdate()
        {
            if (charactersInRangeClearTimer > 0f)
            {
                charactersInRangeClearTimer -= Time.deltaTime;
                if (charactersInRangeClearTimer <= 0f)
                {
                    charactersInRange.Clear();
                }
            } 
            else if (characterObject == null)
            {
                foreach(var character in charactersInRange)
                {
                    if (!character.isOwner) continue;
                    if (IsReserved && character.gameObject != reserveObject) continue; 

                    if (InputProxy.InteractDown)
                    {
                        Debug.Log($"ENTER {character.name}");
                        SetCharacter(character.gameObject);
                        GiveOwnership(character.owner); 
                    }
                }
            }
        }

        private float growthT;
        public void OnLateUpdate()
        {
            if (isOwner)
            {
                if (InputProxy.MOBA_AbilityQDown)
                {
                    UnhookNearestInjector();
                }
                if (InputProxy.MOBA_AbilityEDown)
                {
                    UnhookFurthestInjector();
                }

                if (InputProxy.CursorPrimaryButtonDown)
                {
                    TryInjectFromScreen();
                }
            }

            if (injectionTubes != null)
            {
                foreach (var tube in injectionTubes) tube.Update(Time.deltaTime);
            }

            if (isOwner)
            {
                growthT += Time.deltaTime;
                if (growthT >= 1f)
                {
                    foreach (var tube in injectionTubes) tube.Grow();

                    growthT -= 1f;
                }
            }
        }

        public void OnFixedUpdate()
        {
        }

        public void OnPreFixedUpdate()
        {
        }

        public void OnPostFixedUpdate()
        {
        }


        #region INetworkedWorkoutProvider

        public IWorkoutInputManager InputManager 
        { 
            get => throw new NotImplementedException(); 
            set => throw new NotImplementedException(); 
        }
        public int TickRate 
        { 
            get => throw new NotImplementedException();
            set => throw new NotImplementedException(); 
        }

        public long Tick => throw new NotImplementedException();

        public WorkoutPerformerState PerformerState => throw new NotImplementedException();

        public float Load 
        { 
            get => throw new NotImplementedException(); 
            set => throw new NotImplementedException();
        }

        public WorkoutLoadChangeRequirements LoadChangeRequirements => throw new NotImplementedException();

        public bool CanChangeLoad => throw new NotImplementedException();

        public float LoadChangeDelay => 0f;

        public float LoadChangeDelaySpotter => 0f;

        public float MaxLiftableLoad => throw new NotImplementedException();

        public bool AllowSpotter => false;

        public bool HasSpotter => false;

        public bool CanStartWorkout => throw new NotImplementedException();

        public bool CanStartRep => throw new NotImplementedException();

        public bool CanStartResting => throw new NotImplementedException();

        public bool CanExitWorkout => throw new NotImplementedException();

        public bool CanExitResting => throw new NotImplementedException();

        public float EnterWorkoutTime => throw new NotImplementedException();

        public float ExitWorkoutTime => throw new NotImplementedException();

        public float EnterRestTime => throw new NotImplementedException();

        public float ExitRestTime => throw new NotImplementedException();

        public float EnterRepTime => throw new NotImplementedException();

        public float ExitRepTime => throw new NotImplementedException();

        public WorkoutPerformerState NextPerformerState => throw new NotImplementedException();

        public bool IsTransitioningPerformerState => throw new NotImplementedException();

        public float PerformerTransitionProgress => throw new NotImplementedException();

        public Dictionary<string, ValueCurve> MuscleLoadContributions 
        { 
            get => throw new NotImplementedException(); 
            set => throw new NotImplementedException(); 
        }
        public Dictionary<string, ValueCurve> MuscleFatigueLoadMultipliers 
        { 
            get => throw new NotImplementedException(); 
            set => throw new NotImplementedException(); 
        }
        public Dictionary<string, ValueCurve> MuscleRepPosGrowths 
        { 
            get => throw new NotImplementedException(); 
            set => throw new NotImplementedException(); 
        }
        public Dictionary<string, ValueCurve> MuscleLoadGrowths 
        { 
            get => throw new NotImplementedException(); 
            set => throw new NotImplementedException(); 
        }
        public Dictionary<string, WorkoutMuscleState> MuscleStates 
        { 
            get => throw new NotImplementedException(); 
            set => throw new NotImplementedException(); 
        }

        public IWorkoutState this[int tick] 
        { 
            get => throw new NotImplementedException(); 
            set => throw new NotImplementedException(); 
        }

        public int CompareTo(IExecutableBehaviour other)
        {
            return Priority.CompareTo(other.Priority);
        }

        public void Initialize()
        {
            throw new NotImplementedException();
        }

        public void SetInputManager(IWorkoutInputManager manager)
        {
            throw new NotImplementedException();
        }

        public void ResetTick()
        {
            throw new NotImplementedException();
        }

        public IWorkoutState GetCurrentPerformanceState()
        {
            throw new NotImplementedException();
        }

        public IWorkoutState GetPerformanceState(int tick)
        {
            throw new NotImplementedException();
        }

        public void SetPerformanceState(int tick, IWorkoutState state)
        {
            throw new NotImplementedException();
        }

        public void ListenForPerformanceStateChange(PerformanceStateChangeDelegate listener)
        {
            throw new NotImplementedException();
        }

        public void StopListeningForPerformanceStateChange(PerformanceStateChangeDelegate listener)
        {
            throw new NotImplementedException();
        }

        public IWorkoutState GetState()
        {
            throw new NotImplementedException();
        }

        public void SetState(IWorkoutState state)
        {
            throw new NotImplementedException();
        }

        public void ListenForWorkoutStateChange(WorkoutStateChangeDelegate listener)
        {
            throw new NotImplementedException();
        }

        public void StopListeningForWorkoutStateChange(WorkoutStateChangeDelegate listener)
        {
            throw new NotImplementedException();
        }

        public void SetPerformerState(WorkoutPerformerState state)
        {
            throw new NotImplementedException();
        }

        public void TransitionPerformerState(WorkoutPerformerState targetState, float transitionDuration)
        {
            throw new NotImplementedException();
        }

        public void ListenForPerformerStateChange(PerformerStateChangeDelegate listener)
        {
            throw new NotImplementedException();
        }

        public void StopListeningForPerformerStateChange(PerformerStateChangeDelegate listener)
        {
            throw new NotImplementedException();
        }

        public void Advance()
        {
            throw new NotImplementedException();
        }

        void IWorkoutProvider.Update(float deltaTime)
        {
            throw new NotImplementedException();
        }

        public void SetLoad(float load)
        {
            throw new NotImplementedException();
        }

        public float GetLoad()
        {
            throw new NotImplementedException();
        }

        public bool TryToEnter()
        {
            throw new NotImplementedException();
        }

        public bool TryToIdle()
        {
            throw new NotImplementedException();
        }

        public bool TryToRep()
        {
            throw new NotImplementedException();
        }

        public bool TryToRest()
        {
            throw new NotImplementedException();
        }

        public bool TryToExit()
        {
            throw new NotImplementedException();
        }

        public void SetMuscleLoadContributions(Dictionary<string, ValueCurve> loadContributions)
        {
            throw new NotImplementedException();
        }

        public Dictionary<string, ValueCurve> GetMuscleLoadContributions()
        {
            throw new NotImplementedException();
        }

        public float GetCurrentLoadContribution(string muscleName)
        {
            throw new NotImplementedException();
        }

        public void SetMuscleFatigueLoadMultipliers(Dictionary<string, ValueCurve> fatigueLoadMultipliers)
        {
            throw new NotImplementedException();
        }

        public Dictionary<string, ValueCurve> GetMuscleFatigueLoadMultipliers()
        {
            throw new NotImplementedException();
        }

        public float GetCurrentFatigueLoadMultiplier(string muscleName)
        {
            throw new NotImplementedException();
        }

        public void SetMuscleRepPosGrowths(Dictionary<string, ValueCurve> repPosGrowths)
        {
            throw new NotImplementedException();
        }

        public Dictionary<string, ValueCurve> GetMuscleRepPosGrowths()
        {
            throw new NotImplementedException();
        }

        public float GetCurrentRepPosGrowth(string muscleName)
        {
            throw new NotImplementedException();
        }

        public void SetMuscleLoadGrowths(Dictionary<string, ValueCurve> loadGrowths)
        {
            throw new NotImplementedException();
        }

        public Dictionary<string, ValueCurve> GetMuscleLoadGrowths()
        {
            throw new NotImplementedException();
        }

        public float GetCurrentLoadGrowth(string muscleName)
        {
            throw new NotImplementedException();
        }

        public void SetMuscleStates(Dictionary<string, WorkoutMuscleState> muscleStates)
        {
            throw new NotImplementedException();
        }

        public Dictionary<string, WorkoutMuscleState> GetMuscleStates()
        {
            throw new NotImplementedException();
        }

        public void SetMuscleState(string muscleName, WorkoutMuscleState muscleState)
        {
            throw new NotImplementedException();
        }

        public WorkoutMuscleState GetMuscleState(string muscleName)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        #endregion

    }

}