using Unity.Entities;
using UnityEngine;

namespace V2
{
    public class FunScriptLineColorAuthoring : MonoBehaviour
    {
        public Color Value;
    }
    
    public class FunScriptLineColorBaker : Baker<FunScriptLineColorAuthoring>
    {
        public override void Bake(FunScriptLineColorAuthoring authoring)
        {
            AddComponent(new FunScriptLineColor
            {
                Value = authoring.Value
            });
        }
    }
    
    public struct FunScriptLineColor : IComponentData
    {
        public Color Value;
    }
    
    public struct PatternLineColor : IComponentData
    {
        public Color Value;
    }
}