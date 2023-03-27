using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace V2
{
    [UpdateBefore(typeof(FunActionRendererSystem))]
    [BurstCompile]
    public partial struct FunActionsToCoordsSystem : ISystem
    {
        private EntityQuery _funActionQuery;
        private EntityQuery _funCoordsQuery;
        private NativeList<float2> _funCoords;
        private NativeList<FunAction> _activeFunActions;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _funActionQuery = state.GetEntityQuery(ComponentType.ReadOnly<FunAction>());
            _funCoordsQuery = state.GetEntityQuery(ComponentType.ReadWrite<FunCoords>());

            state.RequireForUpdate<AudioPlaybackTime>();
            state.RequireForUpdate<FunCoords>();

            _funCoords = new NativeList<float2>(1000, Allocator.Persistent);
            _activeFunActions = new NativeList<FunAction>(1000, Allocator.Persistent);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) { }

        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            var audioPlaybackTime = SystemAPI.GetSingleton<AudioPlaybackTime>();

            // Sort FunActions
            var funActions = _funActionQuery.ToComponentDataArray<FunAction>(Allocator.TempJob);
            funActions.Sort(new FunActionSorter());

            _funCoords.Clear();
            _activeFunActions.Clear();
            foreach (var funscriptContainer in SystemAPI.Query<VisualElementRef>().WithAll<FunScriptContainer>())
            {
                // Get active FunActions as pixel coords
                new ConvertFunActionsToCoordinatesJob
                {
                    Width = funscriptContainer.Value.contentRect.width,
                    Height = funscriptContainer.Value.contentRect.height,
                    StartTimeInMilliseconds = audioPlaybackTime.TimeInMilliseconds - 8000,
                    EndTimeInMilliseconds = audioPlaybackTime.TimeInMilliseconds + 8000,
                    WidthInMilliseconds = 16000,
                    FunActions = funActions,
                    FunCoords = _funCoords,
                    ActiveFunActions = _activeFunActions
                }.Schedule().Complete();
            }

            ecb.AddComponent(_funCoordsQuery, new FunCoords { Value = _funCoords });
            ecb.AddComponent(_funCoordsQuery, new ActiveFunActions { Value = _activeFunActions });
        }
    }
}