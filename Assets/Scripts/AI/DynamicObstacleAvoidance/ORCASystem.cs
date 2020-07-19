using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class ORCASystem : SystemBase
{
    protected override void OnUpdate()
    {
        NativeMultiHashMap<int, NeighborData> quadrantMap = QuadrantSystem.quadrantMultiHashMap;
        
        Entities.WithReadOnly(quadrantMap).ForEach((ref Velocity velocity, in ORCATag tag, in Translation translation) =>
        {
            int hashMapKey = QuadrantSystem.GetPositionHashMapKey(translation.Value);
            
            Debug.Log(hashMapKey);

            QuadrantSystem.DebugDrawQuadrant(translation.Value);

            if (quadrantMap.TryGetFirstValue(hashMapKey, out var neighbor, out var nativeMultiHashMapIterator)) {
                do {
                    Debug.DrawLine(translation.Value, new Vector3(neighbor.position.x, translation.Value.y, neighbor.position.y));
                } while (quadrantMap.TryGetNextValue(out neighbor, ref nativeMultiHashMapIterator));
            }
        }).Run();
    }
}
