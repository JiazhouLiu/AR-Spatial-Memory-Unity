using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using VRTK;
using TMPro;
using UnityEngine.UI;
using System.IO;
//using System;
//using VRTK.GrabAttachMechanics;
using System.Linq;

namespace SpatialMemoryTest
{
    public class ReplayManager : MonoBehaviour
    {
        [Header("Prefabs")]
        public GameObject CardPrefab;
        public GameObject pathPoints;
        public GameObject gazePoints;
        public GameObject gazePointPrefab;
        public GameObject pathPointPrefab;
        public GameObject cubeCollider;
        public GameObject cylinderCollider;

        [Header("Task File")]
        public TextAsset Patterns5Flat;
        public TextAsset Patterns5Circular;

        [Header("Predefined Variables")]
        public float hDelta;
        public float vDelta;
        public float cardSize;
        public float memoryTime;
        public float distractorTime;
        public int numberOfRows;
        public int numberOfColumns;

        [Header("Variables")]
        public int participantNumber;
        private Layout layout;
        private int difficultyLevel = 5;

        /// <summary>
        /// local variables
        /// </summary>

        // do not change
        [HideInInspector]
        public int mainHand = 1; // 0: left; 1: right
        private float adjustedHeight = 1;
        private Transform mainController;
        //private VRTK_InteractUse mainHandIU;
        //private VRTK_InteractTouch mainHandIT;
        private int mainHandIndex = -1;
        //private VRTK_ControllerEvents mainHandCE;
        private int maxTrialNo = 20;
        // string variables
        private char lineSeperater = '\n'; // It defines line seperate character
        private char fieldSeperator = ','; // It defines field seperate chracter

        // incremental with process
        [HideInInspector]
        public GameState gameState;
        public int trialNo;

        // refresh every trail
        private List<GameObject> cards;
        private List<GameObject> gameCards;
        private List<GameObject> selectedCards;
        private List<GameObject> selectedGameCards;

        private List<string> LvL5FlatTaskList;
        private List<string> LvL5CircularTaskList;
        private int[] currentPattern;

        // log use
        private int experimentSequence;

        [HideInInspector]
        public string[] lines;
        private List<GameObject> gazePointList;
        private List<GameObject> pathPointList;


        Gradient gradient;
        GradientColorKey[] colorKey;
        GradientAlphaKey[] alphaKey;

        // Start is called before the first frame update
        void Start()
        {
            // initialise variables
            cards = new List<GameObject>();
            gameCards = new List<GameObject>();
            selectedCards = new List<GameObject>();
            selectedGameCards = new List<GameObject>();
            gazePointList = new List<GameObject>();
            pathPointList = new List<GameObject>();

            LvL5FlatTaskList = new List<string>();
            LvL5CircularTaskList = new List<string>();

            ReadPatternsFromInput();

            mainHand = 1;

            // setup adjusted height
            adjustedHeight = 0.75f;

            // setup experimentSequence
            if (participantNumber % 2 == 1)
                experimentSequence = 1;
            else
                experimentSequence = 2;

            string FilePath = "Assets/ExperimentData/ExperimentLog/Participant " + participantNumber + "/Participant_" + participantNumber + "_HeadAndHand.csv";
            lines = File.ReadAllText(FilePath).Split(lineSeperater);
            lines = lines.Skip(1).ToArray();

            gradient = new Gradient();

            // Populate the color keys at the relative time 0 and 1 (0 and 100%)
            colorKey = new GradientColorKey[2];
            colorKey[0].color = new Color(127f / 255f, 191f / 255f, 123f / 255f);
            colorKey[0].time = 0.0f;
            colorKey[1].color = new Color(175f / 255f, 141f / 255f, 195f / 255f);
            colorKey[1].time = 1.0f;

            // Populate the alpha  keys at relative time 0 and 1  (0 and 100%)
            alphaKey = new GradientAlphaKey[2];
            alphaKey[0].alpha = 1.0f;
            alphaKey[0].time = 0.0f;
            alphaKey[1].alpha = 1.0f;
            alphaKey[1].time = 1.0f;

            gradient.SetKeys(colorKey, alphaKey);
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown("b"))
            {
                foreach (GameObject go in gazePointList)
                    Destroy(go);
                foreach (GameObject go in pathPointList)
                    Destroy(go);
                gazePointList = new List<GameObject>();
                pathPointList = new List<GameObject>();
                trialNo++;
                ShowBoard();
            }
            if (Input.GetKeyDown("r"))
            {
                foreach (GameObject go in gazePointList)
                    Destroy(go);
                foreach (GameObject go in pathPointList)
                    Destroy(go);
                gazePointList = new List<GameObject>();
                pathPointList = new List<GameObject>();

                cubeCollider.SetActive(false);
                cylinderCollider.SetActive(false);

                trialNo++;

                if (GetCurrentCardsLayout() != Layout.NULL)
                    layout = GetCurrentCardsLayout();

                if (layout == Layout.Flat)
                    cubeCollider.SetActive(true);
                else
                    cylinderCollider.SetActive(true);

                StartRecording();
            }


