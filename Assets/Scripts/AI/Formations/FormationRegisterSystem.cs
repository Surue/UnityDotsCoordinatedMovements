using System.Diagnostics;
using Unity.Collections;
using Unity.Entities;

[UpdateInGroup(typeof(AiGroup), OrderFirst = true)]
public class FormationRegisterSystem : SystemBase {
    
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
        timerRecoder = new TimeRecorder("FormationRegisterSystem");
        //Timer specific
    }
    
    protected override void OnUpdate()
    {
        //Timer specific
        Job.WithoutBurst().WithCode(() =>
        {
            timer.Start();
        }).Schedule();
        //Timer specific

        var ecb = ecbSystem.CreateCommandBuffer().ToConcurrent();
        
        Entities.ForEach(
            (int entityInQueryIndex, Entity entity, in FormationLeader leader, in FormationRegisterTag registerRequest) =>
            {
                //Update the formation
                var formation = GetComponent<Formation>(leader.formationEntity);
                formation.nbAgent++;
                SetComponent(leader.formationEntity, formation);

                ecb.RemoveComponent<FormationRegisterTag>(entityInQueryIndex, entity);
            }).Schedule();

        Entities.ForEach(
            (int entityInQueryIndex, Entity entity, in FormationFollower follower, in FormationRegisterTag registerRequest) =>
            {
                //Update the formation
                var formation = GetComponent<Formation>(follower.formationEntity);
                formation.nbAgent++;
                SetComponent(follower.formationEntity, formation);

                ecb.RemoveComponent<FormationRegisterTag>(entityInQueryIndex, entity);
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