using DG.Tweening;
using IATK;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Experimental.CrossDimensionalTransfer
{
    public class ExtrusionSBV : MonoBehaviour
    {
        public DataVisualisation ExtrudingVisualisation;
        public ObjectManipulator ExtrusionSliderHandle;
        public bool SliderPersists = false;
        public float SliderResetDistance = 0.1f;
        
        private bool isExtruding = false;
        private Vector3 extrusionPoint;
        private Quaternion extrusionRotation;
        
        private void Start()
        {
            ExtrusionSliderHandle.OnManipulationStarted.AddListener(ExtrusionSliderGrabbed);
            ExtrusionSliderHandle.OnManipulationEnded.AddListener(ExtrusionSliderReleased);
        }

        private void Update()
        {
            if (isExtruding)
            {
                float distance = ExtrusionSliderHandle.transform.localPosition.z * 4;
                ExtrudingVisualisation.ExtrudeDimension(AbstractVisualisation.PropertyType.Z, distance, extrusionPoint, extrusionRotation);
            }
        }

        private void ExtrusionSliderGrabbed(ManipulationEventData arg0)
        {
            isExtruding = true;
            
            var hand = arg0.Pointer.Controller as IMixedRealityHand;
            if (hand.TryGetJoint(TrackedHandJoint.IndexTip, out MixedRealityPose jointPose))
            {
                extrusionPoint = ExtrudingVisualisation.Visualisation.transform.InverseTransformPoint(jointPose.Position);
                extrusionRotation = jointPose.Rotation;
            }
        }

        private void ExtrusionSliderReleased(ManipulationEventData arg0)
        {
            float distance = ExtrusionSliderHandle.transform.localPosition.z * 4;
            if (!SliderPersists || Mathf.Abs(distance) < SliderResetDistance)
            {
                ExtrusionSliderHandle.transform.DOLocalMoveZ(0, 0.1f).OnComplete(() =>
                {
                    isExtruding = false;
                    ExtrudingVisualisation.ExtrudeDimension(AbstractVisualisation.PropertyType.Z, 0, extrusionPoint, extrusionRotation);
                }
                );
            }
        }
    }
}