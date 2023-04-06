using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;
using V2;

public class UIDocumentReferenceManagerSystem : MonoBehaviour
{
    public static UIDocumentReferenceManagerSystem Instance;

    private UIDocument _doc;
    private List<VisualElement> _visualElements = new List<VisualElement>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this)
        {
            Destroy(this);
        }
    }

    async void Start()
    {
        _doc = GetComponent<UIDocument>();

        while (_doc.rootVisualElement == null)
        {
            await Task.Delay(1000);
        }

        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        Entity entity = em.CreateEntity();

        em.AddComponent<UIDocumentProcessed>(entity);

        // Create VisualElementReferences
        _visualElements = _doc.rootVisualElement.Query<VisualElement>().ToList();
        foreach (var element in _visualElements)
        {
            Entity visualElementEntity = em.CreateEntity();
            em.AddComponentData(visualElementEntity, new VisualElementRef
            {
                Value = element
            });
            AddVisualElementTag(em, visualElementEntity, element);
        }
    }

    public static bool TryGetVisualElementByName(string name, out VisualElement visualElement)
    {
        visualElement = null;
        foreach (var element in Instance._visualElements)
        {
            if (element.name == name)
            {
                visualElement = element;
                return true;
            }
        }
        return false;
    }

    // Add Specific tag components
    private void AddVisualElementTag(EntityManager em, Entity entity, VisualElement element)
    {
        switch (element.name)
        {
            case UIConstants.FUNSCRIPT_NAME:
#if UNITY_EDITOR
                em.AddComponentData(entity, new EntityName { Value = new FixedString64Bytes(UIConstants.FUNSCRIPT_NAME) });
#endif
                em.AddComponent<FunScriptContainer>(entity);
                em.AddComponent<FunCoords>(entity);
                break;
            case UIConstants.FILEPATH_NAME:
#if UNITY_EDITOR
                em.AddComponentData(entity, new EntityName { Value = new FixedString64Bytes(UIConstants.FILEPATH_NAME) });
#endif
                em.AddComponent<FilePathLabel>(entity);
                break;
            case UIConstants.TIMEDISPLAY_LABEL_NAME:
#if UNITY_EDITOR
                em.AddComponentData(entity, new EntityName { Value = new FixedString64Bytes(UIConstants.TIMEDISPLAY_LABEL_NAME) });
#endif
                em.AddComponent<AudioPlaybackTimeLabel>(entity);
                break;
            case UIConstants.WAVEFORM_CONTAINER:
#if UNITY_EDITOR
                em.AddComponentData(entity, new EntityName { Value = new FixedString64Bytes(UIConstants.WAVEFORM_CONTAINER) });
#endif
                break;
            case UIConstants.TOOLS_PANEL:
#if UNITY_EDITOR
                em.AddComponentData(entity, new EntityName { Value = new FixedString64Bytes(UIConstants.TOOLS_PANEL) });
#endif
                em.AddComponent<PointSettingsPanel>(entity);
                break;
            case UIConstants.PATTERN_STAMP:
#if UNITY_EDITOR
                em.AddComponentData(entity, new EntityName { Value = new FixedString64Bytes(UIConstants.PATTERN_STAMP) });
#endif
                em.AddComponent<PatternStamp>(entity);
                break;
            case UIConstants.METADATA:
#if UNITY_EDITOR
                em.AddComponentData(entity, new EntityName { Value = new FixedString64Bytes(UIConstants.METADATA) });
#endif
                em.AddComponentData(entity, new MetadataComponent
                {
                    performers = new NativeList<FixedString64Bytes>(Allocator.Persistent),
                    tags = new NativeList<FixedString32Bytes>(Allocator.Persistent)
                });
                break;
        }
    }
}