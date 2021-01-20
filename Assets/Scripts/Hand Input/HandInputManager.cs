using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;

namespace SSVis
{
    public class HandInputManager : MonoBehaviour, IMixedRealitySourceStateHandler, IMixedRealityHandJointHandler
    {
        public static HandInputManager Instance { get; private set; }

        IMixedRealityHandJointService handJointService;

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
            handJointService = CoreServices.GetInputSystemDataProvider<IMixedRealityHandJointService>();
        }

        public bool IsHandTracked(Handedness handedness)
        {
            return handJointService.IsHandTracked(handedness);
        }

        public bool IsFingerClosed(Handedness handedness, TrackedHandJoint handJoint)
        {
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

            return (distance < 0.04f);
        }

        public int GetNumTouchingFingers(Handedness handedness)
        {
            if (!handJointService.IsHandTracked(handedness))
                return 0;

            float closedDistance = 0.03f;

            var thumbTip = handJointService.RequestJointTransform(TrackedHandJoint.ThumbTip, handedness);
            var indexTip = handJointService.RequestJointTransform(TrackedHandJoint.IndexTip, handedness);
            var middleTip = handJointService.RequestJointTransform(TrackedHandJoint.MiddleTip, handedness);
            var ringTip = handJointService.RequestJointTransform(TrackedHandJoint.RingTip, handedness);
            var pinkyTip = handJointService.RequestJointTransform(TrackedHandJoint.PinkyTip, handedness);

            if (Vector3.Distance(thumbTip.position, indexTip.position) > closedDistance)
                return 0;

            if (Vector3.Distance(thumbTip.position, middleTip.position) > closedDistance)
                return 2;

            if (Vector3.Distance(thumbTip.position, ringTip.position) > closedDistance)
                return 3;

            if (Vector3.Distance(thumbTip.position, pinkyTip.position) > closedDistance)
                return 4;

            return 5;
        }

        public bool IsPalmOpen(Handedness handedness)
        {
            if (!handJointService.IsHandTracked(handedness))
                return false;

            // ! Based on the HandConstraintPalmUp.cs script !

            // Check if the triangle's normal formed from the palm, to index, to ring finger tip roughly matches the palm normal
            IMixedRealityHand jointedHand = (IMixedRealityHand)GetController(handedness);
            if (jointedHand.TryGetJoint(TrackedHandJoint.Palm, out MixedRealityPose palmPose))
            {
                if (jointedHand.TryGetJoint(TrackedHandJoint.IndexTip, out MixedRealityPose indexTipPose) &&
                    jointedHand.TryGetJoint(TrackedHandJoint.RingTip, out MixedRealityPose ringTipPose))
                {
                    var handNormal = Vector3.Cross(indexTipPose.Position - palmPose.Position,
                                                    ringTipPose.Position - indexTipPose.Position).normalized;
                    handNormal *= (jointedHand.ControllerHandedness == Handedness.Right) ? 1.0f : -1.0f;

                    if (Vector3.Angle(palmPose.Up, handNormal) > 45)
                    {
                        return false;
                    }
                };
            }

            return false;
        }

        public bool IsPalmOpen(IMixedRealityHand jointedHand)
        {
            // ! Based on the HandConstraintPalmUp.cs script !

            // Check if the triangle's normal formed from the palm, to index, to ring finger tip roughly matches the palm normal
            if (jointedHand.TryGetJoint(TrackedHandJoint.Palm, out MixedRealityPose palmPose))
            {
                if (jointedHand.TryGetJoint(TrackedHandJoint.IndexTip, out MixedRealityPose indexTipPose) &&
                    jointedHand.TryGetJoint(TrackedHandJoint.RingTip, out MixedRealityPose ringTipPose))
                {
                    var handNormal = Vector3.Cross(indexTipPose.Position - palmPose.Position,
                                                    ringTipPose.Position - indexTipPose.Position).normalized;
                    handNormal *= (jointedHand.ControllerHandedness == Handedness.Right) ? 1.0f : -1.0f;

                    if (Vector3.Angle(palmPose.Up, handNormal) > 45)
                    {
                        return false;
                    }
                };
            }

            return true;
        }

