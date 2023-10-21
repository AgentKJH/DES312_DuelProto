using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ResourceBar : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private GameObject FillArea;

    public void UpdateResourceBar(float currentValue, float maxValue)
    {
        slider.value = currentValue/maxValue; // set slider value based on incoming values percentage
        if (slider.value <= 0f) { FillArea.SetActive(false); } // hide fill area when at 0
        else if (slider.value > 0f){ FillArea.SetActive(true); } //set active if value is > 0 and the object is not already active
        //print("Resource Bar Updated");
    }
}
