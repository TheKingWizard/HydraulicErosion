using System;
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

        Viewer viewer = GameObject.Find("Main Camera").GetComponent<Viewer>();

        var menu = root.Q<VisualElement>("menu");
        menu.RegisterCallback<MouseEnterEvent>(evt => viewer.Enable(false));
        menu.RegisterCallback<MouseLeaveEvent>(evt => viewer.Enable(true));
    }

    private void Start()
    {
        display.Simulate(0, false);
    }

    private void Simulate()
    {
        WaterDroplet.Settings = ReadSettings();
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
            inertia = root.Q<SliderInt>("inertia").value / 100f,
            gravity = root.Q<Slider>("gravity").value,
            evaporation = root.Q<SliderInt>("evaporation").value / 100f,
            capacity = root.Q<Slider>("capacity").value,
            erosion = root.Q<SliderInt>("erosion").value / 100f,
            deposition = root.Q<SliderInt>("deposition").value / 100f,
            minErosion = root.Q<SliderInt>("minErosion").value / 1000f
        };
    }
}
