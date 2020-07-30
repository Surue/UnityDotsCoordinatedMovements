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

   private float radiusDefault = 0.6f;
   private float timeHorizonDefault = 5.0f;
   private float neighborsDistDefault = 15.0f;
   private int maxNeighborsDefault = 10;
   private float maxSpeedDefault = 5.0f;
   private float quadrantSizeDefault = 10.0f;
    
    public float Radius => radius;

    public float TimeHorizon
    {
        get => timeHorizon;
        set => timeHorizon = value;
    }

    public float NeighborsDist
    {
        get => neighborsDist;
        set => neighborsDist = value;
    }

    public int MaxNeighbors
    {
        get => maxNeighbors;
        set => maxNeighbors = value;
    }

    public float MaxSpeed
    {
        get => maxSpeed;
        set => maxSpeed = value;
    }

    public float QuadrantSize
    {
        get => quadrantSize;
        set => quadrantSize = value;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this);

            radiusDefault = radius;
            timeHorizonDefault = timeHorizon;
            neighborsDistDefault = neighborsDist;
            maxNeighborsDefault = maxNeighbors;
            maxSpeedDefault = maxSpeed;
            quadrantSizeDefault = quadrantSize;
        }
        else
        {
            Destroy(this);
        }
    }

    public void ResetDefaultValues()
    {
        radius = radiusDefault;
        timeHorizon = timeHorizonDefault;
        neighborsDist = neighborsDistDefault;
        maxNeighbors = maxNeighborsDefault;
        maxSpeed = maxSpeedDefault;
        quadrantSize = quadrantSizeDefault;
    }
}
