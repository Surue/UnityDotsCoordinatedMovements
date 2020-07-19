using Unity.Entities;

[GenerateAuthoringComponent]
public struct FormationLeader : IComponentData{
    public Entity formationEntity;
}