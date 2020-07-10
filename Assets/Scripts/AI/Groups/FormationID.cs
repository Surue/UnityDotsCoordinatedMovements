using Unity.Entities;

[GenerateAuthoringComponent]
public struct FormationID : IComponentData {
    public int formationIndex;
    public int positionIndex;
}
