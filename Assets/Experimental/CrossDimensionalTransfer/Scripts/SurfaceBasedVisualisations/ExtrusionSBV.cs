using DG.Tweening;
using IATK;
using Microsoft.MixedReality.Toolkit;
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
        public bool CloneOnMaxDistance = false;

        private bool isExtruding = false;
        private Vector3 extrusionPoint;
        private Quaternion extrusionRotation;
        private IMixedRealityPointer manipulationPointer;
        private bool isCloning = false;

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
                if (CloneOnMaxDistance && Mathf.Abs(distance) > 1 && !isCloning)
                {
                    isCloning = true;
                    CloneVisualisation();
                }
                else
                {
                    ExtrudingVisualisation.ExtrudeDimension(AbstractVisualisation.PropertyType.Z, distance, extrusionPoint, extrusionRotation);
                }
            }
        }

        private void ExtrusionSliderGrabbed(ManipulationEventData arg0)
        {
            isExtruding = true;

            manipulationPointer = arg0.Pointer;
            var hand = manipulationPointer.Controller as IMixedRealityHand;
            if (hand.TryGetJoint(TrackedHandJoint.IndexTip, out MixedRealityPose jointPose))
            {
                extrusionPoint = ExtrudingVisualisation.Visualisation.transform.InverseTransformPoint(jointPose.Position);
                extrusionRotation = jointPose.Rotation;
            }
        }

        private void ExtrusionSliderReleased(ManipulationEventData arg0)
        {
            float distance = ExtrusionSliderHandle.transform.localPosition.z * 4;
            if (!SliderPersists || Mathf.Abs(distance) < SliderResetDistance || isCloning)
            {
                ExtrusionSliderHandle.transform.DOLocalMoveZ(0, 0.1f).OnComplete(() =>
                {
                    isExtruding = false;
                    isCloning = false;
                    ExtrudingVisualisation.ExtrudeDimension(AbstractVisualisation.PropertyType.Z, 0, extrusionPoint, extrusionRotation);
                }
                );
            }
        }

        private void CloneVisualisation()
        {
            ExtrusionSliderHandle.ForceEndManipulation();

            GameObject go = Instantiate(Resources.Load("DataVisualisation") as GameObject);
            DataVisualisation clonedVis = go.GetComponent<DataVisualisation>();

            clonedVis.DataSource = ExtrudingVisualisation.DataSource;
            clonedVis.VisualisationType = ExtrudingVisualisation.VisualisationType;
            clonedVis.GeometryType = ExtrudingVisualisation.GeometryType;
            clonedVis.XDimension = ExtrudingVisualisation.XDimension;
            clonedVis.YDimension = ExtrudingVisualisation.YDimension;
            clonedVis.ZDimension = ExtrudingVisualisation.ZDimension;
            clonedVis.Colour = ExtrudingVisualisation.Colour;
            clonedVis.SizeByDimension = ExtrudingVisualisation.SizeByDimension;
            clonedVis.ColourByDimension = ExtrudingVisualisation.ColourByDimension;
            clonedVis.Size = ExtrudingVisualisation.Size;
            clonedVis.Scale = ExtrudingVisualisation.Scale;

            go.transform.SetParent((manipulationPointer as MonoBehaviour).transform);
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