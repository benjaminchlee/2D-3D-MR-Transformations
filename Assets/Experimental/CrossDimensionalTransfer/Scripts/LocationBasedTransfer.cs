using IATK;
using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;

namespace Experimental.CrossDimensionalTransfer
{
    public class LocationBasedTransfer : MonoBehaviour
    {
        public string XDimension;
        public string YDimension;
        public string ZDimension;
        public string ColourDimension;
        public string SizeDimension;
        
        [SerializeField]
        private ObjectManipulator objectManipulatorScript;
        
        private void Start()
        {
            if (objectManipulatorScript == null) objectManipulatorScript = GetComponent<ObjectManipulator>();
            
            objectManipulatorScript.OnManipulationStarted.AddListener(HandldeGrabbed);
        }

        private void HandldeGrabbed(ManipulationEventData eventData)
        {
            objectManipulatorScript.ForceEndManipulation();
            
            GameObject go = Instantiate(Resources.Load("DataVisualisation") as GameObject);
            DataVisualisation vis = go.GetComponent<DataVisualisation>();

            go.GetComponentInChildren<Visualisation>().dataSource = DataVisualisationManager.Instance.DataSource;
            go.GetComponentInChildren<Visualisation>().xDimension = "Undefined";
            go.GetComponentInChildren<Visualisation>().yDimension = "Undefined";
            go.GetComponentInChildren<Visualisation>().zDimension = "Undefined";
            
            vis.XDimension = (XDimension == "") ? "Undefined" : XDimension;
            vis.YDimension = (YDimension == "") ? "Undefined" : YDimension;
            vis.ZDimension = (ZDimension == "") ? "Undefined" : ZDimension;
            vis.ColourByDimension = (ColourDimension == "") ? "Undefined" : ColourDimension;
            vis.SizeByDimension = (SizeDimension == "") ? "Undefined" : SizeDimension;
                        
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
            
            vis.Size = 0.1f;
            vis.Scale = new Vector3(0.2f, 0.2f, 0.2f);
        }
    }
}