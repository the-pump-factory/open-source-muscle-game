using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Unity.Mathematics;

using PurrNet;

using Swole;

namespace BulkOut
{

    public class GrowthChamberSerumTank : NetworkBehaviour, ISerumContainer
    {

        public void OnDrawGizmosSelected()
        {
            Gizmos.matrix = Matrix4x4.identity;

            Gizmos.color = Color.magenta; 
            Gizmos.DrawWireSphere(DisposalTargetPositionWorld, 1f);  

            if (canisterPorts != null)
            {
                for (int i = 0; i < canisterPorts.Length; i++)
                {
                    var port = canisterPorts[i];
                    if (port == null) continue;

                    port.Init(i, this);

                    Gizmos.color = Color.blue;
                    Gizmos.DrawSphere(port.InteractionPointWorld, 0.05f);
                    Gizmos.DrawRay(port.InteractionPointWorld, port.InteractionDirectionWorld * port.interactionRadius);

                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(port.InteractionBoundsPointWorld, port.interactionRadius);

                    Gizmos.color = Color.green;
                    Gizmos.DrawSphere(port.PortPositionWorld, 0.05f);
                    Gizmos.DrawRay(port.PortPositionWorld, port.PortRotationWorld * Vector3.up);

                    Gizmos.color = Color.cyan;
                    Gizmos.DrawSphere(port.OriginPositionWorld, 0.1f);
                }
            }
        }

        [Serializable]
        public class CanisterPort
        {

            [NonSerialized]
            public GrowthChamberSerumTank tank;

            [NonSerialized]
            public int index;

            public float transferRate;

            public void Init(int index, GrowthChamberSerumTank tank)
            {
                this.index = index;
                this.tank = tank;
                rotationQuat = Quaternion.Euler(rotation);
                portRotationQuat = Quaternion.Euler(portRotation);
            }

            public Vector3 rotation;
            [NonSerialized]
            public Quaternion rotationQuat = Quaternion.identity;

            public Vector3 originPosition;
            public Vector3 OriginPositionWorld => tank.transform.TransformPoint(originPosition);

            public Vector3 portPosition;
            public Vector3 PortPositionWorld => tank.transform.TransformPoint(rotationQuat * portPosition);

            public Vector3 portRotation;
            [NonSerialized]
            public Quaternion portRotationQuat = Quaternion.identity;
            public Quaternion PortRotationWorld => tank.transform.rotation * (rotationQuat * portRotationQuat);

            public Vector3 interactionDirection;
            public Vector3 InteractionDirectionWorld => tank.transform.TransformDirection(rotationQuat * interactionDirection).normalized;

            public Vector3 interactionPoint;
            public Vector3 InteractionPointWorld => tank.transform.TransformPoint(rotationQuat * interactionPoint);

            public Vector3 interactionBoundsOffset;
            public Vector3 InteractionBoundsPointWorld => tank.transform.TransformPoint(rotationQuat * (interactionPoint + interactionBoundsOffset));

            public float interactionRadius;

            [NonSerialized]
            public GrowthChamberRefillCanister insertedCanister;
            public bool HasCanister => insertedCanister != null;

            public IEnumerator MoveCanisterIntoSlot(float time)
            {
                if (insertedCanister == null) yield break;

                insertedCanister.transform.GetPositionAndRotation(out var startPos, out var startRot);
                var targetPos = PortPositionWorld;
                var targetRot = PortRotationWorld;

                if (time > 0f)
                {
                    float t = 0f;
                    while(t < time)
                    {
                        t += Time.deltaTime;
                        if (t >= time) break;

                        float t_ = t / time;
                        insertedCanister.transform.SetPositionAndRotation(Vector3.LerpUnclamped(startPos, targetPos, t_), Quaternion.SlerpUnclamped(startRot, targetRot, t_));

                        yield return null;
                    }
                }

                insertedCanister.transform.SetPositionAndRotation(targetPos, targetRot);
            }

            public void Update(float deltaTime)
            {
                if (tank.isHost)
                {
                    if (HasCanister)
                    {
                        insertedCanister.TransferSerum(tank, transferRate * deltaTime); 
                    }
                }
            }

        }

        [SerializeField]
        protected GrowthChamberStation station;

        [SerializeField]
        protected SyncVar<GrowthSerum> serum;

        public GrowthSerum Serum 
        {
            get => serum.value;
            set => serum.value = value;
        }

        public bool IsEmpty => Serum.Amount <= 0f;

        [SerializeField]
        protected float serumCapacity;
        public float Capacity => serumCapacity;


        public static GrowthChamberSerumTank operator +(GrowthChamberSerumTank serumA, GrowthSerum serumB)
        {
            serumA.Serum += serumB;
            return serumA;
        }

