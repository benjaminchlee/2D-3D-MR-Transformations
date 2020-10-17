using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Experimental.CrossDimensionalTransfer
{
    public class DimensionChangingSBV : MonoBehaviour
    {
        public Chart ChartScript;
                
        private void Start()
        {
            ChartScript.XDimension = "mpg";
            ChartScript.YDimension = "horesepower";
            ChartScript.ZDimension = "cylinders";
            ChartScript.Scale = new Vector3(0.4f, 0.4f, 0.4f);
            ChartScript.GeometryType = IATK.AbstractVisualisation.GeometryType.Points;
        }
        
        public void AttachToSurface()
        {
            ChartScript.ZDimension = "Undefined";
            ChartScript.SizeByDimension = "cylinders";
        }
        
        public void RemoveFromSurface()
        {
            ChartScript.ZDimension = "cylinders";
            ChartScript.SizeByDimension = "Undefined";
        }
    }
}
