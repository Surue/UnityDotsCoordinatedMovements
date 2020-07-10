using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class FormationSystem : SystemBase {
    private NativeArray<Formation> formations_;

    protected override void OnCreate()
    {
        base.OnCreate();
        
        formations_ = new NativeArray<Formation>(10, Allocator.Persistent);

        //Create 10 group
        for (int i = 0; i < 10; i++)
        {
            formations_[i] = (new Formation()
            {
                ID = formations_.Length - 1,
                referentialForward = float2.zero, 
                shape = Formation.Shape.COLUMN,
                referentialPosition = float2.zero,
                separatedDistance = 5,
                speedFormed = 5,
                speedForming = 1,
                state = Formation.State.FORMING
            });
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        formations_.Dispose();
    }

    protected override void OnUpdate()
    {


        Entities.WithStructuralChanges().ForEach((Entity entity, ref FormationID formationId, ref PathFollow pathFollow, ref Translation translation, ref DesiredVelocity desiredVelocity) =>
        {
            //Check if entity is referential for the group
            //TODO This part can be put out and can even be parallelisezd
            if (formationId.positionIndex == 0)
            {
                var group = formations_[formationId.formationIndex];
                //Update position and forward
                group.referentialPosition = new float2(translation.Value.x, translation.Value.z);
                float2 vel = EntityManager.GetComponentData<DesiredVelocity>(entity).Value;

                if (math.lengthsq(vel) > 0)
                {
                    group.referentialForward = vel;
                }
                else
                {
                    group.referentialForward = EntityManager.GetComponentData<LocalToWorld>(entity).Forward.xz;
                }
                group.referentialForward = EntityManager.GetComponentData<DesiredVelocity>(entity).Value;

                formations_[formationId.formationIndex] = group;
            }
            else
            {
                //Get target position
                float2 targetPosition = formations_[formationId.formationIndex].GetTargetPosition(formationId.positionIndex);

                //Check distance 
                float distanceToTarget =
                    math.distance(new float2(translation.Value.x, translation.Value.z), targetPosition);

                if (distanceToTarget > formations_[formationId.formationIndex].separatedDistance)
                {
                    if (EntityManager.HasComponent<PathFindingRequest>(entity))
                    {
                        
                    }
                    else
                    {
                        //TODO update pathfinding request only when needed
                        EntityManager.AddComponentData(entity, new PathFindingRequest()
                        {
                            startPos = new float2(translation.Value.x, translation.Value.z),
                            endPos = targetPosition
                        });
                    }
                }
                else
                {
                    desiredVelocity.Value = targetPosition - new float2(translation.Value.x, translation.Value.z);
                }
            }
        }).Run();
    }
}
