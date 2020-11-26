using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.Input;
using UnityEngine;

namespace Experimental.CrossDimensionalTransfer
{
    public class HueTouch : MonoBehaviour, IMixedRealityTouchHandler
    {
        public GameObject Panel;
        public GameObject Picker;
        
        public void OnTouchStarted(HandTrackingInputEventData eventData)
        {
            //
        }

        public void OnTouchUpdated(HandTrackingInputEventData eventData)
        {
            Vector3 position = eventData.InputData;
            Vector3 localPos = Panel.transform.InverseTransformPoint(position);
            localPos.z = 0;
            
            Picker.transform.localPosition = localPos;
        }
        
        public void OnTouchCompleted(HandTrackingInputEventData eventData)
        {
            //
        }
    }
}