            if (Input.GetKeyDown("p"))
                ShowPattern();

            if (Input.GetKeyDown("l"))
                ChangeLayout();
        }

        private void ShowBoard()
        {
            if (GetCurrentCardsLayout() != Layout.NULL)
                layout = GetCurrentCardsLayout();
            difficultyLevel = GetCurrentDifficulty();

            if (cards != null)
            {
                foreach (GameObject go in cards)
                    Destroy(go);
                cards.Clear();
            }

            cards = GenerateCards();

            SetCardsPositions(cards, layout);

        }


        // Show pattern (after clicking Start button)
        private void ShowPattern()
        {
            foreach (GameObject card in cards)
            {
                card.SetActive(true);
            }

            // flip to the front
            foreach (GameObject card in cards)
            {
                if (IsCardFilled(card))
                    SetCardsColor(card.transform, Color.white);
                StartCoroutine(Rotate(card.transform, new Vector3(0, 180, 0), 0.5f));
            }
        }

        // Hide pattern 
        private void HidePattern(bool fromFailedTrial)
        {

            if (!fromFailedTrial)
            {
                if (gameState == GameState.Learning)
                {
                    gameState = GameState.Distractor;
                }
                else
                {

                    // flip to the back
                    foreach (GameObject card in cards)
                    {
                        if (IsCardFilled(card))
                            SetCardsColor(card.transform, Color.black);
                        StartCoroutine(Rotate(card.transform, new Vector3(0, 180, 0), 0.5f));
                    }
                    // move to next state
                    gameState = GameState.Recall;
                    // enable the interactable feature
                    foreach (GameObject card in cards)
                    {
                        //card.GetComponent<VRTK_InteractableObject>().enabled = true;
                    }
                }
            }
        }

