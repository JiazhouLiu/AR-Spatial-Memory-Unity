// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.MixedReality.Toolkit.Input;
using UnityEngine;
using UnityEngine.Serialization;

namespace SpatialMemoryTest.Interaction
{
    [AddComponentMenu("Scripts/SpatialMemory/HandInteractionTouchRotate")]
    public class HandInteractionTouchRotate_SpatialMemory : HandInteractionTouch_SpatialMemory, IMixedRealityTouchHandler
    {
        [SerializeField]
        [FormerlySerializedAs("TargetObjectTransform")]
        private Transform targetObjectTransform = null;

        [SerializeField]
        private float rotateSpeed = 300.0f;

        void IMixedRealityTouchHandler.OnTouchUpdated(HandTrackingInputEventData eventData)
        {
            if (targetObjectTransform != null)
            {
                targetObjectTransform.Rotate(Vector3.up * (rotateSpeed * Time.deltaTime));
            }
        }
    }
}