using IATK;
using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SSVis
{
    public class GestureBrushing : MonoBehaviour
    {
        public BrushingAndLinking brushingAndLinkingScript;
        public float BrushRadius = 0.02f;
        public GameObject BrushMarkerPrefab;

        private bool isTracking = false;
        private GameObject brushMarker;

        private void Start()
        {
            brushMarker = GameObject.Instantiate(BrushMarkerPrefab);
            brushMarker.SetActive(false);
            brushMarker.transform.localScale = Vector3.one * BrushRadius * 2;
        }

        private void Update()
        {
            try
            {
                UpdateBrushingAndLinkingVisualisations();
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
            }

            // First, check if a box selection is being done using the two finger guns
            if (HandInputManager.Instance.IsHandFingerGun(Handedness.Left) && HandInputManager.Instance.IsHandFingerGun(Handedness.Right))
            {
                if (!isTracking)
                {
                    Debug.Log("Brushing: Box Selection Started");
                    isTracking = true;
                    ConfigureBrushingAndLinking(Handedness.Both);
                    UpdateBrushingAndLinkingVisualisations();
                }
            }
            // Second, check if a spherical selection is being done by holding the fingers together
            else
            {
                int leftTouch = HandInputManager.Instance.GetNumTouchingFingers(Handedness.Left);
                int rightTouch = HandInputManager.Instance.GetNumTouchingFingers(Handedness.Right);
                if (leftTouch > 2)
                {
                    if (!isTracking)
                    {
                        isTracking = true;
                        ConfigureBrushingAndLinking(Handedness.Left);
                        UpdateBrushingAndLinkingVisualisations();
                    }

                    switch (leftTouch)
                    {
                        case 3:
                            Debug.Log("Brushing: Left Add Start");
                            brushingAndLinkingScript.SELECTION_TYPE = BrushingAndLinking.SelectionType.ADD;
                            break;
                        case 4:
                            Debug.Log("Brushing: Left Subtract Start");
                            brushingAndLinkingScript.SELECTION_TYPE = BrushingAndLinking.SelectionType.SUBTRACT;
                            break;
                        case 5:
                            Debug.Log("Brushing: Left Free Start");
                            brushingAndLinkingScript.SELECTION_TYPE = BrushingAndLinking.SelectionType.FREE;
                            break;
                    }
                }
                else if (rightTouch > 2)
                {
                    if (!isTracking)
                    {
                        isTracking = true;
                        ConfigureBrushingAndLinking(Handedness.Right);
                        UpdateBrushingAndLinkingVisualisations();
                    }

                    switch (rightTouch)
                    {
                        case 3:
                            Debug.Log("Brushing: Right Add Start");
                            brushingAndLinkingScript.SELECTION_TYPE = BrushingAndLinking.SelectionType.ADD;
                            break;
                        case 4:
                            Debug.Log("Brushing: Right Subtract Start");
                            brushingAndLinkingScript.SELECTION_TYPE = BrushingAndLinking.SelectionType.SUBTRACT;
                            break;
                        case 5:
                            Debug.Log("Brushing: Right Free Start");
                            brushingAndLinkingScript.SELECTION_TYPE = BrushingAndLinking.SelectionType.FREE;
                            break;
                    }
                }
                // If no conditions are met, disable the brushing
                else
                {
                    if (isTracking)
                    {
                        Debug.Log("Brushing: End");

                        isTracking = false;
                        brushingAndLinkingScript.input1 = null;
                        brushingAndLinkingScript.input2 = null;
                        brushingAndLinkingScript.isBrushing = false;
                        brushingAndLinkingScript.showBrush = false;
                        brushMarker.SetActive(false);
                    }
                }
            }
        }

        private void ConfigureBrushingAndLinking(Handedness handedness)
        {
            if (handedness == Handedness.Both)
            {
                Transform leftPointer = HandInputManager.Instance.GetJointTransform(Handedness.Left, TrackedHandJoint.IndexKnuckle);
                Transform rightPointer = HandInputManager.Instance.GetJointTransform(Handedness.Right, TrackedHandJoint.IndexKnuckle);

                brushingAndLinkingScript.BRUSH_TYPE = BrushingAndLinking.BrushType.BOXSCREEN;
                brushingAndLinkingScript.SELECTION_TYPE = BrushingAndLinking.SelectionType.FREE;
                brushingAndLinkingScript.input1 = leftPointer;
                brushingAndLinkingScript.input2 = rightPointer;
                brushingAndLinkingScript.isBrushing = true;
                brushingAndLinkingScript.showBrush = true;
            }
            else
            {
                Transform pointer = HandInputManager.Instance.GetJointTransform(handedness, TrackedHandJoint.IndexTip);

                brushMarker.transform.SetParent(pointer);
                brushMarker.transform.localPosition = Vector3.zero;
                brushMarker.SetActive(true);

                brushingAndLinkingScript.BRUSH_TYPE = BrushingAndLinking.BrushType.SPHERE;
                brushingAndLinkingScript.SELECTION_TYPE = BrushingAndLinking.SelectionType.FREE;
                brushingAndLinkingScript.input1 = brushMarker.transform;
                brushingAndLinkingScript.input2 = brushMarker.transform;
                brushingAndLinkingScript.isBrushing = true;
                brushingAndLinkingScript.showBrush = true;
                brushingAndLinkingScript.brushRadius = BrushRadius;
            }
        }

        private void UpdateBrushingAndLinkingVisualisations()
        {
            var visualisations = GameObject.FindObjectsOfType<Visualisation>().ToList();

            for (int i = visualisations.Count - 1; i >= 0; i--)
            {
                if (visualisations[i].dataSource != null)
                {
                    //if (((CSVDataSource)visualisations[i].dataSource).data.name != "auto-mpg")
                    if (((CSVDataSource)visualisations[i].dataSource).data.name != ((CSVDataSource)DataVisualisationManager.Instance.DataSource).data.name)
                    {
                        visualisations.RemoveAt(i);
                    }
                }
            }
            brushingAndLinkingScript.brushingVisualisations = visualisations.ToList();

            var linkingVisualisations = GameObject.FindObjectsOfType<LinkingVisualisations>();
            brushingAndLinkingScript.brushedLinkingVisualisations = linkingVisualisations.ToList();
        }
    }
}
