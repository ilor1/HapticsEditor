using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace V2
{
    public class EntityNameAuthoring : MonoBehaviour { }

    public struct EntityName : IComponentData
    {
        public FixedString64Bytes Value;
    }

    public class EntityNameBaker : Baker<EntityNameAuthoring>
    {
        public override void Bake(EntityNameAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            
            AddComponent(entity, new EntityName
            {
                // Apply GameObject name to the entity
                Value = new FixedString64Bytes(authoring.name)
            });
        }
    }
}