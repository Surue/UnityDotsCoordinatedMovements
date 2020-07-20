using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// Update the position and rotation according to the velocity
/// </summary>
[UpdateAfter(typeof(ORCASystem))]
public class MovementSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float dt = UnityEngine.Time.deltaTime;
        
        Entities.ForEach((ref Translation translation, ref Rotation rotation, in Velocity velocity) =>
        {
            float2 normalizedVel = math.normalizesafe(velocity.Value);
            translation.Value += new float3(normalizedVel.x, 0, normalizedVel.y) * dt;

            if (math.lengthsq(velocity.Value) > 0.1f)
            {
                rotation.Value = quaternion.LookRotationSafe(new float3(normalizedVel.x, 0, normalizedVel.y),
                    new float3(0, 1, 0));
            }
        }).ScheduleParallel();
    }
}
