using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.IO;
using Microsoft.MixedReality.Toolkit;

namespace SpatialMemoryTest
{
    public class ExperimentManager : MonoBehaviour
    {
        public static int trialNo;

        [Header("Resource")]
        public GameObject CardPrefab;
        public AudioClip TimesUp;

        [Header("Reference")]
        public TextMeshPro Instruction;
        public Transform DistractorTask;
        public TextMeshPro DistractorTaskInstruction;
        public GameObject startButton;
        public GameObject resultButton;
        public GameObject nextButton;
        public GameObject wellDoneButton;
        public GameObject breakButton;

        [Header("Task File")]
        public TextAsset Patterns5Flat;
        public TextAsset Patterns5Circular;
        public TextAsset GameTask;

        [Header("Pre-study Variable")]
        public float hDelta;
        public float vDelta;
        public float cardSize;
        public float memoryTime;
        public float distractorTime;
        public float eachDistractorReactTime;
        public int numberOfRows;
        public int numberOfColumns;

        [Header("In-study Variable")]
        public Layout layout;
        public PhysicalEnvironmentDependence PEDependence;
        public GameState gameState;

        #region Game setup private variables
        private int experimentSequence = 1;
        private float adjustedHeight = 0;
        private int difficultyLevel = 5;
        private int maxTrialNo = 24;
        #endregion

        #region String variables
        private char lineSeperater = '\n'; // It defines line seperate character
        private char fieldSeperator = ','; // It defines field seperate chracter
        #endregion

        #region Trial variables
        private List<GameObject> patternCards;
        private List<GameObject> distractorCards;
        private List<GameObject> userSelectedPatternCards;
        private List<GameObject> touchingCards;
        private GameObject touchingCard;
        private bool finishTouching = false;
        private float LocalMemoryTime;
        private float localDistractorTime;
        private float localEachDistractorReactTime;
        private List<string> FlatTaskList;
        private List<string> CircularTaskList;
        private int[] currentPattern;
        private int currentGameNumber;
        private bool allSeen = false;        
        private bool allSelected = false;
        private bool learningTimesUp = false;
        private bool gridCalibrate = false;
        #endregion

        #region Log use variables
        private float scanTime = 0;
        private List<float> scanTimeLog;
        private float selectTime = 0;
        private List<float> selectTimeLog;
        private int accurateNumber = 0;
        private RawLogger rawLogger;
        private InteractionLogger interactionLogger;
        private TaskLogger taskLogger;
        private TrialCardLogger trialCardLogger;
        private AnswerCardLogger answerCardLogger;
        #endregion

        // Start is called before the first frame update
        private void Start()
        {
            #region initialise lists
            // initialise pattern cards and selected pattern cards
            patternCards = new List<GameObject>();
            userSelectedPatternCards = new List<GameObject>();
            distractorCards = new List<GameObject>();
            touchingCards = new List<GameObject>();

            // initialise task list
            FlatTaskList = new List<string>();
            CircularTaskList = new List<string>();

            // initialise time log
            scanTimeLog = new List<float>();
            selectTimeLog = new List<float>();
            #endregion

            // load pattern task
            LoadPatterns();

            // add distractor task cards into list
            foreach (Transform t in DistractorTask)
                distractorCards.Add(t.gameObject);

            // setup adjusted height
            adjustedHeight = Camera.main.transform.position.y - 0.75f;

            if (GameObject.Find("MainExperimentManager") != null)
            {
                // setup experimentSequence
                experimentSequence = StartSceneScript.ExperimentSequence;

                // setup trail Number
                if (StartSceneScript.PublicTrialNumber != 0)
                    trialNo = StartSceneScript.PublicTrialNumber;
                else
                    trialNo = 1;

                // setup writer stream
                SetupLoggingSystem();
            }
            
            // setup timers
            LocalMemoryTime = memoryTime;
            localDistractorTime = distractorTime;
            localEachDistractorReactTime = eachDistractorReactTime;

            // setup experiment
            PrepareExperiment();
        }

