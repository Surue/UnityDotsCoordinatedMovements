using System.Diagnostics;
using Unity.Collections;
using Unity.Entities;

public struct FormationSetupData {
    public Entity entity;
    public Formation formation;
    public FormationSetup setup;
    public int count;
}

[UpdateInGroup(typeof(AiGroup))]
[UpdateAfter(typeof(FormationRegisterSystem))]
public class FormationSetupSystem : SystemBase
{
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
        timerRecoder = new TimeRecorder("FormationSetupSystem");
        //Timer specific
    }

    protected override void OnUpdate()
    {
        var ecb = ecbSystem.CreateCommandBuffer().ToConcurrent();

        var queries = GetEntityQuery(ComponentType.ReadOnly(typeof(FormationSetup)));

        if (queries.CalculateEntityCount() == 0)
        {
            return;
        }
        
        //Timer specific
        Job.WithoutBurst().WithCode(() =>
        {
            timer.Start();
        }).Schedule();
        //Timer specific
        
        NativeArray<FormationSetupData> formationEntityToSetup = new NativeArray<FormationSetupData>(queries.CalculateEntityCount(), Allocator.TempJob);

        float maxSpeed = Blackboard.Instance.MaxSpeed;
        
        Entities.ForEach((
            Entity entity, 
            int entityInQueryIndex,
            ref Formation formation, 
            in FormationSetup formationSetup) =>
        {
            formation.speedFormed = maxSpeed;
            
            formationEntityToSetup[entityInQueryIndex] = new FormationSetupData()
            {
                entity = entity, formation = formation, setup = formationSetup, count = 1
            };

            ecb.RemoveComponent<FormationSetup>(entityInQueryIndex, entity);
        }).ScheduleParallel();
        
        
        //Update leaders
        // Entities.WithoutBurst().WithReadOnly(formationEntityToSetup).ForEach((in FormationLeader formationLeader) =>
        // {
        //     for (int i = 0; i < formationEntityToSetup.Length; i++)
        //     {
        //         if (formationEntityToSetup[i].entity == formationLeader.formationEntity)
        //         {
        //             //TODO Setup
        //             break;
        //         }
        //     }
        // }).Run();
        
        //Update followers
        Entities.WithDeallocateOnJobCompletion(formationEntityToSetup).ForEach((ref FormationFollower follower, ref Velocity velocity) =>
        {
            for (int i = 0; i < formationEntityToSetup.Length; i++)
            {
                if (formationEntityToSetup[i].entity == follower.formationEntity)
                {
                    var setupData = formationEntityToSetup[i];
                    
                    //Setup position id
                    follower.positionID = setupData.count;
                    setupData.count++;

                    velocity.maxSpeed = setupData.formation.speedFormed;
                    
                    formationEntityToSetup[i] = setupData;
                    break;
                }
            }
        }).Schedule();
        
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
