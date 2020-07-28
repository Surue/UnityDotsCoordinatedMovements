using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Blackboard : MonoBehaviour {
    public static  Blackboard Instance;
    
    [SerializeField][Range(0, 10)] float radius = 0.6f;
    [SerializeField][Range(0, 10)] float timeHorizon = 5.0f;
    [SerializeField][Range(0, 50)] float neighborsDist = 15.0f;
    [SerializeField][Range(0, 50)] int maxNeighbors = 10;
    [SerializeField][Range(0, 10)] float maxSpeed = 5.0f;
    [SerializeField][Range(0, 100)] float quadrantSize = 10.0f;

    public float Radius => radius;
    public float TimeHorizon => timeHorizon;
    public float NeighborsDist => neighborsDist;
    public int MaxNeighbors => maxNeighbors;
    public float MaxSpeed => maxSpeed;

    public float QuadrantSize => quadrantSize;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }
    }
}