        private void StartRecording()
        {
            int count = 0;
            foreach (string line in lines)
            {
                string[] data = line.Trim().Split(fieldSeperator);
                //Debug.Log(data.Length);
                if (data.Length > 1)
                {
                    if (int.Parse(data[1]) == trialNo && data[7].Trim().Equals("showPattern"))
                    {
                        Vector3 cameraPosition = new Vector3(float.Parse(data[8]), float.Parse(data[9]), float.Parse(data[10]));
                        GameObject pathPoint = Instantiate(pathPointPrefab, cameraPosition, Quaternion.identity);
                        pathPoint.transform.localEulerAngles += Vector3.left * 90;
                        pathPoint.transform.SetParent(pathPoints.transform);
                        pathPointList.Add(pathPoint);

                        Vector3 cameraDirection = Quaternion.Euler(new Vector3(float.Parse(data[11]), float.Parse(data[12]), float.Parse(data[13]))) * Vector3.forward;

                        RaycastHit hit;
                        int layerMask = 1 << 8;
                        layerMask = ~layerMask;
                        if (Physics.Raycast(cameraPosition, cameraDirection, out hit, Mathf.Infinity, layerMask))
                        {
                            GameObject gazePoint = Instantiate(gazePointPrefab, hit.point, Quaternion.identity);
                            gazePoint.transform.localEulerAngles += Vector3.left * 90;
                            gazePoint.transform.SetParent(gazePoints.transform);

                            gazePointList.Add(gazePoint);
                        }
                        count++;
                    }
                }

            }

            for (int i = 0; i < pathPointList.Count; i++)
            {
                pathPointList[i].transform.position = new Vector3(pathPointList[i].transform.position.x, 0, pathPointList[i].transform.position.z);
                pathPointList[i].transform.localScale += Vector3.one * i * 0.05f / pathPointList.Count;
            }
            int gazeCount = gazePointList.Count;
            for (int i = 0; i < gazeCount; i++)
            {
                gazePointList[i].transform.position = new Vector3(gazePointList[i].transform.position.x, 0, gazePointList[i].transform.position.z);
                gazePointList[i].transform.localScale += Vector3.one * i * 0.03f / gazePointList.Count;
            }
            for (int i = gazePointList.Count - 1; i >= 0; i--)
            {
                if (i % 2 == 0)
                {
                    Destroy(gazePointList[i]);
                    gazePointList.RemoveAt(i);
                }
            }
            for (int i = gazePointList.Count - 1; i >= 0; i--)
            {
                if (i % 2 == 0)
                {
                    Destroy(gazePointList[i]);
                    gazePointList.RemoveAt(i);
                }
            }
            for (int i = gazePointList.Count - 1; i >= 0; i--)
            {
                if (i % 2 == 0)
                {
                    Destroy(gazePointList[i]);
                    gazePointList.RemoveAt(i);
                }
            }
            for (int i = gazePointList.Count - 1; i >= 0; i--)
            {
                if (i % 2 == 0)
                {
                    Destroy(gazePointList[i]);
                    gazePointList.RemoveAt(i);
                }
            }
        }

        private void ChangeLayout()
        {
            if (layout == Layout.Wraparound)
            {
                layout = Layout.Flat;
                SetCardsPositions(cards, Layout.Flat);
            }
        }


        // Get current cards layouts based on sequence
        private Layout GetCurrentCardsLayout()
        {
            switch (experimentSequence)
            {
                case 1:

                case 2:

                case 3:

                case 4:

                default:
                    return Layout.NULL;
            }
        }

        // Get current physical environment dependence based on sequence
        private PhysicalEnvironmentDependence GetCurrentPEDependence()
        {
            switch (experimentSequence)
            {
                case 1:

                case 2:

                case 3:

                case 4:

                default:
                    return PhysicalEnvironmentDependence.NULL;
            }
        }

        private int GetCurrentDifficulty()
        {
            return 5;
        }

        // get current pattern
        private int[] GetCurrentPattern()
        {
            if (layout == Layout.Flat)
            {
                if (LvL5FlatTaskList.Count > 0)
                {
                    int[] PatternID = new int[difficultyLevel];
                    string[] PatternIDString = new string[difficultyLevel];

                    PatternIDString = LvL5FlatTaskList[0].Split(fieldSeperator);

                    LvL5FlatTaskList.RemoveAt(0);

                    for (int i = 0; i < difficultyLevel; i++)
                    {
                        PatternID[i] = int.Parse(PatternIDString[i]);
                    }
                    return PatternID;
                }
            }
            else if (layout == Layout.Wraparound)
            {
                if (LvL5CircularTaskList.Count > 0)
                {
                    int[] PatternID = new int[difficultyLevel];
                    string[] PatternIDString = new string[difficultyLevel];

                    PatternIDString = LvL5CircularTaskList[0].Split(fieldSeperator);

                    LvL5CircularTaskList.RemoveAt(0);

                    for (int i = 0; i < difficultyLevel; i++)
                    {
                        PatternID[i] = int.Parse(PatternIDString[i]);
                    }
                    return PatternID;
                }
            }
            return null;
        }

