using System.Collections;
using System.Collections.Generic;
using System.Linq;
using IATK;
using UnityEngine;

namespace Experimental.CrossDimensionalTransfer
{
    public class TrajectoriesExtrusion : VisualisationExtrusion
    {
        public override ExtrusionIdiom Idiom { get { return ExtrusionIdiom.Trajectories; }}
        
        private float[] extrusionDataPointOffset;
        
        public override void ExtrudeDimension(AbstractVisualisation.PropertyType dimension, float distance, Vector3 extrusionPoint1, Quaternion extrusionRotation1, Vector3? extrusionPoint2 = null, Quaternion? extrusionRotation2 = null)
        {
            // Check if extrusion has stopped
            if (CompareFloats(distance, 0))
            {
                if (isExtruding)
                {
                    isExtruding = false;
                    
                    switch (dimension)
                    {
                        case AbstractVisualisation.PropertyType.X:
                            Visualisation.theVisualizationObject.viewList[0].ZeroPosition(0);
                            break;
                        case AbstractVisualisation.PropertyType.Y:
                            Visualisation.theVisualizationObject.viewList[0].ZeroPosition(1);
                            break;
                        case AbstractVisualisation.PropertyType.Z:
                            Visualisation.theVisualizationObject.viewList[0].ZeroPosition(2);
                            break;
                    }
                    
                    DataVisualisation.Scale = startViewScale;
                }
                
                return;
            }
            
            // If this extrusion has just started, calculate a new offset array to position the data points            
            if (!isExtruding)
            {
                GenerateClosestPointOffset(extrusionPoint1);
                startViewScale = DataVisualisation.Scale;
                isExtruding = true;
            }
            
            // Create a new position array based on the pre-calculated offset
            float[] positions = new float[DataSource.DataCount];
            for (int i = 0; i < DataSource.DataCount; i++)
            {
                positions[i] = extrusionDataPointOffset[i] * Mathf.Abs(distance);
            }
            
            // Set position of the points
            switch (dimension)
            {
                case AbstractVisualisation.PropertyType.X:
                    Visualisation.theVisualizationObject.viewList[0].UpdateXPositions(positions);
                    // Mirror the visualisation object to allow it to extrude in the correct direction
                    if (distance < 0)
                        DataVisualisation.Scale = new Vector3(-startViewScale.x, startViewScale.y, startViewScale.z);
                    else
                        DataVisualisation.Scale = new Vector3(startViewScale.x, startViewScale.y, startViewScale.z);
                    break;
                    
                case AbstractVisualisation.PropertyType.Y:
                    Visualisation.theVisualizationObject.viewList[0].UpdateYPositions(positions);
                    if (distance < 0)
                        DataVisualisation.Scale = new Vector3(startViewScale.x, -startViewScale.y, startViewScale.z);
                    else
                        DataVisualisation.Scale = new Vector3(startViewScale.x, startViewScale.y, startViewScale.z);
                    break;
                    
                case AbstractVisualisation.PropertyType.Z:
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
        
        private void GenerateClosestPointOffset(Vector3 extrusionPoint)
        {            
            if (LinkingDimension == "Undefined")
                return;
            
            int dataCount = DataSource.DataCount;
            float[] xPositions = (XDimension != "Undefined") ? DataSource[XDimension].Data : new float[dataCount];
            float[] yPositions = (YDimension != "Undefined") ? DataSource[YDimension].Data : new float[dataCount];
            float[] zPositions = (ZDimension != "Undefined") ? DataSource[ZDimension].Data : new float[dataCount];
            extrusionDataPointOffset = new float[dataCount];
            
            extrusionPoint.x = NormaliseValue(extrusionPoint.x, 0, DataVisualisation.Width, 0, 1);
            extrusionPoint.y = NormaliseValue(extrusionPoint.y, 0, DataVisualisation.Height, 0, 1);
            extrusionPoint.z = NormaliseValue(extrusionPoint.z, 0, DataVisualisation.Depth, 0, 1);
            //extrusionPoint.z = 0;
            
            // Get all unique paths
            float[] ids = DataSource[LinkingDimension].Data;
            float[] paths = ids.Distinct().ToArray();
            int[] steps = new int[dataCount];
            
            // We assume that the dataset is properly formatted for this (i.e., same paths are adjacent in the dataset)
            foreach (float path in paths)
            {
                bool pathFound = false;
                int pathStartIdx;
                for (int i = 0; i < dataCount; i++)
                {
                    if (ids[i] == path)
                    {
                        pathFound = true;
                        pathStartIdx = i;
                        int pathEndIdx = dataCount - 1;
                        int minIdx = -1;
                        float minDistance = Mathf.Infinity;
                        
                        // Find the index closest to the extrusion point
                        for (int j = pathStartIdx; j < dataCount; j++)
                        {
                            if (ids[j] != path)
                            {
                                pathEndIdx = j - 1;
                                break;
                            }
                            
                            float distance = Vector3.Distance(new Vector3(xPositions[j], yPositions[j], zPositions[j]), extrusionPoint);
                            if (distance < minDistance)
                            {
                                minIdx = j;
                                minDistance = distance;
                            }
                        }
                        
                        // Fill in the steps array with number of steps to reach the closest point from the edges
                        bool pointReached = false;
                        int count = 0;
                        int leftSteps = 0;
                        int midSteps = 0;
                        int rightSteps = 0;
                        for (int j = pathStartIdx; j < dataCount; j++)
                        {
                            if (j == minIdx)
                            {
                                pointReached = true;
                                midSteps = count;
                            }
                            
                            if (ids[j] != path)
                            {
                                rightSteps = steps[j - 1];
                                break;
                            }
                            
                            steps[j] = count;
                            count += pointReached ? -1 : 1;
                        }
                        
                        // Normalise these steps between 0 and 1, inverting the values
                        pointReached = false;
                        for (int j = pathStartIdx; j < dataCount; j++)
                        {
                            if (j == minIdx)
                            {
                                pointReached = true;
                                extrusionDataPointOffset[j] = 1;
                            }
                            else
                            {
                                if (ids[j] != path)
                                {
                                    break;
                                }
                                
                                // Left hand side
                                if (!pointReached)
                                {
                                    extrusionDataPointOffset[j] = NormaliseValue(steps[j], leftSteps, midSteps, 0, 1);
                                }
                                else
                                {
                                    extrusionDataPointOffset[j] = NormaliseValue(steps[j], rightSteps, midSteps, 0, 1);
                                }
                            }
                        }
                        
                        i = pathEndIdx + 1;
                    }
                    else if (pathFound)
                    {
                        break;
                    }
                }
            }
            
            // for (int i = 0; i < dataCount; i++)
            // {
            //     var pos = new Vector3(xPositions[i], yPositions[i], zPositions[i]);
            //     float distance = 1 - Vector3.Distance(extrusionPoint, pos);
            //     extrudedDataPointOffset[i] = distance;
            // }
        }
    }
}