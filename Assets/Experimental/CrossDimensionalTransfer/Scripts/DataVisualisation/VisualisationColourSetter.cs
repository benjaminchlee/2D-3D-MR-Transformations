using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Experimental.CrossDimensionalTransfer
{
    public class VisualisationColourSetter : MonoBehaviour
    {
        public DataVisualisation ParentDataVisualisation;
        
        public void VisualisationColourChanged(Color colour)
        {
            if (ParentDataVisualisation != null)
            {
                ParentDataVisualisation.Colour = colour;
            }
        }
    }
}