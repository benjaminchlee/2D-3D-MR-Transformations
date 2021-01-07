using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using IATK;
using UnityEngine;

namespace SSVis
{
    public class OverplottingExtrusion : BaseVisualisationExtrusion
    {
        [Serializable]
        public enum Mode
        {
            Intervals,
            EqualDistances,
            ColourFrequency,
            ColourGradient
        }

        public override ExtrusionIdiom Idiom { get { return ExtrusionIdiom.Overplotting; }}
        public Mode ExtrusionMode;

        private ExtrusionHandle extrusionHandle;
        private string extrusionDimensionKey;
        private float[] extrusionDataPointOffset;

        public override void InitialiseExtrusionHandles()
        {
            extrusionHandle = (GameObject.Instantiate(Resources.Load("ExtrusionHandle")) as GameObject).GetComponent<ExtrusionHandle>();
            extrusionHandle.Initialise(DataVisualisation, ExtrusionDirection, Vector3.zero, DataVisualisation.Scale, extrusionCloneDistance: 0.25f);
            extrusionHandle.OnExtrusionDistanceChanged.AddListener((e) =>
            {
                ExtrudeDimension(e.distance);
            });
            extrusionHandle.OnExtrusionCloneDistanceReached.AddListener((e) =>
            {
                DataVisualisation.LiftFromSurface(e);
            });
        }

        public override void UpdateExtrusionHandles()
        {
            extrusionHandle.UpdateHandlePositionAndScale(Vector3.zero, DataVisualisation.Scale);
        }

        public override void DestroyThisExtrusion()
        {
            Destroy(extrusionHandle.gameObject);
            Destroy(this);
        }

        private void Start()
        {
            if (ExtrusionMode == Mode.ColourFrequency)
            {
                extrusionDimensionKey = GetExtrusionDimensionKey();
                GenerateOccludedPointOffset_ColourFrequencies();

                Visualisation.OnUpdateViewAction += (property) => {
                    if (property == AbstractVisualisation.PropertyType.X ||
                        property == AbstractVisualisation.PropertyType.Y ||
                        property == AbstractVisualisation.PropertyType.Z
                    )
                    GenerateOccludedPointOffset_ColourFrequencies();
                };
            }
        }

