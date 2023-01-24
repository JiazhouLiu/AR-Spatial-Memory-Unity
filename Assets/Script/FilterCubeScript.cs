using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FilterCubeScript : MonoBehaviour
{
    public float radius;
    public float degreeOfView;
    public ExperimentManager manager;
    public Transform MainCamera;

    private void Start()
    {
        //transform.localScale = new Vector3(degreeOfView * Mathf.PI * radius / 180, 2, 2 * radius);
        transform.GetChild(0).localPosition = new Vector3(degreeOfView * Mathf.PI * radius / 360, 0, 0);
        transform.GetChild(1).localPosition = new Vector3(-degreeOfView * Mathf.PI * radius / 360, 0, 0);

        //Vector3[] vertices = GetComponent<MeshFilter>().mesh.vertices;
        //int[] triangles = GetComponent<MeshFilter>().mesh.triangles;
        //for (int i = 0; i < 18; i++) {
        //    triangles[i] = 0;
        //}

        //Mesh mesh = GetComponent<MeshFilter>().mesh;
        //mesh.Clear();
        //mesh.vertices = vertices;
        //mesh.triangles = triangles;
        //mesh.Optimize();
        //mesh.RecalculateNormals();
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = new Vector3(MainCamera.position.x, 1, MainCamera.position.z);
        if (manager != null) {
            if (manager.layout == Layout.Flat)
                transform.localEulerAngles = Vector3.zero;
            else
                transform.localEulerAngles = new Vector3(0, Camera.main.transform.eulerAngles.y, 0);
        }
        
    }
}
