using DG.Tweening;
using IATK;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SSVis
{
    [Flags]
    public enum AxisDirection
    {
        None = 1,
        X = 2,
        Y = 4,
        Z = 8
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

        [Header("Visualisation Property UI")]
        public VisualisationRadialMenu VisualisationRadialMenu;

        [Header("Misc Visualisation Properties")]
        public bool IsPrototype = false;

        private bool isXAxisScaling = false;
        private bool isYAxisScaling = false;
        private bool isZAxisScaling = false;

        // Variables for visualisation extrusion and splatting
        private GameObject ghostVisualisation;
        private List<BaseVisualisationExtrusion> visualisationExtrusions = new List<BaseVisualisationExtrusion>();
        private List<BaseVisualisationSplatting> visualisationSplattings = new List<BaseVisualisationSplatting>();
        private bool isAttachedToSurface;
        private List<GameObject> collidingSurfaces = new List<GameObject>();
        private GameObject nearestSurface;
        private AxisDirection protrudingDimension = AxisDirection.None;
        private bool preventExtrusionForThisPlacement = false;      // Flag for when a splat script needs to prevent any extrusions from being added

        private bool isManipulating = false;
        private byte nearestSurfaceCheckCounter = 0;

        private List<IMixedRealityPointer> touchingPointers = new List<IMixedRealityPointer>();
        private bool isLeftPalmTouchingAndOpen = false;
        private bool isRightPalmTouchingAndOpen = false;

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
                {
                    visualisation.width = 1f;
                    if (XAxisManipulator != null)
                        XAxisManipulator.transform.localPosition = new Vector3(0.05f, 0, 0);
                }
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
                {
                    visualisation.height = 1f;
                    if (YAxisManipulator != null)
                        YAxisManipulator.transform.localPosition = new Vector3(0, 0.05f, 0);
                }
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
                {
                    visualisation.depth = 1f;
                    if (ZAxisManipulator != null)
                        ZAxisManipulator.transform.localPosition = new Vector3(0, 0, 0.05f);
                }
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
                visualisation.updateViewProperties(AbstractVisualisation.PropertyType.VisualisationScale);

                if (XAxisManipulator != null)
                    XAxisManipulator.transform.localPosition = new Vector3(value, 0, 0);
            }
        }

        public float Height
        {
            get { return visualisation.height; }
            set
            {
                visualisation.height = value;
                visualisation.updateViewProperties(AbstractVisualisation.PropertyType.VisualisationScale);

                if (YAxisManipulator != null)
                    YAxisManipulator.transform.localPosition = new Vector3(0, value, 0);
            }
        }

        public float Depth
        {
            get { return visualisation.depth; }
            set
            {
                visualisation.depth = value;
                visualisation.updateViewProperties(AbstractVisualisation.PropertyType.VisualisationScale);

                if (ZAxisManipulator != null)
                    ZAxisManipulator.transform.localPosition = new Vector3(0, 0, value);
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

                visualisation.updateViewProperties(AbstractVisualisation.PropertyType.VisualisationScale);

                if (AutoCenterVisualisation)
                {
                    AdjustVisualisationLocalPosition();
                    AdjustCollider();
                }
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
            objectManipulator.OnHoverEntered.AddListener(VisualisationTouched);
            objectManipulator.OnHoverExited.AddListener(VisualisationUntouched);

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

            if (isAttachedToSurface)
            {
                // Disables/enables all visualisation extrusions depending on whether there is a palm that is open touching it
                if (touchingPointers.Count > 0)
                {
                    bool openPalmFound = false;

                    foreach (var pointer in touchingPointers)
                    {
                        if (!openPalmFound && HandInputManager.Instance.IsPalmOpen((IMixedRealityHand)pointer.Controller))
                        {
                            openPalmFound = true;

                            if (pointer.Controller.ControllerHandedness == Handedness.Left)
                            {
                                isLeftPalmTouchingAndOpen = true;
                            }
                            else if (pointer.Controller.ControllerHandedness == Handedness.Right)
                            {
                                isRightPalmTouchingAndOpen = true;
                            }

                            foreach (var extrusion in visualisationExtrusions)
                            {
                                extrusion.DisableExtrusionHandles();
                            }
                        }
                    }

                    if (!openPalmFound)
                    {
                        isLeftPalmTouchingAndOpen = false;
                        isRightPalmTouchingAndOpen = false;

                        foreach (var extrusion in visualisationExtrusions)
                        {
                            extrusion.EnableExtrusionHandles();
                        }
                    }
                }
            }
        }

        #region Surface placement

        public void VisualisationGrabbed(ManipulationEventData eventData)
        {
            isManipulating = true;

            // If this data visualisation is marked as a prototype, any attempt to grab it immediately clones it
            if (IsPrototype)
            {
                GameObject idxTip = HandInputManager.Instance.GetJointTransform(eventData.Pointer.Controller.ControllerHandedness, Microsoft.MixedReality.Toolkit.Utilities.TrackedHandJoint.IndexTip).gameObject;
                CloneFromSurface(idxTip);
                objectManipulator.ForceEndManipulation();
            }
            else if (isAttachedToSurface)
            {
                // If the opposite hand's palm was touching the visualisation, then we clone from surface instead
                Handedness hand = eventData.Pointer.Controller.ControllerHandedness;
                if ((isLeftPalmTouchingAndOpen && hand == Handedness.Right) || (isRightPalmTouchingAndOpen && hand == Handedness.Left))
                {
                    GameObject idxTip = HandInputManager.Instance.GetJointTransform(eventData.Pointer.Controller.ControllerHandedness, Microsoft.MixedReality.Toolkit.Utilities.TrackedHandJoint.IndexTip).gameObject;
                    CloneFromSurface(idxTip);
                    objectManipulator.ForceEndManipulation();
                }
                else
                {
                    LiftFromSurface();
                }
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

        public void VisualisationTouched(ManipulationEventData eventData)
        {
            if (!touchingPointers.Contains(eventData.Pointer) && eventData.Pointer.Controller != null && eventData.Pointer.Controller.ControllerHandedness != Microsoft.MixedReality.Toolkit.Utilities.Handedness.None)
            {
                touchingPointers.Add(eventData.Pointer);
            }
        }

        public void VisualisationUntouched(ManipulationEventData eventData)
        {
            if (touchingPointers.Contains(eventData.Pointer))
            {
                touchingPointers.Remove(eventData.Pointer);
            }

            if (touchingPointers.Count == 0)
            {
                isLeftPalmTouchingAndOpen = false;

                foreach (var extrusion in visualisationExtrusions)
                {
                    extrusion.EnableExtrusionHandles();
                }
            }
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
            // We pass the values tuple to the splatting method just in case the specific splatting script has a specific position/rotation to set
            values = UpdateVisualisationSplatting(values);
            transform.DOMove(values.Item1, 0.1f);
            transform.DORotate(values.Item2, 0.1f)
                .OnComplete(()=>
                {
                    isAttachedToSurface = true;
                    UpdateVisualisationExtrusion();
                });
        }

        public void CloneFromSurface(GameObject pointer)
        {
            DataVisualisation newVis = DataVisualisationManager.Instance.CloneDataVisualisation(this);

            // Move the new Data Visualisation such that it follows the hand
            newVis.transform.SetParent(pointer.transform);
            newVis.transform.DOLocalMove(Vector3.zero, 0.1f);

            // Add events to release this Data Visualisation when the grab gesture is released
            // Note that we have to use a PointerHandler instead of hooking into the existing ObjectManipulator script as I don't know how to
            // make it think that the manipulation has even begun (there's ForceManipulationEnd but not for start)
            var pointerHandler = newVis.gameObject.AddComponent<PointerHandler>();
            CoreServices.InputSystem.RegisterHandler<IMixedRealityPointerHandler>(pointerHandler);
            pointerHandler.OnPointerUp.AddListener((e) =>
            {
                newVis.transform.parent = null;
                CoreServices.InputSystem.UnregisterHandler<IMixedRealityPointerHandler>(pointerHandler);
                pointerHandler.OnPointerUp.RemoveAllListeners();
                Destroy(pointerHandler);
            });
            // Hook into the existing VisualisationReleased event handler when this visualisation is lifted and immediately placed back on a surface
            pointerHandler.OnPointerUp.AddListener((e) =>
            {
                newVis.VisualisationReleased(new ManipulationEventData());
            });
        }

        public void LiftFromSurface(GameObject pointer = null)
        {
            isAttachedToSurface = false;
            collidingSurfaces.Clear();  // Clear all surfaces as a failsafe if the OnTriggerExit event does not fire in time

            // Handle visualisation splatting scripts
            if (visualisationSplattings.Count > 0)
            {
                foreach (var sp in visualisationSplattings)
                    sp.DestroyThisSplat();
                visualisationSplattings.Clear();
            }

            // Handle visualisation extrusion scripts
            if (visualisationExtrusions.Count > 0)
            {
                foreach (var ex in visualisationExtrusions)
                    ex.DestroyThisExtrusion();
                visualisationExtrusions.Clear();
            }

            // If a pointer was provided, snap this Data Visualisation to that pointer
            if (pointer != null)
            {
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

        /// <summary>
        /// Contains logic to add splatting scripts *directly before* this Data Visualisation is placed on a surface.
        /// Expects a return tuple in case the splatting requires the placement of the visualisation to be modified in some fashion.
        /// </summary>
        private System.Tuple<Vector3, Vector3> UpdateVisualisationSplatting(System.Tuple<Vector3, Vector3> placementValues)
        {
            // Condition 1: Projection flattening for 3D visualisations
            if (VisualisationType == AbstractVisualisation.VisualisationTypes.SCATTERPLOT && (new string[] { XDimension, YDimension, ZDimension}).Where(x => x != "Undefined").Count() == 3)
            {
                var projectionFlattening = gameObject.AddComponent<ProjectionFlatteningSplatting>();
                projectionFlattening.Initialise(dataSource, this, visualisation);
                projectionFlattening.ApplySplat();
                visualisationSplattings.Add(projectionFlattening);
                preventExtrusionForThisPlacement = true;

                // Override the rotation used to place on the surface
                Vector3 localPosOnSurface = nearestSurface.transform.InverseTransformPoint(gameObject.transform.position);
                localPosOnSurface.z = 0;
                Vector3 pos = nearestSurface.transform.TransformPoint(localPosOnSurface);
                pos = pos - nearestSurface.transform.forward * ((boxCollider.size.z + 0.01f) / 2);
                return new System.Tuple<Vector3, Vector3>(pos, nearestSurface.transform.eulerAngles);
            }

            return placementValues;
        }

        /// <summary>
        /// Contains logic to add extrusion scripts *after* this Data Visualisation is placed on a surface
        /// </summary>
        private void UpdateVisualisationExtrusion()
        {
            if (preventExtrusionForThisPlacement)
            {
                preventExtrusionForThisPlacement = false;
                return;
            }

            // Condition 1: Overplotting extrusion for scatterplots with only 2 dimensions, and if the undefined dimension is also the protruding direction
            if (VisualisationType == AbstractVisualisation.VisualisationTypes.SCATTERPLOT && (new string[] { XDimension, YDimension, ZDimension}).Where(x => x != "Undefined").Count() == 2)
            {
                if (XDimension == "Undefined" && protrudingDimension == AxisDirection.X ||
                    YDimension == "Undefined" && protrudingDimension == AxisDirection.Y ||
                    ZDimension == "Undefined" && protrudingDimension == AxisDirection.Z
                )
                {
                    var overplottingExtrusion = gameObject.AddComponent<OverplottingExtrusion>();
                    overplottingExtrusion.Initialise(dataSource, this, visualisation, protrudingDimension);
                    visualisationExtrusions.Add(overplottingExtrusion);
                }
            }
            // Condition 2: PCPs for scatterplots with only 2 dimensions along the X and Y axes, and if the z dimension is also the protruding direction
            if (VisualisationType == AbstractVisualisation.VisualisationTypes.SCATTERPLOT && XDimension != "Undefined" && YDimension != "Undefined" && ZDimension == "Undefined")
            {
                if (protrudingDimension == AxisDirection.Z)
                {
                    var xPCPExtrusion = gameObject.AddComponent<PCPExtrusion>();
                    xPCPExtrusion.Initialise(dataSource, this, visualisation, AxisDirection.X);
                    visualisationExtrusions.Add(xPCPExtrusion);

                    var yPCPExtrusion = gameObject.AddComponent<PCPExtrusion>();
                    yPCPExtrusion.Initialise(dataSource, this, visualisation, AxisDirection.Y);
                    visualisationExtrusions.Add(yPCPExtrusion);
                }
            }

            // Condition 3: SPLOMs for scatterplots with only 2 dimensions along the X and Y axes, and if the z dimension is also the protruding direction
            if (VisualisationType == AbstractVisualisation.VisualisationTypes.SCATTERPLOT && XDimension != "Undefined" && YDimension != "Undefined" && ZDimension == "Undefined")
            {
                if (protrudingDimension == AxisDirection.Z)
                {
                    var splomExtrusion = gameObject.AddComponent<SPLOMExtrusion>();
                    splomExtrusion.Initialise(dataSource, this, visualisation, protrudingDimension);
                    visualisationExtrusions.Add(splomExtrusion);
                }
            }
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

            foreach (var ex in visualisationExtrusions)
                ex.UpdateExtrusionHandles();
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
            if (-0.025f < pos && pos < 0.025f)
            {
                XDimension = "Undefined";
                XAxisManipulator.transform.DOLocalMoveX(0.025f, 0.1f);
            }
        }

        public void YAxisManipulatorReleased(ManipulationEventData eventData)
        {
            isYAxisScaling = false;

            float pos = YAxisManipulator.transform.localPosition.y;
            if (-0.025f < pos && pos < 0.025f)
            {
                YDimension = "Undefined";
                YAxisManipulator.transform.DOLocalMoveY(0.025f, 0.1f);
            }
        }

        public void ZAxisManipulatorReleased(ManipulationEventData eventData)
        {
            isZAxisScaling = false;

            float pos = ZAxisManipulator.transform.localPosition.z;
            if (-0.025f < pos && pos < 0.025f)
            {
                ZDimension = "Undefined";
                ZAxisManipulator.transform.DOLocalMoveZ(0.025f, 0.1f);
            }
        }

        public void ShowAxisManipulators()
        {
            if (XAxisManipulator != null)
                XAxisManipulator.gameObject.SetActive(true);
            if (YAxisManipulator != null)
                YAxisManipulator.gameObject.SetActive(true);
            if (ZAxisManipulator != null)
                ZAxisManipulator.gameObject.SetActive(true);
            if (VisualisationRadialMenu != null)
                VisualisationRadialMenu.gameObject.SetActive(true);
        }

        public void HideAxisManipulators()
        {
            if (XAxisManipulator != null)
                XAxisManipulator.gameObject.SetActive(false);
            if (YAxisManipulator != null)
                YAxisManipulator.gameObject.SetActive(false);
            if (ZAxisManipulator != null)
                ZAxisManipulator.gameObject.SetActive(false);
            if (VisualisationRadialMenu != null)
                VisualisationRadialMenu.gameObject.SetActive(false);
        }

        public void ShowAxes()
        {
            SetXAxisVisibility(true);
            SetYAxisVisibility(true);
            SetZAxisVisibility(true);
        }

        public void HideAxes()
        {
            SetXAxisVisibility(false);
            SetYAxisVisibility(false);
            SetZAxisVisibility(false);
        }

        public void SetXAxisVisibility(bool visible)
        {
            if (visualisation.theVisualizationObject.X_AXIS != null)
                visualisation.theVisualizationObject.X_AXIS.SetActive(visible);
        }

        public void SetYAxisVisibility(bool visible)
        {
            if (visualisation.theVisualizationObject.Y_AXIS != null)
                visualisation.theVisualizationObject.Y_AXIS.SetActive(visible);
        }

        public void SetZAxisVisibility(bool visible)
        {
            if (visualisation.theVisualizationObject.Z_AXIS != null)
                visualisation.theVisualizationObject.Z_AXIS.SetActive(visible);
        }


        #endregion // Axis Scaling

    }
}