using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class UI : MonoBehaviour
{
    private VisualElement root;
    private Display display;

    void OnEnable()
    {
        display = GetComponent<Display>();

        var doc = gameObject.GetComponent<UIDocument>();
        root = doc.rootVisualElement;
        
        var toggle = root.Q<Toggle>("useGPU");
        var toggleAction = ToggleUseGPU();
        toggle.RegisterCallback<ChangeEvent<bool>>(evt => toggleAction(evt.newValue));

        var button = root.Q<Button>("simulate");
        button.RegisterCallback<ClickEvent>(evt => Simulate());
    }

    private void Simulate()
    {
        Debug.Log("Simulating!");
        WaterDroplet.SetWaterDropletSettings(ReadSettings());
        ErosionRegion.SetErosionRadius(root.Q<SliderInt>("erosionRadius").value);

        int numDrops = root.Q<SliderInt>("numDrops").value;
        bool useGPU = root.Q<Toggle>("useGPU").value;
        display.Simulate(numDrops, useGPU);
    }

    private Action<bool> ToggleUseGPU()
    {
        var radiusSlider = root.Q<SliderInt>("erosionRadius");
        radiusSlider.SetEnabled(!root.Q<Toggle>("useGPU").value);
        var radius = radiusSlider.value;

        return (toggled) =>
        {
            if (toggled)
            {
                radius = radiusSlider.value;
                radiusSlider.value = 0;
                radiusSlider.SetEnabled(false);
            }
            else
            {
                radiusSlider.value = radius;
                radiusSlider.SetEnabled(true);
            }
        };
    }

    private WaterDropletSettings ReadSettings()
    {
        return new WaterDropletSettings
        {
            lifetime = root.Q<SliderInt>("lifetime").value,
            inertia = root.Q<Slider>("inertia").value,
            gravity = root.Q<Slider>("gravity").value,
            evaporation = root.Q<Slider>("evaporation").value,
            capacity = root.Q<Slider>("capacity").value,
            erosion = root.Q<Slider>("erosion").value,
            deposition = root.Q<Slider>("deposition").value,
            minErosion = root.Q<Slider>("minErosion").value
        };
    }
}
