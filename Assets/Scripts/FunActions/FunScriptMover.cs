using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;
using V2;

public class FunScriptMover : MonoBehaviour
{
    public int StartTime;
    public int EndTime;
    public int MillisecondsToMove = 0;

    [ContextMenu("Move FunActions")]
    public void MoveFunActions()
    {
        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        var entity = em.CreateEntity();
        em.AddComponentData(entity, new MoveFunActions { StartTime = StartTime, EndTime = EndTime, MillisecondsToMove = MillisecondsToMove });
    }
}

[BurstCompile]
public partial struct FunScriptMoverSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<MoveFunActions>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        Entity moveEntity = SystemAPI.GetSingletonEntity<MoveFunActions>();
        var moveFunAction = SystemAPI.GetSingleton<MoveFunActions>();

        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var funAction in SystemAPI.Query<RefRW<FunAction>>())
        {
            if (funAction.ValueRW.at > moveFunAction.StartTime && funAction.ValueRW.at < moveFunAction.EndTime)
                funAction.ValueRW.at += moveFunAction.MillisecondsToMove;
        }

        ecb.DestroyEntity(moveEntity);
    }
}

public struct MoveFunActions : IComponentData
{
    public int StartTime;
    public int EndTime;
    public int MillisecondsToMove;
}