        // Note: This Overplotting Extrusion only uses the distance variable
        public override void ExtrudeDimension(float distance, Vector3? extrusionPoint1 = null, Quaternion? extrusionRotation1 = null, Vector3? extrusionPoint2 = null, Quaternion? extrusionRotation2 = null)
        {
            // Check if extrusion has stopped
            if (CompareFloats(distance, 0))
            {
                if (isExtruding)
                {
                    isExtruding = false;

                    switch (ExtrusionDirection)
                    {
                        case AxisDirection.X:
                            Visualisation.theVisualizationObject.viewList[0].ZeroPosition(0);
                            break;
                        case AxisDirection.Y:
                            Visualisation.theVisualizationObject.viewList[0].ZeroPosition(1);
                            break;
                        case AxisDirection.Z:
                            Visualisation.theVisualizationObject.viewList[0].ZeroPosition(2);
                            break;
                    }

                    DataVisualisation.Scale = startViewScale;
                }

                return;
            }

            // If not, generate a new offset array to position the overplotted points if the dimensions have changed
            if (GetExtrusionDimensionKey() != extrusionDimensionKey)
            {
                // Only run this if 1 or 2 dimensions are set, not all 3 (no 4th dimension to extrude into!)
                int numSet = (DataVisualisation.XDimension != "Undefined") ? 1 : 0;
                numSet += (DataVisualisation.YDimension != "Undefined") ? 1 : 0;
                numSet += (DataVisualisation.ZDimension != "Undefined") ? 1 : 0;
                if (numSet == 3)
                    return;

                extrusionDimensionKey = GetExtrusionDimensionKey();

                switch (ExtrusionMode)
                {
                    case Mode.Intervals:
                        GenerateOccludedPointOffset_Intervals();
                        break;

                    case Mode.EqualDistances:
                        GenerateOccludedPointOffset_EqualDistances();
                        break;

                    case Mode.ColourFrequency:
                        GenerateOccludedPointOffset_ColourFrequencies();
                        break;

                    case Mode.ColourGradient:
                        GenerateOccludedPointOffset_ColourGradient();
                        break;
                }
            }

            // If this extrusion has just started, save the starting scale so that we can revert to it later
            if (!isExtruding)
            {
                startViewScale = DataVisualisation.Scale;
                isExtruding = true;
            }

            // Creates a new position array based on the pre-calculated offset
            float[] positions = new float[DataSource.DataCount];
            for (int i = 0; i < DataSource.DataCount; i++)
            {
                positions[i] = extrusionDataPointOffset[i] * Mathf.Abs(distance);
            }

            // Set position of the points
            switch (ExtrusionDirection)
            {
                case AxisDirection.X:
                    Visualisation.theVisualizationObject.viewList[0].UpdateXPositions(positions);
                    // Mirror the visualisation object to allow it to extrude in the correct direction
                    if (distance < 0)
                        DataVisualisation.Scale = new Vector3(-startViewScale.x, startViewScale.y, startViewScale.z);
                    else
                        DataVisualisation.Scale = new Vector3(startViewScale.x, startViewScale.y, startViewScale.z);
                    break;

                case AxisDirection.Y:
                    Visualisation.theVisualizationObject.viewList[0].UpdateYPositions(positions);
                    if (distance < 0)
                        DataVisualisation.Scale = new Vector3(startViewScale.x, -startViewScale.y, startViewScale.z);
                    else
                        DataVisualisation.Scale = new Vector3(startViewScale.x, startViewScale.y, startViewScale.z);
                    break;

                case AxisDirection.Z:
                    Visualisation.theVisualizationObject.viewList[0].UpdateZPositions(positions);
                    if (distance < 0)
                        DataVisualisation.Scale = new Vector3(startViewScale.x, startViewScale.y, -startViewScale.z);
                    else
                        DataVisualisation.Scale = new Vector3(startViewScale.x, startViewScale.y, startViewScale.z);
                    break;

                default:
                    break;
            }
        }

        private void GenerateOccludedPointOffset_Intervals()
        {
            int dataCount = DataSource.DataCount;
            float[] xPositions = (XDimension != "Undefined") ? DataSource[XDimension].Data : new float[dataCount];
            float[] yPositions = (YDimension != "Undefined") ? DataSource[YDimension].Data : new float[dataCount];
            float[] zPositions = (ZDimension != "Undefined") ? DataSource[ZDimension].Data : new float[dataCount];
            float[] numOverlapping = new float[dataCount];

            Dictionary<Vector3, List<int>> visitedPoints = new Dictionary<Vector3, List<int>>();

            // Determine number of overlapping points
            for (int i = 0; i < dataCount; i++)
            {
                var pos = new Vector3(xPositions[i], yPositions[i], zPositions[i]);

                if (visitedPoints.TryGetValue(pos, out List<int> points))
                {
                    points.Add(i);
                }
                else
                {
                    visitedPoints[pos] = new List<int>(i);
                }
            }

            extrusionDataPointOffset = new float[dataCount];

            foreach (var list in visitedPoints.Values)
            {
                if (list.Count > 1)
                {
                    for (int i = 1; i < list.Count; i++)
                    {
                        int idx = list[i];
                        extrusionDataPointOffset[idx] = NormaliseValue(i, 0, list.Count);
                    }
                }
            }
        }

        private void GenerateOccludedPointOffset_EqualDistances()
        {
            int dataCount = DataSource.DataCount;
            float[] xPositions = (XDimension != "Undefined") ? DataSource[XDimension].Data : new float[dataCount];
            float[] yPositions = (YDimension != "Undefined") ? DataSource[YDimension].Data : new float[dataCount];
            float[] zPositions = (ZDimension != "Undefined") ? DataSource[ZDimension].Data : new float[dataCount];
            float[] numOverlapping = new float[dataCount];

            Dictionary<Vector3, List<int>> visitedPoints = new Dictionary<Vector3, List<int>>();

            // Determine number of overlapping points
            for (int i = 0; i < dataCount; i++)
            {
                var pos = new Vector3(xPositions[i], yPositions[i], zPositions[i]);

                if (visitedPoints.TryGetValue(pos, out List<int> points))
                {
                    points.Add(i);
                }
                else
                {
                    visitedPoints[pos] = new List<int>(i);
                }
            }

            extrusionDataPointOffset = new float[dataCount];

            // Get the group of points with the most highest count, and use that as the max normalise range
            int max = visitedPoints.Values.Max(x => x.Count);

            foreach (var list in visitedPoints.Values)
            {
                if (list.Count > 1)
                {
                    for (int i = 1; i < list.Count; i++)
                    {
                        int idx = list[i];
                        extrusionDataPointOffset[idx] = NormaliseValue(i, 0, max);
                    }
                }
            }
        }

