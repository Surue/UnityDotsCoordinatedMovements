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
            translation.Value += new float3(velocity.Value.x, 0, velocity.Value.y) * dt;

            if (math.lengthsq(velocity.Value) > 0.1f)
            {
                rotation.Value = quaternion.LookRotationSafe(new float3(velocity.Value.x, 0, velocity.Value.y),
                    new float3(0, 1, 0));
            }
        }).ScheduleParallel();
    }
}
