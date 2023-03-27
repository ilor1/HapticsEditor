using Unity.Collections;
using Unity.Entities;

namespace V2
{
    public struct AudioFilePath : IComponentData
    {
        public FixedString512Bytes Value;
    }
}