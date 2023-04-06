using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

namespace V2
{
    [UpdateAfter(typeof(AudioUpdateSystem))]
    public partial class FunActionRendererSystem : SystemBase
    {
        private EntityQuery _funActionCoordQuery;
        private NativeList<float2> _positions;
        private bool _generateVisualContentCallBackAdded = false;

        protected override void OnCreate()
        {
            base.OnCreate();
            _positions = new NativeList<float2>(Allocator.Persistent);

            // Create funScriptColorEntity
            Entity funScriptColorEntity = EntityManager.CreateEntity();
            ColorUtility.TryParseHtmlString("#F821D8", out var lineColor);
            EntityManager.AddComponentData(funScriptColorEntity, new FunScriptLineColor
            {
                Value = lineColor
            });
            ColorUtility.TryParseHtmlString("#AB3ED3", out var pointColor);
            EntityManager.AddComponentData(funScriptColorEntity, new FunScriptPointColor()
            {
                Value = pointColor
            });
#if UNITY_EDITOR
            EntityManager.AddComponentData(funScriptColorEntity, new EntityName
            {
                Value = new FixedString64Bytes("FunScriptColorSettings")
            });
#endif
        }

        protected override void OnUpdate()
        {
            if (!_generateVisualContentCallBackAdded)
            {
                // Create FunScriptRenderer if it doesn't exist.
                foreach (var (visualElementRef, entity) in SystemAPI.Query<VisualElementRef>().WithAll<FunScriptContainer>().WithEntityAccess())
                {
                    // Turn the FunScript container into FunScript LineRenderer
                    visualElementRef.Value.generateVisualContent += DrawLine;
                    _generateVisualContentCallBackAdded = true;
                }
            }

            // Repaint
            foreach (var (visualElementRef, funCoords) in SystemAPI.Query<VisualElementRef, RefRO<FunCoords>>().WithChangeFilter<FunCoords>())
            {
                if (funCoords.ValueRO.Value.IsCreated)
                {
                    _positions.CopyFrom(funCoords.ValueRO.Value);
                    visualElementRef.Value.MarkDirtyRepaint();
                }
            }
        }

        private void DrawLine(MeshGenerationContext mgc)
        {
            // Draw
            var painter = mgc.painter2D;
            painter.lineJoin = LineJoin.Round;
            painter.lineCap = LineCap.Round;
            painter.strokeColor = SystemAPI.GetSingleton<FunScriptLineColor>().Value;
            painter.fillColor = SystemAPI.GetSingleton<FunScriptPointColor>().Value;
            painter.lineWidth = 6f;
            painter.BeginPath();

            // Draw line
            if (_positions.IsCreated && _positions.Length > 0)
            {
                painter.MoveTo(_positions[0]);
                for (int i = 1; i < _positions.Length; i++)
                {
                    painter.LineTo(_positions[i]);
                }
            }

            // Draw line till the end even if there's no action at the end
            float width = mgc.visualElement.contentRect.width;
            if (_positions.Length > 0 && _positions[_positions.Length - 1].x < width)
            {
                painter.LineTo(new Vector2(mgc.visualElement.contentRect.width, _positions[_positions.Length - 1].y));
            }

            painter.Stroke();

            // Draw points
            if (_positions.IsCreated && _positions.Length > 0)
            {
                for (int i = 0; i < _positions.Length; i++)
                {
                    painter.BeginPath();
                    painter.Arc(_positions[i], 8.0f, 0.0f, 360.0f);
                    painter.Fill();
                }
            }
        }
    }
}