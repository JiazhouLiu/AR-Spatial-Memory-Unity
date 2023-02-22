using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace SpatialMemoryTest {
    public class Card : MonoBehaviour
    {
        [Header("Reference")]
        [SerializeField]
        private Transform border1;
        [SerializeField]
        private Transform border2;
        [SerializeField]
        private Transform border3;
        [SerializeField]
        private Transform border4;

        [Header("Variable")]
        public bool flipped = false;
        public bool interactable = false;
        public bool filled = false;
        public bool rotating = false;

        public bool seen = false;
        public bool seenLogged = false;
        public bool selected = false;
        public bool selectedLogged = false;

        private ExperimentManager em;
        private StartSceneScript ss;

        private void Awake()
        {
        }

        private void Update()
        {
            Transform[] borders = new Transform[4] { border1, border2, border3, border4 };

            Color bothColor = new Color(27f / 255f, 158f / 255f, 119f / 255f); // green 
            Color seenColor = new Color(217f / 255f, 95f / 255f, 2f / 255f); // orange
            Color touchedColor = new Color(117f / 255f, 112f / 255f, 179f / 255f); // blue

            if (SceneManager.GetActiveScene().name == "StartScene")
            {
                ss = GameObject.Find("MainExperimentManager").GetComponent<StartSceneScript>();

                if (ss != null)
                {
                    if (ss.gameState == GameState.Learning)
                    {
                        if (filled && seen && !selected)
                        {
                            foreach (Transform t in borders)
                                t.GetComponent<Image>().color = seenColor;
                        }
                        else if (filled && !seen && selected)
                        {
                            foreach (Transform t in borders)
                                t.GetComponent<Image>().color = touchedColor;
                        }
                        else if (filled && seen && selected)
                        {
                            foreach (Transform t in borders)
                                t.GetComponent<Image>().color = bothColor;
                        }
                    }
                    else if (ss.gameState == GameState.Distractor) {
                        if (filled && selected)
                        {
                            foreach (Transform t in borders)
                                t.GetComponent<Image>().color = Color.green;
                        }
                        else if (!filled && selected) {
                            foreach (Transform t in borders)
                                t.GetComponent<Image>().color = Color.red;
                        }
                    }
                }
            }
            else
            {
                em = GameObject.Find("ExperimentManager").GetComponent<ExperimentManager>();
                if (em.gameState == GameState.Learning || em.gameState == GameState.Distractor)
                {
                    if (filled && seen && !selected)
                    {
                        foreach (Transform t in borders)
                            t.GetComponent<Image>().color = seenColor;
                    }
                    else if (filled && !seen && selected)
                    {
                        foreach (Transform t in borders)
                            t.GetComponent<Image>().color = touchedColor;
                    }
                    else if (filled && seen && selected)
                    {
                        foreach (Transform t in borders)
                            t.GetComponent<Image>().color = bothColor;
                    }
                    else if (filled && !seen && !selected)
                    {
                        foreach (Transform t in borders)
                            t.GetComponent<Image>().color = Color.white;
                    }
                }
                else if (em.gameState == GameState.Result)
                {
                    if (selected)
                    {
                        foreach (Transform t in borders)
                            t.GetComponent<Image>().color = seenColor;
                    }
                }
                else {
                    foreach (Transform t in borders)
                        t.GetComponent<Image>().color = touchedColor;
                }
            }
        }

        public void ResetBorderColor() {
            Transform[] borders = new Transform[4] { border1, border2, border3, border4 };
            foreach (Transform t in borders)
            {
                t.GetComponent<Image>().color = Color.white;
            }
        }
    }
}