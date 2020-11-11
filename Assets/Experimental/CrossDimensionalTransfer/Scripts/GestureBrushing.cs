using IATK;
using Microsoft.MixedReality.Toolkit.Utilities;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Experimental.CrossDimensionalTransfer
{
    public class GestureBrushing : MonoBehaviour
    {
        public BrushingAndLinking brushingAndLinkingScript;
        public float BrushRadius = 0.025f;
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
            if (HandInputManager.Instance.IsHandClosed(Handedness.Left))
            {
                if (!isTracking)
                {
                    Debug.Log("Brushing: Start");
                    
                    isTracking = true;
                    ConfigureBrushingAndLinking(Handedness.Left);
                    UpdateBrushingAndLinkingVisualisations();
                }
            }
            else if (HandInputManager.Instance.IsHandClosed(Handedness.Right))
            {
                if (!isTracking)
                {
                    Debug.Log("Brushing: Start");
                    
                    isTracking = true;
                    ConfigureBrushingAndLinking(Handedness.Right);
                    UpdateBrushingAndLinkingVisualisations();
                }
            }
            else
            {
                if (isTracking)
                {
                    Debug.Log("Brushing: End");
                        
                    isTracking = false;
                    brushingAndLinkingScript.input1 = null;
                    brushingAndLinkingScript.input2 = null;
                    brushingAndLinkingScript.enabled = false;
                }
            }
        }
        
        private void ConfigureBrushingAndLinking(Handedness handedness)
        {
            Transform pointer = HandInputManager.Instance.GetJointTransform(handedness, TrackedHandJoint.IndexTip);
                        
            brushingAndLinkingScript.enabled = true;
            brushingAndLinkingScript.input1 = pointer;
            brushingAndLinkingScript.input2 = pointer;
            brushingAndLinkingScript.SELECTION_TYPE = BrushingAndLinking.SelectionType.FREE;
            brushingAndLinkingScript.isBrushing = true;
            brushingAndLinkingScript.brushRadius = BrushRadius;
            
            brushMarker.transform.SetParent(pointer);
            brushMarker.transform.localPosition = Vector3.zero;
        }
        
        private void UpdateBrushingAndLinkingVisualisations()
        {
            var visualisations = GameObject.FindObjectsOfType<Visualisation>();
            var linkingVisualisations = GameObject.FindObjectsOfType<LinkingVisualisations>();
            
            brushingAndLinkingScript.brushingVisualisations = visualisations.ToList();
            brushingAndLinkingScript.brushedLinkingVisualisations = linkingVisualisations.ToList();
        }
    }
}
