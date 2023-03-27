using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace V2
{
    /// <summary>
    /// Initializes the Playback entity
    /// </summary>
    public partial class AudioUpdateSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(World.Unmanaged);


            foreach (var (audioClipRef, audioFilePath, entity) in SystemAPI.Query<AudioClipRef, RefRO<AudioFilePath>>().WithEntityAccess().WithNone<AudioPlaybackTime>().WithChangeFilter<AudioFilePath>())
            {
                // Get AudioClip length
                ecb.AddComponent(entity, new AudioPlaybackTime
                {
                    TimeInMilliseconds = 0,
                    LengthInMilliseconds = (int)(audioClipRef.Value.length * 1000f)
                });
                ecb.AddComponent<PlaybackControl>(entity);
                ecb.RemoveComponent<LoadAudioPlayback>(entity);

                // Update title filepath
                if (SystemAPI.HasSingleton<FilePathLabel>())
                {
                    SystemAPI.SetSingleton(new FilePathLabel { Value = audioFilePath.ValueRO.Value });
                }
            }

            // Read PlaybackTime from the AudioSource
            foreach (var (audioSourceRef, playbackTime) in SystemAPI.Query<AudioSourceRef, RefRW<AudioPlaybackTime>>())
            {
                playbackTime.ValueRW.TimeInMilliseconds = (int)math.round((audioSourceRef.Value.timeSamples / (float)audioSourceRef.Value.clip.frequency) * 1000f);
            }
        }
    }
}