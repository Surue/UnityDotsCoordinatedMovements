using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(AiGroup))]
[UpdateAfter(typeof(QuadrantSystem))]
public class VelocitySystem : SystemBase {
    protected override void OnUpdate()
    {
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
    }
}