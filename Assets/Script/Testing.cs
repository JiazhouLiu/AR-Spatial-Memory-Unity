using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Testing : MonoBehaviour
{
    
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = Camera.main.transform.position + Camera.main.transform.TransformDirection(Vector3.forward) * 1f + Vector3.down * 0.5f;
        transform.LookAt(Camera.main.transform.position);
        transform.localEulerAngles = new Vector3(0, transform.localEulerAngles.y + 180, 0);
    }
}
