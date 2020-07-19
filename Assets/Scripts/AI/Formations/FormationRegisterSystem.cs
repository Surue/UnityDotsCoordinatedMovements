using Unity.Entities;

public class FormationRegisterSystem : SystemBase{
    protected override void OnUpdate()
    {
        EntityManager em = EntityManager;
        Entities.WithStructuralChanges().ForEach((Entity entity, FormationRegisterTag registerRequest, FormationLeader leader)=>
        {
            //Update the formation
            var formation = em.GetComponentData<Formation>(leader.formationEntity);
            formation.nbAgent++;
            em.SetComponentData(leader.formationEntity, formation);

            em.RemoveComponent<FormationRegisterTag>(entity);
        }).Run();
        
        Entities.WithStructuralChanges().ForEach((Entity entity, FormationRegisterTag registerRequest, FormationFollower follower)=>
        {
            //Update the formation
            var formation = em.GetComponentData<Formation>(follower.formationEntity);
            formation.nbAgent++;
            em.SetComponentData(follower.formationEntity, formation);
            
            em.RemoveComponent<FormationRegisterTag>(entity);
        }).Run();
    }
}