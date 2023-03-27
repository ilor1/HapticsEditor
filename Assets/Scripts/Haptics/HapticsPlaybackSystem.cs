using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace V2
{
    [BurstCompile]
    public partial struct HapticsPlaybackSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AudioPlaybackTime>();
            state.RequireForUpdate<ActiveFunActions>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
       
        }

        public void OnUpdate(ref SystemState state)
        {
            var playBackTime = SystemAPI.GetSingleton<AudioPlaybackTime>().TimeInMilliseconds;
            var activeFunActions = SystemAPI.GetSingleton<ActiveFunActions>().Value;

            // Set Intensity value
            for (int i = 0; i < activeFunActions.Length; i++)
            {
                if (activeFunActions[i].at == playBackTime)
                {
                    HapticServer.Instance.SetIntensity(activeFunActions[i].pos * 0.01f);
                    break;
                }
                else if (i > 0 && activeFunActions[i].at > playBackTime)
                {
                    float b = activeFunActions[i].at - activeFunActions[i - 1].at;
                    float t = (playBackTime - activeFunActions[i - 1].at) / b;
                    float intensity = math.lerp((float)activeFunActions[i - 1].pos, (float)activeFunActions[i].pos, t) * 0.01f;
                    HapticServer.Instance.SetIntensity(intensity);
                    break;
                }
            }
        }
    }
}