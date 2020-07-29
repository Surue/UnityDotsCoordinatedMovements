using System.Diagnostics;
using Unity.Entities;

[UpdateInGroup(typeof(AiGroup))]
[UpdateAfter(typeof(PathFinderSystem))]
public class PathRequestRemoverSystem : SystemBase {
    
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
        timerRecoder = new TimeRecorder("PathRequestRemoverSystem");
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
        
        EntityCommandBuffer.Concurrent ecb = ecbSystem.CreateCommandBuffer().ToConcurrent();

        Entities.ForEach((int entityInQueryIndex, Entity entity, in PathFindingRequest pathFindingRequest) =>
        {
            ecb.RemoveComponent<PathFindingRequest>(entityInQueryIndex, entity);
        }).ScheduleParallel();

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