        // Update is called once per frame
        private void Update()
        {

            if (gameState == GameState.Learning) {
                if (!gridCalibrate) {
                    gridCalibrate = true;
                    SetGridPosition(layout);
                }
                LearningPhaseCheck();
            }


            if (gameState == GameState.Distractor)
                DistractorPhaseCheck();

            if (gameState == GameState.Recall)
                RecallPhaseCheck();

            //WritingToLog();

            // testing
            if (Input.GetKeyDown("n"))
                NextGameState();
        }

        #region Prepare phase
        public void PrepareExperiment()
        {
            // enable start button
            startButton.gameObject.SetActive(true);

            gameState = GameState.Prepare;
            LocalMemoryTime = memoryTime;
            localDistractorTime = distractorTime;
            localEachDistractorReactTime = eachDistractorReactTime;

            // change trial conditions based on trial number
            layout = GetCurrentCardsLayout();
            PEDependence = GetCurrentPEDependence();
            
            if (layout != Layout.NULL && PEDependence != PhysicalEnvironmentDependence.NULL)
            {
                WriteInteractionToLog("Prepare Phase");

                if (GetTrialID() == "Training")
                    Instruction.text = "Training Task.\n\n Press the Start button when you are ready.";
                else
                    Instruction.text = "Experiment Task: " + GetTrialID() + " / 16.\n\n Press the Start button when you are ready.";

                if (patternCards != null)
                {
                    foreach (GameObject go in patternCards)
                        Destroy(go);
                    patternCards.Clear();

                    foreach (GameObject go in userSelectedPatternCards)
                        Destroy(go);
                    userSelectedPatternCards.Clear();
                }

                allSeen = false;
                allSelected = false;

                scanTime = 0f;
                selectTime = 0f;

                scanTimeLog.Clear();
                selectTimeLog.Clear();

                patternCards = GenerateCards();

                SetCardsPositions(patternCards, layout);

                foreach (GameObject card in patternCards)
                    card.SetActive(false);
            }
            else
                Debug.LogError("layout error! Cannot get current layout.");
        }
        #endregion

        #region Prepare Phase Functions
        // Generate patternCards
        private List<GameObject> GenerateCards()
        {
            List<GameObject> patternCards = new List<GameObject>();
            int k = 0;

            for (int i = 0; i < numberOfRows; i++)
            {
                for (int j = 0; j < numberOfColumns; j++)
                {
                    // calculate index number
                    int index = i * numberOfColumns + j;

                    // generate card game object
                    string name = "Card" + index;
                    GameObject card = Instantiate(CardPrefab, new Vector3(0, 0, 0), Quaternion.identity);
                    card.name = name;
                    card.transform.parent = transform;
                    card.transform.localScale = new Vector3(cardSize, cardSize, 1);

                    // assign position
                    card.transform.localPosition = SetCardPosition(index, i, j);

                    // assign orientation
                    card.transform.localEulerAngles = new Vector3(0, card.transform.localEulerAngles.y, 0);
                    if (layout == Layout.Wraparound)
                    {
                        GameObject center = new GameObject();
                        center.transform.SetParent(transform);
                        center.transform.localPosition = card.transform.localPosition;
                        center.transform.localPosition = new Vector3(0, center.transform.localPosition.y, 0);

                        card.transform.LookAt(center.transform.position);

                        card.transform.localEulerAngles += Vector3.up * 180;
                        Destroy(center);
                    }
                    patternCards.Add(card);

                    k++;
                }
            }

            currentPattern = GetCurrentPattern();

            if (currentPattern != null)
            {
                for (int i = 0; i < currentPattern.Length; i++)
                {
                    patternCards[currentPattern[i]].GetComponent<Card>().filled = true;
                }
            }
            else
                Debug.LogError("Pattern Used Up!!");

            return patternCards;
        }

