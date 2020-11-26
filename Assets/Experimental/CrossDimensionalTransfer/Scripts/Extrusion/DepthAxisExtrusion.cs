using System.Collections;
using System.Collections.Generic;
using IATK;
using UnityEngine;

namespace Experimental.CrossDimensionalTransfer
{
    public class DepthAxisExtrusion : VisualisationExtrusion
    {
        public override ExtrusionIdiom Idiom { get { return ExtrusionIdiom.DepthAxis; }}

        public string DepthAxisName;

        public override void ExtrudeDimension(AbstractVisualisation.PropertyType dimension, float distance, Vector3 extrusionPoint1, Quaternion extrusionRotation1, Vector3? extrusionPoint2 = null, Quaternion? extrusionRotation2 = null)
        {
            // Check if extrusion has stopped
            if (CompareFloats(distance, 0))
            {
                if (isExtruding)
                {
                    isExtruding = false;
                    
                    DataVisualisation.ZDimension = "Undefined";
                    Visualisation.theVisualizationObject.viewList[0].ZeroPosition(2);
                    DataVisualisation.AutoCenterVisualisation = true;             
                    DataVisualisation.Scale = startViewScale;
                }
                
                return;
            }
            
            // If this extrusion has just started, save the starting scale so that we can revert to it later
            if (!isExtruding)
            {
                DataVisualisation.ZDimension = DepthAxisName;
                DataVisualisation.AutoCenterVisualisation = false;
                startViewScale = DataVisualisation.Scale;
                isExtruding = true;
            }
            
            // Update the scale of the visualisation based on the provided distance
            DataVisualisation.Depth = distance / 4;
            Vector3 visPos = Visualisation.transform.localPosition;
            visPos.z = 0;
            Visualisation.transform.localPosition = visPos;
        }
    }
}
