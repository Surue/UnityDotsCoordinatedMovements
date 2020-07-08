using Unity.Entities;
using Unity.Mathematics;

public struct PathFindingRequest : IComponentData {
    public float2 startPos;
    public float2 endPos;
}
