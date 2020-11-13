using DG.Tweening;
using IATK;
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
        
        private bool isExtruding = false;
        private Vector3 extrusionPoint;
        
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
                ExtrudingVisualisation.ExtrudeDimension(AbstractVisualisation.PropertyType.Z, extrusionPoint, distance);
            }
        }

        private void ExtrusionSliderGrabbed(ManipulationEventData arg0)
        {
            isExtruding = true;
            
            extrusionPoint = ExtrudingVisualisation.Visualisation.transform.InverseTransformPoint(arg0.Pointer.Position);
        }

        private void ExtrusionSliderReleased(ManipulationEventData arg0)
        {
            ExtrusionSliderHandle.transform.DOLocalMoveZ(0, 0.1f).OnComplete(() =>
            {
                isExtruding = false;
                ExtrudingVisualisation.ExtrudeDimension(AbstractVisualisation.PropertyType.Z, extrusionPoint, 0);
            }
            );
        }
    }
}