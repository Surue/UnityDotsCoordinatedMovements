using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class MoveOrderSystem : ComponentSystem
{
    protected override void OnUpdate()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit)) return;
        
        float2 worldPos = new float2(hit.point.x, hit.point.z);

        Entities.ForEach((Entity entity, ref Translation translation, ref FormationLeader leader) =>
        {
            EntityManager.AddComponentData(entity, new PathFindingRequest()
            {
                startPos = new float2(translation.Value.x, translation.Value.z),
                endPos = worldPos
            });
        });
    }
}
