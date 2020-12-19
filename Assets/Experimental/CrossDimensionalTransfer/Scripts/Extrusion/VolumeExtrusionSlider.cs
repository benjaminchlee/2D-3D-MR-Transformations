using System.Collections;
using System.Collections.Generic;
using IATK;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;

namespace Experimental.CrossDimensionalTransfer
{
    public class VolumeExtrusionSlider : MonoBehaviour, IMixedRealityPointerHandler
    {
        public DataVisualisation ExtrudingVisualisation;

        private bool isExtruding = false;
        private IMixedRealityHand extrudingLeftHand;
        private IMixedRealityHand extrudingRightHand;
        private Vector3 extrusionPointLeft;
        private Quaternion extrusionRotationLeft;
        private Vector3 extrusionPointRight;
        private Quaternion extrusionRotationRight;
        private float distance;

        public void OnPointerDown(MixedRealityPointerEventData eventData)
        {
            isExtruding = true;

            var hand = eventData.Pointer.Controller as IMixedRealityHand;
            if (hand.TryGetJoint(TrackedHandJoint.IndexTip, out MixedRealityPose jointPose))
            {
                if (hand.ControllerHandedness == Handedness.Left)
                {
                    extrudingLeftHand = hand;
                    extrusionPointLeft = ExtrudingVisualisation.Visualisation.transform.InverseTransformPoint(jointPose.Position);
                    extrusionRotationLeft = jointPose.Rotation;
                }
                else
                {
                    extrudingRightHand = hand;
                    extrusionPointRight = ExtrudingVisualisation.Visualisation.transform.InverseTransformPoint(jointPose.Position);
                    extrusionRotationRight = jointPose.Rotation;
                }
            }

            UpdateExtrusion();
        }

        public void OnPointerDragged(MixedRealityPointerEventData eventData)
        {
            UpdateExtrusion();
        }

        public void OnPointerUp(MixedRealityPointerEventData eventData)
        {
            UpdateExtrusion();

            var hand = eventData.Pointer.Controller as IMixedRealityHand;

            if (hand.ControllerHandedness == Handedness.Left)
                extrudingLeftHand = null;
            else
                extrudingRightHand = null;

            if (extrudingLeftHand == null && extrudingRightHand == null)
            {
                isExtruding = false;
                if (Mathf.Abs(distance) < 0.075f)
                {
                    ExtrudingVisualisation.ExtrudeDimension(AbstractVisualisation.PropertyType.Z, 0, extrusionPointLeft, extrusionRotationLeft);
                }
            }
        }

        public void OnPointerClicked(MixedRealityPointerEventData eventData) { }

        private void UpdateExtrusion()
        {
            if (isExtruding)
            {
                if (extrudingLeftHand != null && extrudingRightHand == null)
                {
                    if (extrudingLeftHand.TryGetJoint(TrackedHandJoint.IndexTip, out MixedRealityPose jointPose))
                    {
                        distance = ExtrudingVisualisation.Visualisation.transform.InverseTransformPoint(jointPose.Position).z * 4;
                        ExtrudingVisualisation.ExtrudeDimension(AbstractVisualisation.PropertyType.Z, distance, extrusionPointLeft, extrusionRotationLeft);
                    }
                }
                else if (extrudingLeftHand == null && extrudingRightHand != null)
                {
                    if (extrudingRightHand.TryGetJoint(TrackedHandJoint.IndexTip, out MixedRealityPose jointPose))
                    {
                        distance = ExtrudingVisualisation.Visualisation.transform.InverseTransformPoint(jointPose.Position).z * 4;
                        ExtrudingVisualisation.ExtrudeDimension(AbstractVisualisation.PropertyType.Z, distance, extrusionPointRight, extrusionRotationRight);
                    }
                }
                else
                {
                    if (extrudingLeftHand.TryGetJoint(TrackedHandJoint.IndexTip, out MixedRealityPose jointPoseLeft))
                    {
                        float distanceLeft = ExtrudingVisualisation.Visualisation.transform.InverseTransformPoint(jointPoseLeft.Position).z * 4;

                        if (extrudingRightHand.TryGetJoint(TrackedHandJoint.IndexTip, out MixedRealityPose jointPoseRight))
                        {
                            float distanceRight = ExtrudingVisualisation.Visualisation.transform.InverseTransformPoint(jointPoseRight.Position).z * 4;
                            distance = Mathf.Max(distanceLeft, distanceRight);
                            ExtrudingVisualisation.ExtrudeDimension(AbstractVisualisation.PropertyType.Z, distance, extrusionPointLeft, extrusionRotationLeft, extrusionPointRight, extrusionRotationRight);
                        }
                    }
                }

            }
        }
    }
}