        public static GrowthChamberSerumTank operator -(GrowthChamberSerumTank serumA, GrowthSerum serumB)
        {
            serumA.Serum -= serumB;
            return serumA;
        }

        public static GrowthChamberSerumTank operator *(GrowthChamberSerumTank serumA, GrowthSerum serumB)
        {
            serumA.Serum *= serumB;
            return serumA;
        }

        public static GrowthChamberSerumTank operator /(GrowthChamberSerumTank serumA, GrowthSerum serumB)
        {
            serumA.Serum /= serumB;
            return serumA;
        }

        public static GrowthChamberSerumTank operator *(GrowthChamberSerumTank serumA, float f)
        {
            serumA.Serum *= f;
            return serumA;
        }

        public static GrowthChamberSerumTank operator /(GrowthChamberSerumTank serumA, float f)
        {
            serumA.Serum /= f;
            return serumA;
        }


        public LayerMask interactionLayerMask;

        public CanisterPort[] canisterPorts;

        public Vector3 disposalTargetPosition;
        public Vector3 DisposalTargetPositionWorld => transform.TransformPoint(disposalTargetPosition);

        [SerializeField]
        protected SkinnedMeshRenderer serumRenderer;
        [SerializeField]
        protected LiquidInBottleShaderSync serumShaderSync;
        [SerializeField]
        protected string emptyBlendShape;
        [NonSerialized]
        protected int emptyBlendShapeIndex = -1;

        protected void Awake()
        {
            if (!string.IsNullOrWhiteSpace(emptyBlendShape))
            {
                if (serumRenderer != null) emptyBlendShapeIndex = serumRenderer.sharedMesh.GetBlendShapeIndex(emptyBlendShape);
            }

            if (canisterPorts != null)
            {
                for (int i = 0; i < canisterPorts.Length; i++)
                {
                    var port = canisterPorts[i];
                    port.Init(i, this);
                }
            }

            if (serumShaderSync == null) serumShaderSync = gameObject.GetComponent<LiquidInBottleShaderSync>();
        }

