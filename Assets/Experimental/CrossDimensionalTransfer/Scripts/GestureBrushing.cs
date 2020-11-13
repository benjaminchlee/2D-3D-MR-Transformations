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
            try
            {
                UpdateBrushingAndLinkingVisualisations();
            }
            catch
            {
                
            }
            
            int leftTouch = HandInputManager.Instance.GetNumTouchingFingers(Handedness.Left);
            int rightTouch = HandInputManager.Instance.GetNumTouchingFingers(Handedness.Right);
            if (leftTouch > 2)
            {
                if (!isTracking)
                {
                    Debug.Log("Brushing: Start");
                    
                    isTracking = true;
                    ConfigureBrushingAndLinking(Handedness.Left);
                    UpdateBrushingAndLinkingVisualisations();
                    
                    switch (leftTouch)
                    {
                        case 3:
                            brushingAndLinkingScript.SELECTION_TYPE = BrushingAndLinking.SelectionType.ADD;
                            break;
                        case 4:
                            brushingAndLinkingScript.SELECTION_TYPE = BrushingAndLinking.SelectionType.SUBTRACT;
                            break;
                        case 5:
                            brushingAndLinkingScript.SELECTION_TYPE = BrushingAndLinking.SelectionType.FREE;
                            break;
                    }
                }
            }
            else if (rightTouch > 2)
            {
                if (!isTracking)
                {
                    Debug.Log("Brushing: Start");
                    
                    isTracking = true;
                    ConfigureBrushingAndLinking(Handedness.Right);
                    UpdateBrushingAndLinkingVisualisations();
                    
                    switch (rightTouch)
                    {
                        case 3:
                            brushingAndLinkingScript.SELECTION_TYPE = BrushingAndLinking.SelectionType.ADD;
                            break;
                        case 4:
                            brushingAndLinkingScript.SELECTION_TYPE = BrushingAndLinking.SelectionType.SUBTRACT;
                            break;
                        case 5:
                            brushingAndLinkingScript.SELECTION_TYPE = BrushingAndLinking.SelectionType.FREE;
                            break;
                    }
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
            brushingAndLinkingScript.isBrushing = true;
            brushingAndLinkingScript.brushRadius = BrushRadius;
            
            brushMarker.transform.SetParent(pointer);
            brushMarker.transform.localPosition = Vector3.zero;
        }
        
        private void UpdateBrushingAndLinkingVisualisations()
        {
            var visualisations = GameObject.FindObjectsOfType<Visualisation>().ToList();
            
            for (int i = visualisations.Count - 1; i >= 0; i--)
            {
                if (((CSVDataSource)visualisations[i].dataSource).data.name != "auto-mpg")
                {
                    visualisations.RemoveAt(i);
                }
            }
            brushingAndLinkingScript.brushingVisualisations = visualisations.ToList();
            
            var linkingVisualisations = GameObject.FindObjectsOfType<LinkingVisualisations>();
            brushingAndLinkingScript.brushedLinkingVisualisations = linkingVisualisations.ToList();
        }
    }
}
