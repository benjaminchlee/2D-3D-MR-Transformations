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

        private List<string> dimensionList;
        private DataVisualisation[,] splomVisualisations;
        private int splomSize = 1;
        private float splomInterval;

        public override void Initialise(DataSource dataSource, DataVisualisation dataVisualisation, Visualisation visualisation, AxisDirection extrusionDirection)
        {
            base.Initialise(dataSource, dataVisualisation, visualisation, extrusionDirection);

            splomVisualisations = new DataVisualisation[dataSource.DimensionCount, dataSource.DimensionCount];
            splomVisualisations[0, 0] = DataVisualisation;
            splomInterval = Mathf.Sqrt(DataVisualisation.Width * DataVisualisation.Width + DataVisualisation.Height * DataVisualisation.Height);

            dimensionList = GetDimensionList(dataSource);
        }

        public override void InitialiseExtrusionHandles()
        {
            // Place the handle such that it overlaps with the visualisation, and sized slightly larger such that it sticks out from the top and right hand sides
            Vector3 scale = new Vector3(DataVisualisation.Width + 0.05f, DataVisualisation.Height + 0.05f, 0.1f);
            Vector3 position = new Vector3(0.025f, 0.025f, 0);

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
            for (int i = 0; i < DataSource.DimensionCount; i++)
            {
                for (int j = 0; j < DataSource.DimensionCount; j++)
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
            distance = Mathf.Pow(Mathf.Abs(distance) * 50, 2) / 50;

            int newSplomSize = Mathf.FloorToInt(distance / splomInterval) + 1;

            if (newSplomSize == splomSize || DataSource.DimensionCount < newSplomSize || newSplomSize <= 0)
                return;

            // If the size of the SPLOM increased, then we have to create visualisations
            if (splomSize < newSplomSize)
            {
                int xDimensionIdx = dimensionList.IndexOf(DataVisualisation.XDimension);
                int yDimensionIdx = dimensionList.IndexOf(DataVisualisation.YDimension);
                int dimensionCount = dimensionList.Count;

                for (int i = 0; i < newSplomSize; i++)
                {
                    for (int j = splomSize; j < newSplomSize; j++)
                    {
                        // Determine dimensions to set
                        int newXDimensionIdx = xDimensionIdx + i;
                        if (newXDimensionIdx >= dimensionCount) newXDimensionIdx = 0;
                        int newYDimensionIdx = yDimensionIdx + j;
                        if (newYDimensionIdx >= dimensionCount) newYDimensionIdx = 0;

                        if (splomVisualisations[i, j] == null)
                        {
                            splomVisualisations[i, j] = DataVisualisationManager.Instance.CreateDataVisualisation(DataSource,
                                                                                                                  AbstractVisualisation.VisualisationTypes.SCATTERPLOT,
                                                                                                                  AbstractVisualisation.GeometryType.Points,
                                                                                                                  xDimension: dimensionList[newXDimensionIdx],
                                                                                                                  yDimension: dimensionList[newYDimensionIdx],
                                                                                                                  size: DataVisualisation.Size,
                                                                                                                  color: DataVisualisation.Colour,
                                                                                                                  scale: DataVisualisation.Scale);
                        }
                        if (splomVisualisations[j, i] == null)
                        {
                            splomVisualisations[j, i] = DataVisualisationManager.Instance.CreateDataVisualisation(DataSource,
                                                                                                                  AbstractVisualisation.VisualisationTypes.SCATTERPLOT,
                                                                                                                  AbstractVisualisation.GeometryType.Points,
                                                                                                                  xDimension: dimensionList[newYDimensionIdx],
                                                                                                                  yDimension: dimensionList[newXDimensionIdx],
                                                                                                                  size: DataVisualisation.Size,
                                                                                                                  color: DataVisualisation.Colour,
                                                                                                                  scale: DataVisualisation.Scale);
                        }
                    }
                }
            }
            // If the size of the SPLOM decreased, then we have to destroy visualisations
            else if (newSplomSize < splomSize)
            {
                for (int i = 0; i < splomSize; i++)
                {
                    for (int j = newSplomSize; j < splomSize; j++)
                    {
                        if (splomVisualisations[i, j] != null)
                            Destroy(splomVisualisations[i, j].gameObject);
                        if (splomVisualisations[j, i] != null)
                            Destroy(splomVisualisations[j, i].gameObject);
                    }
                }
            }

            splomSize = newSplomSize;

            SetSPLOMPositions();
        }

        private void SetSPLOMPositions()
        {
            float width = DataVisualisation.Width;
            float height = DataVisualisation.Height;
            Vector3 right = DataVisualisation.transform.right;
            Vector3 up = DataVisualisation.transform.up;

            for (int i = 0; i < splomSize; i++)
            {
                for (int j = 0; j < splomSize; j++)
                {
                    // Skip the original visualisation
                    if (i == 0 && j == 0)
                        continue;

                    var vis = splomVisualisations[i, j];

                    // Set positions
                    vis.transform.position = DataVisualisation.transform.position + width * right * i + height * up * j;
                    vis.transform.rotation = DataVisualisation.transform.rotation;
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