        // Set patternCards Positions based on current layout
        private void SetCardsPositions(List<GameObject> localCards, Layout localLayout)
        {
            for (int i = 0; i < numberOfRows; i++)
            {
                for (int j = 0; j < numberOfColumns; j++)
                {
                    int index = i * numberOfColumns + j;
                    localCards[index].transform.localPosition = SetCardPosition(index, i, j);

                    localCards[index].transform.localEulerAngles = new Vector3(0, localCards[index].transform.localEulerAngles.y, 0);

                    if (localLayout == Layout.Flat)
                        localCards[index].transform.localEulerAngles = new Vector3(0, 0, 0);
                    else if (localLayout == Layout.Wraparound)// FULL CIRCLE
                    {
                        // change orientation
                        GameObject center = new GameObject();
                        center.transform.SetParent(this.transform);
                        center.transform.localPosition = localCards[index].transform.localPosition;
                        center.transform.localPosition = new Vector3(0, center.transform.localPosition.y, 0);

                        localCards[index].transform.LookAt(center.transform.position);

                        localCards[index].transform.localEulerAngles += Vector3.up * 180;
                        Destroy(center);
                    }
                }
            }
        }

        private void SetGridPosition(Layout localLayout) {
            float positionY = Camera.main.transform.position.y - 0.5f;
            switch (localLayout)
            {
                case Layout.Flat:

                    transform.position = new Vector3(0, positionY, 1);
                    transform.LookAt(Camera.main.transform.position);
                    transform.localEulerAngles = new Vector3(0, transform.localEulerAngles.y + 180, 0);

                    DistractorTask.position = new Vector3(0, positionY, 0.5f);
                    DistractorTask.LookAt(Camera.main.transform.position);
                    DistractorTask.localEulerAngles = new Vector3(0, transform.localEulerAngles.y, 0);
                    break;
                case Layout.Wraparound:
                    transform.position = new Vector3(0, positionY, 0);
                    transform.LookAt(Camera.main.transform.position);
                    transform.localEulerAngles = new Vector3(0, transform.localEulerAngles.y + 180, 0);

                    DistractorTask.position = new Vector3(0, positionY, 0.5f);
                    DistractorTask.LookAt(Camera.main.transform.position);
                    DistractorTask.localEulerAngles = new Vector3(0, transform.localEulerAngles.y, 0);
                    break;
                default:
                    break;
            }
        }

        // Set Card Position
        private Vector3 SetCardPosition(int index, int row, int col)
        {
            float xValue = 0;
            float yValue = 0;
            float zValue = 0;

            switch (layout)
            {
                case Layout.Flat:
                    xValue = (index - (row * numberOfColumns) - (numberOfColumns / 2.0f - 0.5f)) * hDelta;
                    yValue = (numberOfRows - (row + 1)) * vDelta;
                    zValue = 0;
                    break;
                case Layout.Wraparound:
                    xValue = -Mathf.Cos((index - (row * numberOfColumns)) * Mathf.PI / (numberOfColumns / 2.0f)) * ((numberOfColumns - 1) * hDelta / (2.0f * Mathf.PI));
                    yValue = (numberOfRows - (row + 1)) * vDelta;
                    zValue = Mathf.Sin((index - (row * numberOfColumns)) * Mathf.PI / (numberOfColumns / 2.0f)) * ((numberOfColumns - 1) * hDelta / (2.0f * Mathf.PI));
                    break;
                default:
                    break;
            }

            return new Vector3(xValue, yValue, zValue);
        }
        #endregion

        #region Learning phase
        // Show pattern (after clicking Start button)
        public void InitiateLearningPhase()
        {
            Instruction.text = "You now have 15 seconds to remember the positions of 5 white cards.\n" +
                "You will hear a timer sound when you only have 3 seconds left.";
            
            WriteInteractionToLog("Learning Phase");
            
            foreach (GameObject card in patternCards)
            {
                card.SetActive(true);
                if (IsCardFilled(card))
                    SetCardsColor(card.transform, Color.white);
                //card.GetComponent<HandInteractionTouchRotate_SpatialMemory>().SelfRotate();
            }

            gameState = GameState.Learning;
            
            // start timer
            StartCoroutine(LearningTimer(LocalMemoryTime));
        }
        #endregion

        #region Learning Phase Functions
        IEnumerator LearningTimer(float timer)
        {
            while (true)
            {
                yield return new WaitForSeconds(timer - 3); //wait timer seconds
                AudioSource.PlayClipAtPoint(TimesUp, transform.position);
                yield return new WaitForSeconds(3); //wait timer seconds
                learningTimesUp = true;
                yield break;
            }
        }

