using Unity.Entities;

[GenerateAuthoringComponent]
public struct PathFollow : IComponentData {
    public int pathPositionIndex;
}
