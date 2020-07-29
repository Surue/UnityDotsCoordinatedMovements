using System.Diagnostics;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// Update the position and rotation according to the velocity
/// </summary>
[UpdateInGroup(typeof(AiGroup), OrderLast = true)]
[UpdateAfter(typeof(ORCASystem))]
public class MovementSystem : SystemBase {
    
    //Timer specific
    private TimeRecorder timerRecoder;
    static Stopwatch timer = new Stopwatch();
    private static double time = 0;
    //Timer specific
    
    protected override void OnCreate()
    {
        base.OnCreate();
        
        //Timer specific
        timerRecoder = new TimeRecorder("MovementSystem");
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
        
        float dt = UnityEngine.Time.deltaTime;

        Entities.ForEach((ref Translation translation, ref Rotation rotation, in Velocity velocity) =>
        {
            translation.Value += new float3(velocity.Value.x, 0, velocity.Value.y) * dt;

            if (math.lengthsq(velocity.Value) > 0.1f)
            {
                rotation.Value = quaternion.LookRotationSafe(new float3(velocity.Value.x, 0, velocity.Value.y),
                    new float3(0, 1, 0));
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