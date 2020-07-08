using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Assertions.Must;

public class MoveOrderSystem : ComponentSystem
{
    protected override void OnUpdate()
    {
        if (Input.GetMouseButtonDown(0))
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                float2 worldPos = new float2(hit.point.x, hit.point.z);

                Entities.ForEach((Entity entity, ref Translation translation) =>
                {
                    EntityManager.AddComponentData(entity, new PathFindingRequest()
                    {
                        startPos = new float2(translation.Value.x, translation.Value.z),
                        endPos = worldPos
                    });
                });
            }
        }
    }
}
