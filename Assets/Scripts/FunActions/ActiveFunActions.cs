using Unity.Collections;
using Unity.Entities;

namespace V2
{
    public struct ActiveFunActions : IComponentData
    {
        public NativeList<FunAction> Value;
    }
}