        // check all filled cards are scanned
        private void CheckCardsScanned()
        {
            allSeen = true;
            foreach (GameObject go in patternCards)
            {
                if (IsCardFilled(go))
                {
                    if (CoreServices.InputSystem.GazeProvider.GazeTarget && 
                        go == CoreServices.InputSystem.GazeProvider.GazeTarget) // need testing
                    {
                        if (!go.GetComponent<Card>().seenLogged)
                        {
                            go.GetComponent<Card>().seen = true;
                            WriteInteractionToLog(go.name + " seen");
                            scanTimeLog.Add(scanTime);
                            go.GetComponent<Card>().seenLogged = true;
                        }
                    }

                    //Vector3 wtvp = Camera.main.WorldToViewportPoint(go.transform.position);

                    //if (wtvp.x < 0.7f && wtvp.x > 0.3f && wtvp.y < 0.8f && wtvp.y > 0.2f && wtvp.z > 0f)
                    //{
                    //    if (go.transform.GetChild(0).GetComponent<Renderer>().isVisible)
                    //    {
                    //        if (!go.GetComponent<Card>().seenLogged)
                    //        {
                    //            go.GetComponent<Card>().seen = true;
                    //            WriteInteractionToLog(go.name + " seen");
                    //            scanTimeLog.Add(scanTime);
                    //            go.GetComponent<Card>().seenLogged = true;
                    //        }
                    //    }
                    //}

                    if (!go.GetComponent<Card>().seen)
                        allSeen = false;
                }
            }
        }

        // check user selected all filled patternCards
        private void CheckCardsSelected()
        {
            allSelected = true;

            foreach (GameObject go in patternCards)
            {
                if (IsCardFilled(go))
                {
                    if (!go.GetComponent<Card>().selected)
                    {
                        allSelected = false;
                    }
                }
            }
        }

        private void LearningPhaseCheck()
        {
            CheckCardsScanned();
            CheckCardsSelected();

            if (learningTimesUp && allSeen && allSelected)
                InitiateDistractorPhase();
        }
        #endregion

        #region Distractor Phase
        private void InitiateDistractorPhase() {
            HidePattern();
            DistractorTask.gameObject.SetActive(true);

            gameState = GameState.Distractor;
            WriteInteractionToLog("Distractor");

            GetNewDistractorTask();
        }
        #endregion

        #region Distractor Phase Functions
        // Hide pattern 
        private void HidePattern()
        {
            // hide patternCards
            foreach (GameObject card in patternCards)
            {
                if (IsCardFilled(card))
                {
                    SetCardsColor(card.transform, Color.black);
                    card.transform.localEulerAngles = Vector3.zero;
                    card.GetComponent<Card>().selected = false;
                    card.GetComponent<Card>().rotating = false;
                    card.GetComponent<Card>().seen = false;
                    card.GetComponent<Card>().ResetBorderColor();
                }
                card.SetActive(false);
            }
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
            WriteInteractionToLog("Distractor Number: " + task);
        }

        private void HideDistractorCards()
        {
            foreach (GameObject card in distractorCards)
                card.GetComponent<Card>().selected = false; // clear card property
            DistractorTaskInstruction.text = ""; // clear instruction text
            DistractorTaskInstruction.color = Color.white;
            DistractorTask.gameObject.SetActive(false); // hide distractor task
        }

        private void DistractorPhaseCheck() {
            if (localDistractorTime >= 0) {
                localDistractorTime -= Time.deltaTime;

                if (localEachDistractorReactTime <= 0)
                {
                    DistractorTaskInstruction.color = Color.red;
                    localDistractorTime += eachDistractorReactTime;
                    localEachDistractorReactTime = eachDistractorReactTime;

                    GetNewDistractorTask();
                }
                else {
                    localEachDistractorReactTime -= Time.deltaTime;

                    Instruction.text = "Select the number below!\nTimes remaining: " + localDistractorTime.ToString("0.0");

                    if (finishTouching) {
                        finishTouching = false;

                        if (touchingCard.name != currentGameNumber.ToString())
                        {
                            localDistractorTime += eachDistractorReactTime;
                            GetNewDistractorTask();
                            // play sound or show text
                            Instruction.color = Color.red;
                        }
                        else if(touchingCard.name == currentGameNumber.ToString())
                        {
                            Instruction.color = Color.white;
                            DistractorTaskInstruction.color = Color.white;
                            localEachDistractorReactTime = eachDistractorReactTime;
                            GetNewDistractorTask();
                        }
                    }
                }
            }
            else {
                HideDistractorCards();
                InitiateRecallPhase();
            }         
        }
        #endregion

