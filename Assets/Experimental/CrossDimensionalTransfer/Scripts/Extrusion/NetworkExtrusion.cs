using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using IATK;
using UnityEngine;

namespace Experimental.CrossDimensionalTransfer
{
    public class NetworkExtrusion : VisualisationExtrusion
    {
        public override ExtrusionIdiom Idiom { get { return ExtrusionIdiom.Network; }}
        [Header("Network visualisation")]
        public TextAsset EdgesData;
        public string NodeName;
        public string OriginName;
        public string DestinationName;
        public string LinkingName;
        public string EdgeWeightName;

        private float[] extrusionDataPointOffset;
        private Dictionary<string, int> networkNodeStringToIndex;
        private Dictionary<int, List<int>> networkConnectedNodes;
        private Dictionary<System.Tuple<int, int>, float> networkEdgeWeights;
        
        private void Start()
        {
            SetDataSourceEdges();
            Visualisation.graphDimension = "Blah";
            Visualisation.updateViewProperties(AbstractVisualisation.PropertyType.GraphDimension);
        }
        
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
                GenerateWeightedPointOffset(extrusionPoint1);
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
        
        private void SetDataSourceEdges()
        {
            CSVDataSource csvDataSource = (CSVDataSource)DataSource;
            char[] split = new char[] { ',', '\t', ';'};
            
            // Form a dictionary of string names and their indices from the original dataset
            string[] nodeDataLines = csvDataSource.data.text.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            Dictionary<int, List<int>> dataSourceGraphEdges = new Dictionary<int, List<int>>();
            networkNodeStringToIndex = new Dictionary<string, int>();
            int nodeIdx = Array.IndexOf(nodeDataLines[0].Split(split, StringSplitOptions.RemoveEmptyEntries), NodeName);
            
            for (int i = 1; i < nodeDataLines.Length; i++)
            {
                string[] values = nodeDataLines[i].Split(split, StringSplitOptions.RemoveEmptyEntries);
                string name = values[nodeIdx];
                networkNodeStringToIndex[name] = i - 1;
            }
            
            // Generate graph edges list
            networkEdgeWeights = new Dictionary<System.Tuple<int, int>, float>();
            networkConnectedNodes = new Dictionary<int, List<int>>();
            string[] edgeDataLines = EdgesData.text.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            string[] headers = edgeDataLines[0].Split(split, StringSplitOptions.RemoveEmptyEntries);
            int originIdx = Array.IndexOf(headers, OriginName);
            int destinationIdx = Array.IndexOf(headers, DestinationName);
            int weightIdx = Array.IndexOf(headers, EdgeWeightName);
            int linkingIdx = Array.IndexOf(headers, LinkingName);
            List<string> visitedGroups = new List<string>();
            
            for (int i = 1; i < edgeDataLines.Length; i++)
            {
                string[] line = edgeDataLines[i].Split(split, StringSplitOptions.RemoveEmptyEntries);
                int origin = networkNodeStringToIndex[line[originIdx]];
                int destination = networkNodeStringToIndex[line[destinationIdx]];
                float weight = float.Parse(line[weightIdx]);
                networkEdgeWeights[new System.Tuple<int, int>(origin, destination)] = weight;
                
                // Set adjacency list dictionary
                if (networkConnectedNodes.TryGetValue(origin, out List<int> list))
                {
                    list.Add(destination);
                }
                else
                {
                    list = new List<int>() { destination };
                    networkConnectedNodes[origin] = list;
                }
                
                // Draw edges between connected points
                string group = line[linkingIdx];
                List<int> edgeList;
                if (visitedGroups.Contains(group))
                {
                    edgeList = dataSourceGraphEdges[visitedGroups.IndexOf(group) + 1];
                }
                else
                {
                    visitedGroups.Add(group);
                    edgeList = new List<int>();
                    dataSourceGraphEdges[visitedGroups.Count] = edgeList;
                }
                
                edgeList.Add(origin);
                edgeList.Add(destination);
            }
            
            // Set graph edges
            csvDataSource.GraphEdges = dataSourceGraphEdges;
        }

        private void GenerateWeightedPointOffset(Vector3 extrusionPoint)
        {
            if (networkEdgeWeights == null)
                return;
            
            int dataCount = DataSource.DataCount;
            
            float[] xPositions = (XDimension != "Undefined") ? DataSource[XDimension].Data : new float[dataCount];
            float[] yPositions = (YDimension != "Undefined") ? DataSource[YDimension].Data : new float[dataCount];
            float[] zPositions = (ZDimension != "Undefined") ? DataSource[ZDimension].Data : new float[dataCount];
            //extrudedDataPointOffset = new float[dataCount];
            
            extrusionPoint.x = NormaliseValue(extrusionPoint.x, 0, DataVisualisation.Width, 0, 1);
            extrusionPoint.y = NormaliseValue(extrusionPoint.y, 0, DataVisualisation.Height, 0, 1);
            extrusionPoint.z = NormaliseValue(extrusionPoint.z, 0, DataVisualisation.Depth, 0, 1);
            
            // Get the index of the point closest to the extrusion point
            int sourceIdx = -1;
            float minDistance = Mathf.Infinity;
            for (int i = 0; i < dataCount; i++)
            {
                float distance = Vector3.Distance(new Vector3(xPositions[i], yPositions[i], zPositions[i]), extrusionPoint);
                if (distance < minDistance)
                {
                    sourceIdx = i;
                    minDistance = distance;
                }
            }
            
            // Calculate the distance of this point to all other points (Dijkstra's)
            bool[] visited = new bool[dataCount];
            float[] distances = new float[dataCount];
            int count = 1;
            
            for (int i = 0; i < dataCount; i++)
            {
                visited[i] = false;
                distances[i] = Mathf.Infinity;
            }
            
            distances[sourceIdx] = 0;
            
            while (!visited.All(x => x) && count < dataCount - 1)
            {
                // Get minimum index
                int u = -1;
                float min = Mathf.Infinity;
                for (int i = 0; i < dataCount; i++)
                {
                    float distance = distances[i];
                    if (!visited[i] && distance < min)
                    {
                        u = i;
                        min = distance;
                    }
                }
                visited[u] = true;
                
                // Iterate through neighbours
                foreach (int v in networkConnectedNodes[u])
                {
                    float alt = distances[u] + networkEdgeWeights[new System.Tuple<int, int>(u, v)];
                    if (alt < distances[v])
                    {
                        distances[v] = alt;
                    }
                }
                
                count++;
            }
            
            // Get infinites (diconnected points)
            List<int> infinites = new List<int>();
            for (int i = 0; i < dataCount; i++)
            {
                if (distances[i] == Mathf.Infinity)
                {
                    distances[i] = 0;
                    infinites.Add(i);
                }
            }
            
            // Normalise values
            float max = distances.Max();
            for (int i = 0; i < dataCount; i++)
            {
                distances[i] = 1 - NormaliseValue(distances[i], 0, max, 0, 1);
            }
            
            // Set previously infinite points to 0
            foreach (int i in infinites)
            {
                distances[i] = 0;
            }
            
            extrusionDataPointOffset = distances;
        }
    }
}