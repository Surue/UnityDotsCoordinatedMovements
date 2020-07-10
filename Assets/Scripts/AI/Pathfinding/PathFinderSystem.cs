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
        var ecb = ecbSystem.CreateCommandBuffer();
        
        Entities.ForEach((Entity entity, DynamicBuffer<PathPositions> pathPositionBuffer, ref PathFindingRequest request, ref PathFollow pathFollow) =>
        {
            //TODO call pathfinding job here
            pathPositionBuffer.Clear();
            pathPositionBuffer.Add(new PathPositions{ Value = request.endPos});
            pathPositionBuffer.Add(new PathPositions{ Value = request.startPos});

            pathFollow.Value = 1;
            
            //Remove path finding request from the entity
            ecb.RemoveComponent<PathFindingRequest>(entity);
        }).Schedule();

        ecbSystem.AddJobHandleForProducer(Dependency);
    }
}
