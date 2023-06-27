// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using UnityEngine;

namespace Microsoft.MixedReality.SampleQRCodes
{
    public class QRCodesVisualizer : MonoBehaviour
    {
        public static Vector3 CalibratedPositionTopRight = Vector3.zero;
        public static Vector3 CalibratedRotationTopRight = Vector3.zero;
        public static Vector3 CalibratedPositionBottomRight = Vector3.zero;
        public static Vector3 CalibratedRotationBottomRight = Vector3.zero;
        public static Vector3 CalibratedPositionBottomLeft = Vector3.zero;
        public static Vector3 CalibratedRotationBottomLeft = Vector3.zero;
        public static Vector3 CalibratedPositionTopLeft = Vector3.zero;
        public static Vector3 CalibratedRotationTopLeft = Vector3.zero;
        public static bool changeDetected = false;
        public static string changeDirection = "";

        public GameObject qrCodePrefab;

        private SortedDictionary<System.Guid, GameObject> qrCodesObjectsList;
        private bool clearExisting = false;

        struct ActionData
        {
            public enum Type
            {
                Added,
                Updated,
                Removed
            };
            public Type type;
            public Microsoft.MixedReality.QR.QRCode qrCode;

            public ActionData(Type type, Microsoft.MixedReality.QR.QRCode qRCode) : this()
            {
                this.type = type;
                qrCode = qRCode;
            }
        }

        private Queue<ActionData> pendingActions = new Queue<ActionData>();

        // Use this for initialization
        void Start()
        {
            Debug.Log("QRCodesVisualizer start");
            qrCodesObjectsList = new SortedDictionary<System.Guid, GameObject>();

            QRCodesManager.Instance.QRCodesTrackingStateChanged += Instance_QRCodesTrackingStateChanged;
            QRCodesManager.Instance.QRCodeAdded += Instance_QRCodeAdded;
            QRCodesManager.Instance.QRCodeUpdated += Instance_QRCodeUpdated;
            QRCodesManager.Instance.QRCodeRemoved += Instance_QRCodeRemoved;
            if (qrCodePrefab == null)
            {
                throw new System.Exception("Prefab not assigned");
            }
        }
        private void Instance_QRCodesTrackingStateChanged(object sender, bool status)
        {
            if (!status)
            {
                clearExisting = true;
            }
        }

        private void Instance_QRCodeAdded(object sender, QRCodeEventArgs<Microsoft.MixedReality.QR.QRCode> e)
        {
            lock (pendingActions)
            {
                pendingActions.Enqueue(new ActionData(ActionData.Type.Added, e.Data));
            }
        }

        private void Instance_QRCodeUpdated(object sender, QRCodeEventArgs<Microsoft.MixedReality.QR.QRCode> e)
        {
            lock (pendingActions)
            {
                pendingActions.Enqueue(new ActionData(ActionData.Type.Updated, e.Data));
            }
        }

        private void Instance_QRCodeRemoved(object sender, QRCodeEventArgs<Microsoft.MixedReality.QR.QRCode> e)
        {
            lock (pendingActions)
            {
                pendingActions.Enqueue(new ActionData(ActionData.Type.Removed, e.Data));
            }
        }

        private void HandleEvents()
        {
            lock (pendingActions)
            {
                while (pendingActions.Count > 0)
                {
                    var action = pendingActions.Dequeue();
                    if (action.type == ActionData.Type.Added)
                    {
                        GameObject qrCodeObject = Instantiate(qrCodePrefab, new Vector3(0, 0, 0), Quaternion.identity, transform);
                        qrCodeObject.GetComponent<SpatialGraphNodeTracker>().Id = action.qrCode.SpatialGraphNodeId;
                        qrCodeObject.GetComponent<QRCode>().qrCode = action.qrCode;
                        qrCodesObjectsList.Add(action.qrCode.Id, qrCodeObject);
                    }
                    else if (action.type == ActionData.Type.Updated)
                    {
                        if (!qrCodesObjectsList.ContainsKey(action.qrCode.Id))
                        {
                            GameObject qrCodeObject = Instantiate(qrCodePrefab, new Vector3(0, 0, 0), Quaternion.identity, transform);
                            qrCodeObject.GetComponent<SpatialGraphNodeTracker>().Id = action.qrCode.SpatialGraphNodeId;
                            qrCodeObject.GetComponent<QRCode>().qrCode = action.qrCode;
                            qrCodesObjectsList.Add(action.qrCode.Id, qrCodeObject);
                        }
                    }
                    else if (action.type == ActionData.Type.Removed)
                    {
                        if (qrCodesObjectsList.ContainsKey(action.qrCode.Id))
                        {
                            Destroy(qrCodesObjectsList[action.qrCode.Id]);
                            qrCodesObjectsList.Remove(action.qrCode.Id);
                        }
                    }
                }
            }
            if (clearExisting)
            {
                clearExisting = false;
                foreach (var obj in qrCodesObjectsList)
                {
                    Destroy(obj.Value);
                }
                qrCodesObjectsList.Clear();

            }
        }

        // Update is called once per frame
        void Update()
        {
            HandleEvents();
        }

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        // Set Calibration Position and Rotation
        public void SetCalibrationPositionAndRotation(Vector3 position, Vector3 rotation, string direction)
        {
            changeDetected = true;
            changeDirection = direction;
            if (direction == "TopRight")
            {
                CalibratedPositionTopRight = position;
                CalibratedRotationTopRight = rotation;
            }

            if (direction == "BottomRight")
            {
                CalibratedPositionBottomRight = position;
                CalibratedRotationBottomRight = rotation;
            }

            if (direction == "BottomLeft")
            {
                CalibratedPositionBottomLeft = position;
                CalibratedRotationBottomLeft = rotation;
            }

            if (direction == "TopLeft")
            {
                CalibratedPositionTopLeft = position;
                CalibratedRotationTopLeft = rotation;
            }
            Debug.Log("!!!QRVIS: " + direction + ";" + position + ";" + rotation);
        }
    }
}