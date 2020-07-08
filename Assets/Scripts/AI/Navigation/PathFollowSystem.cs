using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

public class PathFollowSystem : ComponentSystem
{
    protected override void OnUpdate()
    {
        //Dynamic buffer must be at the start
        Entities.ForEach((DynamicBuffer<PathPositions> pathPositionBuffer, ref PathFollow pathFollow, ref Translation position, ref PhysicsVelocity velocity) =>
        {
            if (pathFollow.pathPositionIndex >= 0)
            {    
                float3 targetPosition = new float3(pathPositionBuffer[pathFollow.pathPositionIndex].position.x, 0, pathPositionBuffer[pathFollow.pathPositionIndex].position.y);
                
                float3 direction = math.normalizesafe(targetPosition - position.Value);
                float moveSpeed = 3;
                
                velocity.Linear = direction * moveSpeed;
                
                if (math.distance(new float2(position.Value.x, position.Value.z), pathPositionBuffer[pathFollow.pathPositionIndex].position) < 0.1f)
                {
                    pathFollow.pathPositionIndex--;
                }
            }
        });
    }
}
