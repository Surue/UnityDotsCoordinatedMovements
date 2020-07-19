using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.CodeGeneratedJobForEach;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public struct PairEntityFormation {
    public Entity entity;
    public Formation formation;
}

[UpdateAfter(typeof(FormationLeaderSystem))]
public class FormationFollowerSystem : SystemBase {

    private EntityCommandBufferSystem ecbSystem;

    protected override void OnCreate()
    {
        base.OnCreate();
        
        ecbSystem = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
    }
    protected override void OnUpdate()
    {
        var ecb = ecbSystem.CreateCommandBuffer().ToConcurrent();
        
        NativeList<PairEntityFormation> formations = new NativeList<PairEntityFormation>(Allocator.TempJob);
        
        Entities.ForEach((Entity entity, in Formation formation) =>
        {
            formations.Add(new PairEntityFormation()
            {
                entity = entity,
                formation = formation
            });
        }).Schedule();
        
        Entities.WithReadOnly(formations).ForEach((
            Entity entity, 
            int entityInQueryIndex,
            DynamicBuffer<PathPositions> pathPositionBuffer, 
            ref TargetPosition targetPosition, 
            ref PathFollow pathFollow, 
            ref Velocity desiredVelocity,
            in Translation translation, 
            in FormationFollower follower) =>
        {
            //Get Formation
            Formation formation = new Formation();
            for (int i = 0; i < formations.Length; i++)
            {
                if (formations[i].entity == follower.formationEntity)
                {
                    formation = formations[i].formation;
                }
            }
            
            //Get target position
            float2 tmpPosition = formation.GetTargetPosition(follower.positionID);

            //Compute distance 
            float distanceToTarget = math.distance(translation.Value.xz, tmpPosition);

            // Too far must use pathfinding
            if (distanceToTarget > formation.separatedDistance)
            {
                //If the path is already computed
                if (pathFollow.Value != -1)
                {
                    targetPosition.Value = pathPositionBuffer[pathFollow.Value].Value;
                    
                    //If distance between and path and real position is too big ask a new path
                    if (math.distance(pathPositionBuffer[0].Value, tmpPosition) > formation.separatedDistance)
                    {
                        ecb.AddComponent(entityInQueryIndex, entity, new PathFindingRequest()
                        {
                            startPos = translation.Value.xz,
                            endPos = tmpPosition
                        });
                    }
                }
                else
                {
                    ecb.AddComponent(entityInQueryIndex, entity, new PathFindingRequest()
                    {
                        startPos = translation.Value.xz,
                        endPos = tmpPosition
                    });
                }
            }
            else //Close enough to just go to position
            {
                targetPosition.Value = tmpPosition;
                pathFollow.Value = -1;
            }
        }).ScheduleParallel();
        
        Dependency = JobHandle.CombineDependencies(Dependency, formations.Dispose(Dependency));
        
        ecbSystem.AddJobHandleForProducer(Dependency);
    }
}
