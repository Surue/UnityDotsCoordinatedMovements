using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(AiGroup))]
[UpdateAfter(typeof(PathFinderSystem))]
public class PathNextStepSystem : SystemBase
{
    protected override void OnUpdate()
    {
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
    }
}
