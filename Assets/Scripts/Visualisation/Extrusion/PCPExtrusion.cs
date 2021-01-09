using System.Collections;
using System.Collections.Generic;
using IATK;
using UnityEngine;

namespace SSVis
{
    public class PCPExtrusion : BaseVisualisationExtrusion
    {
        public override ExtrusionIdiom Idiom { get { return ExtrusionIdiom.ParallelCoordinates; }}

        private ExtrusionHandle extrusionHandle;
        private GameObject attachedAxisObject;

        private List<DataVisualisation> visualisations = new List<DataVisualisation>();
        private List<LinkingVisualisations> linkingVisualisations = new List<LinkingVisualisations>();
        private Vector3 startPos;
        private Vector3 startRot;
        private Vector3 startDir;
        private string constantDimension;
        private readonly float visualisationInterval = 0.15f;

        private static float[,] correlationMatrix;
        private static int numCols;
        private string[] pcpOrdering;
        private bool isCreateLinksCoroutineRunning = false;

        public override void Initialise(DataSource dataSource, DataVisualisation dataVisualisation, Visualisation visualisation, AxisDirection extrusionDirection)
        {
            base.Initialise(dataSource, dataVisualisation, visualisation, extrusionDirection);

            startPos = DataVisualisation.transform.position;
            startRot = DataVisualisation.transform.eulerAngles;
            startDir = DataVisualisation.transform.forward;

            if (correlationMatrix == null)
                CalculateCorrelationMatrix();
            CalculatePCPOrdering();
        }

        public override void InitialiseExtrusionHandles()
        {
            // For this specific extrusion type, extrusion direction denotes the axis that the user pulls on to enable to PCP
            // We need to find the position and shape of the boxcollider used for the axis's dimension slider for our new extrusion handle
            switch (ExtrusionDirection)
            {
                case AxisDirection.X:
                    attachedAxisObject = Visualisation.theVisualizationObject.X_AXIS;
                    break;
                case AxisDirection.Y:
                    attachedAxisObject = Visualisation.theVisualizationObject.Y_AXIS;
                    break;
                case AxisDirection.Z:
                    attachedAxisObject = Visualisation.theVisualizationObject.Z_AXIS;
                    break;
            }

            BoxCollider b = attachedAxisObject.GetComponentInChildren<BoxCollider>();
            Vector3 pos = DataVisualisation.transform.InverseTransformPoint(b.gameObject.transform.TransformPoint(b.center));
            Vector3 scale = Vector3.zero;
            switch (ExtrusionDirection)
            {
                case AxisDirection.X:
                    scale = new Vector3(DataVisualisation.Width, b.size.y, b.size.z);
                    break;
                case AxisDirection.Y:
                    scale = new Vector3(b.size.x, DataVisualisation.Height, b.size.z);
                    break;
                case AxisDirection.Z:
                    scale = new Vector3(b.size.x, b.size.y, DataVisualisation.Depth);
                    break;
            }

            extrusionHandle = (GameObject.Instantiate(Resources.Load("ExtrusionHandle")) as GameObject).GetComponent<ExtrusionHandle>();
            extrusionHandle.Initialise(DataVisualisation, AxisDirection.Z, pos, scale, cloneOnMaxDistance: false, flipExtrusionCollider: true);
            extrusionHandle.OnExtrusionDistanceChanged.AddListener((e) =>
            {
                ExtrudeDimension(e.distance);
            });
        }

        public override void UpdateExtrusionHandles()
        {
            BoxCollider b = attachedAxisObject.GetComponentInChildren<BoxCollider>();
            Vector3 pos = DataVisualisation.transform.InverseTransformPoint(b.gameObject.transform.TransformPoint(b.center));
            Vector3 scale = Vector3.zero;
            switch (ExtrusionDirection)
            {
                case AxisDirection.X:
                    scale = new Vector3(DataVisualisation.Width, b.size.y, b.size.z);
                    break;
                case AxisDirection.Y:
                    scale = new Vector3(b.size.x, DataVisualisation.Height, b.size.z);
                    break;
                case AxisDirection.Z:
                    scale = new Vector3(b.size.x, b.size.y, DataVisualisation.Depth);
                    break;
            }

            extrusionHandle.UpdateHandlePositionAndScale(pos, scale);
        }

        public override void DestroyThisExtrusion()
        {
            Destroy(extrusionHandle.gameObject);
            Destroy(this);
        }