        #region Recall Phase
        private void InitiateRecallPhase() {
            WriteInteractionToLog("Recall Phase");
            gameState = GameState.Recall;


            // show patternCards
            foreach (GameObject card in patternCards)
                card.SetActive(true);
        }
        #endregion

        #region Recall Phase Functions 
        public bool RecallPhaseCardSelected(GameObject selectedCard) {
            if (selectedCard.name.Contains("Card") && userSelectedPatternCards.Count < difficultyLevel && !userSelectedPatternCards.Contains(selectedCard)) {
                WriteInteractionToLog(selectedCard.name + " answered");
                userSelectedPatternCards.Add(selectedCard);

                SetCardsColor(selectedCard.transform, Color.white);

                return true;
            }
            return false;
        }

        private void RecallPhaseCheck()
        {
            if (userSelectedPatternCards.Count == 0 || userSelectedPatternCards.Count == 1)
                Instruction.text = "You have selected " + userSelectedPatternCards.Count + " card out of " + difficultyLevel + " cards.";
            else if (userSelectedPatternCards.Count == difficultyLevel)
            {
                resultButton.SetActive(true);
                Instruction.text = "You have selected all cards. Please press the Check Result button on your hand menu to see the result.";
            }
            else
                Instruction.text = "You have selected " + userSelectedPatternCards.Count + " cards out of " + difficultyLevel + "cards.";
        }
        #endregion

        #region Result Phase
        public void InitiateResultPhase()
        {
            gameState = GameState.Result;

            WriteInteractionToLog("Result");
            interactionLogger.FlushData();

            CheckResult();
            Instruction.text = "Result: " + accurateNumber + " / " + difficultyLevel;

            resultButton.SetActive(false);
            breakButton.SetActive(false);
            wellDoneButton.SetActive(false);
            nextButton.SetActive(false);
            // button activation based on cases
            if (trialNo == 6 || trialNo == 12 || trialNo == 18) { // break button activated
                breakButton.SetActive(true);
            }
            else {
                if (difficultyLevel - accurateNumber <= 2) // well done button activated
                {
                    wellDoneButton.SetActive(true);
                }
                else // next button activated
                {
                    nextButton.SetActive(true);
                }
            }

            // show correct cards and selected cards
            foreach (GameObject card in patternCards) 
            {
                SetCardsColor(card.transform, Color.black);
                card.transform.localEulerAngles = Vector3.zero;

                if(IsCardSelected(card))
                    SetCardsColor(card.transform, Color.white);

                if (IsCardFilled(card))
                    SetCardsColor(card.transform, Color.white);
            }

            // increase trial No
            if (GetTrialID() != "Training")
            {
                // Write patternCards log for accuracy
                WriteCardsLog();
                // Write to Log
                WriteAnswerToLog();
            }
            trialNo++;
            gridCalibrate = false;

            if (trialNo > maxTrialNo)
                CloseAllWritersAndQuit();
        }
        #endregion

        #region Result Phase Functions
        // check the result
        private bool CheckResult()
        {
            bool finalResult = true;
            int correctNum = 0;

            if (userSelectedPatternCards.Count != difficultyLevel)
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

            string resultStr = (accurateNumber == difficultyLevel ? "Correct" : "Wrong");
            Debug.Log(resultStr + "! " + accurateNumber + "/" + difficultyLevel);

            return finalResult;
        }
        #endregion

        #region Break Phase
        public void InitiateBreakPhase() {
            gameState = GameState.Break;
            Instruction.text = "Please take off your headset and have a break. During the break, you will be asked to fill out a questionnaire." +
                "After the break, press the I'm Ready button to continue.";
        }
        #endregion

        #region ##################################################
        #endregion

