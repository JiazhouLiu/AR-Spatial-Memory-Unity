using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.IO;
using Microsoft.MixedReality.Toolkit;

public class ExperimentManager : MonoBehaviour
{
    public static int trialNo;

    #region prefab and reference variables
    [Header("Resource")]
    public GameObject CardPrefab;
    public AudioClip TimesUp;
    public AudioClip WrongAnswerAudio;
    public AudioClip CorrectAnswerAudio;

    [Header("Reference")]
    public TextMeshPro Instruction;
    public Transform DistractorTask;
    public TextMeshPro DistractorTaskInstruction;
    public GameObject startButton;
    public GameObject resultButton;
    public GameObject nextButton;
    public GameObject wellDoneButton;
    public GameObject breakButton;
    public Transform WorldRig;

    [Header("Task File")]
    public TextAsset Patterns_Fur_Reg;
    public TextAsset Patterns_Fur_Irr;
    public TextAsset Patterns_NoFur_Reg;
    public TextAsset Patterns_NoFur_Irr;
    public TextAsset RegularColumnPositions;
    public TextAsset IrregularColumnPositions;
    public TextAsset RegularColumnRotations;
    public TextAsset IrregularColumnRotations;
    public TextAsset GameTask;
    #endregion

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
    public LayoutCondition layoutCon;
    public FurnitureCondition furnitureCon;
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
    private Vector3[] columnPositions = new Vector3[12];
    private Vector3[] columnRotations = new Vector3[12];
    private List<GameObject> patternCards;
    private List<GameObject> distractorCards;
    private List<GameObject> userSelectedPatternCards;
    private List<GameObject> touchingCards;
    private GameObject touchingCard;
    private bool finishTouching = false;
    private float LocalMemoryTime;
    private float localDistractorTime;
    private float localEachDistractorReactTime;
    private List<string> Fur_Ali_TaskList;
    private List<string> Fur_NotAli_TaskList;
    private List<string> NoFur_Ali_TaskList;
    private List<string> NoFur_NotAli_TaskList;
    private int[] currentPattern;
    private int currentGameNumber;
    private bool allSeen = false;
    private bool allSelected = false;
    private bool learningTimesUp = false;
    private bool readyForDistractor = false;
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
        Fur_Ali_TaskList = new List<string>();
        Fur_NotAli_TaskList = new List<string>();
        NoFur_Ali_TaskList = new List<string>();
        NoFur_NotAli_TaskList = new List<string>();


        // initialise time log
        scanTimeLog = new List<float>();
        selectTimeLog = new List<float>();
        #endregion

        // load pattern task
        LoadPatterns();

        // add distractor task cards into list
        foreach (Transform t in DistractorTask)
            distractorCards.Add(t.gameObject);

        if (GameObject.Find("MainExperimentManager") != null)
        {
            // update calibrated position and rotation
            if (GameObject.Find("##### World Rig #####") != null)
            {
                WorldRig.position = GameObject.Find("##### World Rig #####").transform.position;
                WorldRig.rotation = GameObject.Find("##### World Rig #####").transform.rotation;
            }

            // setup experimentSequence
            experimentSequence = StartSceneScript.ExperimentSequence;

            // setup trail Number
            if (StartSceneScript.PublicTrialNumber != 0)
                trialNo = StartSceneScript.PublicTrialNumber;
            else
                trialNo = 1;

            // setup writer stream
            SetupLoggingSystem();

            if (GameObject.Find("PreferableStand") != null)
            {
                // setup adjusted height
                adjustedHeight = Camera.main.transform.position.y - 0.5f;
                WriteInteractionToLog("Adjusted Height: " + adjustedHeight, "");
            }
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
        if (GameObject.Find("MainExperimentManager") != null)
        {
            // update calibrated position and rotation
            if (GameObject.Find("##### World Rig #####") != null)
            {
                WorldRig.position = GameObject.Find("##### World Rig #####").transform.position;
                WorldRig.rotation = GameObject.Find("##### World Rig #####").transform.rotation;
            }
        }

        if (gameState == GameState.Prepare)
            PreparePhaseCheck();

        if (gameState == GameState.Learning)
            LearningPhaseCheck();

        if (gameState == GameState.Distractor)
            DistractorPhaseCheck();

        if (gameState == GameState.Recall)
            RecallPhaseCheck();

        // write raw logs
        WritingToLog();
    }

