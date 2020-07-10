using Unity.Entities;
using Unity.Mathematics;

[GenerateAuthoringComponent]
public struct DesiredVelocity : IComponentData {
    public float2 Value;
}
