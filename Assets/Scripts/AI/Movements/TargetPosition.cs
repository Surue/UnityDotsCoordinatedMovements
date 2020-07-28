using Unity.Entities;
using Unity.Mathematics;

[GenerateAuthoringComponent]
public struct TargetPosition : IComponentData {
    public float2 Value;
}