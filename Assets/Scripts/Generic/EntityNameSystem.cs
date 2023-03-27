using Unity.Burst;
using Unity.Entities;

namespace V2
{
    /// <summary>
    /// System for naming Entities in the Editor
    /// </summary>
    [RequireMatchingQueriesForUpdate]
    [BurstCompile]
    public partial struct EntityNameSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state) { }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) { }

#if !UNITY_EDITOR
        [BurstCompile]
#endif
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (entityName, entity) in SystemAPI.Query<EntityName>()
                         .WithEntityAccess())
            {
#if UNITY_EDITOR
                ecb.SetName(entity, entityName.Value);
#endif
                ecb.RemoveComponent<EntityName>(entity);
            }
        }
    }
}