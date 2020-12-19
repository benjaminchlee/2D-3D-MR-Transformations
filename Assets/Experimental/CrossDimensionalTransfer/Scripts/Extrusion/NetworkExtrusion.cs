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
        [Serializable]
        public enum Mode
        {
            All,
            ClosestNode,
            Neighbours,
            ShortestPath,
            ShortestPathFloydWarshall
        }

        public override ExtrusionIdiom Idiom { get { return ExtrusionIdiom.Network; }}
        [Header("Network visualisation")]
        public TextAsset EdgesData;
        public string NodeName;
        public string OriginName;
        public string DestinationName;
        public string LinkingName;
        public string EdgeWeightName;
        public Mode ExtrusionMode;

        private float[] extrusionDataPointOffset;
        private Dictionary<string, int> networkNodeStringToIndex;
        private Dictionary<int, List<int>> networkConnectedNodes;
        private Dictionary<System.Tuple<int, int>, float> networkEdgeWeights;

        private List<int> extrudingNodeIndices = new List<int>();
        private Vector3 previousExtrusionPoint1;
        private Vector3 previousExtrusionPoint2;
        private int extrudingSourceIdx;
        private int extrudingDestinationIdx;

        private float[,] shortestPathsMatrix;

        private void Start()
        {
            SetDataSourceEdges();
            Visualisation.graphDimension = "Blah";
            Visualisation.updateViewProperties(AbstractVisualisation.PropertyType.GraphDimension);

            if (ExtrusionMode == Mode.ShortestPathFloydWarshall)
                CreateShortestPathMatrix();
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
                for (int i = 0; i < extrusionDataPointOffset.Length; i++)
                    extrusionDataPointOffset[i] = 0;
                extrudingNodeIndices.Clear();
                // Reset colour
                DataVisualisation.Colour = DataVisualisation.Colour;
                return;
            }

            // We have to calculate the positions array differently depending on the chosen mode
            float[] positions = extrusionDataPointOffset;
            switch (ExtrusionMode)
            {
                case Mode.All:
                {
                    if (!isExtruding)  // If this extrusion has just started, calculate a new offset array to position the data points
                        GenerateWeightedNodeOffset(extrusionPoint1);

                    positions = new float[DataSource.DataCount];    // Create a new position array based on the pre-calculated offset
                    for (int i = 0; i < DataSource.DataCount; i++)
                        positions[i] = extrusionDataPointOffset[i] * Mathf.Abs(distance);
                    break;
                }

                case Mode.ClosestNode:
                {
                    GenerateNodeOffset_Closest(extrusionPoint1, distance);
                    previousExtrusionPoint1 = extrusionPoint1;
                    positions = extrusionDataPointOffset;
                    break;
                }

                case Mode.Neighbours:
                {
                    GenerateNodeOffset_Neighbours(extrusionPoint1, distance);
                    previousExtrusionPoint1 = extrusionPoint1;
                    positions = extrusionDataPointOffset;
                    break;
                }

                case Mode.ShortestPath:
                {
                    if (extrusionPoint2 == null)
                        return;
                    GenerateNodeOffset_ShortestPath(extrusionPoint1, (Vector3)extrusionPoint2, distance);
                    previousExtrusionPoint1 = extrusionPoint1;
                    previousExtrusionPoint2 = (Vector3)extrusionPoint2;
                    positions = extrusionDataPointOffset;
                    break;
                }

                case Mode.ShortestPathFloydWarshall:
                {
                    if (extrusionPoint2 == null)
                        return;
                    GenerateNodeOffset_ShortestPathFloydWarshall(extrusionPoint1, (Vector3)extrusionPoint2, distance);
                    previousExtrusionPoint1 = extrusionPoint1;
                    previousExtrusionPoint2 = (Vector3)extrusionPoint2;
                    positions = extrusionDataPointOffset;
                    break;
                }
            }

            if (!isExtruding)
            {
                startViewScale = DataVisualisation.Scale;
                isExtruding = true;
                Color[] colours = new Color[DataSource.DataCount];
                for (int i = 0; i < DataSource.DataCount; i++)
                    colours[i] = Color.white;
                DataVisualisation.Visualisation.theVisualizationObject.viewList[0].SetColors(colours);
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
                    // if (distance < 0)
                        DataVisualisation.Scale = new Vector3(startViewScale.x, startViewScale.y, -startViewScale.z);
                    // else
                    //     DataVisualisation.Scale = new Vector3(startViewScale.x, startViewScale.y, startViewScale.z);
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

        private void GenerateWeightedNodeOffset(Vector3 extrusionPoint)
        {
            if (networkEdgeWeights == null)
                return;

            int dataCount = DataSource.DataCount;

            float[] xPositions = (XDimension != "Undefined") ? DataSource[XDimension].Data : new float[dataCount];
            float[] yPositions = (YDimension != "Undefined") ? DataSource[YDimension].Data : new float[dataCount];
            float[] zPositions = (ZDimension != "Undefined") ? DataSource[ZDimension].Data : new float[dataCount];

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

            // Get infinites (disconnected points)
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

        private void GenerateNodeOffset_Closest(Vector3 extrusionPoint, float distance)
        {
            if (networkEdgeWeights == null)
                return;

            // Find the closest nodes to the extrusion point if none have already been calculated
            if (extrudingNodeIndices.Count == 0 || extrusionPoint != previousExtrusionPoint1)
            {
                extrudingNodeIndices.Clear();

                int dataCount = DataSource.DataCount;

                // Create the offset array if it doesn't already exist, otherwise we use the same one and just keep updating it
                if (extrusionDataPointOffset == null)
                    extrusionDataPointOffset = new float[dataCount];

                float[] xPositions = (XDimension != "Undefined") ? DataSource[XDimension].Data : extrusionDataPointOffset;
                float[] yPositions = (YDimension != "Undefined") ? DataSource[YDimension].Data : extrusionDataPointOffset;
                float[] zPositions = (ZDimension != "Undefined") ? DataSource[ZDimension].Data : extrusionDataPointOffset;

                extrusionPoint.x = NormaliseValue(extrusionPoint.x, 0, DataVisualisation.Width, 0, 1);
                extrusionPoint.y = NormaliseValue(extrusionPoint.y, 0, DataVisualisation.Height, 0, 1);
                extrusionPoint.z = NormaliseValue(extrusionPoint.z, 0, DataVisualisation.Depth, 0, 1);

                // Get the index of the point closest to the extrusion point
                int closestIdx = -1;
                float minDistance = Mathf.Infinity;
                for (int i = 0; i < dataCount; i++)
                {
                    float d = Vector3.Distance(new Vector3(xPositions[i], yPositions[i], zPositions[i]), extrusionPoint);
                    if (d < minDistance)
                    {
                        closestIdx = i;
                        minDistance = d;
                    }
                }

                extrudingNodeIndices.Add(closestIdx);
            }

            foreach (int idx in extrudingNodeIndices)
            {
                // Update the positions of the extruding nodes
                extrusionDataPointOffset[idx] = Mathf.Abs(distance);
            }
        }

        private void GenerateNodeOffset_Neighbours(Vector3 extrusionPoint, float distance)
        {
            if (networkEdgeWeights == null)
                return;

            // Find the closest nodes to the extrusion point if none have already been calculated
            if (extrudingNodeIndices.Count == 0 || extrusionPoint != previousExtrusionPoint1)
            {
                extrudingNodeIndices.Clear();

                int dataCount = DataSource.DataCount;

                // Create the offset array if it doesn't already exist, otherwise we use the same one and just keep updating it
                if (extrusionDataPointOffset == null)
                    extrusionDataPointOffset = new float[dataCount];

                float[] xPositions = (XDimension != "Undefined") ? DataSource[XDimension].Data : extrusionDataPointOffset;
                float[] yPositions = (YDimension != "Undefined") ? DataSource[YDimension].Data : extrusionDataPointOffset;
                float[] zPositions = (ZDimension != "Undefined") ? DataSource[ZDimension].Data : extrusionDataPointOffset;

                extrusionPoint.x = NormaliseValue(extrusionPoint.x, 0, DataVisualisation.Width, 0, 1);
                extrusionPoint.y = NormaliseValue(extrusionPoint.y, 0, DataVisualisation.Height, 0, 1);
                extrusionPoint.z = NormaliseValue(extrusionPoint.z, 0, DataVisualisation.Depth, 0, 1);

                // Get the index of the point closest to the extrusion point
                int closestIdx = -1;
                float minDistance = Mathf.Infinity;
                for (int i = 0; i < dataCount; i++)
                {
                    float d = Vector3.Distance(new Vector3(xPositions[i], yPositions[i], zPositions[i]), extrusionPoint);
                    if (d < minDistance)
                    {
                        closestIdx = i;
                        minDistance = d;
                    }
                }

                extrudingNodeIndices.Add(closestIdx);

                // Add its neighbours as well
                extrudingNodeIndices.AddRange(networkConnectedNodes[closestIdx]);
            }

            foreach (int idx in extrudingNodeIndices)
            {
                // Update the positions of the extruding nodes
                extrusionDataPointOffset[idx] = Mathf.Abs(distance);
            }
        }

        private void GenerateNodeOffset_ShortestPath(Vector3 extrusionPoint1, Vector3 extrusionPoint2, float distance)
        {
            if (networkEdgeWeights == null)
                return;

            // Find the closest nodes to the two extrusion points if none have already been calculated, then get the nodes which make up the shortest path between the two
            if (extrudingNodeIndices.Count == 0 || extrusionPoint1 != previousExtrusionPoint1 || extrusionPoint2 != previousExtrusionPoint2)
            {
                extrudingNodeIndices.Clear();

                int dataCount = DataSource.DataCount;

                // Create the offset array if it doesn't already exist, otherwise we use the same one and just keep updating it
                if (extrusionDataPointOffset == null)
                    extrusionDataPointOffset = new float[dataCount];

                float[] xPositions = (XDimension != "Undefined") ? DataSource[XDimension].Data : extrusionDataPointOffset;
                float[] yPositions = (YDimension != "Undefined") ? DataSource[YDimension].Data : extrusionDataPointOffset;
                float[] zPositions = (ZDimension != "Undefined") ? DataSource[ZDimension].Data : extrusionDataPointOffset;

                extrusionPoint1.x = NormaliseValue(extrusionPoint1.x, 0, DataVisualisation.Width, 0, 1);
                extrusionPoint1.y = NormaliseValue(extrusionPoint1.y, 0, DataVisualisation.Height, 0, 1);
                extrusionPoint1.z = NormaliseValue(extrusionPoint1.z, 0, DataVisualisation.Depth, 0, 1);
                extrusionPoint2.x = NormaliseValue(extrusionPoint2.x, 0, DataVisualisation.Width, 0, 1);
                extrusionPoint2.y = NormaliseValue(extrusionPoint2.y, 0, DataVisualisation.Height, 0, 1);
                extrusionPoint2.z = NormaliseValue(extrusionPoint2.z, 0, DataVisualisation.Depth, 0, 1);

                // Get the indexes of the points closest to the two extrusion points
                int closestIdx1 = -1;
                int closestIdx2 = -1;
                float minDistance1 = Mathf.Infinity;
                float minDistance2 = Mathf.Infinity;
                for (int i = 0; i < dataCount; i++)
                {
                    float d1 = Vector3.Distance(new Vector3(xPositions[i], yPositions[i], zPositions[i]), extrusionPoint1);
                    float d2 = Vector3.Distance(new Vector3(xPositions[i], yPositions[i], zPositions[i]), extrusionPoint2);
                    if (d1 < minDistance1)
                    {
                        closestIdx1 = i;
                        minDistance1 = d1;
                    }
                    if (d2 < minDistance2)
                    {
                        closestIdx2 = i;
                        minDistance2 = d2;
                    }
                }

                extrudingNodeIndices.Add(closestIdx1);
                extrudingNodeIndices.Add(closestIdx2);

                // Add the points that make up the shortest path (Dijkstra's)
                bool[] visited = new bool[dataCount];
                float[] distances = new float[dataCount];
                int[] previous = new int[dataCount];
                int count = 1;

                for (int i = 0; i < dataCount; i++)
                {
                    visited[i] = false;
                    distances[i] = Mathf.Infinity;
                    previous[i] = -1;
                }
                distances[closestIdx1] = 0;
                while (!visited.All(x => x) && count < dataCount - 1)
                {
                    // Get minimum index
                    int u = -1;
                    float min = Mathf.Infinity;
                    for (int i = 0; i < dataCount; i++)
                    {
                        float dist = distances[i];
                        if (!visited[i] && dist < min)
                        {
                            u = i;
                            min = dist;
                        }
                    }
                    visited[u] = true;

                    // Stop when we find the second index
                    if (u == closestIdx2)
                        break;

                    // Iterate through neighbours
                    foreach (int v in networkConnectedNodes[u])
                    {
                        float alt = distances[u] + networkEdgeWeights[new System.Tuple<int, int>(u, v)];
                        if (alt < distances[v])
                        {
                            distances[v] = alt;
                            previous[v] = u;
                        }
                    }
                    count++;
                }

                if (!visited[closestIdx2])
                {
                    Debug.LogError("Two selected nodes are disconnected from each other.");
                    return;
                }

                List<int> path = new List<int>();
                int w = closestIdx2;
                if (previous[w] != -1 || closestIdx1 == closestIdx2)
                {
                    while (previous[w] != -1)
                    {
                        path.Add(w);
                        w = previous[w];
                    }
                }

                extrudingNodeIndices = path;

                extrusionDataPointOffset = new float[dataCount];
            }

            // Update the positions of the extruding nodes
            foreach (int idx in extrudingNodeIndices)
            {
                extrusionDataPointOffset[idx] = Mathf.Abs(distance);
            }
        }

        private void CreateShortestPathMatrix()
        {
            int dataCount = DataSource.DataCount;

            // Use Floyd-Warshall algorithm
            float[,] distances = new float[dataCount, dataCount];

            for (int i = 0; i < dataCount; i++)
            {
                for (int j = 0; j < dataCount; j++)
                {
                    distances[i, j] = Mathf.Infinity;
                }
            }

            foreach (int source in networkConnectedNodes.Keys)
            {
                foreach (int dest in networkConnectedNodes[source])
                {
                    distances[source, dest] = networkEdgeWeights[new System.Tuple<int, int>(source, dest)];
                }
            }

            for (int i = 0; i < dataCount; i++)
            {
                distances[i, i] = 0;
            }

            for (int k = 0; k < dataCount; k++)
            {
                for (int i = 0; i < dataCount; i++)
                {
                    for (int j = 0; j < dataCount; j++)
                    {
                        if (distances[i, j] > distances[i, k] + distances[k, j])
                            distances[i, j] = distances[i, k] + distances[k, j];
                    }
                }
            }

            shortestPathsMatrix = distances;
        }

        private void GenerateNodeOffset_ShortestPathFloydWarshall(Vector3 extrusionPoint1, Vector3 extrusionPoint2, float distance)
        {

            if (networkEdgeWeights == null)
                SetDataSourceEdges();

            if (networkConnectedNodes == null)
                SetDataSourceEdges();

            if (shortestPathsMatrix == null)
                CreateShortestPathMatrix();

            int dataCount = DataSource.DataCount;

            // Find the closest nodes to the two extrusion points if none have already been calculated, then get the nodes which make up the shortest path between the two
            if (extrudingNodeIndices.Count == 0 || extrusionPoint1 != previousExtrusionPoint1 || extrusionPoint2 != previousExtrusionPoint2)
            {
                extrudingNodeIndices.Clear();

                // Create the offset array if it doesn't already exist, otherwise we use the same one and just keep updating it
                if (extrusionDataPointOffset == null)
                    extrusionDataPointOffset = new float[dataCount];

                float[] xPositions = (XDimension != "Undefined") ? DataSource[XDimension].Data : extrusionDataPointOffset;
                float[] yPositions = (YDimension != "Undefined") ? DataSource[YDimension].Data : extrusionDataPointOffset;
                float[] zPositions = (ZDimension != "Undefined") ? DataSource[ZDimension].Data : extrusionDataPointOffset;

                extrusionPoint1.x = NormaliseValue(extrusionPoint1.x, 0, DataVisualisation.Width, 0, 1);
                extrusionPoint1.y = NormaliseValue(extrusionPoint1.y, 0, DataVisualisation.Height, 0, 1);
                extrusionPoint1.z = NormaliseValue(extrusionPoint1.z, 0, DataVisualisation.Depth, 0, 1);
                extrusionPoint2.x = NormaliseValue(extrusionPoint2.x, 0, DataVisualisation.Width, 0, 1);
                extrusionPoint2.y = NormaliseValue(extrusionPoint2.y, 0, DataVisualisation.Height, 0, 1);
                extrusionPoint2.z = NormaliseValue(extrusionPoint2.z, 0, DataVisualisation.Depth, 0, 1);

                // Get the indexes of the points closest to the two extrusion points
                int closestIdx1 = -1;
                int closestIdx2 = -1;
                float minDistance1 = Mathf.Infinity;
                float minDistance2 = Mathf.Infinity;
                for (int i = 0; i < dataCount; i++)
                {
                    float d1 = Vector3.Distance(new Vector3(xPositions[i], yPositions[i], zPositions[i]), extrusionPoint1);
                    float d2 = Vector3.Distance(new Vector3(xPositions[i], yPositions[i], zPositions[i]), extrusionPoint2);
                    if (d1 < minDistance1)
                    {
                        closestIdx1 = i;
                        minDistance1 = d1;
                    }
                    if (d2 < minDistance2)
                    {
                        closestIdx2 = i;
                        minDistance2 = d2;
                    }
                }
                extrudingNodeIndices.Add(closestIdx1);
                extrudingNodeIndices.Add(closestIdx2);

                // Add the points that make up the shortest path (Dijkstra's)
                bool[] visited = new bool[dataCount];
                float[] distances = new float[dataCount];
                int[] previous = new int[dataCount];
                int count = 1;

                for (int i = 0; i < dataCount; i++)
                {
                    visited[i] = false;
                    distances[i] = Mathf.Infinity;
                    previous[i] = -1;
                }
                distances[closestIdx1] = 0;
                while (!visited.All(x => x) && count < dataCount - 1)
                {
                    // Get minimum index
                    int u = -1;
                    float min = Mathf.Infinity;
                    for (int i = 0; i < dataCount; i++)
                    {
                        float dist = distances[i];
                        if (!visited[i] && dist < min)
                        {
                            u = i;
                            min = dist;
                        }
                    }
                    visited[u] = true;

                    // Stop when we find the second index
                    if (u == closestIdx2)
                        break;

                    // Iterate through neighbours
                    foreach (int v in networkConnectedNodes[u])
                    {
                        float alt = distances[u] + networkEdgeWeights[new System.Tuple<int, int>(u, v)];
                        if (alt < distances[v])
                        {
                            distances[v] = alt;
                            previous[v] = u;
                        }
                    }
                    count++;
                }

                if (!visited[closestIdx2])
                {
                    Debug.LogError("Two selected nodes are disconnected from each other.");
                    return;
                }

                List<int> path = new List<int>();
                path.Add(closestIdx1);
                int w = closestIdx2;
                if (previous[w] != -1 || closestIdx1 == closestIdx2)
                {
                    while (previous[w] != -1)
                    {
                        path.Add(w);
                        w = previous[w];
                    }
                }

                extrudingNodeIndices = path;

                extrusionDataPointOffset = new float[dataCount];
                extrudingSourceIdx = closestIdx1;
                extrudingDestinationIdx = closestIdx2;
            }

            // Update the positions of the extruding nodes
            foreach (int idx in extrudingNodeIndices)
            {
                extrusionDataPointOffset[idx] = Mathf.Abs(distance);
            }

            // Determine the longest "shortest path" between either the source or destination nodes.
            // For a given target node, we pick the shorter path between either the source or destination
            float max = 0;
            for (int i = 0; i < dataCount; i++)
            {
                if (!extrudingNodeIndices.Contains(i))
                {
                    float value = Mathf.Min(shortestPathsMatrix[extrudingSourceIdx, i], shortestPathsMatrix[extrudingDestinationIdx, i]);
                    if (value != Mathf.Infinity && max < value)
                    {
                        max = value;
                    }
                }
            }

            // Set the heights of all of the other nodes based off the shortest path matrix
            // Set the colours of the nodes relative to their heights as well
            Color[] colours = new Color[DataSource.DataCount];
            for (int i = 0; i < DataSource.DataCount; i++)
                colours[i] = Color.white;

            for (int i = 0; i < dataCount; i++)
            {
                //if (!extrudingNodeIndices.Contains(i))
                if (i != extrudingSourceIdx || i != extrudingDestinationIdx)
                {
                    // Use the smaller of shortest paths between the source and destination
                    float weight = Mathf.Min(shortestPathsMatrix[extrudingSourceIdx, i], shortestPathsMatrix[extrudingDestinationIdx, i]);
                    if (weight == Mathf.Infinity)
                    {
                        extrusionDataPointOffset[i] = 0;
                    }
                    else
                    {
                        extrusionDataPointOffset[i] = NormaliseValue(weight, 0, max, Mathf.Abs(distance), 0);

                        // Give all nodes along the shortest path a different colour
                        if (extrudingNodeIndices.Contains(i))
                        {
                            if (i == extrudingSourceIdx || i == extrudingDestinationIdx)
                                colours[i] = new Color(0.5f, 0, 0.5f);
                            else
                                colours[i] = Color.white;
                        }
                        else
                        {
                            colours[i] = Color.HSVToRGB(0, NormaliseValue(weight, 0, max, 0, 1), 1);
                        }
                    }
                }
            }
            DataVisualisation.Visualisation.theVisualizationObject.viewList[0].SetColors(colours);
        }
    }
}