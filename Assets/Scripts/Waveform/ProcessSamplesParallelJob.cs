﻿using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace V2
{
    [BurstCompile]
    public struct ProcessSamplesParallelJob : IJobParallelFor
    {
        [ReadOnly] public int SamplesPerPixel;
        [ReadOnly] public NativeArray<float> Samples;
        [ReadOnly] public int Channels;
        [ReadOnly] public float MaxSampleValue;

        [WriteOnly] public NativeArray<float> LeftHighestSamples;
        [WriteOnly] public NativeArray<float> RightHighestSamples;

        [WriteOnly] public NativeArray<float> LeftRms;
        [WriteOnly] public NativeArray<float> RightRms;

        [BurstCompile]
        public void Execute(int x)
        {
            int firstSampleIndex = SamplesPerPixel * x;
            int lastSampleIndex = firstSampleIndex +SamplesPerPixel;
            float leftRms = 0;
            float rightRms = 0;
            float leftHighestSample = 0;
            float rightHighestSample = 0;

            for (int i = firstSampleIndex; i < lastSampleIndex; i++)
            {
                // https://manual.audacityteam.org/man/audacity_waveform.html
                float leftSample = 0;
                float rightSample = 0;

                leftSample = math.abs(Samples[i * Channels]) / MaxSampleValue;
                rightSample = math.abs(Samples[i * Channels + 1]) / MaxSampleValue;

                // Get highest sample values
                leftHighestSample = leftSample > leftHighestSample ? leftSample : leftHighestSample;
                rightHighestSample = rightSample > rightHighestSample ? rightSample : rightHighestSample;

                // Get RMS sample values
                leftRms += leftSample;
                rightRms += rightSample;
            }

            LeftHighestSamples[x] = leftHighestSample;
            RightHighestSamples[x] = rightHighestSample;

            // average the samples
            LeftRms[x] = leftRms / SamplesPerPixel;
            RightRms[x] = rightRms / SamplesPerPixel;
        }
    } 
}