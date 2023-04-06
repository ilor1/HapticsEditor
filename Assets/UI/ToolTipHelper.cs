using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

public class ToolTipHelper : MonoBehaviour
{
    //
    private VisualElement root;
    private Label label;

    async void Start()
    {
        var doc = GetComponent<UIDocument>();

        while (doc.rootVisualElement == null)
        {
            await Task.Delay(1000);
        }

        root = doc.rootVisualElement;
        label = root.Q<Label>("tooltip");
    }

    void Update()
    {
        if (root == null) return;

        //Debug.Log($"mouse:{Input.mousePosition}, content:{root.contentRect.width}, {root.contentRect.height}, screen:{Screen.width},{Screen.height}");

        string tooltip = CurrentToolTip(root.panel);
        if (tooltip != "")
        {
            label.visible = true;
            label.text = tooltip;
            if (Input.mousePosition.x >= Screen.width * 0.5f)
            {
                label.style.left = Input.mousePosition.x * (root.contentRect.width / Screen.width) - label.contentRect.width - 15;
            }
            else
            {
                label.style.left = Input.mousePosition.x * (root.contentRect.width / Screen.width) + 15;
            }

            label.style.top = Screen.height - Input.mousePosition.y + 5;
        }
        else
        {
            label.visible = false;
        }
    }

    string CurrentToolTip(IPanel panel)
    {
        // https://docs.unity3d.com/2022.2/Documentation/Manual/UIE-faq-event-and-input-system.html

        if (!EventSystem.current.IsPointerOverGameObject()) return "";

        var screenPosition = Input.mousePosition;
        screenPosition.y = Screen.height - screenPosition.y;

        VisualElement ve = panel.Pick(RuntimePanelUtils.ScreenToPanel(panel, screenPosition));
        return ve == null ? "" : ve.tooltip;
    }
}