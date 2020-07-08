using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;

public class DesiredVelocitySystem : ComponentSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((Entity entity, ref PhysicsVelocity velocity, ref DesiredVelocity desiredVelocity) =>
        {
            velocity.Linear = new float3(desiredVelocity.desiredVelocity.x, velocity.Linear.y, desiredVelocity.desiredVelocity.y);
        });
    }
}