        #region Task Related
        private void LoadPatterns()
        {
            string[] lines = new string[10];

            // fixed pattern 5 flat
            lines = new string[20];
            lines = Patterns5Flat.text.Split(lineSeperater);
            FlatTaskList.AddRange(lines);

            // fixed pattern 5 flat
            lines = new string[20];
            lines = Patterns5Circular.text.Split(lineSeperater);
            CircularTaskList.AddRange(lines);
        }

        public int RandomNumber(int min, int max)
        {
            return Random.Range(min, max);
        }
        #endregion

        #region Log System
        private void SetupLoggingSystem() {
            //rawLogger = StartSceneScript.RawLogger;
            interactionLogger = StartSceneScript.InteractionLogger;
            taskLogger = StartSceneScript.TaskLogger;
            trialCardLogger = StartSceneScript.TrialCardLogger;
            answerCardLogger = StartSceneScript.AnswerCardLogger;

        }

        // write to log file
        private void WritingToLog()
        {
            if (rawLogger != null && Camera.main != null)
            {
                rawLogger.AddRow(GetFixedTime() + "," + adjustedHeight + "," + GetTrialNumber() + "," + GetTrialID() + "," + StartSceneScript.ParticipantID + "," + StartSceneScript.ExperimentSequence + "," +
                    GetLayout() + "," + GetPhysicalDependence() + "," + GetGameState() + "," + VectorToString(Camera.main.transform.position) + "," + VectorToString(Camera.main.transform.eulerAngles));
                rawLogger.FlushData();
            }
        }

        private void WriteAnswerToLog()
        {
            if (taskLogger != null && userSelectedPatternCards.Count != 0)
            {
                taskLogger.AddRow(StartSceneScript.ParticipantID + "," + GetTrialNumber() + "," + GetTrialID() + "," + GetLayout() + "," +
                    GetPhysicalDependence() + "," + GetAccuracy() + "," + GetSeenTime() + "," + GetSelectTime());
                taskLogger.FlushData();
            }
        }

        public void WriteInteractionToLog(string info)
        {
            if (interactionLogger != null)
            {
                if (info.Contains("seen"))
                    interactionLogger.AddRow(GetFixedTime() + "," + GetTrialNumber() + "," + GetTrialID() + "," +
                        StartSceneScript.ParticipantID + "," + GetLayout() + "," + "Card," + info.Split(' ')[0].Remove(0, 4) + ",,,");
                else if (info.Contains("selected"))
                    interactionLogger.AddRow(GetFixedTime() + "," + GetTrialNumber() + "," + GetTrialID() + "," +
                       StartSceneScript.ParticipantID + "," + GetLayout() + "," + "Card,," + info.Split(' ')[0].Remove(0, 4) + ",,");
                else if (info.Contains("answered"))
                    interactionLogger.AddRow(GetFixedTime() + "," + GetTrialNumber() + "," + GetTrialID() + "," +
                       StartSceneScript.ParticipantID + "," + GetLayout() + "," + "Card,,," + info.Split(' ')[0].Remove(0, 4) + ",");
                else if (info.Contains("played"))
                    interactionLogger.AddRow(GetFixedTime() + "," + GetTrialNumber() + "," + GetTrialID() + "," +
                       StartSceneScript.ParticipantID + "," + GetLayout() + "," + "DistractorTask,,,," + info.Split(' ')[0]);
                else
                    interactionLogger.AddRow(GetFixedTime() + "," + GetTrialNumber() + "," + GetTrialID() + "," + StartSceneScript.ParticipantID + "," + GetLayout() + "," + info + ",,,");
            }
        }

        private void WriteCardsLog()
        {
            if (trialCardLogger != null && userSelectedPatternCards.Count != 0)
            {
                string final = "";

                foreach (GameObject card in userSelectedPatternCards)
                {
                    final += card.name.Split(' ')[0].Remove(0, 4) + ",";
                }

                final.Remove(final.Length - 1);

                trialCardLogger.AddRow(final);
                trialCardLogger.FlushData();
            }

            if (answerCardLogger != null)
            {
                string final = "";

                foreach (int cardIndex in currentPattern)
                {
                    int cardtmp = cardIndex;
                    final += cardtmp + ",";
                }

                final.Remove(final.Length - 1);

                answerCardLogger.AddRow(final);
                answerCardLogger.FlushData();
            }
        }

