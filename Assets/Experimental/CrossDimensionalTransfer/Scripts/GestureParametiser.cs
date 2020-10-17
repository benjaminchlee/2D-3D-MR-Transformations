using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;

namespace Experimental.CrossDimensionalTransfer
{
    public class GestureParametiser : MonoBehaviour, IMixedRealitySourceStateHandler, IMixedRealityHandJointHandler
    {
        private GameObject indexPoint;
        
        private void Start()
        {
            CoreServices.InputSystem?.RegisterHandler<IMixedRealitySourceStateHandler>(this);
            CoreServices.InputSystem?.RegisterHandler<IMixedRealityHandJointHandler>(this);
            
            indexPoint = GameObject.CreatePrimitive(PrimitiveType.Cube);
            indexPoint.transform.localScale = Vector3.one * 0.05f;
            
        }
        
        void IMixedRealitySourceStateHandler.OnSourceDetected(SourceStateEventData eventData)
        {
            var hand = eventData.Controller as IMixedRealityHand;
            var handJointService = CoreServices.GetInputSystemDataProvider<IMixedRealityHandJointService>();
            
                Debug.Log("123");
            if (handJointService != null)
            {
                Debug.Log("asd");
                Transform jointTransform = handJointService.RequestJointTransform(TrackedHandJoint.IndexTip, hand.ControllerHandedness);
                indexPoint.transform.SetParent(jointTransform);
                indexPoint.transform.localPosition = Vector3.zero;
            }
            
        }

        void IMixedRealitySourceStateHandler.OnSourceLost(SourceStateEventData eventData)
        {
            //
        }

        void IMixedRealityHandJointHandler.OnHandJointsUpdated(InputEventData<IDictionary<TrackedHandJoint, MixedRealityPose>> eventData)
        {
            //
        }
    }
}
