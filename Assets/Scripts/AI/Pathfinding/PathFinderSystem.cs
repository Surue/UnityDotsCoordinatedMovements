using Unity.Entities;

public class PathFinderSystem : ComponentSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((Entity entity, DynamicBuffer<PathPositions> pathPositionBuffer, ref PathFindingRequest request) =>
        {
            //TODO call pathfinding job here
            pathPositionBuffer.Clear();
            pathPositionBuffer.Add(new PathPositions{ position = request.startPos});
            pathPositionBuffer.Add(new PathPositions{ position = request.endPos});
            
            //Remove path finding request from the entity
            PostUpdateCommands.RemoveComponent<PathFindingRequest>(entity);
        });
    }
}
