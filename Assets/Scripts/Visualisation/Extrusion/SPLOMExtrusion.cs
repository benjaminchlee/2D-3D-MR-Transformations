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

        private const float maximumSplomWidth = 1.5f;
        private const float maximumSplomHeight = 1.5f;
        private const float splomSpacing = 0.02f;

        private Vector3 originalVisualisationPosition;
        private Vector3 originalVisualisationScale;
        private int originalVisualisationXDimensionIdx;
        private int originalVisualisationYDimensionIdx;

        private float extrusionInterval;
        private bool isCreatingNewVisualisations = false;
        private float currentExtrusionDistance = 0;
        private float previousExtrusionDistance = 0;

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
            extrusionHandle.Initialise(DataVisualisation, AxisDirection.X | AxisDirection.Y, position, scale, initialHandleWidth: scale.x, initialHandleHeight: scale.y, cloneOnMaxDistance: false, disableNegativeExtrusion: true, layer: "Back Trigger Layer");
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
            // If this is the first time the extrusion distance has changed, we quickly store the original visualisation's scale and starting dimensions
            if (!isExtruding && distance > 0)
            {
                originalVisualisationPosition = DataVisualisation.transform.position;
                originalVisualisationScale = DataVisualisation.Scale;
                originalVisualisationXDimensionIdx = dimensionList.IndexOf(DataVisualisation.XDimension);
                originalVisualisationYDimensionIdx = dimensionList.IndexOf(DataVisualisation.YDimension);
                DataVisualisation.HideAxisManipulators();

                // Calculate and store the interval which causes a the SPLOM to increase in size
                extrusionInterval = Mathf.Max((maximumSplomWidth - originalVisualisationScale.x) / (float)numDimensions,
                                       (maximumSplomHeight - originalVisualisationScale.y) / (float)numDimensions);

                isExtruding = true;
            }

            currentExtrusionDistance = distance;

            int newSplomSize = Mathf.Min(Mathf.FloorToInt(distance / extrusionInterval) + 1, numDimensions);
            if (numDimensions < newSplomSize || newSplomSize <= 0)
                return;

            // If the size of the SPLOM increased, then we have to create visualisations
            if (splomSize < newSplomSize)
            {
                // This flag causes visualisations to be created in the update loop
                isCreatingNewVisualisations = true;
            }
            // If the size of the SPLOM decreased, then we have to destroy visualisations
            else if (newSplomSize < splomSize)
            {
                /// We do that immediately here as the update loop should be able to resolve itself
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

            }

            // Restore the original visualisation's properties if it's the only one left
            if (newSplomSize == 1 && currentExtrusionDistance == 0)
            {
                DataVisualisation.ShowAxisManipulators();
                DataVisualisation.transform.position = originalVisualisationPosition;
                DataVisualisation.Scale = originalVisualisationScale;

                isCreatingNewVisualisations = false;
                previousExtrusionDistance = 0;
                isExtruding = false;
            }


            splomSize = newSplomSize;
        }

        private void Update()
        {
            if (isCreatingNewVisualisations || (previousExtrusionDistance != currentExtrusionDistance))
            {
                // Lerp values between the expected visualisation scale at this SPLOM size and the next SPLOM size
                float thisSplomWidth = Mathf.Min(originalVisualisationScale.x + extrusionInterval * (splomSize - 1), maximumSplomWidth);
                float thisSplomHeight = Mathf.Min(originalVisualisationScale.y + extrusionInterval * (splomSize - 1), maximumSplomHeight);

                float nextSplomWidth = Mathf.Min(originalVisualisationScale.x + extrusionInterval * splomSize, maximumSplomWidth);
                float nextSplomHeight = Mathf.Min(originalVisualisationScale.y + extrusionInterval * splomSize, maximumSplomHeight);

                float t = (currentExtrusionDistance % extrusionInterval) / extrusionInterval;

                float visWidth = Mathf.Lerp((thisSplomWidth - splomSpacing * (splomSize - 1)) / splomSize,
                                            (nextSplomWidth - splomSpacing * splomSize) / (splomSize + 1),
                                            t);
                float visHeight = Mathf.Lerp((thisSplomHeight - splomSpacing * (splomSize - 1)) / splomSize,
                                            (nextSplomHeight - splomSpacing * splomSize) / (splomSize + 1),
                                            t);

                Vector3 right = DataVisualisation.transform.right;
                Vector3 up = DataVisualisation.transform.up;

                int instantiationCount = 0;
                bool isThereMoreToInstantiate = false;

                for (int i = 0; i < splomSize; i++)
                {
                    for (int j = 0; j < splomSize; j++)
                    {
                        // Determine dimensions to set
                        string newXDimension = dimensionList[(originalVisualisationXDimensionIdx + i) % numDimensions];
                        string newYDimension = dimensionList[(originalVisualisationYDimensionIdx + j) % numDimensions];

                        var vis = splomVisualisations[i, j];
                        // Create a new visualisation in the SPLOM if need be
                        if (vis == null)
                        {
                            // We set a max on how many can be created each frame to prevent lag spikes. If we aren't allowed to instantiate any more, set a flag to ensure it attempts again the next frame
                            if (instantiationCount > 4)
                            {
                                isThereMoreToInstantiate = true;
                                continue;
                            }

                            vis = DataVisualisationManager.Instance.CreateDataVisualisation(DataSource, AbstractVisualisation.VisualisationTypes.SCATTERPLOT, AbstractVisualisation.GeometryType.Points,
                                                                                            xDimension: newXDimension, yDimension: newYDimension,
                                                                                            size: DataVisualisation.Size, color: DataVisualisation.Colour, scale: DataVisualisation.Scale);
                            splomVisualisations[i, j] = vis;

                            // Hide parts of the visualisation to improve visibility
                            vis.HideAxisManipulators();
                            if (i > 0)  vis.SetYAxisVisibility(false);
                            if (j > 0)  vis.SetXAxisVisibility(false);

                            // Mark the visualisation as a prototype so the user can grab a copy of any visualisation
                            vis.IsPrototype = true;

                            instantiationCount++;
                        }

                        // Position and resize visualisations. We can do all of them in a single frame if necessary
                        vis.transform.position = originalVisualisationPosition + (visWidth + splomSpacing) * right * i + (visHeight + splomSpacing) * up * j;
                        vis.transform.rotation = DataVisualisation.transform.rotation;
                        vis.Width = visWidth;
                        vis.Height = visHeight;
                    }
                }

                // If we made it to the end and there's no more left to instantiate, we can disable the creating visualisations flag
                if (!isThereMoreToInstantiate)
                {
                    isCreatingNewVisualisations = false;
                }

                previousExtrusionDistance = currentExtrusionDistance;
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