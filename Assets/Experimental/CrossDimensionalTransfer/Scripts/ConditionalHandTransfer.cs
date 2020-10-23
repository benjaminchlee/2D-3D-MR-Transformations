using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;

namespace Experimental.CrossDimensionalTransfer
{
    public class ConditionalHandTransfer : MonoBehaviour
    {
        [SerializeField]
        private DataVisualisation dataVisualisationScript;
        [SerializeField]
        private ObjectManipulator objectManipulatorScript;
        [SerializeField]
        private PlaceableObject placeableObjectScript;
        
        private void Awake()
        {
            if (dataVisualisationScript == null) GetComponentInChildren<DataVisualisation>();
            if (objectManipulatorScript == null) GetComponent<ObjectManipulator>();
            if (placeableObjectScript == null) GetComponent<PlaceableObject>();
            
            objectManipulatorScript.OnManipulationStarted.AddListener(VisualisationGrabbed);
        }

        private void VisualisationGrabbed(ManipulationEventData eventData)
        {
            if (placeableObjectScript.IsPlacedOnSurface)
            {
                // Check for user "gesture" input
                var hand = (PointerUtils.GetPointer<IMixedRealityPointer>(Handedness.Left) == eventData.Pointer) ? Handedness.Left : Handedness.Right;
                
                // If the middle finger is closed, create a duplicate of this object which gets left behind
                if (HandInputManager.Instance.IsFingerClosed(hand, TrackedHandJoint.MiddleTip))
                {
                    GameObject dupe = Instantiate(gameObject, transform.position, transform.rotation);
                    
                    // Transfer visualisation properties
                    DataVisualisation vis = dupe.GetComponent<DataVisualisation>();
                    
                    vis.DataSource = dataVisualisationScript.DataSource;
                    vis.GeometryType = dataVisualisationScript.GeometryType;
                    vis.XDimension = dataVisualisationScript.XDimension;
                    vis.YDimension = dataVisualisationScript.YDimension;
                    vis.ZDimension = dataVisualisationScript.ZDimension;
                    vis.Scale = dataVisualisationScript.Scale;
                    vis.Size = dataVisualisationScript.Size;
                    vis.ColourByDimension = dataVisualisationScript.ColourByDimension;
                    vis.ColourByGradient = dataVisualisationScript.ColourByGradient;
                }
                // If the ring finger is closed, create a duplicate of this object with only dimension settings
                else if (!HandInputManager.Instance.IsFingerClosed(hand, TrackedHandJoint.RingTip))
                {
                    GameObject dupe = Instantiate(gameObject, transform.position, transform.rotation);
                    
                    // Transfer visualisation properties
                    DataVisualisation vis = dupe.GetComponent<DataVisualisation>();
                    
                    vis.DataSource = dataVisualisationScript.DataSource;
                    vis.GeometryType = dataVisualisationScript.GeometryType;
                    vis.XDimension = dataVisualisationScript.XDimension;
                    vis.YDimension = dataVisualisationScript.YDimension;
                    vis.ZDimension = dataVisualisationScript.ZDimension;
                    vis.Scale = dataVisualisationScript.Scale;
                    vis.Size = dataVisualisationScript.Size;
                    dataVisualisationScript.ColourByDimension = "Undefined";
                }
            }
        }
    }    
}