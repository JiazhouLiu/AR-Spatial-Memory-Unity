using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SpatialMemoryTest
{
    public class DataLoggingManager : MonoBehaviour
    {
        [Header("Logging Settings")]
        public string FilePath;
        public float TimeBetweenLogs = 0.05f;

        [Header("Logged Objects")]
        public Transform HeadTransform;
        public Transform LeftControllerTransform;
        public Transform RightControllerTransform;

        [Header("Reference")]
        public ExperimentManager EM;

        [HideInInspector]
        public bool IsLogging = false;

        [HideInInspector]
        public string ParticipantID;

        private StreamWriter headTransformStreamWriter;
        private StreamWriter leftControllerTransformStreamWriter;
        private StreamWriter rightControllerTransformStreamWriter;
        private StreamWriter cardsTransformStreamWriter;
        private float startTime;
        private float timer = 0f;
        private const string format = "F4";

        private void Awake()
        {
            if (!FilePath.EndsWith("/"))
                FilePath += "/";

            ParticipantID = StartSceneScript.ParticipantID.ToString();
        }

        public void StartLogging()
        {
            // Head transform
            string path = string.Format("{0}P{1}_HeadTransform.txt", FilePath, ParticipantID);
            headTransformStreamWriter = new StreamWriter(path, false);
            headTransformStreamWriter.WriteLine("Timestamp\tPosition.x\tPosition.y\tPosition.z\tRotation.x\tRotation.y\tRotation.z\tRotation.w");

            // Left Controller transform
            path = string.Format("{0}P{1}_LeftControllerTransform.txt", FilePath, ParticipantID);
            leftControllerTransformStreamWriter = new StreamWriter(path, false);
            leftControllerTransformStreamWriter.WriteLine("Timestamp\tPosition.x\tPosition.y\tPosition.z\tRotation.x\tRotation.y\tRotation.z\tRotation.w");

            // Right Controller transform
            path = string.Format("{0}P{1}_RightControllerTransform.txt", FilePath, ParticipantID);
            rightControllerTransformStreamWriter = new StreamWriter(path, false);
            rightControllerTransformStreamWriter.WriteLine("Timestamp\tPosition.x\tPosition.y\tPosition.z\tRotation.x\tRotation.y\tRotation.z\tRotation.w");

            // Detailed View transforms and properties
            path = string.Format("{0}P{1}_Cards.txt", FilePath, ParticipantID);
            cardsTransformStreamWriter = new StreamWriter(path, false);
            cardsTransformStreamWriter.WriteLine("Timestamp\tPosition.x\tPosition.y\tPosition.z\tRotation.x\tRotation.y\tRotation.z\tRotation.w");

            IsLogging = true;
            startTime = Time.time;

            Debug.Log("Logging Started");
        }

        public void FixedUpdate()
        {
            if (IsLogging)
            {
                timer += Time.fixedDeltaTime;
                if (timer >= TimeBetweenLogs)
                {
                    timer = 0;
                    LogData();
                }
            }
        }

        public void StopLogging()
        {
            IsLogging = false;
            timer = 0;

            headTransformStreamWriter.Close();
            leftControllerTransformStreamWriter.Close();
            rightControllerTransformStreamWriter.Close();
            cardsTransformStreamWriter.Close();

            Debug.Log("Logging Stopped");
        }

        private void LogData()
        {
            float timestamp = Time.time - startTime;

            // head log
            headTransformStreamWriter.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}",
                timestamp,
                HeadTransform.position.x.ToString(format),
                HeadTransform.position.y.ToString(format),
                HeadTransform.position.z.ToString(format),
                HeadTransform.rotation.x.ToString(format),
                HeadTransform.rotation.y.ToString(format),
                HeadTransform.rotation.z.ToString(format),
                HeadTransform.rotation.w.ToString(format)
            );

            // left controller log
            leftControllerTransformStreamWriter.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}",
                timestamp,
                LeftControllerTransform.position.x.ToString(format),
                LeftControllerTransform.position.y.ToString(format),
                LeftControllerTransform.position.z.ToString(format),
                LeftControllerTransform.rotation.x.ToString(format),
                LeftControllerTransform.rotation.y.ToString(format),
                LeftControllerTransform.rotation.z.ToString(format),
                LeftControllerTransform.rotation.w.ToString(format)
            );

            // right controller log
            rightControllerTransformStreamWriter.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}",
                timestamp,
                RightControllerTransform.position.x.ToString(format),
                RightControllerTransform.position.y.ToString(format),
                RightControllerTransform.position.z.ToString(format),
                RightControllerTransform.rotation.x.ToString(format),
                RightControllerTransform.rotation.y.ToString(format),
                RightControllerTransform.rotation.z.ToString(format),
                RightControllerTransform.rotation.w.ToString(format)
            );

            // cards log
            if (transform.childCount > 0)
            {
                for (int i = 0; i < transform.childCount; i++)
                {
                    Transform card = transform.GetChild(i);

                    cardsTransformStreamWriter.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}",
                        timestamp,
                        card.position.x.ToString(format),
                        card.position.y.ToString(format),
                        card.position.z.ToString(format),
                        card.rotation.x.ToString(format),
                        card.rotation.y.ToString(format),
                        card.rotation.z.ToString(format),
                        card.rotation.w.ToString(format)
                    );
                }
                cardsTransformStreamWriter.Flush();
            }

            headTransformStreamWriter.Flush();
            leftControllerTransformStreamWriter.Flush();
            rightControllerTransformStreamWriter.Flush();
        }

        public void OnApplicationQuit()
        {
            if (IsLogging)
                StopLogging();
        }
    }

}