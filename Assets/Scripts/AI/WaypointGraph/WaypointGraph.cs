using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class WaypointGraph : MonoBehaviour {
    private NativeArray<Waypoint> waypoints;
    private NativeArray<WaypointNeighbors> neighbors;

    void Start()
    {
        var tmpWaypoints = GetComponentsInChildren<WaypointEditor>();

        waypoints = new NativeArray<Waypoint>(tmpWaypoints.Length, Allocator.Persistent);
        

        //Set index for every waypoint in scene and count neighbors
        int neighborsCount = 0;
        for (int i = 0; i < tmpWaypoints.Length; i++)
        {
            tmpWaypoints[i].Index = i;
            
            //New waypoint
            waypoints[i] = new Waypoint()
            {
                position = new float2(tmpWaypoints[i].transform.position.x, tmpWaypoints[i].transform.position.z),
                firstNeighbors = neighborsCount,
                neigborCount = tmpWaypoints[i].neighbors.Count
            };
            
            neighborsCount += tmpWaypoints[i].neighbors.Count;
        }
        neighbors = new NativeArray<WaypointNeighbors>(neighborsCount, Allocator.Persistent);

        //Construct neighbors
        for (int i = 0; i < waypoints.Length; i++)
        {
            Vector3 pos = tmpWaypoints[i].transform.position;
            
            //Get all neighbors
            for (int j = 0; j < waypoints[i].neigborCount; j++)
            {
                int neighborIndex = tmpWaypoints[i].neighbors[j].Index;

                neighbors[waypoints[i].firstNeighbors + j] = new WaypointNeighbors()
                {
                    moveCost = Vector3.Distance(pos, tmpWaypoints[neighborIndex].transform.position),
                    neighborsIndex = neighborIndex
                };

                Debug.Log("["+j+"] = " + (waypoints[i].firstNeighbors + j));
            }
        }
        
        //Destroy old waypoint
        for (int i = tmpWaypoints.Length - 1; i >= 0; i--)
        {
            Destroy(tmpWaypoints[i].gameObject);
        }
    }

    private void OnDestroy()
    {
        waypoints.Dispose();
        neighbors.Dispose();
    }

    private void OnDrawGizmos()
    {
        if (waypoints.IsCreated)
        {
            //Draw sphere
            for (int i = 0; i < waypoints.Length; i++)
            {
                Vector3 pos = new Vector3(waypoints[i].position.x, 0, waypoints[i].position.y);
                Gizmos.DrawWireSphere(pos, 0.5f);

                //Draw lines with neighbors
                for (int j = 0; j < waypoints[i].neigborCount; j++)
                {
                    int neighborIndex = neighbors[j + waypoints[i].firstNeighbors].neighborsIndex;

                    Vector3 neighborPos = new Vector3(waypoints[neighborIndex].position.x, 0,
                        waypoints[neighborIndex].position.y);

                    Vector3 dir = pos - neighborPos;
                    
                    Vector3 perp = new Vector3(dir.z, 0, -dir.x).normalized;
                    
                    Gizmos.DrawLine(pos + perp * 0.1f, neighborPos + perp * 0.1f);
                }
            }
        }
    }
}