        public override void ExtrudeDimension(float distance, Vector3? extrusionPoint1 = null, Quaternion? extrusionRotation1 = null, Vector3? extrusionPoint2 = null, Quaternion? extrusionRotation2 = null)
        {
            // Position main Data Visualisation
            DataVisualisation.transform.position = startPos + startDir * distance;

            int numVisualisations = visualisations.Count;
            int targetVisualisations = Mathf.FloorToInt(Mathf.Abs(distance) / visualisationInterval);

            // Add visualisations
            if (numVisualisations < targetVisualisations)
            {
                for (int i = 0; i < targetVisualisations - numVisualisations; i++)
                {
                    CreateVisualisation();
                }
            }
            // Remove visualisations
            else if (numVisualisations > targetVisualisations)
            {
                for (int i = 0; i < numVisualisations - targetVisualisations; i++)
                {
                    DeleteVisualisation();
                }
            }

            for (int i = 0; i < visualisations.Count; i++)
            {
                visualisations[i].transform.localPosition = Vector3.Lerp(DataVisualisation.transform.localPosition, startPos, (i + 1) / (float)(targetVisualisations));
                visualisations[i].transform.localEulerAngles = startRot;
            }

            if (!isCreateLinksCoroutineRunning)
                StartCoroutine(CreateLinks());
        }


        private void CreateVisualisation()
        {
            GameObject go = Instantiate(Resources.Load("DataVisualisation")) as GameObject;
            DataVisualisation vis = go.GetComponent<DataVisualisation>();
            visualisations.Add(vis);
            vis.transform.rotation = DataVisualisation.transform.rotation;
            vis.DataSource = DataSource;
            vis.VisualisationType = DataVisualisation.VisualisationType;
            vis.GeometryType = DataVisualisation.GeometryType;

            // TODO: Make this work with non x,y scatterplots in different rotations
            switch (ExtrusionDirection)
            {
                case AxisDirection.X:
                    vis.XDimension = DataVisualisation.XDimension;
                    vis.YDimension = pcpOrdering[visualisations.Count];
                    break;

                case AxisDirection.Y:
                    vis.YDimension = DataVisualisation.YDimension;
                    vis.XDimension = pcpOrdering[visualisations.Count];
                    break;
            }

            vis.Width = DataVisualisation.Width;
            vis.Height = DataVisualisation.Height;
            vis.Depth = DataVisualisation.Depth;
            vis.Size = DataVisualisation.Size;
            vis.Colour = DataVisualisation.Colour;

            //vis.transform.SetParent(transform.parent);

            BrushingAndLinking brushingAndLinking = FindObjectOfType<BrushingAndLinking>();

            if (brushingAndLinking.brushedIndices.Count > 0)
            {
                bool pointsBrushed = false;
                for (int i = 0; i < DataSource.DataCount; i++)
                {
                    if (brushingAndLinking.brushedIndices[i] > 0)
                    {
                        pointsBrushed = true;
                        break;
                    }
                }

                if (pointsBrushed)
                {
                    float[] filter = new float[DataSource.DataCount];

                    for (int i = 0; i < DataSource.DataCount; i++)
                    {
                        filter[i] = brushingAndLinking.brushedIndices[i] > 0 ? 0 : 1;
                    }

                    vis.Visualisation.theVisualizationObject.viewList[0].SetFilterChannel(filter);
                }
            }
        }

        private void DeleteVisualisation()
        {
            if (visualisations.Count > 0)
            {
                var vis = visualisations[visualisations.Count - 1];
                visualisations.RemoveAt(visualisations.Count - 1);
                Destroy(vis.gameObject);
            }
        }

        // Coroutine as bandaid fix for HoloLens 2
        private IEnumerator CreateLinks()
        {
            isCreateLinksCoroutineRunning = true;

            yield return null;

            List<DataVisualisation> visesToLink = new List<DataVisualisation>();

            visesToLink.Add(DataVisualisation);
            visesToLink.AddRange(visualisations);

            for (int i = 0; i < visesToLink.Count - 1; i++)
            {
                DataVisualisation vis1 = visesToLink[i];
                DataVisualisation vis2 = visesToLink[i + 1];

                LinkingVisualisations linkVis;

                if (i + 1 > linkingVisualisations.Count)
                {
                    GameObject go = new GameObject();
                    linkVis = go.AddComponent<LinkingVisualisations>();
                    linkingVisualisations.Add(linkVis);
                }
                else
                {
                    linkVis = linkingVisualisations[i];
                }

                linkVis.visualisationSource = vis1.Visualisation;
                linkVis.visualisationTarget = vis2.Visualisation;
                linkVis.linkTransparency = 1;
            }

            for (int i = linkingVisualisations.Count - 1; i >= visesToLink.Count - 1; i--)
            {
                LinkingVisualisations linkVis = linkingVisualisations[i];
                linkingVisualisations.RemoveAt(i);
                Destroy(linkVis.gameObject);
            }

            yield return null;

            foreach (var linkVis in linkingVisualisations)
            {
                linkVis.showLinks = true;
            }

            isCreateLinksCoroutineRunning = false;
        }

