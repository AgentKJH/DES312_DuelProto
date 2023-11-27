using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ResourceBar : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private GameObject FillArea;
    [SerializeField] private Image Background;
    [SerializeField] private Color flashColor;
    [SerializeField] private Color backgroundColor;

    public void UpdateResourceBar(float currentValue, float maxValue)
    {
        slider.value = currentValue/maxValue; // set slider value based on incoming values percentage
        if (slider.value <= 0f) 
        { 
            FillArea.SetActive(false); // hide fill area when at 0
        } 
        else if (slider.value > 0f){ FillArea.SetActive(true); } //set active if value is > 0 and the object is not already active
        //print("Resource Bar Updated");
    }

    private bool flashing;
    public  void BackgroundFlash(float flashDuration)
    {
        if (!flashing) // don't flash if already doing a flash
        {
            print("flash");
            Background.color = flashColor;
            flashing = true;
            Invoke("BackgroundFlashBack", flashDuration);
        }
    }

    private void BackgroundFlashBack()
    {
        print("flashback");
        Background.color = backgroundColor;
        flashing = false;
    }
}
