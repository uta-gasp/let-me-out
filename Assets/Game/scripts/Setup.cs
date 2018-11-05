using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Setup : MonoBehaviour
{
    public enum Mode
    {
        HeadGaze,
        Gaze
    }

    public GameObject ui;
    public Dropdown modeControl;

    public Mode mode { get { return (Mode)modeControl.value; } }

    void Start()
    {
        modeControl.value = (int)Mode.Gaze;
    }

    public void hide()
    {
        ui.SetActive(false);
    }
}
