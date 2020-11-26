using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;

namespace Experimental.CrossDimensionalTransfer
{
    /// <summary>
    /// Clones the visualisation that this script is on when it is grabbed.
    /// </summary>
    public class CloneVisualisationGrab : MonoBehaviour
    {
        [SerializeField]
        private DataVisualisation dataVisualisation;
        [SerializeField]
        private ObjectManipulator objectManipulatorScript;
        // [SerializeField] [Tooltip("If true, removes this script from the cloned visualisation.")]
        // private bool RemoveCloneScriptOnDupe = true;
        
        
        private void Awake()
        {
            if (dataVisualisation == null) dataVisualisation = GetComponent<DataVisualisation>();
            if (objectManipulatorScript == null) objectManipulatorScript = GetComponent<ObjectManipulator>();
            
            objectManipulatorScript.OnManipulationStarted.AddListener(VisualisationGrabbed);
        }

        private void VisualisationGrabbed(ManipulationEventData eventData)
        {
            objectManipulatorScript.ForceEndManipulation();
            
            GameObject go = Instantiate(Resources.Load("DataVisualisation") as GameObject);
            DataVisualisation clonedVis = go.GetComponent<DataVisualisation>();
            
            clonedVis.VisualisationType = dataVisualisation.VisualisationType;
            clonedVis.GeometryType = dataVisualisation.GeometryType;
            clonedVis.XDimension = dataVisualisation.XDimension;
            clonedVis.YDimension = dataVisualisation.YDimension;
            clonedVis.ZDimension = dataVisualisation.ZDimension;
            clonedVis.Colour = dataVisualisation.Colour;
            clonedVis.SizeByDimension = dataVisualisation.SizeByDimension;
            clonedVis.ColourByDimension = dataVisualisation.ColourByDimension;
            clonedVis.Size = dataVisualisation.Size;
            clonedVis.Scale = dataVisualisation.Scale;
            
            go.transform.SetParent((eventData.Pointer as MonoBehaviour).transform);
            go.transform.localPosition = Vector3.zero;
            
            var goPointer = go.AddComponent<PointerHandler>();
            CoreServices.InputSystem.RegisterHandler<IMixedRealityPointerHandler>(goPointer);
            goPointer.OnPointerUp.AddListener((e) =>
            {
               if (e.Pointer is MonoBehaviour monoBehaviourPointer2 && go.transform.parent == monoBehaviourPointer2.transform)
               {
                   go.transform.parent = null;
                   CoreServices.InputSystem.UnregisterHandler<IMixedRealityPointerHandler>(goPointer);
               } 
            });
        }
    }
}
