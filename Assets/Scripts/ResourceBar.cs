using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ResourceBar : MonoBehaviour
{
    [SerializeField] private Slider slider;

    public void UpdateResourceBar(float currentValue, float maxValue)
    {
        slider.value = currentValue/maxValue;
        print("Resource Bar Updated");
    }
}
