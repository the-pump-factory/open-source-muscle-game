using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class BulkTechFacilityRoom : MonoBehaviour
{

#if UNITY_EDITOR

    protected void OnDrawGizmos()
    {
        if (Application.isPlaying) return;

        UnityEngine.Random.InitState(GetInstanceID());

        var col = Color.white;
        col.a = 0.3f;

        Gizmos.color = col;

        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(boundsCenter, boundsExtents * 2);
    }

#endif

    [Serializable, Flags]
    public enum RoomConnectionSocket
    {
        None = 0, A = 1, B = 2, C = 4, D = 8, E = 16, F = 32, G = 64, H = 128, I = 256, J = 512, K = 1024, L = 2048, M = 4096, N = 8192, O = 16384, P = 32768, Q = 65536, R = 131072, S = 262144, T = 524288, U = 1048576, V = 2097152, W = 4194304, X = 8388608, Y = 16777216, Z = 33554432
    }

    public Vector3 boundsCenter;
    public Vector3 boundsExtents;

    public List<BulkTechFacilityWall> walls = new List<BulkTechFacilityWall>();

    public void ResetConfiguration()
    {
        if (walls != null)
        {
            foreach (var wall in walls) if (!wall.locked) wall.gameObject.SetActive(true);
        }
    }

    private static readonly List<int> tempIndices = new List<int>();
    private static readonly List<int> closedIndices = new List<int>();
    public void RandomConfiguration()
    {
        ResetConfiguration();

        if (walls != null)
        {
            foreach (var wall in walls) 
            { 
                if (wall.requireConnection && !wall.locked) wall.gameObject.SetActive(false); 
            }

            for(int a = 0; a < walls.Count; a++)
            {
                var wall = walls[a];

                if (wall.requireConnection || wall.locked || closedIndices.Contains(a)) continue; 

                if (wall.group <= 0)
                {
                    if (wall.mandatory) wall.gameObject.SetActive(true); else wall.gameObject.SetActive(UnityEngine.Random.value > 0.5f);
                } 
                else
                {
                    bool mandatory = wall.mandatory;
                    tempIndices.Clear();
                    for (int b = 0; b < walls.Count; b++)
                    {
                        var wallB = walls[b];
                        if (wallB.group == wall.group)
                        {
                            tempIndices.Add(b);
                            mandatory = mandatory || wallB.mandatory;
                        }
                    }

                    int randomInd = tempIndices[UnityEngine.Random.Range(0, tempIndices.Count)];
                    var randWall = walls[randomInd];
                    if (!randWall.locked)
                    {
                        if (mandatory || randWall.mandatory) wall.gameObject.SetActive(true); else wall.gameObject.SetActive(UnityEngine.Random.value > 0.5f);
                    }
                    foreach(var ind in tempIndices)
                    {
                        if (ind == randomInd) continue; 

                        var wallB = walls[ind];
                        if (!wallB.locked) wall.gameObject.SetActive(false); 
                    }

                    closedIndices.Add(a);
                }
            }
        }
    }

    public static bool CheckCollision(Vector3 roomAPosition, Quaternion roomARotation, BulkTechFacilityRoom roomA, bool ignoreDisabledRoomsA, Vector3 roomBPosition, Quaternion roomBRotation, BulkTechFacilityRoom roomB, bool ignoreDisabledRoomsB, BoxCollider colliderInstance = null, int collisionLayer = 30)
    {
        return CheckCollision(new Vector3Int(-1, -1, -1), roomAPosition, roomARotation, roomA, ignoreDisabledRoomsA, roomBPosition, roomBRotation, roomB, ignoreDisabledRoomsB, colliderInstance, collisionLayer);
    }
    public static bool CheckCollision(Vector3Int connection, Vector3 roomAPosition, Quaternion roomARotation, BulkTechFacilityRoom roomA, bool ignoreDisabledRoomsA, Vector3 roomBPosition, Quaternion roomBRotation, BulkTechFacilityRoom roomB, bool ignoreDisabledRoomsB, BoxCollider colliderInstance = null, int collisionLayer = 30)
    {
        bool destroyColliderInstance = false;
        if (colliderInstance == null)
        {
            colliderInstance = new GameObject("temp_box").AddComponent<BoxCollider>();
        }

        var connectingWallA = connection.x >= 0 && roomA.walls != null ? roomA.walls[connection.x] : null;
        var connectingWallB = connection.y >= 0 && roomB.walls != null ? roomB.walls[connection.y] : null;

        var roomAToLocal = roomA.transform.worldToLocalMatrix;
        var roomBToLocal = roomB.transform.worldToLocalMatrix;
        var roomAToLocalRot = Quaternion.Inverse(roomA.transform.rotation);
        var roomBToLocalRot = Quaternion.Inverse(roomB.transform.rotation);

        var roomATRS = Matrix4x4.TRS(roomAPosition, roomARotation, Vector3.one);
        var roomBTRS = Matrix4x4.TRS(roomBPosition, roomBRotation, Vector3.one);

        var colliderTransform = colliderInstance.transform;

        int originalLayer = colliderInstance.gameObject.layer;
        Transform originalParent = colliderTransform.parent;
        colliderTransform.GetPositionAndRotation(out var originalPos, out var originalRot);
        Vector3 originalScale = colliderTransform.localScale;
        Vector3 originalCenter = colliderInstance.center;
        Vector3 originalSize = colliderInstance.size;

        colliderInstance.gameObject.layer = collisionLayer;
        var layerMask = 1 << collisionLayer;

        colliderTransform.SetParent(null, false);
        colliderTransform.localScale = Vector3.one;

        colliderInstance.center = roomA.boundsCenter;
        colliderInstance.size = roomA.boundsExtents * 2;
        colliderTransform.SetPositionAndRotation(roomAPosition, roomARotation);
        Physics.SyncTransforms();

        if (Physics.CheckBox(roomBTRS.MultiplyPoint(roomB.boundsCenter), roomB.boundsExtents, roomBRotation, layerMask, QueryTriggerInteraction.Ignore))
        {
            return true;
        }

        if (roomB.walls != null)
        {
            for(int a = 0; a < roomB.walls.Count; a++)
            {
                var wall = roomB.walls[a];
                if (ignoreDisabledRoomsB && a != connection.y && !wall.isActiveAndEnabled) continue;

                if (connectingWallB != null && connectingWallB.group > 0 && wall.group == connectingWallB.group && !ReferenceEquals(wall, connectingWallB) && !wall.locked) continue;

                var wallTransform = wall.transform;
                var toWorld = roomBTRS * (roomBToLocal * wallTransform.localToWorldMatrix);
                var rot = roomBRotation * (roomBToLocalRot * wallTransform.rotation);
                if (Physics.CheckBox(toWorld.MultiplyPoint(wall.boundsCenter), wall.boundsExtents, rot, layerMask, QueryTriggerInteraction.Ignore))
                {
                    return true;
                }
            }
        }

        colliderInstance.center = roomB.boundsCenter;
        colliderInstance.size = roomB.boundsExtents * 2;
        colliderTransform.SetPositionAndRotation(roomBPosition, roomBRotation);
        Physics.SyncTransforms();

        if (roomA.walls != null)
        {
            for (int a = 0; a < roomA.walls.Count; a++)
            {
                var wall = roomA.walls[a];
                if (ignoreDisabledRoomsA && a != connection.x && !wall.isActiveAndEnabled) continue;

                if (connectingWallA != null && connectingWallA.group > 0 && wall.group == connectingWallA.group && !ReferenceEquals(wall, connectingWallA) && !wall.locked) continue;

                var wallTransform = wall.transform;
                var toWorld = roomATRS * (roomAToLocal * wallTransform.localToWorldMatrix);
                var rot = roomARotation * (roomAToLocalRot * wallTransform.rotation);
                if (Physics.CheckBox(toWorld.MultiplyPoint(wall.boundsCenter), wall.boundsExtents, rot, layerMask, QueryTriggerInteraction.Ignore))
                {
                    return true;
                }
            }
        }

        if (roomA.walls != null && roomB.walls != null)
        {
            for (int a = 0; a < roomA.walls.Count; a++)
            {
                var wallA = roomA.walls[a];
                if (ignoreDisabledRoomsA && a != connection.x && !wallA.isActiveAndEnabled) continue;

                if (connectingWallA != null && connectingWallA.group > 0 && wallA.group == connectingWallA.group && !ReferenceEquals(wallA, connectingWallA) && !wallA.locked) continue;

                var wallTransform = wallA.transform;
                var toWorld = roomATRS * (roomAToLocal * wallTransform.localToWorldMatrix);
                var boundsPosA = toWorld.MultiplyPoint(wallA.boundsCenter);
                var rot = roomARotation * (roomAToLocalRot * wallTransform.rotation); 

                for (int b = 0; b < roomB.walls.Count; b++)
                {
                    var wallB = roomB.walls[b];
                    if (ignoreDisabledRoomsB && b != connection.y && !wallB.isActiveAndEnabled) continue;

                    if (connectingWallB != null && connectingWallB.group > 0 && wallB.group == connectingWallB.group && !ReferenceEquals(wallB, connectingWallB) && !wallB.locked) continue;
                     
                    colliderInstance.center = wallB.boundsCenter;
                    colliderInstance.size = wallB.boundsExtents * 2;
                    colliderTransform.SetPositionAndRotation(roomBTRS.MultiplyPoint(roomBToLocal.MultiplyPoint(wallB.transform.position)), roomBRotation * (roomBToLocalRot * wallB.transform.rotation));
                    Physics.SyncTransforms(); 

                    if (Physics.CheckBox(boundsPosA, wallA.boundsExtents, rot, layerMask, QueryTriggerInteraction.Ignore))
                    {
                        return true;
                    }
                }
            }
        }

        if (destroyColliderInstance) 
        { 
            GameObject.Destroy(colliderInstance.gameObject); 
        }
        else
        {
            colliderInstance.gameObject.layer = originalLayer;
            colliderTransform.SetParent(originalParent, false);
            colliderTransform.SetPositionAndRotation(originalPos, originalRot);
            colliderTransform.localScale = originalScale;

            colliderInstance.center = originalCenter;
            colliderInstance.size = originalSize;
        }

        return false;
    }

    public List<Vector3Int> GetConnectableRooms(BulkTechFacilityRoom otherRoom, List<Vector3Int> outputList = null)
    {
        if (outputList == null) outputList = new List<Vector3Int>();

        if (walls == null || otherRoom.walls == null) return outputList;

        var socketTypes = Enum.GetValues(typeof(RoomConnectionSocket));
        for (int a = 0; a < walls.Count; a++)
        {
            var wallA = walls[a];
            if (wallA.locked) continue;

            for (int b = 0; b < otherRoom.walls.Count; b++)
            {
                var wallB = otherRoom.walls[b]; 
                if (wallB.locked) continue;

                foreach(Enum socketType in socketTypes)
                {
                    int socketInt = Convert.ToInt32(socketType);
                    if (socketInt <= 0) continue;

                    if (wallA.socketIn.HasFlag(socketType) && wallB.socketOut.HasFlag(socketType))
                    {
                        outputList.Add(new Vector3Int(a, b, socketInt)); 
                        break;
                    }
                }
            }
        }

        return outputList;
    }

    public bool AttemptToPlaceNeighbor(List<BulkTechFacilityRoom> placedRooms, BulkTechFacilityRoom neighborInstance, Vector3Int connection, BoxCollider colliderInstance = null, int collisionLayer = 30)
    {

        var localWall = walls[connection.x];
        var otherWall = neighborInstance.walls[connection.y];

        var localWallTransform = localWall.transform;
        var otherWallTransform = otherWall.transform;

        var otherRoomTransform = neighborInstance.transform;
        otherRoomTransform.GetPositionAndRotation(out var otherRoomPosition, out var otherRoomRotation);

        Vector3 localConnectionPoint = localWallTransform.TransformPoint(localWall.connectionPoint);
        Vector3 localConnectionDir = localWallTransform.TransformDirection(localWall.connectionNormal);
        //Vector3 localConnectionAxis = localWallTransform.TransformDirection(localWall.connectionAngleAxis);

        Vector3 otherConnectionPoint = otherWallTransform.TransformPoint(otherWall.connectionPoint);
        Vector3 otherConnectionDir = otherWallTransform.TransformDirection(otherWall.connectionNormal);
        Vector3 otherConnectionAxis = otherWallTransform.TransformDirection(otherWall.connectionAngleAxis);
        Quaternion connectionRotation = Quaternion.AngleAxis(Vector3.SignedAngle(otherConnectionDir, -localConnectionDir, otherConnectionAxis), otherConnectionAxis);//Quaternion.FromToRotation(otherConnectionDir, -localConnectionDir);
        
        Vector3 offset = localConnectionPoint - ((connectionRotation * (otherConnectionPoint - otherRoomPosition)) + otherRoomPosition); 

        Quaternion newRotation = connectionRotation * otherRoomRotation;  
        Vector3 newPosition = otherRoomPosition + offset;

        transform.GetPositionAndRotation(out var position, out var rotation);
        if (CheckCollision(connection, position, rotation, this, true, newPosition, newRotation, neighborInstance, true, colliderInstance, collisionLayer)) return false;
        if (placedRooms != null)
        {
            foreach (var room in placedRooms) 
            {
                if (ReferenceEquals(room, this) || ReferenceEquals(room, neighborInstance)) continue;

                room.transform.GetPositionAndRotation(out var rPos, out var rRot);
                if (CheckCollision(rPos, rRot, room, true, newPosition, newRotation, neighborInstance, true, colliderInstance, collisionLayer)) return false; 
            }
        }

        if (localWall.group > 0) foreach (var wall in walls) if (!wall.locked && wall.group == localWall.group && !ReferenceEquals(wall, localWall)) wall.gameObject.SetActive(false);
        if (otherWall.group > 0) foreach (var wall in neighborInstance.walls) if (!wall.locked && wall.group == otherWall.group && !ReferenceEquals(wall, otherWall)) wall.gameObject.SetActive(false);

        localWall.gameObject.SetActive(true);
        otherWall.gameObject.SetActive(true);

        //Debug.Log(newPosition + " : " + localWall.transform.parent.name + "/" + localWall.name + " -> " + otherWall.transform.parent.name + "/" + otherWall.name);
        otherRoomTransform.SetPositionAndRotation(newPosition, newRotation); 

        localWall.locked = true;
        otherWall.locked = true;

        return true;
    }

}
