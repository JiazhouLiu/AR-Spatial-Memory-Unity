// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.MixedReality.Toolkit.Input;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace SpatialMemoryTest
{
    [AddComponentMenu("Scripts/SpatialMemory/HandInteractionTouchRotate")]
    public class HandInteractionTouchRotate_SpatialMemory : HandInteractionTouch_SpatialMemory, IMixedRealityTouchHandler
    {
        [SerializeField]
        [FormerlySerializedAs("TargetObjectTransform")]
        private Transform targetObjectTransform = null;

        [SerializeField]
        private Card card;

        private ExperimentManager em;
        private StartSceneScript ss;

        void IMixedRealityTouchHandler.OnTouchCompleted(HandTrackingInputEventData eventData)
        {
            if (SceneManager.GetActiveScene().name == "Experiment")
            {
                em = GameObject.Find("ExperimentManager").GetComponent<ExperimentManager>();
                if (em != null)
                {
                    if (em.gameState == GameState.Distractor)
                        em.RecordTouchingCard(gameObject);
                }
            }
        }

        void IMixedRealityTouchHandler.OnTouchUpdated(HandTrackingInputEventData eventData)
        {

            if (SceneManager.GetActiveScene().name == "StartScene")
            {
                ss = GameObject.Find("MainExperimentManager").GetComponent<StartSceneScript>();

                if (ss != null)
                {
                    if (ss.gameState == GameState.Learning)
                    {
                        if (card.filled)
                            card.selected = true;
                    }
                    else if (ss.gameState == GameState.Distractor)
                    {
                        card.selected = true;
                    }
                    else if (ss.gameState == GameState.Recall)
                    {
                        if (ss.RecallPhaseCardSelected(gameObject) && !card.rotating)
                        {
                            card.selected = true;
                            card.rotating = true;
                            StartCoroutine(Rotate(targetObjectTransform, new Vector3(0, 180, 0), 0.5f));
                        }
                    }
                }
            }
            else if (SceneManager.GetActiveScene().name == "Experiment")
            {
                em = GameObject.Find("ExperimentManager").GetComponent<ExperimentManager>();
                if (em != null)
                {
                    if (em.gameState == GameState.Learning)
                    {
                        if (card.filled)
                            card.selected = true;
                    }
                    else if (em.gameState == GameState.Distractor)
                    {
                        card.selected = true;
                    }
                    else if (em.gameState == GameState.Recall)
                    {
                        if (em.RecallPhaseCardSelected(gameObject) && !card.rotating)
                        {
                            card.selected = true;
                            card.rotating = true;
                            StartCoroutine(Rotate(targetObjectTransform, new Vector3(0, 180, 0), 0.5f));
                        }
                    }
                }
            }
            else {
                card.selected = true;
            }

        }

        public void SelfRotate() {
            if (!card.rotating && card.filled)
            {
                card.rotating = true;
                StartCoroutine(Rotate(targetObjectTransform, new Vector3(0, 180, 0), 0.5f));
            }
        }

        // rotate coroutine with animation
        private IEnumerator Rotate(Transform rotateObject, Vector3 angles, float duration)
        {
            if (rotateObject != null)
            {
                Vector3 startEulerAngle = rotateObject.localEulerAngles;
                Vector3 endEulerAngle = startEulerAngle + angles;
                for (float t = 0; t < duration; t += Time.deltaTime)
                {
                    rotateObject.localEulerAngles = Vector3.Lerp(startEulerAngle, endEulerAngle, t / duration);
                    yield return null;
                }
                rotateObject.localEulerAngles = endEulerAngle;
            }
        }
    }
}