    #region Prepare phase
    public void PrepareExperiment()
    {
        gameState = GameState.Prepare;
        LocalMemoryTime = memoryTime;
        localDistractorTime = distractorTime;
        localEachDistractorReactTime = eachDistractorReactTime;

        // adjust distractor task board position and rotation

        DistractorTask.localPosition = new Vector3(0, 0, 0.5f);
        DistractorTask.position = new Vector3(DistractorTask.position.x, adjustedHeight, DistractorTask.position.z);
        DistractorTask.localEulerAngles = Vector3.zero;

        // change trial conditions based on trial number
        furnitureCon = GetCurrentFurnitureCondition();
        layoutCon = GetCurrentLayoutCondition();

        if (furnitureCon != FurnitureCondition.NULL && layoutCon != LayoutCondition.NULL)
        {
            WriteInteractionToLog("Prepare Phase", "");

            if (GetTrialID() == "Training")
                Instruction.text = "Training Task.\nPlease return to the starting position. " +
                    "After pressing the Start button, you will have 15 seconds " +
                    "to remember the positions of 5 white cards. You will hear a timer sound when you only have 3 seconds left.";
            else
                Instruction.text = "Experiment Task: " + GetTrialID() + " / 20.\nPlease return to the starting position. " +
                    "After pressing the Start button, you will have 15 seconds " +
                    "to remember the positions of 5 white cards. You will hear a timer sound when you only have 3 seconds left. ";

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
            readyForDistractor = false;
            learningTimesUp = false;

            scanTime = 0f;
            selectTime = 0f;

            scanTimeLog.Clear();
            selectTimeLog.Clear();

            patternCards = GenerateCards();

            SetCardsPositionsAndRotations(patternCards);

            foreach (GameObject card in patternCards)
                card.SetActive(false);
        }
        else
            Debug.LogError("condition error! Cannot get current condition.");
    }
    #endregion

    #region Prepare Phase Functions
    private void PreparePhaseCheck()
    {
        if (GameObject.Find("PreferableStand") != null)
        {
            Transform startingPoint = GameObject.Find("PreferableStand").transform;
            Vector3 startingPoint2D = new Vector3(startingPoint.position.x, 0, startingPoint.position.z);

            Vector3 cameraPosition2D = new Vector3(Camera.main.transform.position.x, 0, Camera.main.transform.position.z);
            if (Vector3.Distance(startingPoint2D, cameraPosition2D) <= 0.3f)
                startButton.gameObject.SetActive(true);
            else
                startButton.gameObject.SetActive(false);
        }
        else
            Debug.Log("Cannot find starting point gameobject");
    }

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
                card.transform.localPosition = Vector3.zero;

                // assign orientation
                card.transform.localEulerAngles = Vector3.zero;

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

    // Set patternCards Positions based on current conditions
    private void SetCardsPositionsAndRotations(List<GameObject> localCards)
    {
        for (int index = 0; index < 36; index++)
        {
            localCards[index].transform.localPosition = SetCardPosition(index);
            localCards[index].transform.localEulerAngles = SetCardRotation(index);
        }
    }

    // Set Card Position
    private Vector3 SetCardPosition(int index)
    {
        int columnNo = index % 12;
        int rowNo = index / 12;


        Vector3[] currentColumnPositions = GetCurrentColumnPositions();
        return currentColumnPositions[columnNo] + Vector3.up * rowNo * vDelta;
    }

    // Set Card Rotation
    private Vector3 SetCardRotation(int index)
    {
        int columnNo = index % 12;

        Vector3[] currentColumnRotations = GetCurrentColumnRotations();

        return currentColumnRotations[columnNo];
    }
    #endregion