        // Generate Cards
        private List<GameObject> GenerateCards()
        {
            List<GameObject> cards = new List<GameObject>();
            int k = 0;

            for (int i = 0; i < numberOfRows; i++)
            {
                for (int j = 0; j < numberOfColumns; j++)
                {
                    // calculate index number
                    int index = i * numberOfColumns + j;

                    // generate card game object
                    string name = "Card" + index;
                    GameObject card = (GameObject)Instantiate(CardPrefab, new Vector3(0, 0, 0), Quaternion.identity);
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
                    cards.Add(card);

                    k++;
                }
            }

            currentPattern = GetCurrentPattern();

            if (currentPattern != null)
            {
                for (int i = 0; i < currentPattern.Length; i++)
                {
                    cards[currentPattern[i]].GetComponent<Card>().filled = true;
                }
            }
            else
                Debug.LogError("Pattern Used Up!!");

            return cards;
        }

        // Set Cards Positions based on current layout
        private void SetCardsPositions(List<GameObject> localCards, Layout localLayout)
        {
            for (int i = 0; i < numberOfRows; i++)
            {
                for (int j = 0; j < numberOfColumns; j++)
                {
                    int index = i * numberOfColumns + j;
                    localCards[index].transform.localPosition = SetCardPosition(index, i, j);

                    localCards[index].transform.localEulerAngles = new Vector3(0, localCards[index].transform.localEulerAngles.y, 0);

                    if (localLayout != Layout.Wraparound)
                        localCards[index].transform.localEulerAngles = new Vector3(0, 0, 0);
                    else // FULL CIRCLE
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


            switch (localLayout)
            {
                case Layout.Flat:
                    transform.localPosition = new Vector3(0, adjustedHeight, -1);
                    break;
                case Layout.Wraparound:
                    transform.localPosition = new Vector3(0, adjustedHeight, 0);
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
                    zValue = 2;
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

        // Set Card Color
        private void SetCardsColor(Transform t, Color color)
        {
            if (color == Color.white)
                t.GetChild(0).GetChild(0).GetChild(1).gameObject.SetActive(true);
            else
                t.GetChild(0).GetChild(0).GetChild(1).gameObject.SetActive(false);
        }


        private void ReadPatternsFromInput()
        {
            string[] lines = new string[10];

            // fixed pattern 5 flat
            lines = new string[20];
            lines = Patterns5Flat.text.Split(lineSeperater);
            LvL5FlatTaskList.AddRange(lines);

            // fixed pattern 5 Circular
            lines = new string[20];
            lines = Patterns5Circular.text.Split(lineSeperater);
            LvL5CircularTaskList.AddRange(lines);
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

        private string GetUserID()
        {
            return StartSceneScript.ParticipantID.ToString();
        }

        private string GetTrialNumber()
        {
            return trialNo.ToString();
        }

        private string GetTrialID()
        {
            if (trialNo == 1 || trialNo == 2)
                return "Training";
            else
                return (trialNo - 2) + "";
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

        // Get current cards layouts based on sequence
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

        private string GetDifficulty()
        {
            return difficultyLevel + "";
        }

        private bool GetPadPressed()
        {
            //if (mainHandCE == null)
            //{
            //    mainHandCE = GameObject.Find("RightControllerAlias").GetComponent<VRTK_ControllerEvents>();
            //}

            //if (mainHandCE.touchpadPressed)
            //    return true;
            //else
            return false;
        }

        private bool GetTriggerPressed()
        {
            //if (mainHandCE == null)
            //{
            //    mainHandCE = GameObject.Find("RightControllerAlias").GetComponent<VRTK_ControllerEvents>();
            //}

            //if (mainHandCE.triggerClicked)
            //    return true;
            //else
            return false;
        }

        string VectorToString(Vector3 v)
        {
            string text;
            text = v.x + "," + v.y + "," + v.z;
            return text;
        }

        /// Log functions END

        /// Card property related functions START
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
        /// Card property related functions END

        /// general functions
        private void PrintTextToScreen(Text textBoard, string text)
        {
            textBoard.text = text;
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

        public int RandomNumber(int min, int max)
        {
            return Random.Range(min, max);
        }
    }
}