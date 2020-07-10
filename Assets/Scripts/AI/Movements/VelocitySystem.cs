using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public class VelocitySystem : SystemBase
{
    protected override void OnUpdate()
    {
        float stoppingDistance = 0.1f;
        float slowdownDistance = 1.0f;
        
        Entities.ForEach((ref DesiredVelocity vel, in FormationID formationId, in TargetPosition targetPosition, in Translation translation) =>
        {
            float dist = math.distance(translation.Value.xz, targetPosition.Value);

            float currentSpeed = 5;
            
            if (dist < stoppingDistance)
            {
                vel.Value = float2.zero;
            }
            else
            {
                float2 dir = math.normalizesafe(targetPosition.Value - translation.Value.xz);
                vel.Value = dir * math.min(1, dist / slowdownDistance) * currentSpeed ;
            }
        }).ScheduleParallel();
    }
}
