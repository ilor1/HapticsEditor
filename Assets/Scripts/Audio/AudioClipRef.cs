using Unity.Entities;
using UnityEngine;

namespace V2
{
    public class AudioClipRef : IComponentData
    {
        public AudioClip Value;
    }
}