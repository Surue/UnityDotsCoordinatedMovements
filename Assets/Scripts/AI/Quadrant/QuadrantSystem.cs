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
        }).Schedule();
    }

    public static int GetPositionHashMapKey(float3 pos) {
        return (int)(math.floor(pos.x / quadrantCellSize)) + (int)(quadrantYMultiplier * math.floor(pos.z / quadrantCellSize));
    }
}
