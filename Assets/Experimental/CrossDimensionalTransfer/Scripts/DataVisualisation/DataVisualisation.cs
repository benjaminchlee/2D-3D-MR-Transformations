using IATK;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Experimental.CrossDimensionalTransfer
{
    public class DataVisualisation : MonoBehaviour
    {
        [HideInInspector]
        public string ID;

        [Header("Required Fields")]
        [SerializeField]
        private GameObject visualisationHolder;
        [SerializeField]
        private Visualisation visualisation;
        [SerializeField]
        private BoxCollider boxCollider;
        [SerializeField]
        private DataSource dataSource;

        private VisualisationExtrusion visualisationExtrusion;

        #region Visualisation Properties

        public Visualisation Visualisation
        {
            get { return visualisation; }
        }

        public DataSource DataSource
        {
            get { return visualisation.dataSource; }
            set { visualisation.dataSource = value; }
        }

        public AbstractVisualisation.VisualisationTypes VisualisationType
        {
            get { return visualisation.visualisationType; }
            set
            {
                visualisation.visualisationType = value;
                visualisation.updateViewProperties(AbstractVisualisation.PropertyType.VisualisationType);
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

        public string XDimension
        {
            get { return visualisation.xDimension.Attribute; }
            set
            {
                visualisation.xDimension = value;
                visualisation.updateViewProperties(AbstractVisualisation.PropertyType.X);

                AdjustVisualisationLocalPosition();
                AdjustCollider();
                //GenerateExtrusionOffset();
            }
        }

        public string YDimension
        {
            get { return visualisation.yDimension.Attribute; }
            set
            {
                visualisation.yDimension = value;
                visualisation.updateViewProperties(AbstractVisualisation.PropertyType.Y);

                AdjustVisualisationLocalPosition();
                AdjustCollider();
                //GenerateExtrusionOffset();
            }
        }

        public string ZDimension
        {
            get { return visualisation.zDimension.Attribute; }
            set
            {
                visualisation.zDimension = value;
                visualisation.updateViewProperties(AbstractVisualisation.PropertyType.Z);

                AdjustVisualisationLocalPosition();
                AdjustCollider();
                //GenerateExtrusionOffset();
            }
        }

        public Color Colour
        {
            get { return visualisation.colour; }
            set
            {
                visualisation.colour = value;
                visualisation.updateViewProperties(AbstractVisualisation.PropertyType.Colour);
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

        public string LinkingDimension
        {
            get { return visualisation.linkingDimension; }
            set
            {
                visualisation.linkingDimension = value;
                visualisation.updateViewProperties(AbstractVisualisation.PropertyType.LinkingDimension);

                //GenerateExtrusionOffset();
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
                visualisation.updateViewProperties(AbstractVisualisation.PropertyType.SizeValues);
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

                visualisation.updateViewProperties(AbstractVisualisation.PropertyType.Scaling);
            }
        }

        public GameObject XAxisObject
        {
            get
            {
                return visualisation.theVisualizationObject.X_AXIS;
            }
        }

        public GameObject YAxisObject
        {
            get
            {
                return visualisation.theVisualizationObject.Y_AXIS;
            }
        }

        public GameObject ZAxisObject
        {
            get
            {
                return visualisation.theVisualizationObject.Z_AXIS;
            }
        }

        public bool AutoCenterVisualisation
        {
            get; set;
        } = true;

        #endregion

        private void Awake()
        {
            if (visualisation == null)
                visualisation = visualisationHolder.AddComponent<Visualisation>();

            // Set blank IATK values
            if (visualisation.colourDimension == null || visualisation.colourDimension == "")
                visualisation.colourDimension = "Undefined";
            if (visualisation.colorPaletteDimension == null ||visualisation.colorPaletteDimension == "")
                visualisation.colorPaletteDimension = "Undefined";
            if (visualisation.sizeDimension == null ||visualisation.sizeDimension == "")
                visualisation.sizeDimension = "Undefined";
            if (visualisation.linkingDimension == null ||visualisation.linkingDimension == "")
                visualisation.linkingDimension = "Undefined";
            if (dataSource != null)
                DataSource = dataSource;
            else if (DataSource == null)
                DataSource = DataVisualisationManager.Instance.DataSource;

            // Set extrusion variables
            visualisationExtrusion = GetComponent<VisualisationExtrusion>();
            if (visualisationExtrusion != null)
            {
                visualisationExtrusion.Initialise(dataSource, this, visualisation);
            }
        }

        private void Update()
        {
            AdjustVisualisationLocalPosition();
            AdjustCollider();
        }

        private void AdjustVisualisationLocalPosition()
        {
            if (AutoCenterVisualisation)
            {
                float xPos = (XDimension != "Undefined") ? -Width / 2 : 0;
                float yPos = (YDimension != "Undefined") ? -Height / 2 : 0;
                float zPos = (ZDimension != "Undefined") ? -Depth : 0;

                visualisation.transform.localPosition = new Vector3(xPos, yPos, zPos);
            }
        }

        private void AdjustCollider()
        {
            if (AutoCenterVisualisation)
            {
                float xScale = (XDimension != "Undefined") ? Width : 0.075f;
                float yScale = (YDimension != "Undefined") ? Height : 0.075f;
                float zScale = (ZDimension != "Undefined") ? Depth : 0.075f;
                boxCollider.size = new Vector3(xScale, yScale, zScale);

                float xPos = 0;
                float yPos = 0;
                float zPos = (ZDimension != "Undefined") ? -Depth / 2 : 0;
                boxCollider.center = new Vector3(xPos, yPos, zPos);
            }
        }

        public void ExtrudeDimension(AbstractVisualisation.PropertyType dimension, float distance, Vector3 extrusionPoint1, Quaternion extrusionRotation1, Vector3? extrusionPoint2 = null, Quaternion? extrusionRotation2 = null)
        {
            if (visualisationExtrusion != null)
                visualisationExtrusion.ExtrudeDimension(dimension, distance, extrusionPoint1, extrusionRotation1, extrusionPoint2, extrusionRotation2);
        }
    }
}