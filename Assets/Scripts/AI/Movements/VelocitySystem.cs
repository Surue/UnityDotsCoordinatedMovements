using System.Diagnostics;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(AiGroup))]
[UpdateAfter(typeof(QuadrantSystem))]
public class VelocitySystem : SystemBase {
    
    //Timer specific
    private TimeRecorder timerRecoder;
    static Stopwatch timer = new Stopwatch();
    private static double time = 0;
    //Timer specific
    
    protected override void OnCreate()
    {
        base.OnCreate();
        
        //Timer specific
        timerRecoder = new TimeRecorder("VelocitySystem");
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

        float stoppingDistance = 0.1f;
        float slowdownDistance = 1.0f;

        Entities.ForEach((ref Velocity vel, in TargetPosition targetPosition, in Translation translation) =>
        {
            float dist = math.distance(translation.Value.xz, targetPosition.Value);

            if (dist < stoppingDistance)
            {
                vel.Value = float2.zero;
            }
            else
            {
                float2 dir = math.normalizesafe(targetPosition.Value - translation.Value.xz);
                vel.Value = dir * math.min(1, dist / slowdownDistance) * vel.maxSpeed;
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