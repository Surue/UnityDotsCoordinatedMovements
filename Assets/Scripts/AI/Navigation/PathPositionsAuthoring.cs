using Unity.Entities;
using UnityEngine;

public class PathPositionsAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddBuffer<PathPositions>(entity);
    }
}
