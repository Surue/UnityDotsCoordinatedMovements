using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Group {
    public enum State : short{
        FORMING,
        FORMED,
    }

    public enum Shape : short {
        LINE,
        COLUMN,
        SQUARE
    }
    
    public int ID;
    public State state;
    public Shape shape;
    public float speedForming;
    public float speedFormed;
}
