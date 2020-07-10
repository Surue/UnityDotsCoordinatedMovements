using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[GenerateAuthoringComponent]
public struct Formation : IComponentData {
    public enum State : short{
        FORMING,
        FORMED,
    }

    public enum Shape : short {
        LINE,
        COLUMN,
        SQUARE
    }
    
    public State state;
    public Shape shape;
    public float speedForming;
    public float speedFormed;
    public float separatedDistance;
    
    public float2 referentialPosition;
    public float2 referentialForward;

    public float2 GetTargetPosition(int index)
    {
        return referentialPosition - referentialForward * index;
    }
}
