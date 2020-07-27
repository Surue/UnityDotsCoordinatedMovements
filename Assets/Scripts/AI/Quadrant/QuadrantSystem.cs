using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public struct QuadrantData {
    public QuadrantData(float2 pos, float2 vel)
    {
        position = pos;
        velocity = vel;
    }
    
    public float2 position;
    public float2 velocity;
}

[UpdateInGroup(typeof(AiGroup))]
[UpdateAfter(typeof(PathNextStepSystem))]
public class QuadrantSystem : JobComponentSystem
{
    public static NativeMultiHashMap<int, QuadrantData> quadrantMultiHashMap;
    const int quadrantYMultiplier = 1000;
    const int quadrantCellSize = 10;

    protected override void OnCreate()
    {
        base.OnCreate();
        quadrantMultiHashMap = new NativeMultiHashMap<int, QuadrantData>(0, Allocator.Persistent);  
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        quadrantMultiHashMap.Dispose();
    }

    [BurstCompile]
    struct SetQuadrantDataJob : IJobChunk {

        [ReadOnly] public ArchetypeChunkComponentType<Translation> translationType;
        [ReadOnly] public ArchetypeChunkComponentType<Velocity> velocityType;
        
        public NativeMultiHashMap<int, QuadrantData>.ParallelWriter quadrantHashMap;
        
        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex) {
            NativeArray<Translation> chunkTranslations = chunk.GetNativeArray(translationType);
            NativeArray<Velocity> chunkVelocities = chunk.GetNativeArray(velocityType);
            
            for (int i = 0; i < chunk.ChunkEntityCount; i++) {

                int hashMapKey = GetPositionHashMapKey(chunkTranslations[i].Value);
                quadrantHashMap.Add(hashMapKey, new QuadrantData(new float2(chunkTranslations[i].Value.x, chunkTranslations[i].Value.z), chunkVelocities[i].Value));
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetPositionHashMapKey(float3 pos) {
        return (int)math.floor(pos.x / quadrantCellSize) + (int)(quadrantYMultiplier * math.floor(pos.z / quadrantCellSize));
    }
    
    public static void DebugDrawQuadrant(float3 pos) {
        Color color = Color.black;
        Vector3 lowerLeft = new Vector3((math.floor(pos.x / quadrantCellSize)) * quadrantCellSize, 0, math.floor(pos.z / quadrantCellSize) * quadrantCellSize);
        Debug.DrawLine(lowerLeft, lowerLeft + new Vector3(1, 0, 0) * quadrantCellSize, color);
        Debug.DrawLine(lowerLeft, lowerLeft + new Vector3(0, 0, 1) * quadrantCellSize, color);
        Debug.DrawLine(lowerLeft + new Vector3(1, 0, 0) * quadrantCellSize, lowerLeft + new Vector3(1, 0, 1) * quadrantCellSize, color);
        Debug.DrawLine(lowerLeft + new Vector3(0, 0, 1) * quadrantCellSize, lowerLeft + new Vector3(1, 0, 1) * quadrantCellSize, color);
    }

    public static NativeList<int> GetCurrentCellAndNeighborsKeys(float3 pos)
    {
        int currentKey = GetPositionHashMapKey(pos);

        NativeList<int> neighborsKeys = new NativeList<int>(Allocator.Temp) {currentKey};

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

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        EntityQuery query = GetEntityQuery(typeof(Translation), typeof(Velocity));
        
        quadrantMultiHashMap.Clear();
        if (query.CalculateEntityCount() > quadrantMultiHashMap.Capacity) {
            quadrantMultiHashMap.Capacity = query.CalculateEntityCount();
        }
        
        ArchetypeChunkComponentType<Translation> translationChunk =  GetArchetypeChunkComponentType<Translation>();
        ArchetypeChunkComponentType<Velocity> velocityChunk =  GetArchetypeChunkComponentType<Velocity>();

        //Update quadrants data
        SetQuadrantDataJob setQuadrantData = new SetQuadrantDataJob() {
            translationType = translationChunk,
            velocityType = velocityChunk,
            quadrantHashMap = quadrantMultiHashMap.AsParallelWriter()
        };

        return setQuadrantData.Schedule(query, inputDeps);
    }
}
