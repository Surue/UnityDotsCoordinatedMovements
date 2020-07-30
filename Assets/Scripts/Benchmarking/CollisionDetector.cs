using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public struct Collision : IComponentData{
    public float2 position;
    public float percentage;
    public int frame;
}

[UpdateInGroup(typeof(AiGroup))]
[UpdateAfter(typeof(QuadrantSystem))]
public class CollisionDetector : JobComponentSystem {
    
    private EntityCommandBufferSystem ecbSystem;
    
    public static int key = 0;
    
    protected override void OnCreate()
    {
        base.OnCreate();

        ecbSystem = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        EntityCommandBuffer.Concurrent ecb = ecbSystem.CreateCommandBuffer().ToConcurrent();
        
        EntityQuery query = GetEntityQuery(typeof(Translation), typeof(Velocity));

        ArchetypeChunkComponentType<Translation> translationChunk = GetArchetypeChunkComponentType<Translation>(true);

        //Update quadrants data
        CollisionDetectorJob collisionDetectorJob = new CollisionDetectorJob()
        {
            translationType = translationChunk,
            key = key++,
            quadrantMap = QuadrantSystem.quadrantMultiHashMap,
            ecb = ecb
        };

        var handle = collisionDetectorJob.Schedule(query, inputDeps);
        
        ecbSystem.AddJobHandleForProducer(handle);

        return handle;
    }

    struct CollisionDetectorJob : IJobChunk {
        [ReadOnly] public ArchetypeChunkComponentType<Translation> translationType;
        [ReadOnly] public NativeMultiHashMap<int, QuadrantData> quadrantMap;
        public int key;

        public EntityCommandBuffer.Concurrent ecb;
        
        private const int MAX_QUADRANT_NEIGHBORS = 9;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            NativeArray<Translation> translations = chunk.GetNativeArray(translationType);

            NativeArray<int> quadrantKeys = new NativeArray<int>(MAX_QUADRANT_NEIGHBORS, Allocator.Temp);

            //TODO make something that is dynamic
            float radius = 0.5f;
            float combinedRadius = radius * 2.0f;

            for (int entityIdx = 0; entityIdx < chunk.ChunkEntityCount; entityIdx++)
            {
                float2 currentPosition = translations[entityIdx].Value.xz;

                int countNeighborQuadrant = 0;
                QuadrantSystem.GetCurrentCellAndNeighborsKeys(currentPosition, ref quadrantKeys,
                    ref countNeighborQuadrant);


                //Get nearest neighbors
                for (int i = 0; i < countNeighborQuadrant; i++)
                {
                    if (!quadrantMap.TryGetFirstValue(quadrantKeys[i], out var neighbor,
                        out var nativeMultiHashMapIterator))
                        continue;
                    do
                    {
                        float distance = math.distance(neighbor.position, currentPosition);
                        if (distance < combinedRadius && distance > 0.0001f)
                        {
                            var entity = ecb.CreateEntity(chunkIndex);
                            Collision collision = new Collision()
                            {
                                // percentage = ((combinedRadius - distance) * 0.5f) / radius,
                                percentage = distance,
                                position = (neighbor.position + currentPosition) / 2.0f,
                                frame = key
                            };
                            ecb.AddComponent(chunkIndex, entity, collision);
                        }
                    } while (quadrantMap.TryGetNextValue(out neighbor, ref nativeMultiHashMapIterator));
                }
            }
        }
    }
}

[UpdateAfter(typeof(CollisionDetector))]
public class CollisionCounter : SystemBase {
    [ReadOnly] public static NativeList<KeyValuePair<int, float>> collisions;
    public static NativeList<float2> positions;
    
    private EntityCommandBufferSystem ecbSystem;
    
    protected override void OnCreate()
    {
        base.OnCreate();
        
        collisions = new NativeList<KeyValuePair<int, float>>(Allocator.Persistent);
        positions = new NativeList<float2>(Allocator.Persistent);
        
        ecbSystem = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        collisions.Dispose();
        positions.Dispose();
    }
    
    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = ecbSystem.CreateCommandBuffer();

        var test = collisions;
        
        Entities.ForEach((Entity entity, in Collision collision) =>
        {
            test.Add(new KeyValuePair<int, float>(collision.frame, collision.percentage));
            positions.Add(collision.position);
            ecb.DestroyEntity(entity);
            
        }).Schedule();

        ecbSystem.AddJobHandleForProducer(Dependency);
    }
}
