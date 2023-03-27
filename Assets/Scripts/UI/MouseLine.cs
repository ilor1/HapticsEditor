using UnityEngine;
using UnityEngine.UIElements;

public class MouseLine : MonoBehaviour
{
    private VisualElement _mouseLine;
    private VisualElement _main;
    private VisualElement _playHead;


    private void Start()
    {
        var uiDoc = GetComponent<UIDocument>();
        _main = uiDoc.rootVisualElement.Q("main");
        _mouseLine = _main.Q("mouseline");
        _playHead = _main.Q("playhead");
        
        _mouseLine.parent.RegisterCallback<MouseEnterEvent>(evt => _mouseLine.style.display = DisplayStyle.Flex);
        _mouseLine.parent.RegisterCallback<MouseLeaveEvent>(evt => _mouseLine.style.display = DisplayStyle.None);

        _mouseLine.style.display = DisplayStyle.None;
    }

    private void Update()
    {
        var mousePosition = Input.mousePosition;
        Vector2 mousePositionCorrected = new Vector2(mousePosition.x, Screen.height - mousePosition.y);
        var pos = RuntimePanelUtils.ScreenToPanel(_main.panel, mousePositionCorrected);

        pos.y = _playHead.transform.position.y;
        _mouseLine.transform.position = pos;
    }
}