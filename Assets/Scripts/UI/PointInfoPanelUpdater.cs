using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;
using V2;

public partial class PointInfoPanelUpdater : SystemBase
{
    private const string STEP_MODE = "Mode: Step";
    private const string LINEAR_MODE = "Mode: Linear";
    private const string SNAP_MODE = "Snap mode: ";

    private const string POINT_SETTINGS = "pointsettings";
    private const string PATTERN_SETTINGS = "patternsettings";

    private const string MODE = "mode";
    private const string SNAPPING = "snapping";
    private const string SCALE = "scale";
    private const string REPEAT = "repeat";
    private const string SPACE = "spacing";
    private const string SCALING = "Scaling:";
    private const string REPEATING = "Repeat:";
    private const string SPACING = "Spacing:";
    
    protected override void OnUpdate()
    {
        var modeSelection = SystemAPI.GetSingleton<FunScriptModeSelection>();
        var pointModeSettings = SystemAPI.GetSingleton<FunScriptPointModeSettings>();
        var patternModeSettings = SystemAPI.GetSingleton<FunScriptPatternModeSettings>();

        foreach (var (container, panel) in SystemAPI.Query<VisualElementRef, RefRO<PointSettingsPanel>>())
        {
            var patternSettings = container.Value.Q(PATTERN_SETTINGS);
            var pointSettings = container.Value.Q(POINT_SETTINGS);
            // pointmode
            if (modeSelection.Value == 0)
            {
                pointSettings.style.display = DisplayStyle.Flex;
                patternSettings.style.display = DisplayStyle.None;
                
                var mode = container.Value.Q(MODE) as Label;
                var snapping = container.Value.Q(SNAPPING) as Label;

                mode.text = pointModeSettings.StepMode ? STEP_MODE : LINEAR_MODE;
                snapping.text = SNAP_MODE + pointModeSettings.Snapping;
            }
            // patternmode
            else if (modeSelection.Value == 1)
            {
                pointSettings.style.display = DisplayStyle.None;
                patternSettings.style.display = DisplayStyle.Flex;
                
                var scale = container.Value.Q(SCALE) as Label;
                var repeat = container.Value.Q(REPEAT) as Label;
                var spacing = container.Value.Q(SPACE) as Label;

                scale.text = SCALING + patternModeSettings.Scale;
                repeat.text = REPEATING + patternModeSettings.Repeat;
                spacing.text = SPACING + patternModeSettings.Spacing;
            }
        }
    }
}