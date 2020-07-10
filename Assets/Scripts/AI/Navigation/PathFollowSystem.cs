using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

public class PathFollowSystem : ComponentSystem
{
    protected override void OnUpdate()
    {
        //Dynamic buffer must be at the start
        Entities.ForEach((DynamicBuffer<PathPositions> pathPositionBuffer, ref PathFollow pathFollow, ref Translation position, ref DesiredVelocity velocity) =>
        {
            if (pathFollow.pathPositionIndex >= 0)
            {
                float2 targetPosition = pathPositionBuffer[pathFollow.pathPositionIndex].position;
                float2 pos = new float2(position.Value.x, position.Value.z);
                
                float2 direction = math.normalizesafe(targetPosition - pos);
                float moveSpeed = 3;
                
                velocity.Value = direction * moveSpeed;
                
                if (math.distance(pos, pathPositionBuffer[pathFollow.pathPositionIndex].position) < 0.1f)
                {
                    pathFollow.pathPositionIndex--;

                    if (pathFollow.pathPositionIndex == -1)
                    {
                        velocity.Value = float2.zero;
                    }
                }
            }
        });
    }
}