        public bool IsHandFingerGun(Handedness handedness)
        {
            if (!handJointService.IsHandTracked(handedness))
                return false;

            // Check if thumb tip and index tip form a right angle with the index knuckle
            var thumbTip = handJointService.RequestJointTransform(TrackedHandJoint.ThumbTip, handedness);
            var indexTip = handJointService.RequestJointTransform(TrackedHandJoint.IndexTip, handedness);
            var indexKnuckle = handJointService.RequestJointTransform(TrackedHandJoint.IndexKnuckle, handedness);
            var middleTip = handJointService.RequestJointTransform(TrackedHandJoint.MiddleTip, handedness);
            float angle = Vector3.Angle((thumbTip.position - indexKnuckle.position), (indexTip.position - indexKnuckle.position));
            if (75 < angle && angle < 105)
            {
                // Check if the index tip is not curled up next to the index middle
                if (Vector3.Distance(indexTip.position, indexKnuckle.position) > 0.035f)
                {
                    // Make sure the middle, ring, and pinky fingers are close to the palm
                    var ringTip = handJointService.RequestJointTransform(TrackedHandJoint.RingTip, handedness);
                    var pinkyTip = handJointService.RequestJointTransform(TrackedHandJoint.PinkyTip, handedness);
                    var palm = handJointService.RequestJointTransform(TrackedHandJoint.Palm, handedness);
                    if (Vector3.Distance(middleTip.position, palm.position) < 0.055f &&
                        Vector3.Distance(ringTip.position, palm.position) < 0.04f &&
                        Vector3.Distance(pinkyTip.position, palm.position) < 0.04f
                    )
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public bool IsHandIndexPointing(Handedness handedness)
        {
            if (!handJointService.IsHandTracked(handedness))
                return false;

            // Check if thumb tip OR the thumb distal is next to the middle finger's midlde joint
            var thumbTip = handJointService.RequestJointTransform(TrackedHandJoint.ThumbTip, handedness);
            var thumbDistal = handJointService.RequestJointTransform(TrackedHandJoint.ThumbDistalJoint, handedness);
            var middleMiddle = handJointService.RequestJointTransform(TrackedHandJoint.MiddleMiddleJoint, handedness);
            if (Vector3.Distance(thumbTip.position, middleMiddle.position) < 0.02f || Vector3.Distance(thumbDistal.position, middleMiddle.position) < 0.02f)
            {
                // Check if the index finger is fully extended in a straight line
                var indexTip = handJointService.RequestJointTransform(TrackedHandJoint.IndexTip, handedness);
                var indexDistal = handJointService.RequestJointTransform(TrackedHandJoint.IndexDistalJoint, handedness);
                var indexMiddle = handJointService.RequestJointTransform(TrackedHandJoint.IndexMiddleJoint, handedness);
                var indexKnuckle = handJointService.RequestJointTransform(TrackedHandJoint.IndexKnuckle, handedness);
                if (Vector3.Dot((indexTip.position - indexDistal.position).normalized, (indexMiddle.position - indexKnuckle.position).normalized) > 0.9f)
                {
                    return true;
                }
            }

            return false;
        }

        public Transform GetJointTransform(Handedness handedness, TrackedHandJoint trackedHandJoint)
        {
            if (!handJointService.IsHandTracked(handedness))
                return null;

            return handJointService.RequestJointTransform(trackedHandJoint, handedness);
        }

        public static IMixedRealityController GetController(Handedness handedness)
        {
            foreach (IMixedRealityController c in CoreServices.InputSystem.DetectedControllers)
            {
                if (c.ControllerHandedness.IsMatch(handedness))
                {
                    return c;
                }
            }
            return null;
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
