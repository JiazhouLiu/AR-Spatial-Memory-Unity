using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Testing : MonoBehaviour
{
    public Transform DistractorTask;
    private List<GameObject> distractorCards;
    // Start is called before the first frame update
    void Start()
    {
        foreach (Transform t in DistractorTask)
            distractorCards.Add(t.gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
