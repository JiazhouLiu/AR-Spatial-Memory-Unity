using SpatialMemoryTest;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;



public class Testing : MonoBehaviour
{
    public TextMeshPro textOutput;
    private int ExperimentNumber = 0;
    private int TrialNumber = 0;

    private SetupParameter sp;
    private bool experimentNumberConfirmed = false;
    private bool trialNumberConfirmed = false;

    // Start is called before the first frame update
    void Start()
    {
        sp = SetupParameter.ExperimentNumber;
    }

    // Update is called once per frame
    void Update()
    {
        string eString = "";
        if (!experimentNumberConfirmed)
            eString = "Experiment Number: " + ExperimentNumber + ".\n";
        else
            eString = "Experiment Number: " + ExperimentNumber + " (Comfirmed).\n";

        string tString = "";
        if(!trialNumberConfirmed)
            tString = "Trial Number: " + TrialNumber + ".\n";
        else
            tString = "Trial Number: " + TrialNumber + " (Comfirmed).\n";

        if(experimentNumberConfirmed && sp == SetupParameter.ExperimentNumber)
            sp = SetupParameter.TrialNumber;

        textOutput.text = eString + tString;
    }

    public void AddButton() {
        if (sp == SetupParameter.ExperimentNumber)
            ExperimentNumber++;
        else if(sp == SetupParameter.TrialNumber)
            TrialNumber++;
    }

    public void MinuesButton() {
        if (sp == SetupParameter.ExperimentNumber)
            ExperimentNumber--;
        else if (sp == SetupParameter.TrialNumber)
            TrialNumber--;
    }

    public void ConfirmButton() {
        if (sp == SetupParameter.ExperimentNumber)
            experimentNumberConfirmed = true;
        else if (sp == SetupParameter.TrialNumber)
            trialNumberConfirmed = true;    
    }
}
