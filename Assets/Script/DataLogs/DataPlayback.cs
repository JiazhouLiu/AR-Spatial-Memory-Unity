using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class DataPlayback : MonoBehaviour
{
    [Header("Data Files")]
    public TextAsset HeadTransformData;
    public TextAsset LeftControllerTransformData;
    public TextAsset RightControllerTransformData;
    public TextAsset CardsData;
    public TextAsset DetailedViewsData;

    [Header("Replay Objects")]
    public Transform HeadObject;
    public Transform LeftControllerObject;
    public Transform RightControllerObject;
    public Transform[] CardsObjects;

    [Header("Replay Settings")]
    public bool UseRealTime = true;
    [Range(0, 1)] public float TimeScrubber;
    public float CurrentTime = 0;

    private bool isLiveReplayRunning = false;
    private bool isLiveReplayPaused = false;
    private float prevTimeScrubber;

    private int prevQuestionID = 0;
    [HideInInspector]
    public int currentQuestionID = 0;

    public void StartLiveReplay()
    {
        if (!isLiveReplayRunning)
        {
            isLiveReplayPaused = false;
            StartCoroutine(LiveReplay());
        }
        else if (isLiveReplayPaused)
        {
            isLiveReplayPaused = false;
        }
    }

    public IEnumerator LiveReplay()
    {
        isLiveReplayRunning = true;

        // Read and split data
        string[] headTransformDataLines = HeadTransformData.text.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        List<string[]> headTransformData = new List<string[]>();
        for (int i = 1; i < headTransformDataLines.Length; i++)
            headTransformData.Add(headTransformDataLines[i].Split('\t'));

        string[] leftControllerTransformsDataLines = LeftControllerTransformData.text.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        List<string[]> leftControllerTransformData = new List<string[]>();
        for (int i = 1; i < leftControllerTransformsDataLines.Length; i++)
            leftControllerTransformData.Add(leftControllerTransformsDataLines[i].Split('\t'));

        string[] rightControllerTransformsDataLines = RightControllerTransformData.text.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        List<string[]> rightControllerTransformData = new List<string[]>();
        for (int i = 1; i < rightControllerTransformsDataLines.Length; i++)
            rightControllerTransformData.Add(rightControllerTransformsDataLines[i].Split('\t'));

        string[] cardsDataLines = CardsData.text.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        List<string[]> cardsData = new List<string[]>();
        for (int i = 1; i < cardsDataLines.Length; i++)
            cardsData.Add(cardsDataLines[i].Split('\t'));

        float totalTime = float.Parse(headTransformData[headTransformData.Count - 1][0]);

        int cardsLineIndex = 1;

        for (int i = 0; i < headTransformData.Count; i += (isLiveReplayPaused) ? 0 : 1)
        {
            if (TimeScrubber != prevTimeScrubber)
            {
                // Find the nearest i index that matches the new time
                float newTime = TimeScrubber * totalTime;

                int startIdx = Mathf.FloorToInt(TimeScrubber * headTransformData.Count) - 50;
                startIdx = Mathf.Max(startIdx, 0);
                for (int j = startIdx; j < headTransformData.Count; j++)
                {
                    float thisTime = float.Parse(headTransformData[j][0]);
                    if (thisTime > newTime)
                    {
                        newTime = float.Parse(headTransformData[Mathf.Max(j - 1, 0)][0]);
                        i = j;
                        break;
                    }
                }

                cardsLineIndex = 1;
                prevTimeScrubber = TimeScrubber;
            }

            if (isLiveReplayPaused)
            {
                yield return new WaitForEndOfFrame();
                continue;
            }

            float currentTime = float.Parse(headTransformData[i][0]);

            CurrentTime = currentTime;
            TimeScrubber = currentTime / totalTime;
            prevTimeScrubber = TimeScrubber;

            // Set the positions and rotations of the head
            string[] headTransformsLine = headTransformData[i];
            HeadObject.position = new Vector3(float.Parse(headTransformsLine[1]), float.Parse(headTransformsLine[2]), float.Parse(headTransformsLine[3]));
            HeadObject.rotation = new Quaternion(float.Parse(headTransformsLine[4]), float.Parse(headTransformsLine[5]), float.Parse(headTransformsLine[6]), float.Parse(headTransformsLine[7]));

            // Set the positions and rotations of the head
            string[] leftControllerTransformsLine = leftControllerTransformData[i];
            LeftControllerObject.position = new Vector3(float.Parse(leftControllerTransformsLine[1]), float.Parse(leftControllerTransformsLine[2]), float.Parse(leftControllerTransformsLine[3]));
            LeftControllerObject.rotation = new Quaternion(float.Parse(leftControllerTransformsLine[4]), float.Parse(leftControllerTransformsLine[5]),
                float.Parse(leftControllerTransformsLine[6]), float.Parse(leftControllerTransformsLine[7]));

            // Set the positions and rotations of the head
            string[] rightControllerTransformsLine = rightControllerTransformData[i];
            RightControllerObject.position = new Vector3(float.Parse(rightControllerTransformsLine[1]), float.Parse(rightControllerTransformsLine[2]), float.Parse(rightControllerTransformsLine[3]));
            RightControllerObject.rotation = new Quaternion(float.Parse(rightControllerTransformsLine[4]), float.Parse(rightControllerTransformsLine[5]),
                float.Parse(rightControllerTransformsLine[6]), float.Parse(rightControllerTransformsLine[7]));


            /////// Cards //////
            // Find the line index of the cards data which has the same time as the current head transforms line
            for (int j = cardsLineIndex; j < cardsDataLines.Length; j++)
            {
                string[] cardsLine = cardsData[j];

                if (currentTime == float.Parse(cardsLine[0]))
                {
                    cardsLineIndex = j;
                    currentQuestionID = int.Parse(cardsLine[1]);

                    if (currentQuestionID != prevQuestionID)
                    { // Get current Landmarks Objects
                        prevQuestionID = currentQuestionID;

                        // clear previous landmarks
                        foreach (Transform t in CardsObjects)
                            Destroy(t.gameObject);
                        if (CardsObjects.Length != 0)
                            Debug.Log("Clear Issue!!!");
                    }
                }
            }

            //Now that we've found it, set the properties of the landmarks
            for (int j = 0; j < CardsObjects.Length; j++)
            {
                string[] cardsLine = cardsData[cardsLineIndex + j];

                foreach (Transform t in CardsObjects)
                {
                    if (t.name == cardsLine[2])
                    {
                        t.transform.position = new Vector3(float.Parse(cardsLine[1]), float.Parse(cardsLine[2]), float.Parse(cardsLine[3]));
                        t.transform.rotation = new Quaternion(float.Parse(cardsLine[4]), float.Parse(cardsLine[5]),
                            float.Parse(cardsLine[6]), float.Parse(cardsLine[7]));

                    }
                }
            }
            yield return null;
        }

        isLiveReplayRunning = false;
        isLiveReplayPaused = false;
    }

    public void PauseLiveReplay()
    {
        isLiveReplayPaused = true;
    }

    public void RestartLiveReplay()
    {
        if (isLiveReplayRunning)
        {
            StopCoroutine(LiveReplay());
            isLiveReplayRunning = false;
        }

        StartLiveReplay();
    }
}
