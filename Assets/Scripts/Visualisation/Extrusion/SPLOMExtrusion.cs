using System;
using System.Collections;
using System.Collections.Generic;
using IATK;
using UnityEngine;

namespace SSVis
{
    public class SPLOMExtrusion : BaseVisualisationExtrusion
    {
        public override ExtrusionIdiom Idiom { get { return ExtrusionIdiom.ScatterplotMatrix; }}

        private ExtrusionHandle extrusionHandle;

        private int numDimensions;
        private List<string> dimensionList;
        private DataVisualisation[,] splomVisualisations;
        private int splomSize = 1;

        private Coroutine createVisualisationsCoroutine;
        private bool isCreateVisualisationsCoroutineRunning = false;

        private const float maximumExtrusionLength = 0.5f;
        private const float maximumSplomWidth = 1f;
        private const float maximumSplomHeight = 1f;
        private const float splomSpacing = 0.02f;
        private Vector3 originalVisualisationScale;
        private int originalVisualisationXDimensionIdx;
        private int originalVisualisationYDimensionIdx;


        public override void Initialise(DataSource dataSource, DataVisualisation dataVisualisation, Visualisation visualisation, AxisDirection extrusionDirection)
        {
            base.Initialise(dataSource, dataVisualisation, visualisation, extrusionDirection);

            splomVisualisations = new DataVisualisation[dataSource.DimensionCount, dataSource.DimensionCount];
            splomVisualisations[0, 0] = DataVisualisation;

            dimensionList = GetDimensionList(dataSource);
            numDimensions = dimensionList.Count;
        }

        public override void InitialiseExtrusionHandles()
        {
            // Place the handle such that it overlaps with the visualisation, and sized slightly larger such that it sticks out from the top and right hand sides
            Vector3 scale = new Vector3(DataVisualisation.Width + 0.1f, DataVisualisation.Height + 0.1f, 0.1f);
            Vector3 position = new Vector3(0.05f, 0.05f, 0);

            extrusionHandle = (GameObject.Instantiate(Resources.Load("ExtrusionHandle")) as GameObject).GetComponent<ExtrusionHandle>();
            extrusionHandle.Initialise(DataVisualisation, AxisDirection.X | AxisDirection.Y, position, scale, initialHandleWidth: scale.x, initialHandleHeight: scale.y, cloneOnMaxDistance: false, disableNegativeExtrusion: true, layer: "UI 2");
            extrusionHandle.OnExtrusionDistanceChanged.AddListener((e) =>
            {
                ExtrudeDimension(e.distance);
            });
        }

        public override void UpdateExtrusionHandles()
        {
            // Place the handle such that it overlaps with the visualisation, and sized slightly larger such that it sticks out from the top and right hand sides
            Vector3 scale = new Vector3(DataVisualisation.Width + 0.1f, DataVisualisation.Height + 0.1f, 0.1f);
            Vector3 position = new Vector3(0.05f, 0.05f, 0);

            extrusionHandle.UpdateHandlePositionAndScale(position, scale);
        }

        public override void DestroyThisExtrusion()
        {
            for (int i = 0; i < numDimensions; i++)
            {
                for (int j = 0; j < numDimensions; j++)
                {
                    var vis = splomVisualisations[i, j];
                    if (vis != DataVisualisation)
                    {
                        Destroy(vis);
                    }
                }
            }

            Destroy(extrusionHandle.gameObject);
            Destroy(this);
        }

        public override void EnableExtrusionHandles()
        {
            extrusionHandle.enabled = true;
        }

        public override void DisableExtrusionHandles()
        {
            extrusionHandle.enabled = false;
        }

