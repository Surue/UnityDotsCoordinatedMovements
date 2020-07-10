
using Unity.Entities;
using Unity.Transforms;

public class FormationLeaderSystem : SystemBase{
    protected override void OnUpdate()
    {
        EntityManager em = EntityManager;
        Entities.WithStructuralChanges().ForEach((DynamicBuffer<PathPositions> pathPositionBuffer, ref TargetPosition targetPosition, in PathFollow pathFollow, in FormationLeader leader, in LocalToWorld localToWorld) =>
        {
            var formation = em.GetComponentData<Formation>(leader.formationEntity);

            formation.referentialPosition = localToWorld.Position.xz;
            formation.referentialForward = localToWorld.Forward.xz;
            
            em.SetComponentData(leader.formationEntity, formation);

            if (pathFollow.Value >= 0)
            {
                targetPosition.Value = pathPositionBuffer[pathFollow.Value].Value;
            }
        }).Run();
    }
}