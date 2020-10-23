using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;

namespace Experimental.CrossDimensionalTransfer
{
    public class HandInputManager : MonoBehaviour, IMixedRealitySourceStateHandler, IMixedRealityHandJointHandler
    {
        public static HandInputManager Instance { get; private set; }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
            
            CoreServices.InputSystem?.RegisterHandler<IMixedRealitySourceStateHandler>(this);
            CoreServices.InputSystem?.RegisterHandler<IMixedRealityHandJointHandler>(this);
        }
        
        public bool IsFingerClosed(Handedness handedness, TrackedHandJoint handJoint)
        {
            var handJointService = CoreServices.GetInputSystemDataProvider<IMixedRealityHandJointService>();
            float distance = Mathf.Infinity;
            
            switch (handJoint)
            {
                case TrackedHandJoint.IndexTip:
                    var indexTip = handJointService.RequestJointTransform(TrackedHandJoint.IndexTip, handedness);
                    var indexKnuckle = handJointService.RequestJointTransform(TrackedHandJoint.IndexKnuckle, handedness);
                    distance = Vector3.Distance(indexTip.position, indexKnuckle.position);
                    break;
                
                case TrackedHandJoint.MiddleTip:
                    var middleTip = handJointService.RequestJointTransform(TrackedHandJoint.MiddleTip, handedness);
                    var middleKnuckle = handJointService.RequestJointTransform(TrackedHandJoint.MiddleKnuckle, handedness);
                    distance = Vector3.Distance(middleTip.position, middleKnuckle.position);
                    break;
                
                case TrackedHandJoint.RingTip:
                    var ringTip = handJointService.RequestJointTransform(TrackedHandJoint.RingTip, handedness);
                    var ringKnuckle = handJointService.RequestJointTransform(TrackedHandJoint.RingKnuckle, handedness);
                    distance = Vector3.Distance(ringTip.position, ringKnuckle.position);
                    break;
                
                case TrackedHandJoint.PinkyTip:
                    var pinkyTip = handJointService.RequestJointTransform(TrackedHandJoint.PinkyTip, handedness);
                    var pinkyKnuckle = handJointService.RequestJointTransform(TrackedHandJoint.PinkyKnuckle, handedness);
                    distance = Vector3.Distance(pinkyTip.position, pinkyKnuckle.position);
                    break;
            }
            
            Debug.Log(distance);
            return (distance < 0.04f);
        }

        public void OnSourceDetected(SourceStateEventData eventData)
        {
            //
        }

        public void OnSourceLost(SourceStateEventData eventData)
        {
            //
        }

        public void OnHandJointsUpdated(InputEventData<IDictionary<TrackedHandJoint, MixedRealityPose>> eventData)
        {
            //
        }
    }
}
