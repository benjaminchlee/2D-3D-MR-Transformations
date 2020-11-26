using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using IATK;
using UnityEngine;

namespace Experimental.CrossDimensionalTransfer
{
    public class HistogramExtrusion : VisualisationExtrusion
    {
        public override ExtrusionIdiom Idiom { get { return ExtrusionIdiom.Histogram; }}
        public float extrusionBarDistance = 0.05f;
        
        private float[] extrusionHistogramYPointOffset;
        private float[] extrusionHistogramZPointOffset;
        private int currentBins = 0;
        
        public override void ExtrudeDimension(AbstractVisualisation.PropertyType dimension, float distance, Vector3 extrusionPoint1, Quaternion extrusionRotation1, Vector3? extrusionPoint2 = null, Quaternion? extrusionRotation2 = null)
        {
            // Check if extrusion has stopped
            if (CompareFloats(distance, 0))
            {
                if (isExtruding)
                {
                    isExtruding = false;
                    // Force reset all dimensions
                    DataVisualisation.XDimension = XDimension;
                    DataVisualisation.YDimension = YDimension;
                    DataVisualisation.ZDimension = ZDimension;
                    DataVisualisation.Scale = startViewScale;
                }
                return;
            }
            
            // We have to keep generating a new offset
            GenerateHistogramPointOffset(extrusionPoint1, distance);
                
            if (!isExtruding)
            {
                startViewScale = DataVisualisation.Scale;
                isExtruding = true;
            }
            
            // Create a new position array based on the pre-calculated offset
            float[] positions = new float[DataSource.DataCount];
            for (int i = 0; i < DataSource.DataCount; i++)
            {
                positions[i] = extrusionHistogramZPointOffset[i] * Mathf.Abs(distance);
            }
            
            // Set the x position of the points
            Visualisation.theVisualizationObject.viewList[0].UpdateZPositions(positions);
            Visualisation.theVisualizationObject.viewList[0].UpdateYPositions(extrusionHistogramYPointOffset);
            
            // Mirror the visualisation object to allow it to extrude in the correct direction 
            if (distance < 0)
                DataVisualisation.Scale = new Vector3(startViewScale.x, startViewScale.y, -startViewScale.z);
            else
                DataVisualisation.Scale = new Vector3(startViewScale.x, startViewScale.y, startViewScale.z);
        }

        // Here we override the points set by the Visualisation by calculating our own bins
        private void GenerateHistogramPointOffset(Vector3 extrusionPoint, float distance)
        {
            // Only run this if the number of bins has changed
            float size = DataVisualisation.Size * 0.05f;
            int numBins = Mathf.FloorToInt(Mathf.Abs(distance) / size);
            float remainder = Mathf.Abs(distance) % size;
            numBins = Mathf.Max(1, numBins);
            
            if (numBins == currentBins && extrusionHistogramYPointOffset != null)
                return;
                
            currentBins = numBins;
            int dataCount = DataSource.DataCount;
            
            float[] xPositions = (XDimension != "Undefined") ? DataSource[XDimension].Data : new float[dataCount];
            float[] yPositions = (YDimension != "Undefined") ? DataSource[YDimension].Data : new float[dataCount];
            float[] zPositions = (ZDimension != "Undefined") ? DataSource[ZDimension].Data : new float[dataCount];
            
            // Create bins that we will use for the extruded z axis
            DiscreteBinner binner = new DiscreteBinner();
            binner.MakeIntervals(yPositions, numBins);
            
            // TODO: Here we blindly assume that the x dimension is the categorical axis, and the y is quantitative
            // Also here we work mostly with floats, rather than their string representations
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
            extrusionHistogramZPointOffset = new float[dataCount];
            extrusionHistogramYPointOffset = new float[dataCount];
            for (int i = 0; i < categories.Length; i++)
            {
                // Determine which bin each data point belongs to, and set its z offset position accordingly
                List<int>[] binnedIndices = new List<int>[numBins];
                for (int j = 0; j < binnedIndices.Length; j++) { binnedIndices[j] = new List<int>(); }
                foreach (int idx in dataIndices[i])
                {
                    int bin = binner.Bin(yPositions[idx]);
                    binnedIndices[bin].Add(idx);
                    extrusionHistogramZPointOffset[idx] = (bin / (float)numBins) * (1 - remainder);
                }
                
                // For each bin, calculate its height
                for (int j = 0; j < binnedIndices.Length; j++)
                {
                    float height = binnedIndices[j].Count / (float)dataCount;
                    foreach (int idx in binnedIndices[j])
                    {
                        extrusionHistogramYPointOffset[idx] = height;
                    }
                }
            }
        }
    }
}
