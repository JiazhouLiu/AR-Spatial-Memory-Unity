using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.IO;
using Microsoft.MixedReality.Toolkit;

namespace SpatialMemoryTest
{
    public class DistractorTaskSeg : MonoBehaviour
    {
        public Transform DistractorTask;
        public TextMeshPro Instruction;
        public TextMeshPro DistractorTaskInstruction;

        private int currentGameNumber;
        private List<GameObject> distractorCards;
        private GameObject touchingCard;

        private float eachDistractorReactTime = 5;
        private float localDistractorTime;
        private float localEachDistractorReactTime;
        private bool finishTouching = false;
        private bool gridCalibrate = false;

        // Start is called before the first frame update
        void Start()
        {
            distractorCards = new List<GameObject>();

            // add distractor task cards into list
            foreach (Transform t in DistractorTask)
                distractorCards.Add(t.gameObject);

            InitiateDistractorPhase();
        }

        // Update is called once per frame
        void Update()
        {
            DistractorPhaseCheck();
            if (!gridCalibrate)
            {
                gridCalibrate = true;
                SetGridPosition();
            }
        }

        private void InitiateDistractorPhase()
        {
            DistractorTask.gameObject.SetActive(true);
            GetNewDistractorTask();
        }

        private void GetNewDistractorTask()
        {
            // shuffle order for patternCards
            for (int i = 0; i < DistractorTask.childCount; i++)
            {
                // refresh card select property
                DistractorTask.GetChild(i).GetComponent<Card>().selected = false;

                Vector3 temp = DistractorTask.GetChild(i).localPosition;
                int randomIndex = Random.Range(i, DistractorTask.childCount);
                DistractorTask.GetChild(i).localPosition = DistractorTask.GetChild(randomIndex).localPosition;
                DistractorTask.GetChild(randomIndex).localPosition = temp;
            }

            int task = RandomNumber(1, 9);
            currentGameNumber = task;

            DistractorTaskInstruction.text = task + "";
        }

        private void DistractorPhaseCheck()
        {
            if (localDistractorTime >= 0)
            {
                localDistractorTime -= Time.deltaTime;

                if (localEachDistractorReactTime <= 0)
                {
                    DistractorTaskInstruction.color = Color.red;
                    localDistractorTime += eachDistractorReactTime;
                    localEachDistractorReactTime = eachDistractorReactTime;

                    GetNewDistractorTask();
                }
                else
                {
                    localEachDistractorReactTime -= Time.deltaTime;

                    Instruction.text = "Select the number below!\nTimes remaining: " + localDistractorTime.ToString("0.0");

                    if (finishTouching)
                    {
                        finishTouching = false;

                        if (touchingCard.name != currentGameNumber.ToString())
                        {
                            localDistractorTime += eachDistractorReactTime;
                            GetNewDistractorTask();
                            // play sound or show text
                            Instruction.color = Color.red;
                        }
                        else if (touchingCard.name == currentGameNumber.ToString())
                        {
                            Instruction.color = Color.white;
                            DistractorTaskInstruction.color = Color.white;
                            localEachDistractorReactTime = eachDistractorReactTime;
                            GetNewDistractorTask();
                        }
                    }
                }
            }
            else
            {
                Debug.Log("Finished");
            }
        }
        public int RandomNumber(int min, int max)
        {
            return Random.Range(min, max);
        }

        private void SetGridPosition()
        {
            DistractorTask.position = Camera.main.transform.position + Camera.main.transform.TransformDirection(Vector3.forward) * 0f + Vector3.down * 0.5f;
            DistractorTask.LookAt(Camera.main.transform.position);
            DistractorTask.localEulerAngles = new Vector3(0, transform.localEulerAngles.y, 0);
        }
    }
}