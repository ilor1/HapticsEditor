using Unity.Entities;
using UnityEngine;

namespace V2
{
    /// <summary>
    /// Controls the Playback
    /// TODO: edit so that these manage the data component and not the audiosource itself.
    /// </summary>
    [UpdateBefore(typeof(AudioUpdateSystem))]
    public partial class AudioControlsSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            // Toggle playback
            if (Input.GetKeyDown(KeyCode.Space))
            {
                foreach (var playbackControl in SystemAPI.Query<RefRW<PlaybackControl>>())
                {
                    playbackControl.ValueRW.IsPlaying = !playbackControl.ValueRW.IsPlaying;
                }
            }

            // Fast-Forward (4x)
            if (Input.GetKey(KeyCode.RightArrow))
            {
                float multiplier = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) ? 16 : 4;

                foreach (var (src, playback) in SystemAPI.Query<AudioSourceRef, RefRO<PlaybackControl>>())
                {
                    if (playback.ValueRO.IsPlaying) src.Value.Pause();

                    float currentTime = src.Value.time;
                    float length = src.Value.clip.length;
                    float t = currentTime + multiplier * deltaTime;
                    if (playback.ValueRO.IsPlaying) t -= deltaTime; // adjust for the PlayHead moving forward
                    src.Value.time = t > length ? t - length : t;
                }
            }
            else if (Input.GetKey(KeyCode.LeftArrow))
            {
                float multiplier = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) ? 16 : 4;

                foreach (var (src, playback) in SystemAPI.Query<AudioSourceRef, RefRO<PlaybackControl>>())
                {
                    float currentTime = src.Value.time;
                    float length = src.Value.clip.length;
                    float t = currentTime - multiplier * deltaTime;
                    if (playback.ValueRO.IsPlaying) t -= deltaTime; // adjust for the PlayHead moving forward
                    src.Value.time = t < 0 ? length + t : t;
                }
            }

            // Play/Pause AudioSource
            foreach (var (audioSource, playbackControl, entity) in SystemAPI.Query<AudioSourceRef, RefRW<PlaybackControl>>().WithEntityAccess())
            {
                var src = audioSource.Value;

                // no clip. stop playback
                if (src.clip == null)
                {
                    playbackControl.ValueRW.IsPlaying = false;
                }

                if (playbackControl.ValueRO.IsPlaying != src.isPlaying)
                {
                    if (playbackControl.ValueRO.IsPlaying) audioSource.Value.Play();
                    else audioSource.Value.Pause();
                }
            }
        }
    }
}