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
        public TapToPlace TapToPlaceScript;
        public Renderer SurfaceRenderer;
        public Color activeColour = Color.yellow;

        private Color inactiveColour;

        private void Start()
        {
            if (TapToPlaceScript == null) TapToPlaceScript = GetComponent<TapToPlace>();
            if (SurfaceRenderer == null) SurfaceRenderer = GetComponent<Renderer>();
            
            inactiveColour = SurfaceRenderer.material.color;
            
            SetSurfacePlacement(false);
        }

        public void SetSurfacePlacement(bool active)
        {
            TapToPlaceScript.enabled = active;
            
            SurfaceRenderer.material.color = active ? activeColour : inactiveColour;
        }
    }
}