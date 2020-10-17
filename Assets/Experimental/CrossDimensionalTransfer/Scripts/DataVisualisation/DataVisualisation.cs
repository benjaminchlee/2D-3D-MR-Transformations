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
        private Visualisation visualisation;
        [SerializeField]
        private BoxCollider boxCollider;

        private DataSource dataSource
        {
            get { return visualisation.dataSource; }
            set { visualisation.dataSource = value; }
        }

        private void Start()
        {
            // Set blank IATK values
            visualisation.visualisationType = AbstractVisualisation.VisualisationTypes.SCATTERPLOT;
            visualisation.colourDimension = "Undefined";
            visualisation.colorPaletteDimension = "Undefined";
            visualisation.sizeDimension = "Undefined";
            visualisation.linkingDimension = "Undefined";
            
            GeometryType = AbstractVisualisation.GeometryType.Points;

            dataSource = DataVisualisationManager.Instance.DataSource;
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
            get {return visualisation.sizeDimension; }
            set
            {
                visualisation.sizeDimension = value;
                visualisation.updateViewProperties(AbstractVisualisation.PropertyType.Size);
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

        public Vector3 Scale
        {
            get {return new Vector3(Width, Height, Depth); }
            set
            {
                visualisation.width = value.x;
                visualisation.height = value.y;
                visualisation.depth = value.z;

                visualisation.updateViewProperties(AbstractVisualisation.PropertyType.Scaling);
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