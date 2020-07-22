using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Rendering;
using UnityEngine;

public struct FormationSetupData {
    public Entity entity;
    public Formation formation;
    public FormationSetup setup;
    public int count;
}

[UpdateAfter(typeof(FormationRegisterSystem))]
public class FormationSetupSystem : SystemBase
{
    private EntityCommandBufferSystem ecbSystem;

    protected override void OnCreate()
    {
        base.OnCreate();
        
        ecbSystem = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        // var ecb = ecbSystem.CreateCommandBuffer().ToConcurrent();
        
        NativeList<FormationSetupData> formationEntityToSetup = new NativeList<FormationSetupData>(Allocator.TempJob);

        Entities.WithStructuralChanges().WithoutBurst().ForEach((
            Entity entity, 
            int entityInQueryIndex,
            in Formation formation, 
            in FormationSetup formationSetup) =>
        {
            formationEntityToSetup.Add(new FormationSetupData()
            {
                entity = entity, formation = formation, setup = formationSetup, count = 1
            });

            EntityManager.RemoveComponent<FormationSetup>(entity);
            // ecb.RemoveComponent<FormationSetup>(entityInQueryIndex, entity);
        }).Run();
        

        if (formationEntityToSetup.Length == 0)
        {
            formationEntityToSetup.Dispose();
            return;
        }
        
        //Update leaders
        Entities.WithoutBurst().WithReadOnly(formationEntityToSetup).ForEach((in FormationLeader formationLeader) =>
        {
            for (int i = 0; i < formationEntityToSetup.Length; i++)
            {
                if (formationEntityToSetup[i].entity == formationLeader.formationEntity)
                {
                    //TODO Setup
                    break;
                }
            }
        }).Run();
        
        //Update followers
        Entities.WithoutBurst().ForEach((ref FormationFollower follower) =>
        {
            for (int i = 0; i < formationEntityToSetup.Length; i++)
            {
                if (formationEntityToSetup[i].entity == follower.formationEntity)
                {
                    var setupData = formationEntityToSetup[i];
                    
                    //Setup position id
                    follower.positionID = setupData.count;
                    setupData.count++;
                    
                    
                    formationEntityToSetup[i] = setupData;
                    break;
                }
            }
        }).Run();
        
        formationEntityToSetup.Dispose();
    }
}
