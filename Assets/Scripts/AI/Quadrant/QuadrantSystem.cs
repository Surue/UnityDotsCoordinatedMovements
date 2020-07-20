using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public struct NeighborData {
    public NeighborData(float2 pos, float2 vel)
    {
        position = pos;
        velocity = vel;
    }
    
    public float2 position;
    public float2 velocity;
}

public class QuadrantSystem : SystemBase
{
    public static NativeMultiHashMap<int, NeighborData> quadrantMultiHashMap;
    const int quadrantYMultiplier = 1000;
    const int quadrantCellSize = 10;

    protected override void OnCreate()
    {
        base.OnCreate();
        quadrantMultiHashMap = new NativeMultiHashMap<int, NeighborData>(0, Allocator.Persistent);  
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        quadrantMultiHashMap.Dispose();
    }

    protected override void OnUpdate()
    {
        EntityQuery queryFollower = GetEntityQuery(typeof(FormationFollower));
        EntityQuery queryLeader = GetEntityQuery(typeof(FormationLeader));
        
        quadrantMultiHashMap.Clear();
        int count = queryFollower.CalculateEntityCount() + queryLeader.CalculateEntityCount();
        if (count > quadrantMultiHashMap.Capacity) {
            quadrantMultiHashMap.Capacity = count;
        }
        
        //TODO Change to use burst compiler
        Entities.WithoutBurst().WithAny<FormationFollower, FormationLeader>().ForEach((in Translation translation, in Velocity velocity) =>
        {
            int hashMapKey = GetPositionHashMapKey(translation.Value);
            quadrantMultiHashMap.Add(hashMapKey, new NeighborData(new float2(translation.Value.x, translation.Value.z), velocity.Value));
        }).Run();
    }

    public static int GetPositionHashMapKey(float3 pos) {
        return (int)math.floor(pos.x / quadrantCellSize) + (int)(quadrantYMultiplier * math.floor(pos.z / quadrantCellSize));
    }
    
    public static void DebugDrawQuadrant(float3 pos) {
        Vector3 lowerLeft = new Vector3((math.floor(pos.x / quadrantCellSize)) * quadrantCellSize, 0, math.floor(pos.z / quadrantCellSize) * quadrantCellSize);
        Debug.DrawLine(lowerLeft, lowerLeft + new Vector3(1, 0, 0) * quadrantCellSize);
        Debug.DrawLine(lowerLeft, lowerLeft + new Vector3(0, 0, 1) * quadrantCellSize);
        Debug.DrawLine(lowerLeft + new Vector3(1, 0, 0) * quadrantCellSize, lowerLeft + new Vector3(1, 0, 1) * quadrantCellSize);
        Debug.DrawLine(lowerLeft + new Vector3(0, 0, 1) * quadrantCellSize, lowerLeft + new Vector3(1, 0, 1) * quadrantCellSize);
    }

    public static NativeList<int> GetCurrentCellAndNeighborsKeys(float3 pos)
    {
        int currentKey = GetPositionHashMapKey(pos);
        
        NativeList<int> neighborsKeys = new NativeList<int>(Allocator.Temp);
        neighborsKeys.Add(currentKey);

        float3 lowerLeft = new float3((math.floor(pos.x / quadrantCellSize)) * quadrantCellSize, 0, math.floor(pos.z / quadrantCellSize) * quadrantCellSize);
        float3 topRight = lowerLeft + new float3(1, 0, 1) * quadrantCellSize;
        
        //Check bottom
        bool bottom = false;
        if (math.length(math.cross(new float3(1, 0, 0), pos - lowerLeft)) < 3)
        {
            neighborsKeys.Add(currentKey - quadrantYMultiplier);
            bottom = true;
        } 
        
        //Check left
        bool left = false;
        if (math.length(math.cross(new float3(0, 0, 1), pos - lowerLeft)) < 3)
        {
            neighborsKeys.Add(currentKey - 1);
            left = true;
        } 
        
        //Check top
        bool top = false;
        if (math.length(math.cross(new float3(1, 0, 0), pos - topRight)) < 3)
        {
            neighborsKeys.Add(currentKey + quadrantYMultiplier);
            top = true;
        } 
        
        //Check right
        bool right = false;
        if (math.length(math.cross(new float3(0, 0, 1), pos - topRight)) < 3)
        {
            neighborsKeys.Add(currentKey + 1);
            right = true;
        } 
        
        //Check bottomLeft
        if (bottom && left)
        {
            neighborsKeys.Add(currentKey - quadrantYMultiplier - 1);
        }
        
        //Check topLeft
        if (top && left)
        {
            neighborsKeys.Add(currentKey + quadrantYMultiplier - 1);
        }
        
        //Check bottomRight
        if (bottom && right)
        {
            neighborsKeys.Add(currentKey - quadrantYMultiplier + 1);
        }
        
        //Check bottomRight
        if (top && right)
        {
            neighborsKeys.Add(currentKey + quadrantYMultiplier + 1);
        }
        
        return neighborsKeys;
    }
}
