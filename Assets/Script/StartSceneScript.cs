using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using Random = UnityEngine.Random;
using TMPro;
using Microsoft.MixedReality.Toolkit;

namespace SpatialMemoryTest
{
    public class StartSceneScript : MonoBehaviour
    {
        public static int ExperimentSequence;
        public static int ParticipantID;
        public static string CurrentDateTime;
        public static int PublicTrialNumber;
        public static float lastTimePast;

        [Header("Do Not Change")]
        public TextMeshPro instruction;
        public GameObject StartButton;
        public Transform DistractorTask;
        public Transform MemoryTask;

        [Header("Experiment Parameter")]
        public int ExperimentID;
        public int TrialNumber;
        public GameState gameState = GameState.NULL;

        private List<GameObject> cardLists;

        private bool exploreActivated = false;
        private bool learningActivated = false;
        private bool distractorActivated = false;
        private bool distractor2Activated = false;
        private bool recallActivated = false;
        private bool resultActivated = false;
        private bool nextActivated = false;
        private bool backActivated = false;

        private float adjustedHeight = 0;
        private bool firstTimeSetup = false;

        private int recallCardSelected = 0;
        private List<GameObject> userSelectedPatternCards;
        private int accurateNumber = 0;

        // Start is called before the first frame update
        void Start()
        {
            // initialise pattern task cards into list
            cardLists = new List<GameObject>{
                MemoryTask.GetChild(0).gameObject,
                MemoryTask.GetChild(1).gameObject,
                MemoryTask.GetChild(2).gameObject,
                MemoryTask.GetChild(3).gameObject
            };

            userSelectedPatternCards = new List<GameObject>();

            PublicTrialNumber = TrialNumber;

            if (ExperimentID > 0) // experiment stream
            {
                ParticipantID = ExperimentID;
                ExperimentSequence = GetExperimentSequence();
            }
            else // testing stream
            { 
                ExperimentSequence = 1;
            }

            // randomise distractor numbers
            for (int i = 0; i < DistractorTask.childCount; i++)
            {
                Vector3 temp = DistractorTask.GetChild(i).localPosition;
                int randomIndex = Random.Range(i, DistractorTask.childCount);
                DistractorTask.GetChild(i).localPosition = DistractorTask.GetChild(randomIndex).localPosition;
                DistractorTask.GetChild(randomIndex).localPosition = temp;
            }

            if (TrialNumber == 0) // new experiment
                LogDataHeader(true);
            else                  // existing experiment
                LogDataHeader(false);
        }

        private void Update()
        {
            if (SceneManager.GetActiveScene().name == "StartScene") // training scene
            {
                if(gameState == GameState.Learning)
                    CheckFilledScanned();

                #region reset everything
                if (backActivated) // reset all
                {
                    // clear instruction text
                    instruction.text = "";
                    gameState = GameState.NULL;
                    userSelectedPatternCards = new List<GameObject>();

                    // clear card attributes
                    foreach (GameObject go in cardLists)
                    {
                        go.GetComponent<Card>().seen = false;
                        go.GetComponent<Card>().selected = false;
                        go.GetComponent<Card>().rotating = false;
                        if(go.activeSelf)
                            go.GetComponent<Card>().ResetBorderColor();
                    }

                    foreach (Transform t in DistractorTask)
                        t.GetComponent<Card>().ResetBorderColor();

                    // flip cards to their back
                    foreach (GameObject card in cardLists)
                    {
                        SetCardsColor(card.transform, Color.black);
                        card.transform.localEulerAngles = Vector3.zero;
                    }
                    backActivated = false;
                }
                #endregion

                #region button actions
                if (exploreActivated)
                {
                    instruction.text = "This is the explore phase. Stand at the original position and " +
                        "press a button to go to the learning phase when you are ready";
 
                    exploreActivated = false;
                }

                if (learningActivated)
                {
                    instruction.text = "This is the learning phase. There are five white cards out of 36 cards. " +
                        "You need to remember the position for all the white cards in 15 seconds.";
                    gameState = GameState.Learning;

                    if (!firstTimeSetup)
                    {
                        adjustedHeight = Camera.main.transform.position.y - 0.75f;
                        firstTimeSetup = true;
                    }
                    SetupTaskPositionRotation(MemoryTask);

                    // flip to the front
                    foreach (GameObject card in cardLists)
                    {
                        if (IsCardFilled(card))
                        {
                            SetCardsColor(card.transform, Color.white);
                            card.GetComponent<HandInteractionTouchRotate_SpatialMemory>().SelfRotate();
                        }
                    }

                    learningActivated = false;
                }

                if (distractorActivated)
                {
                    if (!firstTimeSetup)
                    {
                        adjustedHeight = Camera.main.transform.position.y - 0.75f;
                        firstTimeSetup = true;
                    }
                    SetupTaskPositionRotation(DistractorTask);

                    instruction.text = "This is the distractor phase. You will play a number touching game in 15 seconds. There will be multiple tasks for you in this phase\n" +
                        "Press the Show Task button to see the task number.";
                    gameState = GameState.Distractor;

                    distractorActivated = false;
                }

                if (distractor2Activated) {
                    instruction.text = "Now you see a number, and you need to touch the same number in front of you in 5 seconds. " +
                        "In the real experiment, the number will directly show up on this board. " +
                        "Penalties will be given if no reaction or inaccurate touch.";

                    distractor2Activated = false;
                }


                if (recallActivated)
                {
                    instruction.text = "This is the recall phase. You need to select (touch the card) 5 cards as you remembered. " +
                        "There is no time limit for this phase. " +
                        "You cannot undo what you’ve selected.";

                    if (!firstTimeSetup)
                    {
                        adjustedHeight = Camera.main.transform.position.y - 0.75f;
                        firstTimeSetup = true;
                    }
                    SetupTaskPositionRotation(MemoryTask);
                    gameState = GameState.Recall;

                    recallActivated = false;
                }

                if (resultActivated)
                {
                    CheckResult();

                    instruction.text = "This is the result phase. You will see the correct answers and the following message during the experiment.\n\n" +
                        "Result: " + accurateNumber + " / " + 2;

                    // show correct cards and selected cards
                    foreach (GameObject card in cardLists)
                    {
                        SetCardsColor(card.transform, Color.black);

                        if (IsCardFilled(card))
                            SetCardsColor(card.transform, Color.white);
                    }

                    resultActivated = false;
                }


                if (nextActivated)
                {
                    instruction.text = "Now, please stand still and press the Start button to start the experiment.";
                }
                #endregion
            }
        }

