using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class StartSceneScript : MonoBehaviour
{
    public static float adjustedHeight = 1;
    public static int ExperimentSequence;
    public static int ParticipantID;
    public static string CurrentDateTime;
    public static int PublicTrialNumber;
    public static float lastTimePast;

    [Header("Do Not Change")]
    public Text instruction;
    public Transform CardGame;
    public Transform Cards;

    [Header("Experiment Parameter")]
    public int ExperimentID;
    public int TrialNumber;

    private List<GameObject> cardLists;

    private bool exploreActivated = false;
    private bool learningActivated = false;
    private bool distractorActivated = false;
    private bool recallActivated = false;
    private bool resultActivated = false;
    private bool nextActivated = false;

    private bool showPatternFlag = false;

    // Start is called before the first frame update
    void Start()
    {
        cardLists = new List<GameObject>
        {
            Cards.GetChild(0).gameObject,
            Cards.GetChild(1).gameObject,
            Cards.GetChild(2).gameObject,
            Cards.GetChild(3).gameObject
        };


        PublicTrialNumber = TrialNumber;

        if (ExperimentID > 0)
        {
            ParticipantID = ExperimentID;

            switch (ExperimentID % 4)
            {
                case 1:
                    ExperimentSequence = 1;
                    break;
                case 2:
                    ExperimentSequence = 2;
                    break;
                case 3:
                    ExperimentSequence = 3;
                    break;
                case 0:
                    ExperimentSequence = 4;
                    break;
                default:
                    break;
            }
        }
        else
        { // testing stream
            ExperimentSequence = 1;
        }
        for (int i = 0; i < CardGame.childCount; i++)
        {
            Vector3 temp = CardGame.GetChild(i).localPosition;
            int randomIndex = Random.Range(i, CardGame.childCount);
            CardGame.GetChild(i).localPosition = CardGame.GetChild(randomIndex).localPosition;
            CardGame.GetChild(randomIndex).localPosition = temp;
        }
        if (TrialNumber == 0)
        {
            CurrentDateTime = GetDateTimeString();

            // Raw data log
            string writerFilePath = "Assets/ExperimentData/ExperimentLog/Participant " + ParticipantID + "/Participant_" + ParticipantID + "_RawData.csv";
            StreamWriter writer = new StreamWriter(writerFilePath, false);
            string logFileHeader = "TimeSinceStart,UserHeight,TrialNo,TrialID,ParticipantID,ExperimentSequence,Layout,Difficulty,TrialState,CameraPosition.x," +
                "CameraPosition.y,CameraPosition.z,CameraEulerAngles.x,CameraEulerAngles.y,CameraEulerAngles.z,MainControllerPosition.x,MainControllerPosition.y," +
                "MainControllerPosition.z,MainControllerEulerAngles.x,MainControllerEulerAngles.y,MainControllerEulerAngles.z";
            writer.WriteLine(logFileHeader);
            writer.Close();

            // head and hand data log
            string writerHeadFilePath = "Assets/ExperimentData/ExperimentLog/Participant " + ParticipantID + "/Participant_" + ParticipantID + "_HeadAndHand.csv";
            writer = new StreamWriter(writerHeadFilePath, false);
            writer.WriteLine("TimeSinceStart,TrialNo,TrialID,ParticipantID,ExperimentSequence,Layout,Difficulty,TrialState,CameraPosition.x," +
                "CameraPosition.y,CameraPosition.z,CameraEulerAngles.x,CameraEulerAngles.y,CameraEulerAngles.z,MainControllerPosition.x,MainControllerPosition.y," +
                "MainControllerPosition.z,MainControllerEulerAngles.x,MainControllerEulerAngles.y,MainControllerEulerAngles.z");
            writer.Close();

            // interaction log
            string writerInteractionFilePath = "Assets/ExperimentData/ExperimentLog/Participant " + ParticipantID + "/Participant_" + ParticipantID + "_Interaction.csv";
            writer = new StreamWriter(writerInteractionFilePath, false);
            writer.WriteLine("TimeSinceStart,TrialNo,TrialID,ParticipantID,Layout,Info,CardSeen,CardSelected,CardAnswered,CardPlayed");
            writer.Close();

            // Answers data log
            string writerAnswerFilePath = "Assets/ExperimentData/ExperimentLog/Participant " + ParticipantID + "/Participant_" + ParticipantID + "_Answers.csv";
            writer = new StreamWriter(writerAnswerFilePath, false);
            writer.WriteLine("ParticipantID,TrialNo,TrialID,Layout,Difficulty,AnswerAccuracy,Card1SeenTime,Card2SeenTime,Card3SeenTime,Card4SeenTime,Card5SeenTime," +
                "Card1SelectTime,Card2SelectTime,Card3SelectTime,Card4SelectTime,Card5SelectTime");
            writer.Close();
        }
        else
        {
            string lastFileName = "";

            string folderPath = "Assets/ExperimentData/ExperimentLog/Participant " + ParticipantID + "/";
            DirectoryInfo info = new DirectoryInfo(folderPath);
            FileInfo[] fileInfo = info.GetFiles();
            foreach (FileInfo file in fileInfo)
            {
                if (file.Name.Contains("Participant_" + ParticipantID + "_RawData.csv") && !file.Name.Contains("meta"))
                {
                    lastFileName = file.Name;
                }
            }
            if (lastFileName == "")
            {
                Debug.LogError("No previous file found!");
            }
            else
            {
                string writerFilePath = "Assets/ExperimentData/ExperimentLog/Participant " + ParticipantID + "/" + lastFileName;
                string lastLine = File.ReadAllLines(writerFilePath)[File.ReadAllLines(writerFilePath).Length - 1];
                float lastTime = float.Parse(lastLine.Split(',')[0]);
                float height = float.Parse(lastLine.Split(',')[1]);

                lastTimePast = lastTime;
                adjustedHeight = height;
            }
        }
    }

    private void Update()
    {
        if (SceneManager.GetActiveScene().name == "StartScene")
        {
            if (exploreActivated || learningActivated || distractorActivated || recallActivated || resultActivated)
            {
                // clear instruction text
                instruction.text = "";

                // clear card attributes
                foreach (GameObject go in cardLists)
                {
                    go.GetComponent<Card>().seen = false;
                    go.GetComponent<Card>().seenLogged = false;
                    go.GetComponent<Card>().selected = false;
                    go.GetComponent<Card>().selectLogged = false;
                }
                // flip cards to their back
                foreach (GameObject card in cardLists)
                {
                    if (IsCardFilled(card))
                        SetCardsColor(card.transform, Color.black);
                    card.transform.localEulerAngles = new Vector3(0, 180, 0);
                }
            }

            if (learningActivated)
            {
                if (!showPatternFlag)
                {
                    Cards.position = new Vector3(0, Camera.main.transform.position.y, 0);
                    adjustedHeight = Camera.main.transform.position.y - 0.75f;

                    // flip to the front
                    foreach (GameObject card in cardLists)
                    {
                        if (IsCardFilled(card))
                            SetCardsColor(card.transform, Color.white);
                    }
                    showPatternFlag = true;
                }

                CheckFilledScanned();
            }

            if (distractorActivated)
            {
                instruction.text = "3";
                //instruction.transform.parent.parent.position = new Vector3(0, CardGame.position.y + 0.6f, 1f);


            }

            if (recallActivated)
            {

            }

            if (nextActivated)
            {
                //instruction.text = "Note: when you see the image below, go back to the original position.";
                //instruction.text = "Now, please stand still and press the <color=green>Start</color> button to start the experiment.";
                //SceneManager.LoadScene("Experiment", LoadSceneMode.Single);
            }
        }
    }

    string GetDateTimeString()
    {
        return DateTime.Now.Month.ToString("D2") + DateTime.Now.Day.ToString("D2") + "-" + DateTime.Now.Hour.ToString("D2") + DateTime.Now.Minute.ToString("D2") + DateTime.Now.Second.ToString("D2");
    }

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    // check user viewport
    private void CheckFilledScanned()
    {
        foreach (GameObject go in cardLists)
        {
            if (IsCardFilled(go))
            {
                Vector3 wtvp = Camera.main.WorldToViewportPoint(go.transform.position);

                if (wtvp.x < 0.7f && wtvp.x > 0.3f && wtvp.y < 0.8f && wtvp.y > 0.2f && wtvp.z > 0f)
                {
                    if (go.transform.GetChild(0).GetComponent<Renderer>().isVisible)
                    {
                        go.GetComponent<Card>().seen = true;
                        if (!go.GetComponent<Card>().seenLogged)
                            go.GetComponent<Card>().seenLogged = true;
                    }
                }
            }
        }
    }

    // Check if card filled property is true
    private bool IsCardFilled(GameObject go)
    {
        if (go.GetComponent<Card>().filled)
            return true;
        return false;
    }

    // Check if card flipped property is true
    private bool IsCardRotating(GameObject go)
    {
        if (go.GetComponent<Card>().rotating)
            return true;
        return false;
    }

    // Check if card flipped property is true
    private bool IsCardFlipped(GameObject go)
    {
        if (go.GetComponent<Card>().flipped)
            return true;
        return false;
    }

    // rotate coroutine with animation
    private IEnumerator Rotate(Transform rotateObject, Vector3 angles, float duration)
    {
        if (rotateObject != null)
        {
            rotateObject.GetComponent<Card>().rotating = true;
            //rotateObject.GetComponent<VRTK_InteractableObject>().isUsable = false;
            Quaternion startRotation = rotateObject.rotation;
            Quaternion endRotation = Quaternion.Euler(angles) * startRotation;
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                rotateObject.rotation = Quaternion.Lerp(startRotation, endRotation, t / duration);
                yield return null;
            }
            rotateObject.rotation = endRotation;
            //rotateObject.GetComponent<VRTK_InteractableObject>().isUsable = true;

            rotateObject.GetComponent<Card>().rotating = false;
        }
    }

    // Set Card Color
    private void SetCardsColor(Transform t, Color color)
    {
        if (color == Color.white)
            t.GetChild(0).GetChild(0).GetChild(1).gameObject.SetActive(true);
        else
            t.GetChild(0).GetChild(0).GetChild(1).gameObject.SetActive(false);
    }
}