        private void GenerateOccludedPointOffset_ColourFrequencies()
        {
            int dataCount = DataSource.DataCount;
            float[] xPositions = (XDimension != "Undefined") ? DataSource[XDimension].Data : new float[dataCount];
            float[] yPositions = (YDimension != "Undefined") ? DataSource[YDimension].Data : new float[dataCount];
            float[] zPositions = (ZDimension != "Undefined") ? DataSource[ZDimension].Data : new float[dataCount];
            float[] numOverlapping = new float[dataCount];

            Dictionary<Vector3, List<int>> visitedPoints = new Dictionary<Vector3, List<int>>();

            // Determine number of overlapping points
            for (int i = 0; i < dataCount; i++)
            {
                var pos = new Vector3(xPositions[i], yPositions[i], zPositions[i]);

                if (visitedPoints.TryGetValue(pos, out List<int> points))
                {
                    points.Add(i);
                }
                else
                {
                    visitedPoints[pos] = new List<int>(i);
                }
            }

            extrusionDataPointOffset = new float[dataCount];

            // Get the group of points with the most highest count, and use that as the max normalise range
            int max = visitedPoints.Values.Max(x => x.Count);
            Color[] colours = new Color[dataCount];
            for (int i = 0; i < dataCount; i++)
                colours[i] = Color.white;

            foreach (var list in visitedPoints.Values)
            {
                if (list.Count > 1)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        int idx = list[i];
                        extrusionDataPointOffset[idx] = NormaliseValue(i, 0, max);
                        colours[idx] = Color.HSVToRGB(0, list.Count / (float)max, 1);
                    }
                }
            }
            DataVisualisation.Visualisation.theVisualizationObject.viewList[0].SetColors(colours);
        }

        private void GenerateOccludedPointOffset_ColourGradient()
        {
            int dataCount = DataSource.DataCount;
            float[] xPositions = (XDimension != "Undefined") ? DataSource[XDimension].Data : new float[dataCount];
            float[] yPositions = (YDimension != "Undefined") ? DataSource[YDimension].Data : new float[dataCount];
            float[] zPositions = (ZDimension != "Undefined") ? DataSource[ZDimension].Data : new float[dataCount];
            float[] numOverlapping = new float[dataCount];

            Dictionary<Vector3, List<int>> visitedPoints = new Dictionary<Vector3, List<int>>();

            // Determine number of overlapping points
            for (int i = 0; i < dataCount; i++)
            {
                var pos = new Vector3(xPositions[i], yPositions[i], zPositions[i]);

                if (visitedPoints.TryGetValue(pos, out List<int> points))
                {
                    points.Add(i);
                }
                else
                {
                    visitedPoints[pos] = new List<int>(i);
                }
            }

            extrusionDataPointOffset = new float[dataCount];

            // Get the group of points with the most highest count, and use that as the max normalise range
            int max = visitedPoints.Values.Max(x => x.Count);
            Color[] colours = new Color[dataCount];
            for (int i = 0; i < dataCount; i++)
                colours[i] = Color.white;

            foreach (var list in visitedPoints.Values)
            {
                if (list.Count > 1)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        int idx = list[i];
                        extrusionDataPointOffset[idx] = NormaliseValue(i, 0, max);
                        colours[idx] = Color.HSVToRGB(0, i / (float)max, 1);
                    }
                }
            }
            DataVisualisation.Visualisation.theVisualizationObject.viewList[0].SetColors(colours);
        }
    }
}