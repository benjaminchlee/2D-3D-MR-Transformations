using IATK;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Experimental.CrossDimensionalTransfer
{
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
        
        [Header("Network visualisation")]
        [SerializeField]
        private TextAsset edgesData;
        [SerializeField]
        private string nodeName;
        [SerializeField]
        private string originName;
        [SerializeField]
        private string destinationName;
        [SerializeField]
        private string linkingName;
        [SerializeField]
        private string edgeWeightName;
        
        // [Header("Histogram visualisation")]
        // [SerializeField]
        // private string 
        
        // Data extrusion variables
        private string extrusionDataPointKey;
        private float[] extrudedDataPointOffset;
        private bool isExtruding = false;
        private Vector3 startViewScale;
        
        // Network variables
        private Dictionary<string, int> networkNodeStringToIndex;
        private Dictionary<int, List<int>> networkConnectedNodes;
        private Dictionary<System.Tuple<int, int>, float> networkEdgeWeights;

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
                //GenerateExtrusionOffset();
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
                //GenerateExtrusionOffset();
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
                //GenerateExtrusionOffset();
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
                visualisation.updateViewProperties(AbstractVisualisation.PropertyType.Scaling);
            }
        }

        public float Height
        {
            get { return visualisation.height; }
            set
            {
                visualisation.height = value;
                visualisation.updateViewProperties(AbstractVisualisation.PropertyType.Scaling);
            }
        }

        public float Depth
        {
            get { return visualisation.depth; }
            set
            {
                visualisation.depth = value;
                visualisation.updateViewProperties(AbstractVisualisation.PropertyType.Scaling);
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

                visualisation.updateViewProperties(AbstractVisualisation.PropertyType.Scaling);
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
                DataSource = DataVisualisationManager.Instance.DataSource;
        }
        
        private void Start()
        {
            if (edgesData != null && dataSource != null && nodeName != "" && originName != "" && destinationName != "")
            {
                SetDataSourceEdges();
                visualisation.graphDimension = "Blah";
                GeometryType = AbstractVisualisation.GeometryType.LinesAndDots;
            }
        }
        
        private void Update()
        {
            AdjustVisualisationLocalPosition();
            AdjustCollider();
        }

        private void AdjustVisualisationLocalPosition()
        {
            float xPos = (XDimension != "Undefined") ? -Width / 2 : 0;
            float yPos = (YDimension != "Undefined") ? -Height / 2 : 0;
            float zPos = (ZDimension != "Undefined") ? -Depth : 0;

            visualisation.transform.localPosition = new Vector3(xPos, yPos, zPos);
        }
        
        private void AdjustCollider()
        {
            float xScale = (XDimension != "Undefined") ? Width : 0.075f;
            float yScale = (YDimension != "Undefined") ? Height : 0.075f;
            float zScale = (ZDimension != "Undefined") ? Depth : 0.075f;
            boxCollider.size = new Vector3(xScale, yScale, zScale);

            float xPos = 0;
            float yPos = 0;
            float zPos = (ZDimension != "Undefined") ? -Depth / 2 : 0;
            boxCollider.center = new Vector3(xPos, yPos, zPos);
        }
        
        private void SetDataSourceEdges()
        {
            CSVDataSource csvDataSource = (CSVDataSource)dataSource;
            char[] split = new char[] { ',', '\t', ';'};
            
            // Form a dictionary of string names and their indices from the original dataset
            string[] nodeDataLines = csvDataSource.data.text.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            Dictionary<int, List<int>> dataSourceGraphEdges = new Dictionary<int, List<int>>();
            networkNodeStringToIndex = new Dictionary<string, int>();
            int nodeIdx = Array.IndexOf(nodeDataLines[0].Split(split, StringSplitOptions.RemoveEmptyEntries), nodeName);
            
            for (int i = 1; i < nodeDataLines.Length; i++)
            {
                string[] values = nodeDataLines[i].Split(split, StringSplitOptions.RemoveEmptyEntries);
                string name = values[nodeIdx];
                networkNodeStringToIndex[name] = i - 1;
            }
            
            // Generate graph edges list
            networkEdgeWeights = new Dictionary<System.Tuple<int, int>, float>();
            networkConnectedNodes = new Dictionary<int, List<int>>();
            string[] edgeDataLines = edgesData.text.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            string[] headers = edgeDataLines[0].Split(split, StringSplitOptions.RemoveEmptyEntries);
            int originIdx = Array.IndexOf(headers, originName);
            int destinationIdx = Array.IndexOf(headers, destinationName);
            int weightIdx = Array.IndexOf(headers, edgeWeightName);
            int linkingIdx = Array.IndexOf(headers, linkingName);
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

        private void GenerateExtrusionOffset(Vector3 extrusionPoint)
        {
            if (VisualisationType == AbstractVisualisation.VisualisationTypes.SCATTERPLOT)
            {
                switch (GeometryType)
                {
                    case AbstractVisualisation.GeometryType.Points:
                    case AbstractVisualisation.GeometryType.Spheres:
                    case AbstractVisualisation.GeometryType.Bars:
                    case AbstractVisualisation.GeometryType.Cubes:
                    case AbstractVisualisation.GeometryType.Quads:
                        if (gameObject.transform.parent.gameObject.name == "NetworkChart")
                        {
                            extrusionDataPointKey = GetExtrusionDimensionKey();
                            GenerateWeightedPointOffset(extrusionPoint);
                            break;
                        }
                        
                    
                        // Only run this if 1 or 2 dimensions are set, not all 3 (no 4th dimension to extrude into!)
                        int numSet = (XDimension != "Undefined") ? 1 : 0;
                        numSet += (YDimension != "Undefined") ? 1 : 0;
                        numSet += (ZDimension != "Undefined") ? 1 : 0;
                        if (numSet == 3)
                            return;
                        string thisKey = GetExtrusionDimensionKey();
                        if (string.Equals(thisKey, extrusionDataPointKey))
                            return;
                        extrusionDataPointKey = thisKey;
                        GenerateOccludedPointOffset(extrusionPoint);
                        break;
                    
                    case AbstractVisualisation.GeometryType.Lines:
                        extrusionDataPointKey = GetExtrusionDimensionKey();
                        GenerateDistancePointOffset(extrusionPoint);
                        break;
                        
                    case AbstractVisualisation.GeometryType.LinesAndDots:
                        extrusionDataPointKey = GetExtrusionDimensionKey();
                        GenerateWeightedPointOffset(extrusionPoint);
                        break;
                }
            }
        }

        private void GenerateOccludedPointOffset(Vector3 extrusionPoint)
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
            
            extrudedDataPointOffset = new float[dataCount];
            
            foreach (var list in visitedPoints.Values)
            {
                if (list.Count > 1)
                {
                    for (int i = 1; i < list.Count; i++)
                    {
                        int idx = list[i];                        
                        extrudedDataPointOffset[idx] = NormaliseValue(i, 0, list.Count);
                    }
                }
            }
        }
        
        private void GenerateDistancePointOffset(Vector3 extrusionPoint)
        {            
            if (LinkingDimension == "Undefined")
                return;
            
            int dataCount = DataSource.DataCount;
            
            float[] xPositions = (XDimension != "Undefined") ? DataSource[XDimension].Data : new float[dataCount];
            float[] yPositions = (YDimension != "Undefined") ? DataSource[YDimension].Data : new float[dataCount];
            float[] zPositions = (ZDimension != "Undefined") ? DataSource[ZDimension].Data : new float[dataCount];
            extrudedDataPointOffset = new float[dataCount];
            
            extrusionPoint.x = NormaliseValue(extrusionPoint.x, 0, Width, 0, 1);
            extrusionPoint.y = NormaliseValue(extrusionPoint.y, 0, Height, 0, 1);
            //extrusionPoint.z = NormaliseValue(extrusionPoint.z, 0, Depth, 0, 1);
            extrusionPoint.z = 0;
            
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
                                extrudedDataPointOffset[j] = 1;
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
                                    extrudedDataPointOffset[j] = NormaliseValue(steps[j], leftSteps, midSteps, 0, 1);
                                }
                                else
                                {
                                    extrudedDataPointOffset[j] = NormaliseValue(steps[j], rightSteps, midSteps, 0, 1);
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
        
        private void GenerateWeightedPointOffset(Vector3 extrusionPoint)
        {
            if (networkEdgeWeights == null)
                return;
            
            int dataCount = DataSource.DataCount;
            
            float[] xPositions = (XDimension != "Undefined") ? DataSource[XDimension].Data : new float[dataCount];
            float[] yPositions = (YDimension != "Undefined") ? DataSource[YDimension].Data : new float[dataCount];
            float[] zPositions = (ZDimension != "Undefined") ? DataSource[ZDimension].Data : new float[dataCount];
            //extrudedDataPointOffset = new float[dataCount];
            
            extrusionPoint.x = NormaliseValue(extrusionPoint.x, 0, Width, 0, 1);
            extrusionPoint.y = NormaliseValue(extrusionPoint.y, 0, Height, 0, 1);
            //extrusionPoint.z = NormaliseValue(extrusionPoint.z, 0, Depth, 0, 1);
            extrusionPoint.z = 0;
            
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
            
            extrudedDataPointOffset = distances;
        }
        
        private string GetExtrusionDimensionKey()
        {
            return string.Format("X:{0}Y:{1}Z:{2}Linking:{3}", XDimension, YDimension, ZDimension, LinkingDimension);
        }
        
        private float NormaliseValue(float value, float i0, float i1, float j0 = 0, float j1 = 1)
        {
            float L = (j0 - j1) / (i0 - i1);
            return (j0 - (L * i0) + (L * value));
        }


        public void ExtrudeDimension(AbstractVisualisation.PropertyType dimension, Vector3 extrusionPoint, float distance)
        {
            if (!isExtruding && (GetExtrusionDimensionKey() != extrusionDataPointKey ||
                GeometryType == AbstractVisualisation.GeometryType.LinesAndDots ||
                GeometryType == AbstractVisualisation.GeometryType.Lines ||
                gameObject.transform.parent.name == "NetworkChart"))
                GenerateExtrusionOffset(extrusionPoint);
            
            if (distance == 0)
            {
                if (isExtruding)
                {
                    isExtruding = false;
                    
                    switch (dimension)
                    {
                        case AbstractVisualisation.PropertyType.X:
                            visualisation.theVisualizationObject.viewList[0].ZeroPosition(0);
                            break;
                            
                        case AbstractVisualisation.PropertyType.Y:
                            visualisation.theVisualizationObject.viewList[0].ZeroPosition(1);
                            break;
                            
                        case AbstractVisualisation.PropertyType.Z:
                            visualisation.theVisualizationObject.viewList[0].ZeroPosition(2);
                            break;
                        
                        default:
                            break;
                    }
                    
                    Scale = startViewScale;
                }
                return;
            }
            
            if (!isExtruding)
            {
                startViewScale = Scale;
                isExtruding = true;
            }
            
            float[] data = new float[DataSource.DataCount];
            for (int i = 0; i < DataSource.DataCount; i++)
            {
                data[i] = extrudedDataPointOffset[i] * Mathf.Abs(distance);
            }
            
            switch (dimension)
            {
                case AbstractVisualisation.PropertyType.X:
                    visualisation.theVisualizationObject.viewList[0].UpdateXPositions(data);
                    if (distance < 0)
                        Scale = new Vector3(-startViewScale.x, startViewScale.y, startViewScale.z);
                    else
                        Scale = new Vector3(startViewScale.x, startViewScale.y, startViewScale.z);
                    break;
                    
                case AbstractVisualisation.PropertyType.Y:
                    visualisation.theVisualizationObject.viewList[0].UpdateYPositions(data);
                    if (distance < 0)
                        Scale = new Vector3(startViewScale.x, -startViewScale.y, startViewScale.z);
                    else
                        Scale = new Vector3(startViewScale.x, startViewScale.y, startViewScale.z);
                    break;
                    
                case AbstractVisualisation.PropertyType.Z:
                    visualisation.theVisualizationObject.viewList[0].UpdateZPositions(data);
                    if (distance < 0)
                        Scale = new Vector3(startViewScale.x, startViewScale.y, -startViewScale.z);       
                    else
                        Scale = new Vector3(startViewScale.x, startViewScale.y, startViewScale.z);        
                    break;
                
                default:
                    break;
            }
        }

        // private void AdjustBoundingBoxSize()
        // {
        //     if (XDimension == "Undefined" && YDimension == "Undefined" && ZDimension == "Undefined")
        //     {
        //         BoundingBoxProxy.size = Vector3.zero;
        //     }
        //     else
        //     {
        //         float xScale = (XDimension != "Undefined") ? Width : 0.1f;
        //         float yScale = (YDimension != "Undefined") ? Height : 0.1f;
        //         float zScale = (ZDimension != "Undefined") ? Depth : 0.1f;

        //         BoundingBoxProxy.size = new Vector3(xScale, yScale, zScale);
        //     }
        // }
    }
}