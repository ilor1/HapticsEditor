using Unity.Entities;
using UnityEngine;

namespace V2
{
    public class FunScriptPointColorAuthoring : MonoBehaviour
    {
        public Color Value;
    }
    
    public class FunScriptPointColorBaker : Baker<FunScriptPointColorAuthoring>
    {
        public override void Bake(FunScriptPointColorAuthoring authoring)
        {
            AddComponent(new FunScriptPointColor
            {
                Value = authoring.Value
            });
        }
    }
    public struct FunScriptPointColor : IComponentData
    {
        public Color Value;
    }
    
    public struct PatternPointColor : IComponentData
    {
        public Color Value;
    }
}