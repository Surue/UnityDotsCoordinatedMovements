using Unity.Entities;
using UnityEngine;

[GenerateAuthoringComponent]
public struct FormationSetup : IComponentData {
    public Color leaderColor;
    public Color followerColor;
}