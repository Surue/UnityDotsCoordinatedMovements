using System.Diagnostics;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(AiGroup))]
[UpdateAfter(typeof(PathFinderSystem))]
public class PathNextStepSystem : SystemBase {
    
    //Timer specific
    private TimeRecorder timerRecoder;
    static Stopwatch timer = new Stopwatch();
    private static double time = 0;
    //Timer specific

    protected override void OnCreate()
    {
        base.OnCreate();
        
        //Timer specific
        timerRecoder = new TimeRecorder("PathNextStepSystem");
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
        
        //Dynamic buffer must be at the start
        Entities.ForEach((ref PathIndex pathFollow, in Translation position, in TargetPosition targetPosition) =>
        {
            //TODO Not good for //
            if (pathFollow.Value == -1) return;

            if (math.distance(position.Value.xz, targetPosition.Value) < 0.55f)
            {
                pathFollow.Value--;
            }
        }).ScheduleParallel();
        
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