using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Debug = UnityEngine.Debug;

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
public class QuadrantSystem : JobComponentSystem {
    public static NativeMultiHashMap<int, QuadrantData> quadrantMultiHashMap;
    const int quadrantYMultiplier = 1000;
    const float quadrantCellSize = 15.0f;
    const float neighborsCellDistance = quadrantCellSize / 2.0f;

    //Timer specific
    private TimeRecorder timerRecoder;
    static Stopwatch timer = new System.Diagnostics.Stopwatch();
    private static double time = 0;
    //Timer specific

    protected override void OnCreate()
    {
        base.OnCreate();
        quadrantMultiHashMap = new NativeMultiHashMap<int, QuadrantData>(0, Allocator.Persistent);
        
        //Timer specific
        timerRecoder = new TimeRecorder("QuadrantSystem");
        //Timer specific
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

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            NativeArray<Translation> chunkTranslations = chunk.GetNativeArray(translationType);
            NativeArray<Velocity> chunkVelocities = chunk.GetNativeArray(velocityType);

            for (int i = 0; i < chunk.ChunkEntityCount; i++)
            {
                int hashMapKey = GetPositionHashMapKey(chunkTranslations[i].Value.xz);
                quadrantHashMap.Add(hashMapKey,
                    new QuadrantData(new float2(chunkTranslations[i].Value.x, chunkTranslations[i].Value.z),
                        chunkVelocities[i].Value));
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetPositionHashMapKey(float2 pos)
    {
        return (int) math.floor(pos.x / quadrantCellSize) +
               (int) (quadrantYMultiplier * math.floor(pos.y / quadrantCellSize));
    }

    public static void DebugDrawQuadrant(float3 pos)
    {
        Color color = Color.black;
        Vector3 lowerLeft = new Vector3((math.floor(pos.x / quadrantCellSize)) * quadrantCellSize, 0,
            math.floor(pos.z / quadrantCellSize) * quadrantCellSize);
        Debug.DrawLine(lowerLeft, lowerLeft + new Vector3(1, 0, 0) * quadrantCellSize, color);
        Debug.DrawLine(lowerLeft, lowerLeft + new Vector3(0, 0, 1) * quadrantCellSize, color);
        Debug.DrawLine(lowerLeft + new Vector3(1, 0, 0) * quadrantCellSize,
            lowerLeft + new Vector3(1, 0, 1) * quadrantCellSize, color);
        Debug.DrawLine(lowerLeft + new Vector3(0, 0, 1) * quadrantCellSize,
            lowerLeft + new Vector3(1, 0, 1) * quadrantCellSize, color);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void GetCurrentCellAndNeighborsKeys(float2 pos, ref NativeArray<int> neighborsKey, ref int count)
    {
        int currentKey = GetPositionHashMapKey(pos);

        neighborsKey[0] = currentKey;
        count++;

        float2 lowerLeft = new float2((math.floor(pos.x / quadrantCellSize)) * quadrantCellSize,
            math.floor(pos.y / quadrantCellSize) * quadrantCellSize);
        float2 topRight = lowerLeft + new float2(1, 1) * quadrantCellSize;

        //Check bottom and top
        bool bottom = false;
        bool top = false;
        if (math.length(Det(new float2(1, 0), pos - lowerLeft)) < neighborsCellDistance)
        {
            neighborsKey[count] = currentKey - quadrantYMultiplier;
            count++;
            bottom = true;
        }
        else if (math.length(Det(new float2(1, 0), pos - topRight)) < neighborsCellDistance)
        {
            neighborsKey[count] = currentKey + quadrantYMultiplier;
            count++;
            top = true;
        }

        //Check left
        if (math.length(Det(new float2(0, 1), pos - lowerLeft)) < neighborsCellDistance)
        {
            neighborsKey[count] = currentKey - 1;
            count++;

            //Check bottomLeft
            if (bottom)
            {
                neighborsKey[count] = currentKey - quadrantYMultiplier - 1;
                count++;
            }

            //Check topLeft
            if (top)
            {
                neighborsKey[count] = currentKey + quadrantYMultiplier - 1;
                count++;
            }
        }
        else if (math.length(Det(new float2(0, 1), pos - topRight)) < neighborsCellDistance)
        {
            neighborsKey[count] = currentKey + 1;
            count++;

            //Check bottomRight
            if (bottom)
            {
                neighborsKey[count] = currentKey - quadrantYMultiplier + 1;
                count++;
            }

            //Check bottomRight
            if (top)
            {
                neighborsKey[count] = currentKey + quadrantYMultiplier + 1;
                count++;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static float Det(float2 v1, float2 v2)
    {
        return v1.x * v2.y - v1.y * v2.y;
    }
    
    struct StartTimerJob : IJob {
        public void Execute()
        {
            timer.Start();
        }
    }
    
    struct EndTimerJob : IJob {
        public void Execute()
        {
            double ticks = timer.ElapsedTicks;
            double milliseconds = (ticks / Stopwatch.Frequency) * 1000;
            
            time = milliseconds;
            timer.Stop();
            timer.Reset();
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        //Timer specific
        var startTimerJob = new StartTimerJob();
        var handle = startTimerJob.Schedule(inputDeps);
        //Timer specific
        
        EntityQuery query = GetEntityQuery(typeof(Translation), typeof(Velocity));

        quadrantMultiHashMap.Clear();
        if (query.CalculateEntityCount() > quadrantMultiHashMap.Capacity)
        {
            quadrantMultiHashMap.Capacity = query.CalculateEntityCount();
        }

        ArchetypeChunkComponentType<Translation> translationChunk = GetArchetypeChunkComponentType<Translation>(true);
        ArchetypeChunkComponentType<Velocity> velocityChunk = GetArchetypeChunkComponentType<Velocity>(true);

        //Update quadrants data
        SetQuadrantDataJob setQuadrantData = new SetQuadrantDataJob()
        {
            translationType = translationChunk,
            velocityType = velocityChunk,
            quadrantHashMap = quadrantMultiHashMap.AsParallelWriter()
        };
        
        var handle2 = setQuadrantData.Schedule(query, handle);
        
        //Timer specific
        var endTimerJob = new EndTimerJob();
        timerRecoder.RegisterTimeInMS(time);
        //Timer specific

        return endTimerJob.Schedule(handle2);
    }
}