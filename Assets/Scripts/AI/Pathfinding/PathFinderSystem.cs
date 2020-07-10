using Unity.Entities;

public class PathFinderSystem : SystemBase
{
    private EntityCommandBufferSystem ecbSystem;

    protected override void OnCreate()
    {
        base.OnCreate();
        
        ecbSystem = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var ecb = ecbSystem.CreateCommandBuffer().ToConcurrent();
        
        Entities.ForEach((Entity entity, int entityInQueryIndex, DynamicBuffer<PathPositions> pathPositionBuffer, ref PathFindingRequest request, ref PathFollow pathFollow) =>
        {
            //TODO call pathfinding job here
            pathPositionBuffer.Clear();
            pathPositionBuffer.Add(new PathPositions{ Value = request.endPos});
            pathPositionBuffer.Add(new PathPositions{ Value = request.startPos});

            pathFollow.Value = 1;
            
            //Remove path finding request from the entity
            ecb.RemoveComponent<PathFindingRequest>(entityInQueryIndex, entity);
        }).ScheduleParallel();

        ecbSystem.AddJobHandleForProducer(Dependency);
        
        Dependency.Complete();
    }
}
