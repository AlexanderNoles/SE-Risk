using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OptionsManagement : MonoBehaviour
{
    public Slider volumeSlider;
    private float tTillTestSound;

    public void VolumeSliderCallback()
    {
        AudioManagement.UpdateGeneralVolume(volumeSlider.value);
        tTillTestSound = 0.3f;
    }

    private void Update()
    {
        if (tTillTestSound > 0.0f)
        {
            tTillTestSound -= Time.deltaTime;

            if(tTillTestSound <= 0.0f)
            {
                AudioManagement.PlaySound("ButtonPress");
            }
        }
    }
}
