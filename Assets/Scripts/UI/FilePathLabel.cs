using Unity.Collections;
using Unity.Entities;

namespace V2
{
    public struct FilePathLabel : IComponentData
    {
        public FixedString512Bytes Value;
    }
}