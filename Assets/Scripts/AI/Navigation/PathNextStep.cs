using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public class PathNextStep : SystemBase
{
    protected override void OnUpdate()
    {
        //Dynamic buffer must be at the start
        Entities.ForEach((DynamicBuffer<PathPositions> pathPositionBuffer, ref PathFollow pathFollow, in Translation position, in TargetPosition targetPosition) =>
        {
            //TODO Not good for //
            if (pathFollow.Value < 0 || pathPositionBuffer.Length <= 0) return;
            
            if (math.distance(position.Value.xz, targetPosition.Value) < 0.1f)
            {
                pathFollow.Value--;
            }
        }).ScheduleParallel();
    }
}
