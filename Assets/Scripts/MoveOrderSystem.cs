using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class MoveOrderSystem : SystemBase
{
    private EntityCommandBufferSystem ecbSystem;


    protected override void OnCreate()
    {
        base.OnCreate();

        ecbSystem = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
    }
    
    protected override void OnUpdate()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit)) return;
        
        float2 worldPos = new float2(hit.point.x, hit.point.z);
        
        EntityCommandBuffer.Concurrent ecb = ecbSystem.CreateCommandBuffer().ToConcurrent();

        Entities.ForEach((int entityInQueryIndex, Entity entity, in Translation translation, in FormationLeader leader) =>
        {
            ecb.AddComponent(entityInQueryIndex, entity, new PathFindingRequest()
            {
                startPos = new float2(translation.Value.x, translation.Value.z),
                endPos = worldPos
            });
        }).ScheduleParallel();
        
        ecbSystem.AddJobHandleForProducer(Dependency);
    }
}