        private void CloseAllWritersAndQuit() {
            QuitGame();
        }
        #endregion

        #region Set functions
        // Set Card Color
        private void SetCardsColor(Transform t, Color color)
        {
            if (color == Color.white)
                t.GetChild(0).GetChild(0).GetChild(1).gameObject.SetActive(true);
            else
                t.GetChild(0).GetChild(0).GetChild(1).gameObject.SetActive(false);
        }

        private void NextGameState() {
            if (gameState == GameState.Prepare)
                InitiateLearningPhase();
            else if (gameState == GameState.Learning)
                InitiateDistractorPhase();
            else if (gameState == GameState.Distractor) {
                HideDistractorCards();
                InitiateRecallPhase();
            }
            else if (gameState == GameState.Recall)
                InitiateResultPhase();
            else if (gameState == GameState.Result)
                InitiateBreakPhase();
            else if (gameState == GameState.Break)
                PrepareExperiment();
        }

        public void RecordTouchingCards(GameObject card) {
            if (!touchingCards.Contains(card))
                touchingCards.Add(card);
        }

        public void RemoveTouchingCardFromList(GameObject card) {
            if (touchingCards.Contains(card))
                touchingCards.Remove(card);
            else
                Debug.Log("removing touching card wrong! " + card.name);
        }

        public void RecordTouchingCard(GameObject card) {
            finishTouching = true;
            touchingCard = card;
        }
        #endregion

        #region Get functions
        // Get current patternCards layouts based on sequence
        private Layout GetCurrentCardsLayout()
        {
            switch (experimentSequence)
            {
                case 0:
                    if ((trialNo <= 6 && trialNo >= 1) || (trialNo <= 18 && trialNo >= 13))
                        return Layout.Flat;
                    else
                        return Layout.Wraparound;
                case 1:
                    if ((trialNo <= 6 && trialNo >= 1) || (trialNo <= 18 && trialNo >= 13))
                        return Layout.Flat;
                    else
                        return Layout.Wraparound;
                case 2:
                    if (trialNo <= 24 && trialNo >= 13)
                        return Layout.Flat;
                    else
                        return Layout.Wraparound;
                case 3:
                    if (trialNo <= 12 && trialNo >= 1)
                        return Layout.Flat;
                    else
                        return Layout.Wraparound;
                case 4:
                    if ((trialNo <= 12 && trialNo >= 7) || (trialNo <= 24 && trialNo >= 19))
                        return Layout.Flat;
                    else
                        return Layout.Wraparound;
                default:
                    return Layout.NULL;
            }
        }

        // Get current physical environment dependence based on sequence
        private PhysicalEnvironmentDependence GetCurrentPEDependence()
        {
            switch (experimentSequence)
            {
                case 0:
                    if (trialNo <= 12 && trialNo >= 1)
                        return PhysicalEnvironmentDependence.Low;
                    else
                        return PhysicalEnvironmentDependence.High;
                case 1:
                    if (trialNo <= 12 && trialNo >= 1)
                        return PhysicalEnvironmentDependence.Low;
                    else
                        return PhysicalEnvironmentDependence.High;
                case 2:
                    if ((trialNo <= 6 && trialNo >= 1) || (trialNo <= 18 && trialNo >= 13))
                        return PhysicalEnvironmentDependence.Low;
                    else
                        return PhysicalEnvironmentDependence.High;
                case 3:
                    if ((trialNo <= 12 && trialNo >= 7) || (trialNo <= 24 && trialNo >= 19))
                        return PhysicalEnvironmentDependence.Low;
                    else
                        return PhysicalEnvironmentDependence.High;
                case 4:
                    if (trialNo <= 24 && trialNo >= 13)
                        return PhysicalEnvironmentDependence.Low;
                    else
                        return PhysicalEnvironmentDependence.High;
                default:
                    return PhysicalEnvironmentDependence.NULL;
            }
        }

