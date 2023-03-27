using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class VerticalGridDrawer : MonoBehaviour
{
    [SerializeField] private UIDocument _uiDocument;
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private Color32 _color;
    
    private VisualElement _gridContainer;
    private float _prevTime;

    void Start()
    {
        _gridContainer = _uiDocument.rootVisualElement.Q("vertical-grid");
        _gridContainer.generateVisualContent += DrawGrid;
    }

    void Update()
    {
        if (_audioSource.time != _prevTime)
        {
            _prevTime = _audioSource.time;
            _gridContainer.MarkDirtyRepaint();
        }
    }

    private void DrawGrid(MeshGenerationContext mgc)
    {
        float width = _gridContainer.contentRect.width;
        float height = _gridContainer.contentRect.height;
        float pixelsPerSecond = width / 16;
        float offset = -_prevTime % 1.0f * pixelsPerSecond;

        var painter = mgc.painter2D;
        painter.lineJoin = LineJoin.Miter;
        painter.lineCap = LineCap.Butt;

        painter.strokeColor = _color;
        painter.lineWidth = 2f;

        painter.BeginPath();

        for (int i = 0; i < 17; i++)
        {
            var a = new Vector2(i * pixelsPerSecond + offset, 0);
            var b = new Vector2(i * pixelsPerSecond + offset, height);
            painter.MoveTo(a);
            painter.LineTo(b);
        }

        painter.Stroke();
    }
}