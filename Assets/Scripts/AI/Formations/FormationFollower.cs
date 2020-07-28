using Unity.Entities;

[GenerateAuthoringComponent]
public struct FormationFollower : IComponentData{
    public Entity formationEntity;
    public int positionID;
}