using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace V2
{
    public struct FunCoords : IComponentData
    {
        public NativeList<float2> Value;
    }
}