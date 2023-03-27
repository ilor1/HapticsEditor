using Unity.Entities;

namespace V2
{
    public struct AudioPlaybackTime : IComponentData
    {
        public int TimeInMilliseconds;
        public int LengthInMilliseconds;
    }

    public struct PlaybackControl : IComponentData
    {
        public bool IsPlaying;
    }
}