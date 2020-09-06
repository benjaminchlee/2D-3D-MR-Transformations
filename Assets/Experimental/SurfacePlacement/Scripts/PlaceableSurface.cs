using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;

namespace Experimental.SurfacePlacement
{
    /// <summary>
    /// A script to toggle the positioning of a placeable surface in lieu of the scene understanding API.
    /// </summary>
    public class PlaceableSurface : MonoBehaviour
    {
        public ObjectManipulator ObjectManipulatorScript;
        public BoundingBox BoundingBoxScript;

        private void Start()
        {
            if (ObjectManipulatorScript == null)
            {
                ObjectManipulatorScript = gameObject.GetComponent<ObjectManipulator>();
            }
            if (BoundingBoxScript == null)
            {
                BoundingBoxScript = gameObject.GetComponent<BoundingBox>();
            }

            ObjectManipulatorScript.enabled = false;
            BoundingBoxScript.enabled = false;
        }

        public void ToggleSurfacePlacement()
        {
            ObjectManipulatorScript.enabled = !ObjectManipulatorScript.enabled;
            BoundingBoxScript.enabled = ObjectManipulatorScript.enabled;
        }
    }
}