using DG.Tweening;
using IATK;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SSVis
{
    public enum AxisDirection
    {
        None,
        X,
        Y,
        Z
    }

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
        [SerializeField]
        private ObjectManipulator objectManipulator;

        // Variables for changing the size of the visualisation through grab handle objects
        [Header("Axis Scaling")]
        public ObjectManipulator XAxisManipulator;
        public ObjectManipulator YAxisManipulator;
        public ObjectManipulator ZAxisManipulator;

        private bool isXAxisScaling = false;
        private bool isYAxisScaling = false;
        private bool isZAxisScaling = false;

        // Variables for visualisation extrusion and splatting
        private GameObject ghostVisualisation;

        private BaseVisualisationExtrusion visualisationExtrusion;
        private bool isAttachedToSurface;
        private List<GameObject> collidingSurfaces = new List<GameObject>();
        private GameObject nearestSurface;
        private AxisDirection protrudingDimension = AxisDirection.None;
        private bool splatLineInitialised = false;

        private bool isManipulating = false;
        private byte nearestSurfaceCheckCounter = 0;

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

                if (value == "Undefined")
                    visualisation.width = 1f;
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

                if (value == "Undefined")
                    visualisation.height = 1f;
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

                if (value == "Undefined")
                    visualisation.depth = 1f;
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
            {
                DataSource = DataVisualisationManager.Instance.DataSource;
                dataSource = DataSource;
            }
        }

        private void Start()
        {
            objectManipulator = GetComponent<ObjectManipulator>();
            objectManipulator.OnManipulationStarted.AddListener(VisualisationGrabbed);
            objectManipulator.OnManipulationEnded.AddListener(VisualisationReleased);

            if (XAxisManipulator != null && YAxisManipulator != null && ZAxisManipulator != null)
            {
                XAxisManipulator.OnManipulationStarted.AddListener(XAxisManipulatorGrabbed);
                YAxisManipulator.OnManipulationStarted.AddListener(YAxisManipulatorGrabbed);
                ZAxisManipulator.OnManipulationStarted.AddListener(ZAxisManipulatorGrabbed);
                XAxisManipulator.OnManipulationEnded.AddListener(XAxisManipulatorReleased);
                YAxisManipulator.OnManipulationEnded.AddListener(YAxisManipulatorReleased);
                ZAxisManipulator.OnManipulationEnded.AddListener(ZAxisManipulatorReleased);

                XAxisManipulator.transform.DOLocalMoveX(XDimension != "Undefined" ? Width : 0.05f, 0f);
                YAxisManipulator.transform.DOLocalMoveY(YDimension != "Undefined" ? Height : 0.05f, 0f);
                ZAxisManipulator.transform.DOLocalMoveZ(ZDimension != "Undefined" ? Depth : 0.05f, 0f);
            }
        }

        private void Update()
        {
            if (AutoCenterVisualisation)
            {
                AdjustVisualisationLocalPosition();
                AdjustCollider();
            }

            if (isXAxisScaling && XDimension != "Undefined" && XAxisManipulator.transform.localPosition.x != Width)
            {
                Width = XAxisManipulator.transform.localPosition.x;
            }
            if (isYAxisScaling && YDimension != "Undefined" && YAxisManipulator.transform.localPosition.y != Height)
            {
                Height = YAxisManipulator.transform.localPosition.y;
            }
            if (isZAxisScaling && ZDimension != "Undefined" && ZAxisManipulator.transform.localPosition.z != Depth)
            {
                Depth = ZAxisManipulator.transform.localPosition.z;
            }

            if (isManipulating)
            {
                if (collidingSurfaces.Count > 0)
                {
                    if (nearestSurfaceCheckCounter > 5)
                    {
                        nearestSurfaceCheckCounter = 0;
                        // Find the closest colliding surface by distance
                        GameObject surface = null;
                        float min = Mathf.Infinity;
                        for (int i = 0; i < collidingSurfaces.Count; i++)
                        {
                            if (collidingSurfaces[i] == null)
                            {
                                collidingSurfaces.RemoveAt(i);
                                i--;
                            }
                            else
                            {
                                GameObject surf = collidingSurfaces[i];
                                Collider surfCollider = surf.GetComponent<Collider>();
                                float distance = Vector3.Distance(surfCollider.ClosestPoint(transform.position), boxCollider.ClosestPoint(surf.transform.position));
                                if (distance < min)
                                {
                                    surface = surf;
                                    min = distance;
                                }
                            }
                        }
                        nearestSurface = surface;
                    }
                    else
                    {
                        nearestSurfaceCheckCounter++;
                    }

                    if (nearestSurface != null)
                    {
                        if (ghostVisualisation == null)
                            ghostVisualisation = (Instantiate(Resources.Load("GhostVisualisation") as GameObject));
                        System.Tuple<Vector3, Vector3> values = CalculatePositionAndRotationOnSurface(nearestSurface);
                        ghostVisualisation.transform.position = values.Item1;
                        ghostVisualisation.transform.eulerAngles = values.Item2;
                        ghostVisualisation.transform.localScale = boxCollider.size;
                    }
                }
                else
                {
                    if (ghostVisualisation != null)
                        Destroy(ghostVisualisation);
                }
            }
        }

        #region Surface placement

        public void VisualisationGrabbed(ManipulationEventData eventData)
        {
            isManipulating = true;

            if (isAttachedToSurface)
            {
                LiftFromSurface();
            }
        }

        public void VisualisationReleased(ManipulationEventData eventData)
        {
            if (collidingSurfaces.Count > 0)
            {
                if (nearestSurface != null)
                {
                    PlaceOnSurface(nearestSurface);
                }
            }

            if (ghostVisualisation != null)
                Destroy(ghostVisualisation);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.tag == "SceneWall" && !collidingSurfaces.Contains(other.gameObject))
            {
                collidingSurfaces.Add(other.gameObject);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.tag == "SceneWall" && collidingSurfaces.Contains(other.gameObject))
            {
                collidingSurfaces.Remove(other.gameObject);
            }
        }

        private void OnDestroy()
        {
            if (ghostVisualisation != null)
                Destroy(ghostVisualisation);
        }

        public void PlaceOnSurface(GameObject targetSurface)
        {
            System.Tuple<Vector3, Vector3> values = CalculatePositionAndRotationOnSurface(targetSurface);
            transform.DOMove(values.Item1, 0.1f);
            transform.DORotate(values.Item2, 0.1f)
                .OnComplete(()=>
                {
                    isAttachedToSurface = true;
                    boxCollider.enabled = false;
                    UpdateVisualisationExtrusion();
                });
        }

        public void LiftFromSurface(GameObject pointer = null)
        {
            isAttachedToSurface = false;
            boxCollider.enabled = true;
            objectManipulator.enabled = true;
            collidingSurfaces.Clear();  // Clear all surfaces as a failsafe if the OnTriggerExit event does not fire in time

            // Regain control of the Data Visualisation when the extrusion script is removed
            if (pointer != null && visualisationExtrusion != null)
            {
                visualisationExtrusion.DestroyThisExtrusion();
                // Move this Data Visualisation such that it follows the hand
                transform.SetParent(pointer.transform);
                transform.DOLocalMove(Vector3.zero, 0.1f);
                // Add events to release this Data Visualisation when the grab gesture is released
                // Note that we have to use a PointerHandler instead of hooking into the existing ObjectManipulator script as I don't know how to
                // make it think that the manipulation has even begun (there's ForceManipulationEnd but not for start)
                var pointerHandler = gameObject.AddComponent<PointerHandler>();
                CoreServices.InputSystem.RegisterHandler<IMixedRealityPointerHandler>(pointerHandler);
                pointerHandler.OnPointerUp.AddListener((e) =>
                {
                    transform.parent = null;
                    CoreServices.InputSystem.UnregisterHandler<IMixedRealityPointerHandler>(pointerHandler);
                    pointerHandler.OnPointerUp.RemoveAllListeners();
                    Destroy(pointerHandler);
                });
                // Hook into the existing VisualisationReleased event handler when this visualisation is lifted and immediately placed back on a surface
                pointerHandler.OnPointerUp.AddListener((e) =>
                {
                    VisualisationReleased(new ManipulationEventData());
                });
            }
        }

        private void UpdateVisualisationExtrusion()
        {
            if (VisualisationType == AbstractVisualisation.VisualisationTypes.SCATTERPLOT)
            {
                visualisationExtrusion = gameObject.AddComponent<OverplottingExtrusion>();
            }

            visualisationExtrusion.Initialise(dataSource, this, visualisation, protrudingDimension);
            // This Data Visualisation relinquishes control of all manipulation events until the extrusion script tells it to
            objectManipulator.enabled = false;
        }

        private void AdjustVisualisationLocalPosition()
        {
            float xPos = (XDimension != "Undefined") ? -Width / 2 : 0;
            float yPos = (YDimension != "Undefined") ? -Height / 2 : 0;
            float zPos = (ZDimension != "Undefined") ? -Depth / 2 : 0;
            visualisation.transform.localPosition = new Vector3(xPos, yPos, zPos);
        }

        private void AdjustCollider()
        {
            float xScale = (XDimension != "Undefined") ? Width : 0.05f;
            float yScale = (YDimension != "Undefined") ? Height : 0.05f;
            float zScale = (ZDimension != "Undefined") ? Depth : 0.05f;
            boxCollider.size = new Vector3(xScale, yScale, zScale);

            // float xPos = 0;
            // float yPos = 0;
            // float zPos = (ZDimension != "Undefined") ? -Depth / 2 : 0;
            // boxCollider.center = new Vector3(xPos, yPos, zPos);

            if (visualisationExtrusion != null)
                visualisationExtrusion.UpdateExtrusionHandles();
        }

        private System.Tuple<Vector3, Vector3> CalculatePositionAndRotationOnSurface(GameObject surface)
        {
            Vector3 surfaceNormal = surface.transform.forward;

            /// First we calculate rotation as we need it to properly determine position away from the surface
            // Get the axis which is closest to the surface's normal
            Vector3[] directions = new Vector3[] {
                gameObject.transform.right,
                -gameObject.transform.right,
                gameObject.transform.up,
                -gameObject.transform.up,
                gameObject.transform.forward,
                -gameObject.transform.forward
            };

            int direction = 0;
            float min = Mathf.Infinity;
            for (int i = 0; i < 6; i++)
            {
                float angle = Vector3.Angle(directions[i], surfaceNormal);
                if (angle < min)
                {
                    direction = i;
                    min = angle;
                }
            }
            Vector3 retRotation = (Quaternion.FromToRotation(directions[direction], surfaceNormal) * transform.rotation).eulerAngles;
            // Lock the protruding axis rotation to the nearest 90 degrees
            if (direction < 2)
            {
                protrudingDimension = AxisDirection.X;
                retRotation.x = Mathf.RoundToInt(retRotation.x / 90f) * 90;
            }
            else if (direction < 4)
            {
                protrudingDimension = AxisDirection.Y;
                retRotation.y = Mathf.RoundToInt(retRotation.y / 90f) * 90;
            }
            else
            {
                protrudingDimension = AxisDirection.Z;
                retRotation.z = Mathf.RoundToInt(retRotation.z / 90f) * 90;
            }

            /// Then we calculate the position
            Vector3 localPosOnSurface = surface.transform.InverseTransformPoint(gameObject.transform.position);
            // Trap this GameObject within the confines of the given surface
            //localPosOnSurface = FixLocalPositionWithinSurfaceBounds(localPosOnSurface, surface);

            // Move this object away from the surface based on its depth
            // The depth size is based on the protruding direction value calculated earlier
            float depthSize = 0;
            switch (protrudingDimension)
            {
                case AxisDirection.X:
                    depthSize = boxCollider.size.x + 0.01f;
                    if (Width < 0) depthSize = -depthSize + 0.02f;
                    break;
                case AxisDirection.Y:
                    depthSize = boxCollider.size.y + 0.01f;
                    if (Height < 0) depthSize = -depthSize + 0.02f;
                    break;
                case AxisDirection.Z:
                    depthSize = boxCollider.size.z + 0.01f;
                    if (Depth < 0) depthSize = -depthSize + 0.02f;
                    break;
            }

            localPosOnSurface.z = 0;
            Vector3 retPosition = surface.transform.TransformPoint(localPosOnSurface);
            retPosition = retPosition - surfaceNormal * (depthSize / 2);

            return new System.Tuple<Vector3, Vector3>(retPosition, retRotation);
        }

        /// <summary>
        /// Updates the given localPos to ensure that this gameobject is set within the bounds of the given surface.
        /// It does so by calculating how much to move the position of the gameobject such that it fits "inside" of the surface, based on the
        /// two opposing corners of the gameobject's boxcollider.
        ///
        /// NOTE: this only works when the gameobject has a boxcollider.
        /// NOTE: this does not work properly when the surface is smaller than this gameobject.
        /// </summary>
        /// <param name="localPos"></param>
        /// <param name="surface"></param>
        /// <returns></returns>
        private Vector3 FixLocalPositionWithinSurfaceBounds(Vector3 localPos, GameObject surface)
        {
            // Get corners of box collider
            BoxCollider b = gameObject.GetComponent<BoxCollider>();
            Vector3 tl = gameObject.transform.TransformPoint(b.center + new Vector3(-b.size.x, b.size.y, 0) * 0.5f);
            Vector3 br = gameObject.transform.TransformPoint(b.center + new Vector3(b.size.x, -b.size.y, 0) * 0.5f);

            // Convert corner vectors into surface's local space
            tl = surface.transform.InverseTransformPoint(tl);
            br = surface.transform.InverseTransformPoint(br);

            Vector3 translation = Vector3.zero;

            // Case 1: vertex is too far to the top
            if (0.5f <= tl.y)
            {
                float delta = tl.y - 0.5f;
                translation.y -= delta;
            }
            // Case 2: vertex is too far to the bottom
            else if (br.y <= -0.5f)
            {
                float delta = -0.5f - br.y;
                translation.y += delta;
            }
            // Case 3: vertex is too far to the left
            if (tl.x <= -0.5f)
            {
                float delta = -0.5f - tl.x;
                translation.x += delta;
            }
            // Case 4: vertex is too far to the right
            else if (0.5f <= br.x)
            {
                float delta = br.x - 0.5f;
                translation.x -= delta;
            }

            return localPos + translation;
        }

        #endregion // Surface placement

        #region Axis Scaling

        public void XAxisManipulatorGrabbed(ManipulationEventData eventData)
        {
            isXAxisScaling = true;

            if (XDimension == "Undefined")
            {
                Width = XAxisManipulator.transform.localPosition.x;
                XDimension = DataSource[0].Identifier;
            }
        }

        public void YAxisManipulatorGrabbed(ManipulationEventData eventData)
        {
            isYAxisScaling = true;

            if (YDimension == "Undefined")
            {
                Height = XAxisManipulator.transform.localPosition.y;
                YDimension = DataSource[0].Identifier;
            }
        }

        public void ZAxisManipulatorGrabbed(ManipulationEventData eventData)
        {
            isZAxisScaling = true;

            if (ZDimension == "Undefined")
            {
                Depth = XAxisManipulator.transform.localPosition.z;
                ZDimension = DataSource[0].Identifier;
            }
        }

        public void XAxisManipulatorReleased(ManipulationEventData eventData)
        {
            isXAxisScaling = false;

            float pos = XAxisManipulator.transform.localPosition.x;
            if (-0.05f < pos && pos < 0.05f)
            {
                XDimension = "Undefined";
                XAxisManipulator.transform.DOLocalMoveX(0.05f, 0.1f);
            }
        }

        public void YAxisManipulatorReleased(ManipulationEventData eventData)
        {
            isYAxisScaling = false;

            float pos = YAxisManipulator.transform.localPosition.y;
            if (-0.05f < pos && pos < 0.05f)
            {
                YDimension = "Undefined";
                YAxisManipulator.transform.DOLocalMoveY(0.05f, 0.1f);
            }
        }

        public void ZAxisManipulatorReleased(ManipulationEventData eventData)
        {
            isZAxisScaling = false;

            float pos = ZAxisManipulator.transform.localPosition.z;
            if (-0.05f < pos && pos < 0.05f)
            {
                ZDimension = "Undefined";
                ZAxisManipulator.transform.DOLocalMoveZ(0.05f, 0.1f);
            }
        }

        #endregion // Axis Scaling
    }
}