        // get current pattern
        private int[] GetCurrentPattern()
        {
            if (difficultyLevel == 5)
            {
                if (layout == Layout.Flat)
                {
                    if (FlatTaskList.Count > 0)
                    {
                        int[] PatternID = new int[difficultyLevel];
                        string[] PatternIDString = new string[difficultyLevel];

                        PatternIDString = FlatTaskList[0].Split(fieldSeperator);

                        FlatTaskList.RemoveAt(0);

                        for (int i = 0; i < difficultyLevel; i++)
                        {
                            PatternID[i] = int.Parse(PatternIDString[i]);
                        }
                        return PatternID;
                    }
                }
                else if (layout == Layout.Wraparound)
                {
                    if (CircularTaskList.Count > 0)
                    {
                        int[] PatternID = new int[difficultyLevel];
                        string[] PatternIDString = new string[difficultyLevel];

                        PatternIDString = CircularTaskList[0].Split(fieldSeperator);

                        CircularTaskList.RemoveAt(0);

                        for (int i = 0; i < difficultyLevel; i++)
                        {
                            PatternID[i] = int.Parse(PatternIDString[i]);
                        }
                        return PatternID;
                    }
                }
            }
            return null;
        }

        float GetFixedTime()
        {
            float finalTime = 0;
            if (StartSceneScript.lastTimePast != 0)
                finalTime = StartSceneScript.lastTimePast + Time.fixedTime;
            else
                finalTime = Time.fixedTime;
            return finalTime;
        }

        private string GetTrialNumber()
        {
            return trialNo.ToString();
        }

        private string GetTrialID()
        {
            if ((trialNo - 1) % 6 == 0)
                return "Training";
            else { 
                int tmp = (trialNo - 1) / 6;
                return (trialNo - 1 - tmp) + "";
            } 
        }

        private string GetGameState()
        {
            switch (gameState)
            {
                case GameState.Prepare:
                    return "prepare";
                case GameState.Result:
                    return "result"; 
                case GameState.Recall:
                    return "recall";
                case GameState.Learning:
                    return "learning";
                case GameState.Distractor:
                    return "distractor";
                default:
                    return "";
            }
        }

        // Get current patternCards layouts based on sequence
        private string GetLayout()
        {
            switch (layout)
            {
                case Layout.Flat:
                    return "Flat";
                case Layout.Wraparound:
                    return "Wraparound";
                default:
                    return "NULL";
            }
        }

        private string GetPhysicalDependence()
        {
            switch (PEDependence)
            {
                case PhysicalEnvironmentDependence.High:
                    return "High Dependence";
                case PhysicalEnvironmentDependence.Low:
                    return "Low Dependence";
                default:
                    return "NULL";
            }
        }

        private string GetAccuracy()
        {
            return accurateNumber + "";
        }

        private string GetSeenTime()
        {
            return scanTimeLog[0] + "," + scanTimeLog[1] + "," + scanTimeLog[2] + "," + scanTimeLog[3] + "," + scanTimeLog[4];
        }

        private string GetSelectTime()
        {
            if (difficultyLevel == 2)
                return selectTimeLog[0] + "," + selectTimeLog[1] + "," + "," + ",";
            else if (difficultyLevel == 3)
                return selectTimeLog[0] + "," + selectTimeLog[1] + "," + selectTimeLog[2] + "," + ",";
            else if (difficultyLevel == 5)
                return selectTimeLog[0] + "," + selectTimeLog[1] + "," + selectTimeLog[2] + "," + selectTimeLog[3] + "," + selectTimeLog[4];
            return "";
        }


        string VectorToString(Vector3 v)
        {
            string text;
            text = v.x + "," + v.y + "," + v.z;
            return text;
        }
        #endregion

        #region Card Properties Check
        // Check if card filled property is true
        private bool IsCardFilled(GameObject go)
        {
            if (go.GetComponent<Card>().filled)
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

        // Check if card flipped property is true
        private bool IsCardRotating(GameObject go)
        {
            if (go.GetComponent<Card>().rotating)
                return true;
            return false;
        }

        // Check if card selected property is true
        private bool IsCardSelected(GameObject go)
        {
            if (go.GetComponent<Card>().selected)
                return true;
            return false;
        }
        #endregion

        public void QuitGame()
        {
            // save any game data here
        #if UNITY_EDITOR
            // Application.Quit() does not work in the editor so
            // UnityEditor.EditorApplication.isPlaying need to be set to false to end the game
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
        }
    }
}