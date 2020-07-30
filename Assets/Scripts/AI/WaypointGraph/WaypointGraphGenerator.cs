using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class WaypointGraphGenerator : MonoBehaviour {
    [SerializeField] private float width;
    [SerializeField] private float height;
    
    [SerializeField] private Vector2 offset;

    [SerializeField] private float spaceBetweenWaypoint = 10;

    [SerializeField] private WaypointGraph waypointGraph;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireCube(new Vector3(0 + offset.x, 0, 0 + offset.y), new Vector3(width, 0, height));

        int minX = (int)(offset.x - width * 0.5f);
        int maxX = (int)(offset.x + width * 0.5f);
        
        int minY = (int)(offset.y - height * 0.5f);
        int maxY = (int)(offset.y + height * 0.5f);
        
        for (float x = minX; x < maxX; x += spaceBetweenWaypoint)
        {
            for (float y = minY; y < maxY; y += spaceBetweenWaypoint)
            {
                Gizmos.DrawWireSphere(new Vector3(x, 0, y), 0.5f);
            }
        }
    }

    public WaypointGraph GetWaypointGraph()
    {
        return waypointGraph;
    }
    
    public Vector2 GetOffset()
    {
        return offset;
    }
    
    public float GetWidth()
    {
        return width;
    }
    
    public float GetHeight()
    {
        return height;
    }
    
    public float GetSpace()
    {
        return spaceBetweenWaypoint;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(WaypointGraphGenerator))]
public class WaypointGrapGeneratorEditor : Editor {
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        WaypointGraphGenerator instance = (WaypointGraphGenerator) target;

        if (GUILayout.Button("Generate"))
        {
            var w = instance.GetWaypointGraph();

            foreach (var componentInChild in w.GetComponentsInChildren<WaypointEditor>())
            {
                DestroyImmediate(componentInChild.gameObject);
            }

            var offset = instance.GetOffset();
            var width = instance.GetWidth();
            var height = instance.GetHeight();
            var spaceBetweenWaypoint = instance.GetSpace();
            
            int minX = (int)(offset.x - width * 0.5f);
            int maxX = (int)(offset.x + width * 0.5f);
        
            int minY = (int)(offset.y - height * 0.5f);
            int maxY = (int)(offset.y + height * 0.5f);

            int sizeX = (int) ((maxX - minX) / spaceBetweenWaypoint) + 1;
            int sizeY = (int) ((maxY - minY) / spaceBetweenWaypoint) + 1;
            
            WaypointEditor[,] waypoints = new WaypointEditor[sizeX, sizeY];

            int i = 0, j = 0;
            for (float x = minX; x < maxX; x += spaceBetweenWaypoint)
            {
                for (float y = minY; y < maxY; y += spaceBetweenWaypoint)
                {
                    var obj = new GameObject();
                    obj.AddComponent<WaypointEditor>();
                    obj.transform.parent = w.transform;
                    obj.transform.position = new Vector3(x, 0, y);

                    waypoints[i, j] = obj.GetComponent<WaypointEditor>();
                    j++;
                }

                i++;
                j = 0;
            }
            
            BoundsInt bounds = new BoundsInt(-1, 0, -1, 3, 1, 3);
            for (int k = 0; k < sizeX; k++)
            {
                for (int l = 0; l < sizeY; l++)
                {
                    waypoints[k, l].neighbors = new List<WaypointEditor>();
                    foreach (var index in bounds.allPositionsWithin)
                    {
                        if(index.x == 0 && index.z == 0) continue;
                        if(k + index.x < 0 || k + index.x >= sizeX || l + index.z < 0 || l + index.z >= sizeY) continue;
                        
                        waypoints[k, l].neighbors.Add(waypoints[k + index.x, l + index.z]);
                    }
                }
            }
        }
    }
}

#endif
