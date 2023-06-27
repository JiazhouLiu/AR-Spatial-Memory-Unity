// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;

[AddComponentMenu("Scripts/SpatialMemory/HandInteractionTouch")]
public class HandInteractionTouch_SpatialMemory : MonoBehaviour, IMixedRealityTouchHandler
{
    [SerializeField]
    private TextMesh debugMessage = null;
    [SerializeField]
    private TextMesh debugMessage2 = null;

    #region Event handlers
    public TouchEvent OnTouchCompleted;
    public TouchEvent OnTouchStarted;
    public TouchEvent OnTouchUpdated;
    #endregion

    private void Start()
    {
    }

    void IMixedRealityTouchHandler.OnTouchCompleted(HandTrackingInputEventData eventData)
    {
        OnTouchCompleted.Invoke(eventData);

        if (debugMessage != null)
        {
            debugMessage.text = "OnTouchCompleted: " + Time.unscaledTime.ToString();
        }
    }

    void IMixedRealityTouchHandler.OnTouchStarted(HandTrackingInputEventData eventData)
    {
        OnTouchStarted.Invoke(eventData);

        if (debugMessage != null)
        {
            debugMessage.text = "OnTouchStarted: " + Time.unscaledTime.ToString();
        }
    }

    void IMixedRealityTouchHandler.OnTouchUpdated(HandTrackingInputEventData eventData)
    {
        OnTouchUpdated.Invoke(eventData);

        if (debugMessage2 != null)
        {
            debugMessage2.text = "OnTouchUpdated: " + Time.unscaledTime.ToString();
        }
    }
}