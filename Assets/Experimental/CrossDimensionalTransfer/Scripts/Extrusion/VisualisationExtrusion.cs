using IATK;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Experimental.CrossDimensionalTransfer
{
    public enum ExtrusionIdiom
    {
        Overplotting,
        Trajectories,
        Network,
        Histogram,
        ParallelCoordinates,
        DepthAxis
    }
    
    public abstract class VisualisationExtrusion : MonoBehaviour
    {
        public abstract ExtrusionIdiom Idiom { get; }
        
        protected DataSource DataSource;
        protected DataVisualisation DataVisualisation;
        protected Visualisation Visualisation;
        
        // Common extrusion variables
        protected bool isExtruding = false;
        protected Vector3 startViewScale;
        
        protected string XDimension
        {
            get { return Visualisation.xDimension.Attribute; }
        }
        
        protected string YDimension
        {
            get { return Visualisation.yDimension.Attribute; }
        }
        
        protected string ZDimension
        {
            get { return Visualisation.zDimension.Attribute; }
        }
        
        protected string LinkingDimension
        {
            get { return Visualisation.linkingDimension; }
        }
        
        public void Initialise(DataSource dataSource, DataVisualisation dataVisualisation, Visualisation visualisation)
        {
            this.DataSource = dataSource;
            this.DataVisualisation = dataVisualisation;
            this.Visualisation = visualisation;
        }
        
        public abstract void ExtrudeDimension(AbstractVisualisation.PropertyType dimension, float distance, Vector3 extrusionPoint1, Quaternion extrusionRotation1, Vector3? extrusionPoint2 = null, Quaternion? extrusionRotation2 = null);
        
        protected string GetExtrusionDimensionKey()
        {
            return string.Format("X:{0}Y:{1}Z:{2}Linking:{3}", XDimension, YDimension, ZDimension, LinkingDimension);
        }
        
        protected float NormaliseValue(float value, float i0, float i1, float j0 = 0, float j1 = 1)
        {
            float L = (j0 - j1) / (i0 - i1);
            return (j0 - (L * i0) + (L * value));
        }
        
        protected bool CompareFloats(float a, float b, float epsilon = 0.0001f)
        {
            return Mathf.Abs(a - b) < epsilon;
        }
    }
}