        public override void ExtrudeDimension(float distance, Vector3? extrusionPoint1 = null, Quaternion? extrusionRotation1 = null, Vector3? extrusionPoint2 = null, Quaternion? extrusionRotation2 = null)
        {
            float interval = maximumExtrusionLength / (float) numDimensions;
            int newSplomSize = Mathf.Min(Mathf.FloorToInt(distance / interval), numDimensions);

            if (newSplomSize == splomSize || numDimensions < newSplomSize || newSplomSize <= 0)
                return;

            // If this is the first time increasing the size of the SPLOM, we quickly store the original visualisation's scale and starting dimensions
            if (splomSize == 1 && newSplomSize > 1)
            {
                originalVisualisationScale = DataVisualisation.Scale;
                originalVisualisationXDimensionIdx = dimensionList.IndexOf(DataVisualisation.XDimension);
                originalVisualisationYDimensionIdx = dimensionList.IndexOf(DataVisualisation.YDimension);
                DataVisualisation.HideAxisManipulators();
            }

            // If the size of the SPLOM increased, then we have to create visualisations
            if (splomSize < newSplomSize)
            {
                if (isCreateVisualisationsCoroutineRunning)
                {
                    StopCoroutine(createVisualisationsCoroutine);
                    isCreateVisualisationsCoroutineRunning = false;
                }

                createVisualisationsCoroutine = StartCoroutine(CreateAndPositionVisualisationEachFrame(newSplomSize));
            }
            // If the size of the SPLOM decreased, then we have to destroy visualisations
            else if (newSplomSize < splomSize || newSplomSize == 1)
            {
                if (isCreateVisualisationsCoroutineRunning)
                {
                    // Restart the coroutine with the new splom value
                    StopCoroutine(createVisualisationsCoroutine);
                    createVisualisationsCoroutine = StartCoroutine(CreateAndPositionVisualisationEachFrame(newSplomSize));
                }

                for (int i = 0; i < splomSize; i++)
                {
                    for (int j = newSplomSize; j < splomSize; j++)
                    {
                        if (splomVisualisations[i, j] != null)
                        {
                            Destroy(splomVisualisations[i, j].gameObject);
                            splomVisualisations[i, j] = null;
                        }
                        if (splomVisualisations[j, i] != null)
                        {
                            Destroy(splomVisualisations[j, i].gameObject);
                            splomVisualisations[j, i] = null;
                        }
                    }
                }

                SetSPLOMPositions();
            }

            splomSize = newSplomSize;
        }

        private IEnumerator CreateAndPositionVisualisationEachFrame(int targetSplomSize)
        {
            isCreateVisualisationsCoroutineRunning = true;

            float width = Mathf.Min(originalVisualisationScale.x, (maximumSplomWidth - splomSpacing * (targetSplomSize - 1)) / splomSize);
            float height = Mathf.Min(originalVisualisationScale.y, (maximumSplomHeight - splomSpacing * (targetSplomSize - 1)) / splomSize);
            Vector3 right = DataVisualisation.transform.right;
            Vector3 up = DataVisualisation.transform.up;
            int instantiationCount = 0;

            for (int i = 0; i < targetSplomSize; i++)
            {
                for (int j = 0; j < targetSplomSize; j++)
                {
                    // Determine dimensions to set
                    string newXDimension = dimensionList[(originalVisualisationXDimensionIdx + i) % numDimensions];
                    string newYDimension = dimensionList[(originalVisualisationYDimensionIdx + j) % numDimensions];

                    // Instantiate visualisations if they do not yet exist
                    var vis = splomVisualisations[i, j];
                    if (vis == null)
                    {
                        vis = DataVisualisationManager.Instance.CreateDataVisualisation(DataSource, AbstractVisualisation.VisualisationTypes.SCATTERPLOT, AbstractVisualisation.GeometryType.Points,
                                                                                        xDimension: newXDimension, yDimension: newYDimension,
                                                                                        size: DataVisualisation.Size, color: DataVisualisation.Colour, scale: DataVisualisation.Scale);
                        splomVisualisations[i, j] = vis;

                        // Hide parts of the visualisation to improve visibility
                        vis.HideAxisManipulators();
                        if (i > 0)  vis.SetYAxisVisibility(false);
                        if (j > 0)  vis.SetXAxisVisibility(false);

                        instantiationCount++;
                    }
                    vis.transform.position = DataVisualisation.transform.position + (width + splomSpacing) * right * i + (height + splomSpacing) * up * j;
                    vis.transform.rotation = DataVisualisation.transform.rotation;
                    vis.Width = width;
                    vis.Height = height;

                    if (instantiationCount >= 2)
                    {
                        instantiationCount = 0;
                        yield return null;
                    }
                }
            }

            isCreateVisualisationsCoroutineRunning = false;
        }

        private void SetSPLOMPositions()
        {
            if (splomSize == 1)
            {
                DataVisualisation.Scale = originalVisualisationScale;
                DataVisualisation.ShowAxisManipulators();
                return;
            }

            float width = Mathf.Min(originalVisualisationScale.x, maximumSplomWidth / splomSize);
            float height = Mathf.Min(originalVisualisationScale.y, maximumSplomHeight / splomSize);
            Vector3 right = DataVisualisation.transform.right;
            Vector3 up = DataVisualisation.transform.up;

            for (int i = 0; i < splomSize; i++)
            {
                for (int j = 0; j < splomSize; j++)
                {
                    var vis = splomVisualisations[i, j];
                    if (vis != null)
                    {
                        // Set positions
                        vis.transform.position = DataVisualisation.transform.position + width * right * i + height * up * j;
                        vis.transform.rotation = DataVisualisation.transform.rotation;
                        // Set scales
                        vis.Width = width;
                        vis.Height = height;
                    }
                }
            }
        }

        private List<string> GetDimensionList(DataSource dataSource)
        {
            List<string> dimensions = new List<string>();
            for (int i = 0; i < dataSource.DimensionCount; ++i)
            {
                dimensions.Add(dataSource[i].Identifier);
            }
            return dimensions;
        }

    }
}