        /// <summary>
        /// Generates a matrix of correlation values between different dimensions in the dataset
        /// </summary>
        [RuntimeInitializeOnLoadMethod]
        public static void CalculateCorrelationMatrix()
        {
            numCols = DataVisualisationManager.Instance.DataSource.DimensionCount;
            correlationMatrix = new float[numCols, numCols];

            for (int i = 0; i < numCols; i++)
            {
                for (int j = 0; j < numCols; j++)
                {
                    var corr = Correlation(DataVisualisationManager.Instance.DataSource[i].Data, DataVisualisationManager.Instance.DataSource[j].Data);
                    correlationMatrix[i,j] = Mathf.Abs(corr);
                }
            }
        }

        /// <summary>
        ///  Calculates the correlation value between two arrays
        /// </summary>
        /// <param name="array1"></param>
        /// <param name="array2"></param>
        /// <returns></returns>
        private static float Correlation(float[] array1, float[] array2)
        {
            double[] array_xy = new double[array1.Length];
            double[] array_xp2 = new double[array1.Length];
            double[] array_yp2 = new double[array1.Length];
            for (int i = 0; i < array1.Length; i++)
            array_xy[i] = array1[i] * array2[i];
            for (int i = 0; i < array1.Length; i++)
            array_xp2[i] = System.Math.Pow(array1[i], 2.0);
            for (int i = 0; i < array1.Length; i++)
            array_yp2[i] = System.Math.Pow(array2[i], 2.0);
            double sum_x = 0;
            double sum_y = 0;
            foreach (double n in array1)
                sum_x += n;
            foreach (double n in array2)
                sum_y += n;
            double sum_xy = 0;
            foreach (double n in array_xy)
                sum_xy += n;
            double sum_xpow2 = 0;
            foreach (double n in array_xp2)
                sum_xpow2 += n;
            double sum_ypow2 = 0;
            foreach (double n in array_yp2)
                sum_ypow2 += n;
            double Ex2 = System.Math.Pow(sum_x, 2.00);
            double Ey2 = System.Math.Pow(sum_y, 2.00);

            return (float)((array1.Length * sum_xy - sum_x * sum_y) /
                System.Math.Sqrt((array1.Length * sum_xpow2 - Ex2) * (array1.Length * sum_ypow2 - Ey2)));
        }

        /// <summary>
        /// Calculates the order that the PCP should be in based on the calculated correlation matrix and the starting dimension
        /// </summary>
        public void CalculatePCPOrdering()
        {
            pcpOrdering = new string[numCols];
            List<int> validIndices = new List<int>();
            for (int i = 0; i < numCols; i++)
                validIndices.Add(i);

            // Find the name of the dimension of the other axis. In this case we assume that there is always two axes defined
            string start = "";
            switch (ExtrusionDirection)
            {
                case AxisDirection.X:
                    start = (DataVisualisation.YDimension) != "Undefined" ? DataVisualisation.YDimension : DataVisualisation.ZDimension;
                    break;
                case AxisDirection.Y:
                    start = (DataVisualisation.XDimension) != "Undefined" ? DataVisualisation.XDimension : DataVisualisation.ZDimension;
                    break;
                case AxisDirection.Z:
                    start = (DataVisualisation.XDimension) != "Undefined" ? DataVisualisation.XDimension : DataVisualisation.YDimension;
                    break;
            }

            int row = DataSource[start].Index;
            pcpOrdering[0] = start;
            validIndices.Remove(row);

            for (int i = 1; i < numCols; i++)
            {
                pcpOrdering[i] = DataSource[row].Identifier;

                float max = 0;
                int idx = -1;

                foreach (int col in validIndices)
                {
                    if (correlationMatrix[row, col] > max)
                    {
                        idx = col;
                        max = correlationMatrix[row, col];
                    }
                }

                pcpOrdering[i] = DataSource[idx].Identifier;
                validIndices.Remove(idx);
            }
        }
    }
}