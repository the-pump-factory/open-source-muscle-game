using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using PurrNet;
using Swole;
using Swole.API.Unity.Animation;

namespace BulkOut
{

    public class GrowthChamberRefillCanister : NetworkBehaviour, IBulkOutCarryable, ISerumContainer
    {

        public GameObject GameObject => gameObject;
        public Transform Transform => transform;

        public void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.TransformPoint(unscrewDir * unscrewMaxOffset), transform.TransformPoint(unscrewDir * -carryOffset));

            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.TransformPoint(pickupLocalOffset) + pickupOffset, pickupRadius);

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.TransformPoint(-unscrewDir * length));
        }

        [SerializeField]
        protected LiquidInBottleShaderSync serumShaderSync;

        [SerializeField]
        protected Transform cap;

        public float popForce = 2f;

        public float carryOffset = 0.5f;

        public float length = 1f;

        public Vector3 unscrewDir = Vector3.up;
        public float unscrewMaxOffset = 0.1f;
        public float unscrewMaxRotation = 1440f;

        [Range(0f, 1f)]
        public SyncVar<float> unscrew;

        public bool HasCap => cap != null;

        [ObserversRpc()]
        public void PopCap()
        {
            unscrew.value = 1f;
            if (cap != null)
            {
                cap.SetParent(null, true);
                var rb = cap.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = cap.gameObject.AddComponent<Rigidbody>();
                    rb.mass = 1f;
                }

                rb.AddForce(transform.TransformDirection(unscrewDir) * popForce, ForceMode.Impulse);

                cap = null;
            }
        }

        public float pickupRadius = 0.5f;
        public Vector3 pickupLocalOffset = Vector3.zero;
        public Vector3 pickupOffset = new Vector3(0f, 1f, 0f);

        public LayerMask pickupLayerMask;
        public bool pickupOriginIsLeft;
        public Vector3 pickupOriginLocalOffset;
        public Vector3 pickupEndLocalOffset;

        protected Quaternion capStartRot;
        protected Vector3 capStartPos;

        new protected Rigidbody rigidbody;
        public Rigidbody Rigidbody => rigidbody;

        protected void Awake()
        {
            if (cap != null)
            {
                capStartRot = cap.localRotation;
                capStartPos = cap.localPosition;
            }

            rigidbody = gameObject.GetComponent<Rigidbody>();

            if (serumShaderSync == null) serumShaderSync = gameObject.GetComponent<LiquidInBottleShaderSync>();

            var cont = ((ISerumContainer)this);
            var serum = new GrowthSerum();
            this.Serum = serum;

            cont.AddSerum(new GrowthSerum() { basis = UnityEngine.Random.value * 15f });
            cont.AddSerum(new GrowthSerum() { gorillex = new Vector2(UnityEngine.Random.value * 15f, 1f) });
            cont.AddSerum(new GrowthSerum() { stallion = new Vector2(UnityEngine.Random.value * 15f, 1f) });
            cont.AddSerum(new GrowthSerum() { bull = new Vector2(UnityEngine.Random.value * 15f, 1f) });
            cont.AddSerum(new GrowthSerum() { monk = new Vector2(UnityEngine.Random.value * 15f, 1f) }); 
        }

        protected static readonly Collider[] tempColliders = new Collider[64];
        public void LateUpdate()
        {
            if (cap != null)
            {
                cap.SetLocalPositionAndRotation(capStartPos + unscrewDir * unscrew * unscrewMaxOffset, Quaternion.AngleAxis(unscrewMaxRotation * unscrew, unscrewDir) * capStartRot);
            }

            if (!IsBeingCarried && !rigidbody.isKinematic)
            {
                int overlapCount = Physics.OverlapSphereNonAlloc(transform.TransformPoint(pickupLocalOffset) + pickupOffset, pickupRadius, tempColliders, pickupLayerMask, QueryTriggerInteraction.Ignore);
                if (overlapCount > 0)
                {
                    for (int i = 0; i < overlapCount; i++)
                    {
                        var col = tempColliders[i];
                        var root = col.transform.root;
                        var player = root.gameObject.GetComponent<GrowthChamberPlayer>();
                        if (player == null || player.IsCarryingObject) continue;

                        if (player.isOwner && InputProxy.ObjectInteraction_PickupPressed)
                        {
                            //player.Pickup(this);
                            player.RegisterForPickup(this, Vector3.Distance(root.position, transform.position));
                        }
                    }
                }
            }

            if (serumShaderSync != null) 
            { 
                serumShaderSync.fillAmount = ((ISerumContainer)this).Fill;
                var mats = serumShaderSync.Materials;
                if (mats != null)
                {
                    var serumColor = Serum.Color;
                    foreach(var mat in mats)
                    {
                        mat.SetColor("_BaseColor", serumColor.baseColor); 
                        mat.SetColor("_TopColor", serumColor.baseColor);

                        mat.SetColor("_RimColor", serumColor.rimColor); 
                    }
                }
            }
        }

        [NonSerialized]
        protected IBulkOutObjectCarrier carrier;

        public IBulkOutObjectCarrier Carrier
        {
            get => carrier;
            set => carrier = value;
        }

        [NonSerialized]
        protected Transform leftHolder;
        public Transform LeftHolder
        {
            get => leftHolder;
            set
            {
                leftHolder = value;
                if (pickupSync != null) pickupSync.leftHolder = leftHolder;
            }
        }

        [NonSerialized]
        protected Transform rightHolder;
        public Transform RightHolder
        {
            get => rightHolder;
            set
            {
                rightHolder = value;
                if (pickupSync != null) pickupSync.rightHolder = rightHolder;
            }
        }

        protected PickupSync pickupSync;
        public bool IsBeingCarried => carrier != null;

        public void StartSyncingWithHolder()
        {
            if (pickupSync != null) pickupSync.AddToCallstack();
        }
        public void StopSyncingWithHolder()
        {
            if (pickupSync != null) pickupSync.RemoveFromCallstack();
        }

        public bool Pickup(IBulkOutObjectCarrier carrier, Transform leftHolder, Transform rightHolder)
        {
            if (IsBeingCarried && this.carrier != carrier) return false;

            if (rigidbody != null)
            {
                if (!rigidbody.isKinematic)
                {
                    if (pickupSync == null) pickupSync = new PickupSync();

                    pickupSync.canister = this;
                    pickupSync.leftHolder = leftHolder;
                    pickupSync.rightHolder = rightHolder;

                    StartSyncingWithHolder();

                    rigidbody.isKinematic = true;
                }
            }

            transform.SetParent(carrier.Transform, true);

            this.carrier = carrier;

            return true;
        }
        public bool Drop()
        {
            StopSyncingWithHolder();

            if (rigidbody != null) rigidbody.isKinematic = false;

            if (carrier != null)
            {
                if (transform.parent == carrier.Transform) transform.SetParent(null, true); 

                carrier = null;
                leftHolder = null;
                rightHolder = null;

                return true;
            }

            return false;
        }

        public void Toss(Vector3 velocity)
        {
            Drop();

            if (rigidbody != null) rigidbody.AddForce(velocity, ForceMode.VelocityChange);
        }


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


        public static GrowthChamberRefillCanister operator +(GrowthChamberRefillCanister serumA, GrowthSerum serumB)
        {
            serumA.Serum += serumB;
            return serumA;
        }

        public static GrowthChamberRefillCanister operator -(GrowthChamberRefillCanister serumA, GrowthSerum serumB)
        {
            serumA.Serum -= serumB;
            return serumA;
        }

        public static GrowthChamberRefillCanister operator *(GrowthChamberRefillCanister serumA, GrowthSerum serumB)
        {
            serumA.Serum *= serumB;
            return serumA;
        }

        public static GrowthChamberRefillCanister operator /(GrowthChamberRefillCanister serumA, GrowthSerum serumB)
        {
            serumA.Serum /= serumB;
            return serumA;
        }

        public static GrowthChamberRefillCanister operator *(GrowthChamberRefillCanister serumA, float f)
        {
            serumA.Serum *= f;
            return serumA;
        }

        public static GrowthChamberRefillCanister operator /(GrowthChamberRefillCanister serumA, float f)
        {
            serumA.Serum /= f;
            return serumA;
        }

        protected class PickupSync : IExecutableBehaviour
        {

            public GrowthChamberRefillCanister canister;

            public Transform leftHolder;
            public Transform rightHolder;

            public int Priority => CustomAnimatorUpdater.FinalAnimationBehaviourPriority + 1;

            public int UpdatePriority => Priority;

            public int LateUpdatePriority => Priority;

            public int FixedUpdatePriority => Priority;

            public int CompareTo(IExecutableBehaviour other)
            {
                return Priority.CompareTo(other.Priority);
            }

            public void OnFixedUpdate()
            {
            }

            public void OnLateUpdate()
            {
                var leftHolder = this.leftHolder;
                var rightHolder = this.rightHolder;
                if (!canister.pickupOriginIsLeft)
                {
                    rightHolder = this.leftHolder;
                    leftHolder = this.rightHolder;
                }

                if (canister != null && leftHolder != null && rightHolder != null)
                {
                    var originPos = leftHolder.TransformPoint(canister.pickupOriginLocalOffset);
                    var endPos = rightHolder.TransformPoint(canister.pickupEndLocalOffset);
                    Vector3 dir = (endPos - originPos).normalized;
                    canister.transform.position = originPos + dir * canister.carryOffset;
                    canister.transform.rotation = Quaternion.FromToRotation(canister.transform.rotation * canister.unscrewDir, dir) * canister.transform.rotation;
                }
            }

            public void OnPostFixedUpdate()
            {
            }

            public void OnPreFixedUpdate()
            {
            }

            public void OnUpdate()
            {
            }

        }

    }

}
