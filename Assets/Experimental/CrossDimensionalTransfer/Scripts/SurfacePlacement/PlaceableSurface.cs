using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities.Solvers;
using UnityEngine;

namespace Experimental.CrossDimensionalTransfer
{
    /// <summary>
    /// A script to toggle the positioning of a placeable surface in lieu of the scene understanding API.
    /// </summary>
    public class PlaceableSurface : MonoBehaviour
    {
        public ObjectManipulator ObjectManipulatorScript;
        public BoundingBox BoundingBoxScript;
        public TapToPlace TapToPlaceScript;

        private bool isActive = false;

        private void Start()
        {
            SetScriptStatus(false);
        }

        public void ToggleSurfacePlacement()
        {
            isActive = !isActive;
            SetScriptStatus(isActive);
        }

        private void SetScriptStatus(bool active)
        {
            if (ObjectManipulatorScript != null)
            {
                ObjectManipulatorScript.enabled = active;
            }
            if (BoundingBoxScript != null)
            {
                BoundingBoxScript.enabled = active;
            }
            if (TapToPlaceScript != null)
            {
                TapToPlaceScript.enabled = active;
            }
        }
    }
}