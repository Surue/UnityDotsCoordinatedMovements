using System;
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

    public int nbAgent;
    
    public State state;
    public Shape shape;
    public float speedForming;
    public float speedFormed;
    public float separatedDistance;

    public float agentSpacing;
    
    public float2 referentialPosition;
    public float2 referentialForward;

    public float2 GetTargetPosition(int index)
    {
        switch (shape)
        {
            case Shape.LINE:
                if (index % 2 == 0)
                {
                    return referentialPosition + new float2(referentialForward.y, -referentialForward.x) * (index / 2.0f * agentSpacing);
                }
                else
                {
                    return referentialPosition - new float2(referentialForward.y, -referentialForward.x) * (Mathf.CeilToInt(index / 2.0f) * agentSpacing);
                }
            case Shape.COLUMN:
                return referentialPosition - referentialForward * index * agentSpacing;
            case Shape.SQUARE:
                int maxCol = Mathf.FloorToInt(Mathf.Sqrt(nbAgent));

                int col = index % maxCol;
                int row = index / maxCol;

                if (col % 2 == 0)
                {
                    return referentialPosition +
                           new float2(referentialForward.y, -referentialForward.x) * (col / 2.0f * agentSpacing) -
                           referentialForward * (row * agentSpacing);
                }
                else
                {
                    return referentialPosition - 
                           new float2(referentialForward.y, -referentialForward.x) * (Mathf.CeilToInt(col / 2.0f) * agentSpacing) -
                           referentialForward * (row * agentSpacing);
                }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
