using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Card : MonoBehaviour
{
    public bool flipped = false;
    public bool interactable = false;
    public bool filled = false;
    public bool rotating = false;

    public bool seen = false;
    public bool selected = false;

    public bool seenLogged = false;
    public bool selectLogged = false;

    private Transform[] borders;

    private ExperimentManager em;

    private void Start()
    {
        if (GameObject.Find("ExperimentManager") != null) {
            em = GameObject.Find("ExperimentManager").GetComponent<ExperimentManager>();
        }
        borders = new Transform[4]{
            transform.GetChild(0).GetChild(0).GetChild(2),
            transform.GetChild(0).GetChild(0).GetChild(3),
            transform.GetChild(0).GetChild(0).GetChild(4),
            transform.GetChild(0).GetChild(0).GetChild(5)
        };

    }

    private void Update()
    {
        Color bothColor = new Color(27f / 255f, 158f / 255f, 119f / 255f);
        Color seenColor = new Color(217f / 255f, 95f / 255f, 2f / 255f);
        Color touchedColor = new Color(117f / 255f, 112f / 255f, 179f / 255f);
        if (em == null)
        {
            if (filled && seen && !selected)
            {
                foreach (Transform t in borders)
                {
                    t.GetComponent<Image>().color = seenColor;
                }
            }
            else if (filled && !seen && selected)
            {
                foreach (Transform t in borders)
                {
                    t.GetComponent<Image>().color = touchedColor;
                }
            }
            else if (filled && seen && selected)
            {
                foreach (Transform t in borders)
                {
                    t.GetComponent<Image>().color = bothColor; 
                }
            }
        }
        else
        {
            if (em.gameState == GameState.ShowPattern || em.gameState == GameState.Distractor)
            {
                if (filled && seen && !selected)
                {
                    foreach (Transform t in borders)
                    {
                        t.GetComponent<Image>().color = seenColor;
                    }
                }
                else if (filled && !seen && selected)
                {
                    foreach (Transform t in borders)
                    {
                        t.GetComponent<Image>().color = touchedColor;
                    }
                }
                else if (filled && seen && selected)
                {
                    foreach (Transform t in borders)
                    {
                        t.GetComponent<Image>().color = bothColor;
                    }
                }
                else if (filled && !seen && !selected)
                {
                    foreach (Transform t in borders)
                    {
                        t.GetComponent<Image>().color = Color.white;
                    }
                }



            }
            else
            {
                foreach (Transform t in borders)
                {
                    t.GetComponent<Image>().color = Color.white;
                }
            }
        }
    }

    public void ResetBorderColor() {
        foreach (Transform t in borders)
        {
            t.GetComponent<Image>().color = Color.white;
        }
    }
}
