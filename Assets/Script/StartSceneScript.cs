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
        #region Global Static Variables
        public static int ExperimentSequence;
        public static int ParticipantID;
        public static string CurrentDateTime;
        public static int PublicTrialNumber;
        public static float lastTimePast;
        #endregion

        #region Data Logger
        public static RawLogger RawLogger;
        public static InteractionLogger InteractionLogger;
        public static TaskLogger TaskLogger;
        public static TrialCardLogger TrialCardLogger;
        public static AnswerCardLogger AnswerCardLogger;
        #endregion

        [Header("Reference")]
        public TextMeshPro instruction;
        public TextMeshPro SetupText;
        public GameObject StartButton;
        public Transform DistractorTask;
        public Transform MemoryTask;
        public GameObject HandMenu;
        public GameObject SetupMenu;

        [Header("Experiment Parameter")]
        public GameState gameState = GameState.NULL;

        #region Setup Experiment ID
        private int ExperimentID = 0;
        private int TrialNumber = 0;
        public static string participantIDText = "";
        public static string trialIDText = "";
        private SetupParameter sp;
        private bool experimentNumberConfirmed = false;
        private bool trialNumberConfirmed = false;
        private bool finalConfirmed = false;
        #endregion

        #region Game Variables
        private List<GameObject> cardLists;
        private List<GameObject> userSelectedPatternCards;
        private int accurateNumber = 0;
        private float adjustedHeight = 0;
        private bool firstTimeSetup = false;
        private bool LogHeaderFinished = false;
        #endregion

        #region Button Booleans
        private bool exploreActivated = false;
        private bool learningActivated = false;
        private bool distractorActivated = false;
        private bool distractor2Activated = false;
        private bool recallActivated = false;
        private bool resultActivated = false;
        private bool nextActivated = false;
        private bool backActivated = false;
        #endregion

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

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

            // randomise distractor numbers
            for (int i = 0; i < DistractorTask.childCount; i++)
            {
                Vector3 temp = DistractorTask.GetChild(i).localPosition;
                int randomIndex = Random.Range(i, DistractorTask.childCount);
                DistractorTask.GetChild(i).localPosition = DistractorTask.GetChild(randomIndex).localPosition;
                DistractorTask.GetChild(randomIndex).localPosition = temp;
            }

            sp = SetupParameter.ExperimentNumber;
            RawLogger = GetComponent<RawLogger>();
            InteractionLogger = GetComponent<InteractionLogger>();
            TaskLogger = GetComponent<TaskLogger>();
            TrialCardLogger = GetComponent<TrialCardLogger>();
            AnswerCardLogger = GetComponent<AnswerCardLogger>();
        }

        private void Update()
        {
            if (SceneManager.GetActiveScene().name == "StartScene") {
                // setup experiment/participant ID
                if (!finalConfirmed)
                {
                    if (!experimentNumberConfirmed)
                        participantIDText = "Experiment Number: " + ExperimentID + ".\n";
                    else
                        participantIDText = "Experiment Number: " + ExperimentID + " (Comfirmed).\n";

                    if (!trialNumberConfirmed)
                        trialIDText = "Trial Number: " + TrialNumber + ".\n";
                    else
                        trialIDText = "Trial Number: " + TrialNumber + " (Comfirmed).\n";

                    if (experimentNumberConfirmed && sp == SetupParameter.ExperimentNumber)
                        sp = SetupParameter.TrialNumber;

                    SetupText.text = participantIDText + trialIDText;
                }else // All Confirmed
                {
                    if (ExperimentID == 0)
                    { // testing stream
                        ParticipantID = 0;
                        ExperimentSequence = 1;
                    }
                    else
                    {
                        ParticipantID = ExperimentID;
                        ExperimentSequence = GetExperimentSequence();
                    }

                    PublicTrialNumber = TrialNumber;

                    if (!LogHeaderFinished)
                    {
                        LogHeaderFinished = true;
                        LogDataHeader();
                    }

                    if (gameState == GameState.Learning)
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
                            if (go.activeSelf)
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

                    if (distractor2Activated)
                    {
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
        }

        #region Recall Phase
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
        #endregion

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
            t.position = Camera.main.transform.position + Camera.main.transform.TransformDirection(Vector3.forward) * 1f + Vector3.down * 0.5f;

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
        private void LogDataHeader()
        {
            // Raw data log
            string rawFileName = "Participant_" + ParticipantID + "_Raw";
            RawLogger.StartNewCSV(rawFileName);

            // interaction log
            string interactionFileName = "Participant_" + ParticipantID + "_Interaction";
            InteractionLogger.StartNewCSV(interactionFileName);

            // Task log
            string taskFileName = "Participant_" + ParticipantID + "_Task";
            TaskLogger.StartNewCSV(taskFileName);

            // Trial Card Log
            string trialCardFileName = "Participant_" + ParticipantID + "_trialCards";
            TrialCardLogger.StartNewCSV(trialCardFileName);

            // Answer Card Log
            string answerCardFileName = "Participant_" + ParticipantID + "_answerCards";
            AnswerCardLogger.StartNewCSV(answerCardFileName);
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

        public void AddButton()
        {
            if (sp == SetupParameter.ExperimentNumber)
                ExperimentID++;
            else if (sp == SetupParameter.TrialNumber)
                TrialNumber++;
        }

        public void MinuesButton()
        {
            if (sp == SetupParameter.ExperimentNumber)
                ExperimentID--;
            else if (sp == SetupParameter.TrialNumber)
                TrialNumber--;
        }

        public void ConfirmButton()
        {
            if (!experimentNumberConfirmed)
                experimentNumberConfirmed = true;
            else if (!trialNumberConfirmed)
                trialNumberConfirmed = true;
            else {
                finalConfirmed = true;
                SetupMenu.SetActive(false);
                HandMenu.SetActive(true);
            }
        }
        #endregion
    }
}