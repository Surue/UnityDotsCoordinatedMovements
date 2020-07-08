using Unity.Entities;

public class PathFinderSystem : ComponentSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((Entity entity, DynamicBuffer<PathPositions> pathPositionBuffer, ref PathFindingRequest request, ref PathFollow pathFollow) =>
        {
            //TODO call pathfinding job here
            pathPositionBuffer.Clear();
            pathPositionBuffer.Add(new PathPositions{ position = request.endPos});
            pathPositionBuffer.Add(new PathPositions{ position = request.startPos});

            pathFollow.pathPositionIndex = 1;
            
            //Remove path finding request from the entity
            PostUpdateCommands.RemoveComponent<PathFindingRequest>(entity);
        });
    }
}
