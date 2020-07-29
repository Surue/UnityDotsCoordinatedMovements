using System.Diagnostics;
using Unity.Entities;
using Unity.Transforms;

[UpdateInGroup(typeof(AiGroup))]
[UpdateAfter(typeof(FormationSetupSystem))]
public class FormationLeaderSystem : SystemBase {
    
    //Timer specific
    private TimeRecorder timerRecoder;
    static Stopwatch timer = new System.Diagnostics.Stopwatch();
    private static double time = 0;
    //Timer specific
    
    protected override void OnCreate()
    {
        base.OnCreate();
        
        
        //Timer specific
        timerRecoder = new TimeRecorder("FormationLeaderSystem");
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
      
        
        Entities.ForEach(
            (DynamicBuffer<PathPositions> pathPositionBuffer, ref TargetPosition targetPosition,
                in PathIndex pathFollow, in FormationLeader leader, in LocalToWorld localToWorld) =>
            {
                var formation = GetComponent<Formation>(leader.formationEntity);
                
                formation.referentialPosition = localToWorld.Position.xz;
                formation.referentialForward = localToWorld.Forward.xz;
                
                
                SetComponent(leader.formationEntity, formation);

                if (pathFollow.Value != -1)
                {
                    targetPosition.Value = pathPositionBuffer[pathFollow.Value].Value;
                }
            }).Schedule();

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