        protected struct PortWithDist
        {
            public CanisterPort port;
            public float distance;
        }
        protected static readonly Collider[] tempColliders = new Collider[128];
        protected static readonly Dictionary<GrowthChamberPlayer, PortWithDist> perPlayerPorts = new Dictionary<GrowthChamberPlayer, PortWithDist>();
        protected void LateUpdate()
        {
            if (serumRenderer != null)
            {
                if (emptyBlendShapeIndex >= 0) serumRenderer.SetBlendShapeWeight(emptyBlendShapeIndex, Mathf.Clamp01(1f - ((ISerumContainer)this).Fill) * 100f);  
            }

            if (serumShaderSync != null) serumShaderSync.fillAmount = ((ISerumContainer)this).Fill; 

            if (canisterPorts != null)
            {
                perPlayerPorts.Clear();
                foreach (var port in canisterPorts)
                {
                    port.Update(Time.deltaTime);

                    if (port.HasCanister)
                    {
                        var boundsCenter = port.InteractionBoundsPointWorld;
                        int overlapCount = Physics.OverlapSphereNonAlloc(boundsCenter, port.interactionRadius, tempColliders, interactionLayerMask, QueryTriggerInteraction.Ignore);
                        if (overlapCount > 0) 
                        {
                            Vector3 interactDir = -port.InteractionDirectionWorld;

                            for (int i = 0; i < overlapCount; i++)
                            {
                                var col = tempColliders[i];
                                var root = col.transform.root;
                                var player = root.gameObject.GetComponent<GrowthChamberPlayer>();
                                if (player == null || player.IsCarryingObject) continue;

                                if (player.State == GrowthChamberPlayer.ActionState.Default && player.isOwner && InputProxy.InteractDown)
                                {
                                    float dist = (root.position - boundsCenter).sqrMagnitude;
                                    if (!perPlayerPorts.TryGetValue(player, out var closestPort)) closestPort.distance = float.MaxValue;

                                    if (dist < closestPort.distance)
                                    {
                                        var playerFwd = player.transform.forward;
                                        float interactDot = Vector3.Dot(interactDir, playerFwd);
                                        if (interactDot > 0.35f)
                                        {
                                            closestPort.port = port;
                                            closestPort.distance = dist;
                                            perPlayerPorts[player] = closestPort;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        var boundsCenter = port.InteractionBoundsPointWorld;
                        int overlapCount = Physics.OverlapSphereNonAlloc(boundsCenter, port.interactionRadius, tempColliders, interactionLayerMask, QueryTriggerInteraction.Ignore);
                        if (overlapCount > 0)
                        {
                            Vector3 interactDir = -port.InteractionDirectionWorld;

                            for (int i = 0; i < overlapCount; i++)
                            {
                                var col = tempColliders[i];
                                var root = col.transform.root;
                                var player = root.gameObject.GetComponent<GrowthChamberPlayer>();
                                if (player == null || !(player.CarriedObject is GrowthChamberRefillCanister canister)) continue;
                              
                                if (player.State == GrowthChamberPlayer.ActionState.Default && player.isOwner && InputProxy.InteractDown)
                                {
                                    float dist = (root.position - port.PortPositionWorld).sqrMagnitude;
                                    if (!perPlayerPorts.TryGetValue(player, out var closestPort)) closestPort.distance = float.MaxValue;

                                    if (dist < closestPort.distance)
                                    {
                                        var playerFwd = player.transform.forward;
                                        float interactDot = Vector3.Dot(interactDir, playerFwd);
                                        if (interactDot > 0.35f)
                                        {
                                            closestPort.port = port;
                                            closestPort.distance = dist;
                                            perPlayerPorts[player] = closestPort;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                foreach (var entry in perPlayerPorts)
                {
                    var player = entry.Key;

                    if (player.IsCarryingObject)
                    {
                        entry.Key.StartLoadingCanister(entry.Value.port);
                    }
                    else
                    {
                        entry.Key.StartUnloadingCanister(entry.Value.port); 
                    }
                }  
            }
        }

    }

    public interface ISerumContainer
    {
        public GrowthSerum Serum { get; set; }

        public float Amount => Serum.Amount;
        public float Fill => Amount / Capacity;
        public float Capacity { get; }
    }

    public static class SerumContainerExtensions
    {

        public static GrowthSerum AddSerum(this ISerumContainer container, GrowthSerum serum)
        {
            var current = container.Serum;
            var next = current + serum;
            var final = next - (next * Mathf.Clamp01((next.Amount - container.Capacity) / next.Amount));

            container.Serum = final;
            return next - final;
        }
        public static GrowthSerum SubtractSerum(this ISerumContainer container, GrowthSerum serum)
        {
            var current = container.Serum;
            var next = current - serum;
            var final = next - (next * Mathf.Clamp01((next.Amount - container.Capacity) / next.Amount));

            container.Serum = final;
            return next - final;
        }
        public static GrowthSerum MultiplySerum(this ISerumContainer container, GrowthSerum serum)
        {
            var current = container.Serum;
            var next = current * serum;
            var final = next - (next * Mathf.Clamp01((next.Amount - container.Capacity) / next.Amount));

            container.Serum = final;
            return next - final;
        }
        public static GrowthSerum DivideSerum(this ISerumContainer container, GrowthSerum serum)
        {
            var current = container.Serum;
            var next = current / serum;
            var final = next - (next * Mathf.Clamp01((next.Amount - container.Capacity) / next.Amount));

            container.Serum = final;
            return next - final;
        }
        public static GrowthSerum MultiplySerum(this ISerumContainer container, float f)
        {
            var current = container.Serum;
            var next = current * f;
            var final = next - (next * Mathf.Clamp01((next.Amount - container.Capacity) / next.Amount));

            container.Serum = final;
            return next - final;
        }
        public static GrowthSerum DivideSerum(this ISerumContainer container, float f) 
        {
            var current = container.Serum;
            var next = current / f;
            var final = next - (next * Mathf.Clamp01((next.Amount - container.Capacity) / next.Amount));

            container.Serum = final;
            return next - final;
        }

        public static void TransferSerum(this ISerumContainer container, ISerumContainer otherContainer, float transferAmount)
        {
            transferAmount = Mathf.Clamp(transferAmount, -1.0f, 1.0f);
            if (transferAmount > 0f)
            {
                if (container.Serum.Amount <= 0f) return;

                var serum = container.Serum * Mathf.Clamp01(Mathf.Abs(transferAmount) / container.Serum.Amount);
                serum -= container.SubtractSerum(serum);
                otherContainer.AddSerum(serum);
            }
            else if (transferAmount < 0f)
            {
                if (otherContainer.Serum.Amount <= 0f) return;

                var serum = otherContainer.Serum * Mathf.Clamp01(Mathf.Abs(transferAmount) / otherContainer.Serum.Amount);   
                serum -= container.AddSerum(serum);
                otherContainer.SubtractSerum(serum); 
            }
        }

    }

    [Serializable]
    public struct SerumColor
    {
        public Color baseColor;
        public Color rimColor;

        public static SerumColor operator +(SerumColor serumA, SerumColor serumB)
        {
            var val = serumA;

            val.baseColor += serumB.baseColor;
            val.rimColor += serumB.rimColor;

            return val;
        }

        public static SerumColor operator -(SerumColor serumA, SerumColor serumB)
        {
            var val = serumA;

            val.baseColor -= serumB.baseColor;
            val.rimColor -= serumB.rimColor;

            return val;
        }

        public static SerumColor operator *(SerumColor serumA, SerumColor serumB)
        {
            var val = serumA;

            val.baseColor *= serumB.baseColor;
            val.rimColor *= serumB.rimColor;

            return val;
        }

        public static SerumColor operator *(SerumColor serumA, float f)
        {
            var val = serumA;

            val.baseColor *= f;
            val.rimColor *= f;

            return val;
        }

        public static SerumColor operator /(SerumColor serumA, float f)
        {
            var val = serumA;

            val.baseColor /= f;
            val.rimColor /= f;

            return val;
        }

        public static SerumColor LerpUnclamped(SerumColor a, SerumColor b, float t)
        {
            var val = a;

            val.baseColor = Color.LerpUnclamped(a.baseColor, b.rimColor, t);
            val.rimColor = Color.LerpUnclamped(a.baseColor, b.rimColor, t);

            return val;
        }
    }
    
    [Serializable]
    public struct GrowthSerum 
    {

        public static SerumColor colorBasis;
        public static SerumColor colorGorillex;
        public static SerumColor colorStallion;
        public static SerumColor colorBull;
        public static SerumColor colorMonk; 

        public SerumColor Color
        {
            get
            {
                float amount = Amount;
                SerumColor col = new SerumColor();

                col = col + (colorBasis * (basis / amount));

                col = col + (colorGorillex * (gorillex.x / amount));
                col = col + (colorStallion * (stallion.x / amount));
                col = col + (colorBull * (bull.x / amount));
                col = col + (colorMonk * (monk.x / amount));  

                col.baseColor.a = 1f;
                col.rimColor.a = 1f;
                return col;
            }
        }

        public float basis;

        /// <summary>
        /// boosts upper body growth
        /// </summary>
        public Vector2 gorillex;

        /// <summary>
        /// boosts lower body growth
        /// </summary>
        public Vector2 stallion;

        /// <summary>
        /// enhances max stamina
        /// </summary>
        public Vector2 bull;

        /// <summary>
        /// enhances stamina regen rate
        /// </summary>
        public Vector2 monk;

        public float Amount => basis + gorillex.x + stallion.x + bull.x + monk.x;

        public static GrowthSerum operator +(GrowthSerum serumA, GrowthSerum serumB)
        {
            var val = serumA;

            val.basis += serumB.basis;

            val.gorillex += serumB.gorillex;
            val.stallion += serumB.stallion;
            val.bull += serumB.bull;
            val.monk += serumB.monk;

            return val;
        }

        public static GrowthSerum operator -(GrowthSerum serumA, GrowthSerum serumB)
        {
            var val = serumA;

            val.basis -= serumB.basis;

            val.gorillex -= serumB.gorillex;
            val.stallion -= serumB.stallion;
            val.bull -= serumB.bull;
            val.monk -= serumB.monk;

            return val; 
        }

        public static GrowthSerum operator *(GrowthSerum serumA, GrowthSerum serumB)
        {
            var val = serumA;

            val.basis *= serumB.basis;

            val.gorillex *= serumB.gorillex;
            val.stallion *= serumB.stallion;
            val.bull *= serumB.bull;
            val.monk *= serumB.monk;

            return val;
        }

        public static GrowthSerum operator /(GrowthSerum serumA, GrowthSerum serumB)
        {
            var val = serumA;

            val.basis /= serumB.basis;

            val.gorillex /= serumB.gorillex;
            val.stallion /= serumB.stallion;
            val.bull /= serumB.bull;
            val.monk /= serumB.monk;

            return val;
        }

        public static GrowthSerum operator *(GrowthSerum serumA, float f)
        {
            var val = serumA;

            val.basis *= f;

            val.gorillex *= f;
            val.stallion *= f;
            val.bull *= f;
            val.monk *= f;

            return val;
        }

        public static GrowthSerum operator /(GrowthSerum serumA, float f)
        {
            var val = serumA;

            val.basis /= f;

            val.gorillex /= f;
            val.stallion /= f;
            val.bull /= f;
            val.monk /= f;

            return val;
        }

        public static GrowthSerum LerpUnclamped(GrowthSerum a, GrowthSerum b, float t) 
        {
            var val = a;

            val.basis = math.lerp(a.basis, b.basis, t);

            val.gorillex = math.lerp(a.gorillex, b.gorillex, t);
            val.stallion = math.lerp(a.stallion, b.stallion, t);
            val.bull = math.lerp(a.bull, b.bull, t);
            val.monk = math.lerp(a.monk, b.monk, t);

            return val;
        }

    }

}