using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Experimental.CrossDimensionalTransfer
{
    [RequireComponent(typeof(SnapOnProximityPlaceableObject))]
    public class OrientationBasedTransfer : MonoBehaviour
    {
        [SerializeField]
        private DataVisualisation dataVisualisationScript;
        [SerializeField]
        private SnapOnProximityPlaceableObject snapOnProximityScript;
        
        private void Awake()
        {
            if (dataVisualisationScript == null) GetComponentInChildren<DataVisualisation>();
            if (snapOnProximityScript == null) GetComponent<SnapOnProximityPlaceableObject>();
            
            snapOnProximityScript.OnBeforeObjectPlacedOnSurface.AddListener(VisualisationPlacedOnSurface);
            snapOnProximityScript.OnAfterObjectLiftedFromSurface.AddListener(VisualisationLiftedOnSurface);
        }

        private void VisualisationPlacedOnSurface(PlacedObjectEventData eventData)
        {
            // Determine the axis which is "sticking outwards" from the surface
            Vector3[] directions = new Vector3[] {
                gameObject.transform.right,
                -gameObject.transform.right,
                gameObject.transform.up,
                -gameObject.transform.up,
                gameObject.transform.forward,
                -gameObject.transform.forward
            };
            
            int idx = 0;
            float min = Mathf.Infinity;
            for (int i = 0; i < 6; i++)
            {
                float angle = Vector3.Angle(directions[i], eventData.Surface.transform.forward);
                if (angle < min)
                {
                    idx = i;
                    min = angle;
                }
            }
            
            // x-axis
            if (idx < 2)
            {
                dataVisualisationScript.XDimension = "Undefined";
            }
            // y-axis
            else if (idx < 4)
            {
                dataVisualisationScript.YDimension = "Undefined";
            }
            // z-axis
            else
            {
                dataVisualisationScript.ZDimension = "Undefined";                
            }
        }
        
        private void VisualisationLiftedOnSurface(PlacedObjectEventData eventData)
        {
            dataVisualisationScript.XDimension = "mpg";
            dataVisualisationScript.YDimension = "cylinders";
            dataVisualisationScript.ZDimension = "horesepower";
            
            dataVisualisationScript.Scale = Vector3.one * 0.3f;
        }

    }
}
