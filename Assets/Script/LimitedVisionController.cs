using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LimitedVisionController : MonoBehaviour
{
    public Transform CardsParent;
    public ExperimentManager EM;
    public Transform CameraTransform;

    private Layout currentLayout;
    private Dictionary<Transform, Vector3> cardsRelativePosition;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (EM.gameState == GameState.ShowPattern || EM.gameState == GameState.SelectCards) {
            cardsRelativePosition = new Dictionary<Transform, Vector3>();
            currentLayout = EM.layout;
            cardsRelativePosition.Clear();

            foreach (Transform t in CardsParent)
            {
                Vector3 relativePosition = new Vector3(t.position.x, CameraTransform.position.y, t.position.z);
                cardsRelativePosition.Add(t, relativePosition);
            }

            if (currentLayout == Layout.Flat)
                HideFlatLayout();
            else if (currentLayout == Layout.Wraparound)
                HideCircularLayout();
        }
    }

    private void HideFlatLayout() {
        Dictionary<Transform, float> relativeDistance = new Dictionary<Transform, float>();
        foreach (KeyValuePair<Transform, Vector3> card in cardsRelativePosition) {
            relativeDistance.Add(card.Key, Vector3.Distance(card.Value, CameraTransform.position));
        }
        relativeDistance.OrderBy(value => value.Value);

        int i = 0;
        foreach (KeyValuePair<Transform, float> distance in relativeDistance.OrderBy(value => value.Value)) {
            if (i < 9)
            {
                distance.Key.GetChild(0).gameObject.SetActive(true);
            }
            else {
                distance.Key.GetChild(0).gameObject.SetActive(false);
            }
            i++;
        }
    }

    private void HideCircularLayout() {
        Dictionary<Transform, float> relativeAngle = new Dictionary<Transform, float>();

        foreach (KeyValuePair<Transform, Vector3> card in cardsRelativePosition)
        {
            relativeAngle.Add(card.Key, Vector3.Angle((card.Value - CameraTransform.position), CameraTransform.forward));
        }
        relativeAngle.OrderBy(value => value.Value);

        int i = 0;
        foreach (KeyValuePair<Transform, float> angle in relativeAngle.OrderBy(value => value.Value))
        {
            if (i < 9)
            {
                angle.Key.GetChild(0).gameObject.SetActive(true);
            }
            else
            {
                angle.Key.GetChild(0).gameObject.SetActive(false);
            }
            i++;
        }
    }
}
