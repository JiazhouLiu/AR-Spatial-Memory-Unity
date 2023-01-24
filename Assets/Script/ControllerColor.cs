using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public enum ControllerType
{ 
    HTCVIVE,
    Odyssey
}

public class ControllerColor : MonoBehaviour {
    public Material white;
    public Material red;
    public Material green;

    public ControllerType controllerType;

    private int bodyIndex;
    private int gripIndex;
    private int grip2Index;
    private int menuIndex;
    private int thumbstickIndex;
    private int trackpadIndex;
    private int triggerIndex;
    private int systemIndex;

    private void Start()
    {
        if (controllerType == ControllerType.HTCVIVE)
        {
            bodyIndex = 1;
            gripIndex = 7;
            grip2Index = 8;
            menuIndex = 2;
            thumbstickIndex = -1;
            trackpadIndex = 13;
            triggerIndex = 16;
            systemIndex = 11;

            transform.parent.Find("TrackPadText").position = new Vector3(0, 0.01f, -0.05f);
            transform.parent.Find("TrackPadText").localEulerAngles = new Vector3(90, 0, 0);
            transform.parent.Find("TrackPadText").GetChild(0).GetComponent<TextMeshProUGUI>().fontSize = 10;
        }
        else if (controllerType == ControllerType.Odyssey) {
            bodyIndex = 0;
            gripIndex = 1;
            grip2Index = -1;
            menuIndex = 2;
            thumbstickIndex = 6;
            trackpadIndex = 8;
            triggerIndex = 9;
            systemIndex = -1;
        }
    }

    // Update is called once per frame
    void Update () {

        if (transform.childCount > 0)
        {
            if (bodyIndex >= 0)
                transform.GetChild(bodyIndex).GetComponent<MeshRenderer>().material = white;

            if (gripIndex > 0)
                transform.GetChild(gripIndex).GetComponent<MeshRenderer>().material = white;

            if (grip2Index > 0)
                transform.GetChild(grip2Index).GetComponent<MeshRenderer>().material = white;

            if (menuIndex > 0)
                transform.GetChild(menuIndex).GetComponent<MeshRenderer>().material = white;

            if (thumbstickIndex > 0)
                transform.GetChild(thumbstickIndex).GetComponent<MeshRenderer>().material = white;

            if (systemIndex > 0)
                transform.GetChild(systemIndex).GetComponent<MeshRenderer>().material = white;

            if (trackpadIndex > 0)
                transform.GetChild(trackpadIndex).GetComponent<MeshRenderer>().material = white;

            if (triggerIndex > 0)
                transform.GetChild(triggerIndex).GetComponent<MeshRenderer>().material = white;

            if (transform.parent.name == "Controller (right)") // highlight two buttons
            {
                if (trackpadIndex > 0)
                    transform.GetChild(trackpadIndex).GetComponent<MeshRenderer>().material = green;

                if (triggerIndex > 0)
                    transform.GetChild(triggerIndex).GetComponent<MeshRenderer>().material = red;
            }
        }
	}
}
