using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public class MovementSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float dt = UnityEngine.Time.deltaTime;
        
        Entities.ForEach((ref Translation translation, in DesiredVelocity velocity) =>
        {
            float2 normalizedVel = math.normalizesafe(velocity.Value);
            translation.Value += new float3(normalizedVel.x, 0, normalizedVel.y) * dt;
        }).ScheduleParallel();
    }
}
