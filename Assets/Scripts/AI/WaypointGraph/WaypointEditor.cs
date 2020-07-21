using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct WaypointNeighbors {
    public float moveCost;
    public int neighborsIndex;
}

public struct Waypoint {
    public float2 position;
    public int firstNeighbors;
    public int neigborCount;
}

public class WaypointEditor : MonoBehaviour {
    public List<WaypointEditor> neighbors;

    private int index;

    public int Index
    {
        get => index;
        set => index = value;
    }

    private void OnValidate()
    {
        if (neighbors != null)
        {
            foreach (var neighbors in neighbors)
            {
                bool exist = false;
                foreach (var indirectNeighbors in neighbors.neighbors)
                {
                    if (indirectNeighbors == this)
                    {
                        exist = true;
                        break;
                    }
                }

                if (!exist)
                {
                    neighbors.neighbors.Add(this);
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.5f);

        if (neighbors != null)
        {
            foreach (var waypointEditor in neighbors)
            {
                Vector3 dir = waypointEditor.transform.position - transform.position;
                dir.Normalize();
                Vector3 perp = new Vector3(dir.z, 0, -dir.x);
                Gizmos.DrawLine(transform.position + perp * 0.1f, waypointEditor.transform.position + perp * 0.1f);
            }
        }
    }
}
