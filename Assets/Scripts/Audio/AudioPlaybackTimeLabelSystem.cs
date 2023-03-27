using System;
using Unity.Entities;
using UnityEngine.UIElements;

namespace V2
{
    public partial class AudioPlaybackTimeLabelSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            if (SystemAPI.HasSingleton<AudioPlaybackTime>())
            {
                var audioPlayBack = SystemAPI.GetSingleton<AudioPlaybackTime>();

                // Assume 'milliseconds' is the input value in milliseconds
                var timeSpan = TimeSpan.FromMilliseconds(audioPlayBack.TimeInMilliseconds);
                var timeSpanLength = TimeSpan.FromMilliseconds(audioPlayBack.LengthInMilliseconds);

                // Extract hours, minutes, seconds, and tenths of a second
                int hours = timeSpan.Hours;
                int minutes = timeSpan.Minutes;
                int seconds = timeSpan.Seconds;
                int tenths = timeSpan.Milliseconds / 100;

                int lengthHours = timeSpanLength.Hours;
                int lengthMinutes = timeSpanLength.Minutes;
                int lengthSeconds = timeSpanLength.Seconds;

                foreach (var visualElementRef in SystemAPI.Query<VisualElementRef>().WithAll<AudioPlaybackTimeLabel>())
                {
                    Label label = visualElementRef.Value as Label;
                    if (lengthHours != 0)
                    {
                        label.text = $"{hours}:{minutes:00}:{seconds:00}.{tenths}/{lengthHours}:{lengthMinutes:00}:{lengthSeconds:00}";
                    }
                    else if (lengthMinutes != 0)
                    {
                        label.text = $"{minutes:00}:{seconds:00}.{tenths}/{lengthMinutes:00}:{lengthSeconds:00}";
                    }
                    else
                    {
                        label.text = $"{seconds:00}.{tenths}/{lengthSeconds:00}";
                    }
                }
            }
        }
    }
}