    #region Learning phase
    // Show pattern (after clicking Start button)
    public void InitiateLearningPhase()
    {
        Instruction.text = "Please remember the positions of 5 white cards.";

        WriteInteractionToLog("Learning Phase", "");

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
                        WriteInteractionToLog(go.name + " seen", VectorToString(go.transform.position));
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
    private void InitiateDistractorPhase()
    {
        HidePattern();
        DistractorTask.gameObject.SetActive(true);

        gameState = GameState.Distractor;
        WriteInteractionToLog("Distractor", "");

        Instruction.text = "Please return to the starting point!";

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
        WriteInteractionToLog("Distractor Number: " + task, "");
    }

    private void HideDistractorCards()
    {
        foreach (GameObject card in distractorCards)
            card.GetComponent<Card>().selected = false; // clear card property
        DistractorTaskInstruction.text = ""; // clear instruction text
        DistractorTaskInstruction.color = Color.white;
        DistractorTask.gameObject.SetActive(false); // hide distractor task
    }

    private void DistractorPhaseCheck()
    {
        if (GameObject.Find("PreferableStand") != null && !readyForDistractor) // check participant back to origin point
        {
            Transform startingPoint = GameObject.Find("PreferableStand").transform;
            Vector3 startingPoint2D = new Vector3(startingPoint.position.x, 0, startingPoint.position.z);

            Vector3 cameraPosition2D = new Vector3(Camera.main.transform.position.x, 0, Camera.main.transform.position.z);
            if (Vector3.Distance(startingPoint2D, cameraPosition2D) <= 0.3f)
                readyForDistractor = true;
        }
        else
            Debug.Log("Cannot find starting point gameobject");

        if (readyForDistractor)
        {
            Instruction.text = "Select the number below!\nTimes remaining: " + localDistractorTime.ToString("0.0");
            if (localDistractorTime >= 0)
            {
                localDistractorTime -= Time.deltaTime;

                if (localEachDistractorReactTime <= 0)
                {
                    // play sound 
                    AudioSource.PlayClipAtPoint(WrongAnswerAudio, DistractorTask.position);
                    localDistractorTime += eachDistractorReactTime;
                    if (localDistractorTime > 15)
                        localDistractorTime = 15;
                    localEachDistractorReactTime = eachDistractorReactTime;

                    GetNewDistractorTask();
                }
                else
                {
                    localEachDistractorReactTime -= Time.deltaTime;

                    if (finishTouching)
                    {
                        finishTouching = false;

                        if (touchingCard.name != currentGameNumber.ToString())
                        {
                            localDistractorTime += eachDistractorReactTime;
                            if (localDistractorTime > 15)
                                localDistractorTime = 15;
                            GetNewDistractorTask();
                            // play sound 
                            AudioSource.PlayClipAtPoint(WrongAnswerAudio, DistractorTask.position);
                        }
                        else if (touchingCard.name == currentGameNumber.ToString())
                        {
                            // play sound 
                            AudioSource.PlayClipAtPoint(CorrectAnswerAudio, DistractorTask.position);
                            DistractorTaskInstruction.color = Color.white;
                            localEachDistractorReactTime = eachDistractorReactTime;
                            GetNewDistractorTask();
                        }
                    }
                }
            }
            else
            {
                HideDistractorCards();
                InitiateRecallPhase();
            }
        }
    }
    #endregion

    #region Recall Phase
    private void InitiateRecallPhase()
    {
        WriteInteractionToLog("Recall Phase", "");
        gameState = GameState.Recall;

        // show patternCards
        foreach (GameObject card in patternCards)
            card.SetActive(true);
    }
    #endregion

    #region Recall Phase Functions 
    public bool RecallPhaseCardSelected(GameObject selectedCard)
    {
        if (selectedCard.name.Contains("Card") && userSelectedPatternCards.Count < difficultyLevel && !userSelectedPatternCards.Contains(selectedCard))
        {
            WriteInteractionToLog(selectedCard.name + " answered", VectorToString(selectedCard.transform.position));
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

        WriteInteractionToLog("Result", "");
        interactionLogger.FlushData();

        CheckResult();
        Instruction.text = "Result: " + accurateNumber + " / " + difficultyLevel;

        resultButton.SetActive(false);
        breakButton.SetActive(false);
        wellDoneButton.SetActive(false);
        nextButton.SetActive(false);
        // button activation based on cases
        if (trialNo == 6 || trialNo == 12 || trialNo == 18)
        { // break button activated
            breakButton.SetActive(true);
        }
        else
        {
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

            if (IsCardSelected(card))
            {
                card.transform.localEulerAngles += Vector3.up * 180;
                SetCardsColor(card.transform, Color.white);
            }

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

        // flush raw log
        FlushingRawLog();
        //gridCalibrate = false;

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
    public void InitiateBreakPhase()
    {
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

        // pattern of 5 cards for furniture and regular layout
        lines = new string[20];
        lines = Patterns_Fur_Reg.text.Split(lineSeperater);
        Fur_Ali_TaskList.AddRange(lines);

        // pattern of 5 cards for furniture and irregular layout
        lines = new string[20];
        lines = Patterns_Fur_Irr.text.Split(lineSeperater);
        Fur_NotAli_TaskList.AddRange(lines);

        // pattern of 5 cards for no furniture and regular layout
        lines = new string[20];
        lines = Patterns_NoFur_Reg.text.Split(lineSeperater);
        NoFur_Ali_TaskList.AddRange(lines);

        // pattern of 5 cards for no furniture and irregular layout
        lines = new string[20];
        lines = Patterns_NoFur_Irr.text.Split(lineSeperater);
        NoFur_NotAli_TaskList.AddRange(lines);
    }

    public int RandomNumber(int min, int max)
    {
        return Random.Range(min, max);
    }
    #endregion

    #region Log System
    private void SetupLoggingSystem()
    {
        rawLogger = StartSceneScript.RawLogger;
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
            rawLogger.AddRow(GetFixedTime() + "," + GetTrialID() + "," + GetGameState() + "," +
                VectorToString(Camera.main.transform.position) + "," + VectorToString(Camera.main.transform.eulerAngles));
            //FlushingRawLog();
        }
    }

    private void FlushingRawLog()
    {
        if (rawLogger != null && Camera.main != null)
            rawLogger.FlushData();
    }

    private void WriteAnswerToLog()
    {
        if (taskLogger != null && userSelectedPatternCards.Count != 0)
        {
            taskLogger.AddRow(StartSceneScript.ParticipantID + "," + GetTrialNumber() + "," + GetTrialID() + "," + GetFurnitureCondition() + "," +
                GetLayoutCondition() + "," + GetAccuracy() + "," + GetSeenTime() + "," + GetSelectTime());
            taskLogger.FlushData();
        }
    }

    public void WriteInteractionToLog(string info, string cardPosition)
    {
        if (interactionLogger != null)
        {
            if (info.Contains("seen"))
                interactionLogger.AddRow(GetFixedTime() + "," + GetTrialNumber() + "," + GetTrialID() + "," +
                    StartSceneScript.ParticipantID + "," + GetFurnitureCondition() + "," + GetLayoutCondition() + "," + "Card," + info.Split(' ')[0].Remove(0, 4) + ",," + cardPosition);
            else if (info.Contains("answered"))
                interactionLogger.AddRow(GetFixedTime() + "," + GetTrialNumber() + "," + GetTrialID() + "," +
                    StartSceneScript.ParticipantID + "," + GetFurnitureCondition() + "," + GetLayoutCondition() + "," + "Card,," + info.Split(' ')[0].Remove(0, 4) + "," + cardPosition);
            else
                interactionLogger.AddRow(GetFixedTime() + "," + GetTrialNumber() + "," + GetTrialID() + "," + StartSceneScript.ParticipantID + "," +
                    GetFurnitureCondition() + "," + GetLayoutCondition() + "," + info + ",,,,,");
            //else if (info.Contains("selected"))
            //    interactionLogger.AddRow(GetFixedTime() + "," + GetTrialNumber() + "," + GetTrialID() + "," +
            //        StartSceneScript.ParticipantID + "," + GetFurnitureCondition() + "," + GetLayoutCondition() + "," + "Card,," + info.Split(' ')[0].Remove(0, 4) + ",,");
            //else if (info.Contains("played"))
            //    interactionLogger.AddRow(GetFixedTime() + "," + GetTrialNumber() + "," + GetTrialID() + "," +
            //        StartSceneScript.ParticipantID + "," + GetFurnitureCondition() + "," + GetLayoutCondition() + "," + "DistractorTask,,,," + info.Split(' ')[0]);

        }
    }

    private void WriteCardsLog()
    {
        if (trialCardLogger != null && userSelectedPatternCards.Count != 0)
        {
            string final = "";

            foreach (GameObject card in userSelectedPatternCards)
            {
                final += card.name.Split(' ')[0].Remove(0, 4) + "," + VectorToString(card.transform.position);
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
                GameObject cardGO = patternCards[cardIndex];
                if (cardGO != null) {
                    final += cardtmp + "," + VectorToString(cardGO.transform.position);
                }else 
                    final += cardtmp + ",";
            }

            final.Remove(final.Length - 1);

            answerCardLogger.AddRow(final);
            answerCardLogger.FlushData();
        }
    }

    private void CloseAllWritersAndQuit()
    {
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

    private void NextGameState()
    {
        if (gameState == GameState.Prepare)
            InitiateLearningPhase();
        else if (gameState == GameState.Learning)
            InitiateDistractorPhase();
        else if (gameState == GameState.Distractor)
        {
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

    public void RecordTouchingCards(GameObject card)
    {
        if (!touchingCards.Contains(card))
            touchingCards.Add(card);
    }

    public void RemoveTouchingCardFromList(GameObject card)
    {
        if (touchingCards.Contains(card))
            touchingCards.Remove(card);
        else
            Debug.Log("removing touching card wrong! " + card.name);
    }

    public void RecordTouchingCard(GameObject card)
    {
        finishTouching = true;
        touchingCard = card;
    }
    #endregion

    #region Get functions
    // Get current patternCards conditions based on sequence
    private FurnitureCondition GetCurrentFurnitureCondition()
    {
        switch (experimentSequence)
        {
            case 0:
                if ((trialNo <= 6 && trialNo >= 1) || (trialNo <= 18 && trialNo >= 13))
                    return FurnitureCondition.HasFurniture;
                else
                    return FurnitureCondition.NoFurniture;
            case 1:
                if ((trialNo <= 6 && trialNo >= 1) || (trialNo <= 18 && trialNo >= 13))
                    return FurnitureCondition.HasFurniture;
                else
                    return FurnitureCondition.NoFurniture;
            case 2:
                if (trialNo <= 24 && trialNo >= 13)
                    return FurnitureCondition.HasFurniture;
                else
                    return FurnitureCondition.NoFurniture;
            case 3:
                if (trialNo <= 12 && trialNo >= 1)
                    return FurnitureCondition.HasFurniture;
                else
                    return FurnitureCondition.NoFurniture;
            case 4:
                if ((trialNo <= 12 && trialNo >= 7) || (trialNo <= 24 && trialNo >= 19))
                    return FurnitureCondition.HasFurniture;
                else
                    return FurnitureCondition.NoFurniture;
            default:
                return FurnitureCondition.NULL;
        }
    }

    // Get current physical environment dependence based on sequence
    private LayoutCondition GetCurrentLayoutCondition()
    {
        switch (experimentSequence)
        {
            case 0:
                if (trialNo <= 12 && trialNo >= 1)
                    return LayoutCondition.Regular;
                else
                    return LayoutCondition.Irregular;
            case 1:
                if (trialNo <= 12 && trialNo >= 1)
                    return LayoutCondition.Regular;
                else
                    return LayoutCondition.Irregular;
            case 2:
                if ((trialNo <= 6 && trialNo >= 1) || (trialNo <= 18 && trialNo >= 13))
                    return LayoutCondition.Regular;
                else
                    return LayoutCondition.Irregular;
            case 3:
                if ((trialNo <= 12 && trialNo >= 7) || (trialNo <= 24 && trialNo >= 19))
                    return LayoutCondition.Regular;
                else
                    return LayoutCondition.Irregular;
            case 4:
                if (trialNo <= 24 && trialNo >= 13)
                    return LayoutCondition.Regular;
                else
                    return LayoutCondition.Irregular;
            default:
                return LayoutCondition.NULL;
        }
    }

    // get current pattern
    private int[] GetCurrentPattern()
    {
        if (difficultyLevel == 5)
        {
            if (furnitureCon == FurnitureCondition.HasFurniture && layoutCon == LayoutCondition.Regular)
            {
                if (Fur_Ali_TaskList.Count > 0)
                {
                    int[] PatternID = new int[difficultyLevel];
                    string[] PatternIDString = new string[difficultyLevel];

                    PatternIDString = Fur_Ali_TaskList[0].Split(fieldSeperator);

                    Fur_Ali_TaskList.RemoveAt(0);

                    for (int i = 0; i < difficultyLevel; i++)
                    {
                        PatternID[i] = int.Parse(PatternIDString[i]);
                    }
                    return PatternID;
                }
            }
            else if (furnitureCon == FurnitureCondition.HasFurniture && layoutCon == LayoutCondition.Irregular)
            {
                if (Fur_NotAli_TaskList.Count > 0)
                {
                    int[] PatternID = new int[difficultyLevel];
                    string[] PatternIDString = new string[difficultyLevel];

                    PatternIDString = Fur_NotAli_TaskList[0].Split(fieldSeperator);

                    Fur_NotAli_TaskList.RemoveAt(0);

                    for (int i = 0; i < difficultyLevel; i++)
                    {
                        PatternID[i] = int.Parse(PatternIDString[i]);
                    }
                    return PatternID;
                }
            }
            else if (furnitureCon == FurnitureCondition.NoFurniture && layoutCon == LayoutCondition.Regular)
            {
                if (NoFur_Ali_TaskList.Count > 0)
                {
                    int[] PatternID = new int[difficultyLevel];
                    string[] PatternIDString = new string[difficultyLevel];

                    PatternIDString = NoFur_Ali_TaskList[0].Split(fieldSeperator);

                    NoFur_Ali_TaskList.RemoveAt(0);

                    for (int i = 0; i < difficultyLevel; i++)
                    {
                        PatternID[i] = int.Parse(PatternIDString[i]);
                    }
                    return PatternID;
                }
            }
            else if (furnitureCon == FurnitureCondition.NoFurniture && layoutCon == LayoutCondition.Irregular)
            {
                if (NoFur_NotAli_TaskList.Count > 0)
                {
                    int[] PatternID = new int[difficultyLevel];
                    string[] PatternIDString = new string[difficultyLevel];

                    PatternIDString = NoFur_NotAli_TaskList[0].Split(fieldSeperator);

                    NoFur_NotAli_TaskList.RemoveAt(0);

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

    private Vector3[] GetCurrentColumnPositions()
    {
        Vector3[] columnPositions = new Vector3[12];
        string[] lines = new string[12];

        if (layoutCon == LayoutCondition.Regular)
            lines = RegularColumnPositions.text.Split(lineSeperater);
        else
            lines = IrregularColumnPositions.text.Split(lineSeperater);

        for (int i = 0; i < 12; i++)
        {
            string[] vectorString = new string[3];
            vectorString = lines[i].Split(fieldSeperator);

            Vector3 vectorValue = new Vector3(float.Parse(vectorString[0]), float.Parse(vectorString[1]), float.Parse(vectorString[2]));
            columnPositions[i] = vectorValue;
        }

        return columnPositions;
    }

    private Vector3[] GetCurrentColumnRotations()
    {
        Vector3[] columnRotations = new Vector3[12];

        string[] lines = new string[12];

        if (layoutCon == LayoutCondition.Regular)
            lines = RegularColumnRotations.text.Split(lineSeperater);
        else
            lines = IrregularColumnRotations.text.Split(lineSeperater);

        for (int i = 0; i < 12; i++)
        {
            string[] vectorString = new string[3];
            vectorString = lines[i].Split(fieldSeperator);

            Vector3 vectorValue = new Vector3(float.Parse(vectorString[0]), float.Parse(vectorString[1]), float.Parse(vectorString[2]));
            columnRotations[i] = vectorValue;
        }

        return columnRotations;
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
        else
        {
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

    // Get current patternCards conditions based on sequence
    private string GetFurnitureCondition()
    {
        switch (furnitureCon)
        {
            case FurnitureCondition.HasFurniture:
                return "HasFurniture";
            case FurnitureCondition.NoFurniture:
                return "NoFurniture";
            default:
                return "NULL";
        }
    }

    private string GetLayoutCondition()
    {
        switch (layoutCon)
        {
            case LayoutCondition.Regular:
                return "Regular";
            case LayoutCondition.Irregular:
                return "Irregular";
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