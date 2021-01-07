using IATK;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Experimental.CrossDimensionalTransfer
{
    public class Chart : MonoBehaviour
    {

        [HideInInspector]
        public string ID;

        public BoxCollider BoundingBoxProxy;

        [SerializeField]
        private Visualisation visualisation;
        [SerializeField]
        private GameObject chartOptionsMenu;
        private bool isScaling = false;

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

            dataSource = ChartManager.Instance.DataSource;
        }

        public string XDimension
        {
            get { return visualisation.xDimension.Attribute; }
            set
            {
                visualisation.xDimension = value;
                visualisation.updateViewProperties(AbstractVisualisation.PropertyType.X);

                AdjustVisualisationPosition();
                AdjustBoundingBoxSize();
            }
        }

        public string YDimension
        {
            get { return visualisation.yDimension.Attribute; }
            set
            {
                visualisation.yDimension = value;
                visualisation.updateViewProperties(AbstractVisualisation.PropertyType.Y);

                AdjustVisualisationPosition();
            }
        }

        public string ZDimension
        {
            get { return visualisation.zDimension.Attribute; }
            set
            {
                visualisation.zDimension = value;
                visualisation.updateViewProperties(AbstractVisualisation.PropertyType.Z);

                AdjustVisualisationPosition();
            }
        }

        public string SizeByDimension
        {
            get {return visualisation.sizeDimension; }
            set
            {
                visualisation.sizeDimension = value;
                visualisation.updateViewProperties(AbstractVisualisation.PropertyType.Size);

                AdjustVisualisationPosition();
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

                AdjustVisualisationPosition();
            }
        }

        public float Height
        {
            get { return visualisation.height; }
            set
            {
                visualisation.height = value;
                visualisation.updateViewProperties(AbstractVisualisation.PropertyType.Scaling);

                AdjustVisualisationPosition();
            }
        }

        public float Depth
        {
            get { return visualisation.depth; }
            set
            {
                visualisation.depth = value;
                visualisation.updateViewProperties(AbstractVisualisation.PropertyType.Scaling);

                AdjustVisualisationPosition();
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

                AdjustVisualisationPosition();
            }
        }

        public void BoundingBoxScalingStart()
        {
            isScaling = true;
        }

        public void BoundingBoxScalingEnd()
        {
            isScaling = false;
        }

        private void Update()
        {
            if (isScaling)
            {
                Scale = BoundingBoxProxy.size;
            }
        }


        private void AdjustVisualisationPosition()
        {
            float xPos = (XDimension != "Undefined") ? -Width / 2 : 0;
            float yPos = (YDimension != "Undefined") ? -Height / 2 : 0;
            float zPos = (ZDimension != "Undefined") ? -Depth / 2 : 0;

            visualisation.transform.localPosition = new Vector3(xPos, yPos, zPos);
            chartOptionsMenu.transform.localPosition = new Vector3(0, yPos - 0.1f, zPos);

            AdjustBoundingBoxSize();
        }

        private void AdjustBoundingBoxSize()
        {
            if (XDimension == "Undefined" && YDimension == "Undefined" && ZDimension == "Undefined")
            {
                BoundingBoxProxy.size = Vector3.zero;
            }
            else
            {
                float xScale = (XDimension != "Undefined") ? Width : 0.1f;
                float yScale = (YDimension != "Undefined") ? Height : 0.1f;
                float zScale = (ZDimension != "Undefined") ? Depth : 0.1f;

                BoundingBoxProxy.size = new Vector3(xScale, yScale, zScale);
            }
        }
    }
}