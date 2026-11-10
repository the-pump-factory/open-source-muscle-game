using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class BulkTechFacility : MonoBehaviour
{

    public bool regenerate; 

    public int seed = 0;
    public int generateRoomCount = 15;
    public List<BulkTechFacilityRoom> roomPool = new List<BulkTechFacilityRoom>();
    private List<BulkTechFacilityRoom> roomInstances; 

    private void Awake()
    {
        Generate(); 
    }

    public void Update()
    {
        if (regenerate)
        {
            regenerate = false;
            Generate();
        }
    }

    private readonly List<Vector3Int> connections = new List<Vector3Int>();
    public void Generate()
    {
        if (roomInstances == null) roomInstances = new List<BulkTechFacilityRoom>();
        foreach (var inst in roomInstances) GameObject.Destroy(inst.gameObject); 
        roomInstances.Clear();

        BoxCollider tempCollider = new GameObject("temp_box").AddComponent<BoxCollider>();

        if (seed <= 0) seed = Time.frameCount;
        UnityEngine.Random.InitState(seed);

        for(int a = 0; a < generateRoomCount; a++)
        {
            if (roomInstances.Count <= 0)
            {
                var prefab = roomPool[UnityEngine.Random.Range(0, roomPool.Count)];
                
                var inst = Instantiate(prefab, Vector3.zero, Quaternion.identity);
                inst.RandomConfiguration(); 
                roomInstances.Add(inst);
            } 
            else
            {
                for(int tri = 0; tri < 5; tri++)
                {
                    var prefab = roomPool[UnityEngine.Random.Range(0, roomPool.Count)];
                    BulkTechFacilityRoom inst = null;

                    int b = 0;
                    while(b < roomInstances.Count * 1.3f)
                    {
                        b++;
                        var connectInst = roomInstances[UnityEngine.Random.Range(0, roomInstances.Count)];

                        connections.Clear();
                        connectInst.GetConnectableRooms(prefab, connections);   
                        if (connections.Count <= 0) continue;

                        var connection = connections[UnityEngine.Random.Range(0, connections.Count)];
                        if (inst == null) inst = Instantiate(prefab, Vector3.zero, Quaternion.identity);
                        inst.RandomConfiguration();
                        if (!connectInst.AttemptToPlaceNeighbor(roomInstances, inst, connection, tempCollider)) continue; 
                        
                        roomInstances.Add(inst); 
                        break;
                    }

                    if (inst != null && roomInstances.Count > 0 && ReferenceEquals(roomInstances[roomInstances.Count - 1], inst)) break; else if (inst != null) GameObject.Destroy(inst.gameObject);
                }
            }
        }

        foreach(var inst in roomInstances)
        {
            var spawners = inst.gameObject.GetComponentsInChildren<GroupSpawnables>();
            foreach (var spawner in spawners) 
            { 
                for(int a = 0; a < spawner.defaultSpawnCount; a++) spawner.ChanceSpawnIfNotSpawned(UnityEngine.Random.Range(0, spawner.SpawnGroupCount), UnityEngine.Random.value);  
            }
        }

        Destroy(tempCollider.gameObject); 
    }
}
