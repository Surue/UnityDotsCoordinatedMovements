using System.Diagnostics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public struct PairEntityFormation {
    public Entity entity;
    public Formation formation;
}

[UpdateInGroup(typeof(AiGroup))]
[UpdateAfter(typeof(FormationLeaderSystem))]
public class FormationFollowerSystem : SystemBase {
    private EntityCommandBufferSystem ecbSystem;

    //Timer specific
    private TimeRecorder timerRecoder;
    static Stopwatch timer = new Stopwatch();
    private static double time = 0;
    //Timer specific
    
    
    protected override void OnCreate()
    {
        base.OnCreate();

        ecbSystem = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
        
        
        //Timer specific
        timerRecoder = new TimeRecorder("FormationFollowerSystem");
        //Timer specific
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }

    protected override void OnUpdate()
    {
        var ecb = ecbSystem.CreateCommandBuffer().ToConcurrent();

        //Timer specific
        Job.WithoutBurst().WithCode(() =>
        {
            timer.Start();
        }).Schedule();
        //Timer specific

        EntityQuery formationQuery = GetEntityQuery(ComponentType.ReadOnly(typeof(Formation)));

        NativeArray<PairEntityFormation> formations = new NativeArray<PairEntityFormation>(formationQuery.CalculateEntityCount(), Allocator.Persistent);

        //TODO To parallel, Use chunk to bind entity to formation
        Entities.ForEach((int entityInQueryIndex, Entity entity, in Formation formation) =>
        {
            formations[entityInQueryIndex] = new PairEntityFormation()
            {
                entity = entity,
                formation = new Formation()
                {
                    agentSpacing = formation.agentSpacing,
                    nbAgent = formation.nbAgent,
                    referentialForward = formation.referentialForward,
                    referentialPosition = formation.referentialPosition,
                    separatedDistance = formation.separatedDistance,
                    shape = formation.shape,
                    speedFormed = formation.speedFormed,
                    speedForming = formation.speedForming,
                    state = Formation.State.FORMED
                }
            };
        }).ScheduleParallel();

        Entities.WithReadOnly(formations).ForEach((
            Entity entity,
            int entityInQueryIndex,
            DynamicBuffer<PathPositions> pathPositionBuffer,
            ref TargetPosition targetPosition,
            in PathIndex pathFollow,
            in Translation translation,
            in FormationFollower follower) =>
        {
            //Get Formation
            Formation currentFormation = new Formation();
            for (int i = 0; i < formations.Length; i++)
            {
                if (formations[i].entity == follower.formationEntity)
                {
                    currentFormation = formations[i].formation;
                    break;
                }
            }

            //Get target position
            float2 tmpPosition = currentFormation.GetTargetPosition(follower.positionID);

            //Compute distance 
            float distanceToTarget = math.distance(translation.Value.xz, tmpPosition);

            // Too far must use pathfinding
            if (distanceToTarget > currentFormation.separatedDistance)
            {
                //if there is not path, compute a new one
                if (pathFollow.Value == -1)
                {
                    ecb.AddComponent(entityInQueryIndex, entity, new PathFindingRequest()
                    {
                        startPos = translation.Value.xz,
                        endPos = tmpPosition
                    });

                    targetPosition.Value = translation.Value.xz;
                    //If distance from last pos in path is too far from the tmpPosition, compute a new path    
                }
                else if (math.distance(pathPositionBuffer[0].Value, tmpPosition) >
                         currentFormation.separatedDistance * 2)
                {
                    ecb.AddComponent(entityInQueryIndex, entity, new PathFindingRequest()
                    {
                        startPos = translation.Value.xz,
                        endPos = tmpPosition
                    });

                    targetPosition.Value = translation.Value.xz;
                }
                else
                {
                    targetPosition.Value = pathPositionBuffer[pathFollow.Value].Value;
                }
            }
            else //Close enough to just go to position
            {
                targetPosition.Value = tmpPosition;
            }
        }).ScheduleParallel();

        //TODO Must be parallelized
        //Define if formation are forming or formed
        Entities.ForEach((
            in PathIndex pathFollow,
            in FormationFollower follower) =>
        {
            //Get target position
            if (pathFollow.Value == -1) return;
            
            //Get Formation
            Formation formation = new Formation();
            int i;
            for (i = 0; i < formations.Length; i++)
            {
                if (formations[i].entity != follower.formationEntity) continue;
                    
                formation = formations[i].formation;
                break;
            }

            formation.state = Formation.State.FORMING;
            formations[i] = new PairEntityFormation()
            {
                entity = formations[i].entity,
                formation = formation
            };
        }).Schedule();

        //Set speed for leader
        Entities.WithReadOnly(formations).ForEach((
            ref Velocity velocity,
            in FormationLeader leader) =>
        {
            //Get Formation
            Formation formation = new Formation();
            for (int i = 0; i < formations.Length; i++)
            {
                if (formations[i].entity == leader.formationEntity)
                {
                    formation = formations[i].formation;
                    break;
                }
            }

            if (formation.state == Formation.State.FORMED)
            {
                velocity.maxSpeed = formation.speedFormed * 0.9f;
            }
            else
            {
                velocity.maxSpeed = formation.speedForming * 0.9f;
            }
        }).ScheduleParallel();

        JobHandle.CombineDependencies(Dependency, formations.Dispose(Dependency));
        
        ecbSystem.AddJobHandleForProducer(Dependency);
        
        //Timer specific
        Job.WithoutBurst().WithCode(() =>
        {
            double ticks = timer.ElapsedTicks;
            double milliseconds = (ticks / Stopwatch.Frequency) * 1000;
            
            time = milliseconds;
            timer.Stop();
            timer.Reset();
        }).Schedule();
        timerRecoder.RegisterTimeInMS(time);
        //Timer specific
    }
}