using IATK;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Experimental.CrossDimensionalTransfer
{
    public class DataVisualisation : MonoBehaviour
    {
        [HideInInspector]
        public string ID;

        [SerializeField]
        private GameObject visualisationHolder;
        [SerializeField]
        private Visualisation visualisation;
        [SerializeField]
        private BoxCollider boxCollider;
        
        private void Awake()
        {
            if (visualisation == null)
                visualisation = visualisationHolder.AddComponent<Visualisation>();      
        }
        
        private void Start()
        {      
            
            DataSource = DataVisualisationManager.Instance.DataSource;
            
            // Set blank IATK values
            visualisation.visualisationType = AbstractVisualisation.VisualisationTypes.SCATTERPLOT;
            if (visualisation.colourDimension == "")
                visualisation.colourDimension = "Undefined";
            if (visualisation.colorPaletteDimension == "")
                visualisation.colorPaletteDimension = "Undefined";
            if (visualisation.sizeDimension == "")
                visualisation.sizeDimension = "Undefined";
            if (visualisation.linkingDimension == "")
                visualisation.linkingDimension = "Undefined";
                
            GeometryType = AbstractVisualisation.GeometryType.Points;
        }

        public DataSource DataSource
        {
            get { return visualisation.dataSource; }
            set { visualisation.dataSource = value; }
        }
        
        public string XDimension
        {
            get { return visualisation.xDimension.Attribute; }
            set
            {
                visualisation.xDimension = value;
                visualisation.updateViewProperties(AbstractVisualisation.PropertyType.X);
            }
        }

        public string YDimension
        {
            get { return visualisation.yDimension.Attribute; }
            set
            {
                visualisation.yDimension = value;
                visualisation.updateViewProperties(AbstractVisualisation.PropertyType.Y);
            }
        }

        public string ZDimension
        {
            get { return visualisation.zDimension.Attribute; }
            set
            {
                visualisation.zDimension = value;
                visualisation.updateViewProperties(AbstractVisualisation.PropertyType.Z);
            }
        }

        public string SizeByDimension
        {
            get { return visualisation.sizeDimension; }
            set
            {
                visualisation.sizeDimension = value;
                visualisation.updateViewProperties(AbstractVisualisation.PropertyType.Size);
            }
        }
        
        public string ColourByDimension
        {
            get { return visualisation.colourDimension; }
            set
            {
                visualisation.colourDimension = value;
                visualisation.updateViewProperties(AbstractVisualisation.PropertyType.Colour);
            }
        }
        
        public Gradient ColourByGradient
        {
            get { return visualisation.dimensionColour; }
            set 
            {
                visualisation.dimensionColour = value;
                visualisation.updateViewProperties(AbstractVisualisation.PropertyType.Colour);
            }
        }

        public AbstractVisualisation.GeometryType GeometryType
        {
            get { return visualisation.geometry; }
            set
            {
                visualisation.geometry = value;
                visualisation.updateViewProperties(AbstractVisualisation.PropertyType.GeometryType);
            }
        }

        public float Width
        {
            get { return visualisation.width; }
            set
            {
                visualisation.width = value;
                visualisation.updateViewProperties(AbstractVisualisation.PropertyType.Scaling);
            }
        }

        public float Height
        {
            get { return visualisation.height; }
            set
            {
                visualisation.height = value;
                visualisation.updateViewProperties(AbstractVisualisation.PropertyType.Scaling);
            }
        }

        public float Depth
        {
            get { return visualisation.depth; }
            set
            {
                visualisation.depth = value;
                visualisation.updateViewProperties(AbstractVisualisation.PropertyType.Scaling);
            }
        }
        
        public float Size
        {
            get { return visualisation.size; }
            set
            {
                visualisation.size = value;
                visualisation.updateViewProperties(AbstractVisualisation.PropertyType.Size);
            }
        }

        public Vector3 Scale
        {
            get { return new Vector3(Width, Height, Depth); }
            set
            {
                visualisation.width = value.x;
                visualisation.height = value.y;
                visualisation.depth = value.z;

                visualisation.updateViewProperties(AbstractVisualisation.PropertyType.VisualisationSize);
            }
        }

        private void Update()
        {
            AdjustVisualisationPosition();
            AdjustColliderSize();
        }


        private void AdjustVisualisationPosition()
        {
            float xPos = (XDimension != "Undefined") ? -Width / 2 : 0;
            float yPos = (YDimension != "Undefined") ? -Height / 2 : 0;
            float zPos = (ZDimension != "Undefined") ? -Depth / 2 : 0;

            visualisation.transform.localPosition = new Vector3(xPos, yPos, zPos);
        }
        
        private void AdjustColliderSize()
        {
            float xScale = (XDimension != "Undefined") ? Width : 0.1f;
            float yScale = (YDimension != "Undefined") ? Height : 0.1f;
            float zScale = (ZDimension != "Undefined") ? Depth : 0.1f;

            boxCollider.size = new Vector3(xScale, yScale, zScale);
        }

        // private void AdjustBoundingBoxSize()
        // {
        //     if (XDimension == "Undefined" && YDimension == "Undefined" && ZDimension == "Undefined")
        //     {
        //         BoundingBoxProxy.size = Vector3.zero;
        //     }
        //     else
        //     {
        //         float xScale = (XDimension != "Undefined") ? Width : 0.1f;
        //         float yScale = (YDimension != "Undefined") ? Height : 0.1f;
        //         float zScale = (ZDimension != "Undefined") ? Depth : 0.1f;

        //         BoundingBoxProxy.size = new Vector3(xScale, yScale, zScale);
        //     }
        // }
    }
}