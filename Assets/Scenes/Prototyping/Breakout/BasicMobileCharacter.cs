using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TempGame;

[RequireComponent(typeof(CharacterController))]
public class BasicMobileCharacter : MonoBehaviour
{

    public float drag = 0.01f;

    public Vector3 gravity;

    private CharacterController characterController;

    [SerializeField]
    private Vector3 velocity;

    public Vector3 nextMoveFrame;

    private Vector3 floorNormal;
    private Vector3 wallNormal;

    public float minFloorDot = 0.35f;
    public float minFloorDotGravity = 0.8f;
    private float floorDot;

    private struct CollisionData
    {
        public Collider collider;
        public Rigidbody rigidbody;
        public Vector3 normal;
    }
    private readonly List<CollisionData> collisions = new List<CollisionData>();

    private void Awake()
    {
        characterController = gameObject.AddOrGetComponent<CharacterController>();
    }
    public void Update()
    {
        Vector3 move = Vector3.zero;

        if (floorDot < minFloorDotGravity) velocity += gravity * Time.deltaTime; else move += Vector3.down * 0.1f;

        velocity = velocity * (1.0f - drag * Time.deltaTime);

        float speed = velocity.magnitude;
        if (speed > 0)
        {
            velocity = velocity - (nextMoveFrame * Mathf.Max(0, -Vector3.Dot(nextMoveFrame.normalized, nextMoveFrame.normalized))) / speed;
            move += velocity;
        }

        collisions.Clear();
        characterController.Move((move * Time.deltaTime) + nextMoveFrame); // creates collisions

        floorDot = -1;
        floorNormal = Vector3.zero;
        wallNormal = Vector3.zero;
        foreach (var collision in collisions)
        {
            float dot = Vector3.Dot(collision.normal, Vector3.up);
            if (dot >= minFloorDot)
            {
                floorNormal = floorNormal + collision.normal;
            } 
            else
            {
                wallNormal = wallNormal + collision.normal;
            }

            floorDot = Mathf.Max(floorDot, dot);
        }
        floorNormal = floorNormal.normalized;
        wallNormal = wallNormal.normalized;

        nextMoveFrame = Vector3.zero;
    }

    float CalculateFriction(PhysicMaterial mat, /*float mass,*/ Vector3 velocity, Vector3 surfaceNormal, float speed)
    {

        float dynamicFriction = 0.9f;
        float staticFriction = 0.9f;
        if (mat != null)
        {
            dynamicFriction = mat.dynamicFriction;
            staticFriction = mat.staticFriction;
        }

        float surfaceAngle = Vector3.Angle(velocity, surfaceNormal);
        float coefficient = speed >= 0.01f ? dynamicFriction : staticFriction;
        //float normalForce = mass * speed;
        float friction = speed * coefficient * Mathf.Cos(surfaceAngle);

        return friction;
    }

    float pushPower = 4.0f;
    void OnControllerColliderHit(ControllerColliderHit hit)
    {

        velocity = velocity - (hit.normal * Vector3.Dot(hit.normal, velocity));

        float speed = velocity.magnitude;
        if (speed > 0)
        {
            Vector3 dir = velocity / speed;
            Vector3 friction = dir * CalculateFriction(hit.collider.sharedMaterial, velocity, hit.normal, speed);
            velocity = velocity + friction * Time.deltaTime;
        } 

        Rigidbody body = hit.collider.attachedRigidbody; 

        collisions.Add(new CollisionData()
        {
            collider = hit.collider,
            rigidbody = body,
            normal = hit.normal
        });

        // no rigidbody
        if (body == null || body.isKinematic)
        {
            return;
        }

        // We dont want to push objects below us
        if (hit.moveDirection.y < -0.3)
        {
            return;
        }

        // Calculate push direction from move direction,
        // we only push objects to the sides never up and down
        Vector3 pushDir = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z) * characterController.velocity.magnitude;// new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);

        // If you know how fast your character is trying to move,
        // then you can also multiply the push velocity by that.

        // Apply the push
        //body.velocity = pushDir * pushPower; // tf?
        body.AddForce(pushDir * pushPower, ForceMode.VelocityChange);
    }

}
