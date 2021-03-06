using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using IATK;
using UnityEngine;

namespace SSVis
{
    public class HistogramExtrusion : BaseVisualisationExtrusion
    {
        public override ExtrusionIdiom Idiom { get { return ExtrusionIdiom.Histogram; }}

        public bool LiftOnMaxDistance = true;

        private ExtrusionHandle extrusionHandle;
        private float[] extrusionHistogramYPointOffset;
        private float[] extrusionHistogramZPointOffset;
        private int currentBins = 0;
        private int maxBarCount;

        public override void InitialiseExtrusionHandles()
        {
            extrusionHandle = (GameObject.Instantiate(Resources.Load("ExtrusionHandle")) as GameObject).GetComponent<ExtrusionHandle>();
            extrusionHandle.Initialise(DataVisualisation, ExtrusionDirection, Vector3.zero, DataVisualisation.Scale, extrusionCloneDistance: 0.25f);
            extrusionHandle.OnExtrusionDistanceChanged.AddListener((e) =>
            {
                ExtrudeDimension(e.distance, e.extrusionPointRight != null? e.extrusionPointRight : e.extrusionPointLeft);
            });
            extrusionHandle.OnExtrusionCloneDistanceReached.AddListener((e) =>
            {
                if (LiftOnMaxDistance)
                    DataVisualisation.LiftFromSurface(e);
            });
        }

        public override void UpdateExtrusionHandles()
        {
            extrusionHandle.UpdateHandlePositionAndScale(Vector3.zero, DataVisualisation.Scale);
        }

        public override void DestroyThisExtrusion()
        {
            // Reset all overplotting done
            DataVisualisation.XDimension = DataVisualisation.XDimension;
            DataVisualisation.YDimension = DataVisualisation.YDimension;
            DataVisualisation.ZDimension = DataVisualisation.ZDimension;
            DataVisualisation.Colour = DataVisualisation.Colour;

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
            // Check if extrusion has stopped
            if (CompareFloats(distance, 0))
            {
                if (isExtruding)
                {
                    isExtruding = false;
                    // Force reset dimensions
                    DataVisualisation.XDimension = XDimension;
                    DataVisualisation.ZDimension = ZDimension;
                    DataVisualisation.Scale = startViewScale;
                    maxBarCount = 0;
                }
                return;
            }

            // We have to keep generating a new offset
            GenerateHistogramPointOffset((Vector3)extrusionPoint1, distance);

            if (!isExtruding)
            {
                startViewScale = DataVisualisation.Scale;
                isExtruding = true;
            }

            // Create a new position array based on the calculated offset
            float absoluteDistance = Mathf.Abs(distance * (1 / DataVisualisation.Depth));
            float[] positions = new float[DataSource.DataCount];
            for (int i = 0; i < DataSource.DataCount; i++)
            {
                positions[i] = extrusionHistogramZPointOffset[i] * absoluteDistance + (absoluteDistance / currentBins / 2f);
            }

            // Set the positions of the points
            Visualisation.theVisualizationObject.viewList[0].UpdateYPositions(extrusionHistogramYPointOffset);
            Visualisation.theVisualizationObject.viewList[0].UpdateZPositions(positions);

            // Mirror the visualisation object to allow it to extrude in the correct direction
            if (distance < 0)
                DataVisualisation.Scale = new Vector3(startViewScale.x, startViewScale.y, -startViewScale.z);
            else
                DataVisualisation.Scale = new Vector3(startViewScale.x, startViewScale.y, startViewScale.z);

            // Update the size of the bars to match the extrusion
            Visualisation.theVisualizationObject.viewList[0].BigMesh.SharedMaterial.SetFloat("_Depth", absoluteDistance / currentBins / 2f);
        }

        // Here we override the points set by the Visualisation by calculating our own bins
        private void GenerateHistogramPointOffset(Vector3 extrusionPoint, float distance)
        {
            // Only run this if the number of bins has changed
            //float size = DataVisualisation.Width / (float)DataVisualisation.NumXBins;
            float size = DataVisualisation.Width * 0.05f;
            int numBins = Mathf.FloorToInt(Mathf.Abs(distance) / size);
            float remainder = Mathf.Abs(distance) % size;
            numBins = Mathf.Max(1, numBins);

            if (numBins == currentBins && extrusionHistogramYPointOffset != null)
                return;

            currentBins = numBins;
            int dataCount = DataSource.DataCount;

            float[] xPositions = Visualisation.theVisualizationObject.viewList[0].GetPositions().Select(x => x.x).ToArray();
            float[] yPositions = DataSource[YDimension].Data;

            // Create bins that we will use for the extruded z axis
            DiscreteBinner binner = new DiscreteBinner();
            binner.MakeIntervals(yPositions, numBins);

            // The x dimension should already be binned for us
            float[] categories = xPositions.Distinct().ToArray();

            // Sort each data point into separate categories
            List<int>[] dataIndices = new List<int>[categories.Length];
            for (int i = 0; i < categories.Length; i++)
            {
                dataIndices[i] = new List<int>();
            }
            for (int i = 0; i < dataCount; i++)
            {
                int idx = Array.IndexOf(categories, xPositions[i]);
                dataIndices[idx].Add(i);
            }

            // For each category, determine which points are in which bins, then set the position values accordingly based on count
            extrusionHistogramYPointOffset = new float[dataCount];
            extrusionHistogramZPointOffset = new float[dataCount];
            float[] masterBars = new float[dataCount];

            for (int i = 0; i < categories.Length; i++)
            {
                // Determine which bin each data point belongs to, and set its z offset position accordingly
                List<int>[] binnedIndices = new List<int>[numBins];
                for (int j = 0; j < binnedIndices.Length; j++)
                    binnedIndices[j] = new List<int>();

                foreach (int idx in dataIndices[i])
                {
                    int bin = binner.Bin(yPositions[idx]);
                    binnedIndices[bin].Add(idx);
                    extrusionHistogramZPointOffset[idx] = (bin / (float)numBins) * (1 - remainder);
                }

                // For each bin, calculate its height
                for (int j = 0; j < binnedIndices.Length; j++)
                {
                    maxBarCount = Mathf.Max(binnedIndices[j].Count, maxBarCount);
                    float height = binnedIndices[j].Count / (float)maxBarCount;
                    foreach (int idx in binnedIndices[j])
                    {
                        extrusionHistogramYPointOffset[idx] = height;
                    }

                    // Also set one of them as the master bar
                    if (binnedIndices[j].Count > 0)
                        masterBars[binnedIndices[j][0]] = 1;
                }
            }

            // Send the master values to the mesh now
            Visualisation.theVisualizationObject.viewList[0].BigMesh.MapUVChannel(0, 1, masterBars);
        }
    }
}