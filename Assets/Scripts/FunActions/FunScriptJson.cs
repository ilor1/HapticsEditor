using System;
using Unity.Collections;
using Unity.Entities;

namespace V2
{
    [Serializable]
    public struct FunScriptJson
    {
        public FunAction[] actions;
        public bool inverted;
        public Metadata metadata;
    }
    
    [Serializable]
    public struct Metadata 
    {
        public string creator;
        public string description;
        public int duration;
        public string license;
        public string notes;
        public string[] performers;
        public string script_url;
        public string[] tags;
        public string title;
        public string type;
        public string video_url;
        public int range;
        public string version;
    }
    
    public struct MetadataComponent : IComponentData
    {
        public bool inverted;
        public FixedString64Bytes creator;
        public FixedString512Bytes description;
        public int duration;
        public FixedString128Bytes license;
        public FixedString512Bytes notes;
        public NativeList<FixedString64Bytes> performers;
        public FixedString128Bytes script_url;
        public NativeList<FixedString32Bytes> tags;
        public FixedString128Bytes title;
        public FixedString32Bytes type;
        public FixedString128Bytes video_url;
        public int range;
        public FixedString32Bytes version;
    }
}