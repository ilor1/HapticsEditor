using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;
using V2;

public class AudioPlaybackSlider : MonoBehaviour
{
    [SerializeField] private AudioSource _audioSource;
    private Slider _slider;
    private bool _hover;

    private void Start()
    {
        _slider = GetComponent<UIDocument>().rootVisualElement.Q(UIConstants.WAVEFORM_SLIDER) as Slider;
        _slider.RegisterValueChangedCallback(evt =>
        {
            if (_hover) _audioSource.time = _slider.value;
        });
        _slider.RegisterCallback<MouseEnterEvent>(evt => _hover = true);
        _slider.RegisterCallback<MouseLeaveEvent>(evt => _hover = false);
        
        _slider.parent.RegisterCallback<MouseEnterEvent>(evt => _slider.style.display = DisplayStyle.Flex);
        _slider.parent.RegisterCallback<MouseLeaveEvent>(evt => _slider.style.display = DisplayStyle.None);

        _slider.style.display = DisplayStyle.None;
    }

    private void Update()
    {
        if (_audioSource.clip != null && _slider.highValue != _audioSource.clip.length - 1.0f)
        {
            _slider.highValue = _audioSource.clip.length - 1.0f;
        }

        if (!_hover) _slider.value = _audioSource.time;
    }
}