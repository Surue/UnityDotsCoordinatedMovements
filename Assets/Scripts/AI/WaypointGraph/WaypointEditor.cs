﻿using System;
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

[ExecuteInEditMode]
public class WaypointEditor : MonoBehaviour {
    private List<WaypointEditor> previousNeighbors;
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
            //remove an neighbors 
            if (previousNeighbors != null)
            {
                foreach (var waypoint in previousNeighbors)
                {
                    if (!neighbors.Contains(waypoint))
                    {
                        waypoint.neighbors.Remove(this);
                    }
                }
            }
            else
            {
                previousNeighbors = new List<WaypointEditor>();
            }
            
            //Add new neighbors
            foreach (var neighbors in neighbors)
            {
                if(neighbors == null) continue;
                if (!neighbors.neighbors.Contains(this))
                {
                    neighbors.neighbors.Add(this);
                }
            }
            
            previousNeighbors.Clear();

            foreach (var waypointEditor in neighbors)
            {
                if (waypointEditor == null) continue;
                previousNeighbors.Add(waypointEditor);
            }

            for (int i = neighbors.Count - 1; i >= 0; i--)
            {
                if (neighbors[i] == null || neighbors[i] == this)
                {
                    neighbors.RemoveAt(i);
                }
            }
        }
    }

    public void RemoveNeighbors(WaypointEditor waypointEditor)
    {
        if (neighbors.Contains(waypointEditor))
        {
            neighbors.Remove(waypointEditor);
        }

        if (previousNeighbors.Contains(waypointEditor))
        {
            previousNeighbors.Remove(waypointEditor);
        }
    }

    private void OnDestroy()
    {
        foreach (var waypointEditor in neighbors)
        {
            waypointEditor.RemoveNeighbors(this);
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
                if(waypointEditor == null) continue;
                Vector3 dir = waypointEditor.transform.position - transform.position;
                dir.Normalize();
                Vector3 perp = new Vector3(dir.z, 0, -dir.x);
                Gizmos.DrawLine(transform.position + perp * 0.1f, waypointEditor.transform.position + perp * 0.1f);
            }
        }
    }
}