        public bool RecallPhaseCardSelected(GameObject selectedCard)
        {
            if (selectedCard.name.Contains("Card") && userSelectedPatternCards.Count < 2 && !userSelectedPatternCards.Contains(selectedCard))
            {
                userSelectedPatternCards.Add(selectedCard);

                SetCardsColor(selectedCard.transform, Color.white);

                return true;
            }
            return false;
        }

        private bool CheckResult()
        {
            bool finalResult = true;
            int correctNum = 0;

            if (userSelectedPatternCards.Count != 2)
                finalResult = false;
            else
            {
                foreach (GameObject selectedCard in userSelectedPatternCards)
                {
                    if (!IsCardFilled(selectedCard))
                        finalResult = false;
                    else
                        correctNum++;
                }
            }

            accurateNumber = correctNum;

            return finalResult;
        }

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        #region check card propterty
        // check user viewport
        private void CheckFilledScanned()
        {
            foreach (GameObject go in cardLists)
            {
                if (IsCardFilled(go))
                {
                    if (CoreServices.InputSystem.GazeProvider.GazeTarget &&
                        go == CoreServices.InputSystem.GazeProvider.GazeTarget) // need testing
                    {
                        if (!go.GetComponent<Card>().seenLogged)
                        {
                            go.GetComponent<Card>().seen = true;
                        }
                    }
                    //Vector3 wtvp = Camera.main.WorldToViewportPoint(go.transform.position);

                    //if (wtvp.x < 0.7f && wtvp.x > 0.3f && wtvp.y < 0.8f && wtvp.y > 0.2f && wtvp.z > 0f)
                    //{
                    //    if (go.transform.GetChild(0).GetComponent<Renderer>().isVisible)
                    //        go.GetComponent<Card>().seen = true;
                    //}
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
        #endregion

        #region setters
        private void SetupTaskPositionRotation(Transform t)
        {
            t.position = Camera.main.transform.position + Camera.main.transform.TransformDirection(Vector3.forward) * 0.5f + Vector3.down * 0.5f;

            t.LookAt(Camera.main.transform.position);
            t.localEulerAngles = new Vector3(0, t.localEulerAngles.y + 180, 0);
        }

        // Set Card Color
        private void SetCardsColor(Transform t, Color color)
        {
            if (color == Color.white)
                t.GetChild(0).GetChild(0).GetChild(1).gameObject.SetActive(true);
            else
                t.GetChild(0).GetChild(0).GetChild(1).gameObject.SetActive(false);
        }
        #endregion

        #region getters
        private int GetExperimentSequence()
        {
            int experimentSequence = 1;

            switch (ExperimentID % 4)
            {
                case 1:
                    experimentSequence = 1;
                    break;
                case 2:
                    experimentSequence = 2;
                    break;
                case 3:
                    experimentSequence = 3;
                    break;
                case 0:
                    experimentSequence = 4;
                    break;
                default:
                    break;
            }

            return experimentSequence;
        }
        #endregion

        #region log related
        private void LogDataHeader(bool newLog)
        {
            if (newLog)
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

        string GetDateTimeString()
        {
            return DateTime.Now.Month.ToString("D2") + DateTime.Now.Day.ToString("D2") + "-" + DateTime.Now.Hour.ToString("D2") + DateTime.Now.Minute.ToString("D2") + DateTime.Now.Second.ToString("D2");
        }
        #endregion

        #region button groups
        public void ExploreButtonPressed()
        {
            exploreActivated = true;
        }

        public void LearningButtonPressed()
        {
            learningActivated = true;
        }

        public void DistractorButtonPressed()
        {
            distractorActivated = true;
        }

        public void Distractor2ButtonPressed()
        {
            distractor2Activated = true;
        }

        public void RecallButtonPressed()
        {
            recallActivated = true;
        }

        public void ResultButtonPressed()
        {
            resultActivated = true;
        }

        public void NextButtonPressed()
        {
            nextActivated = true;
        }

        public void BackButtonPressed()
        {
            backActivated = true;
            firstTimeSetup = false;
            if (nextActivated)
            {
                StartButton.SetActive(false);
                nextActivated = false;
            }
        }

        public void StartButtonPressed()
        {
            SceneManager.LoadScene("Experiment", LoadSceneMode.Single);
        }
        #endregion
    }
}