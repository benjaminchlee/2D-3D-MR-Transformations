using DG.Tweening;
using IATK;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Experimental.CrossDimensionalTransfer
{
    /// <summary>
    /// A prototype surface based visualisation which creates new visualisations when the handle is pulled away from the screen.
    /// </summary>
    public class PullSliderSBV : MonoBehaviour
    {
        public DataVisualisation SlidingVisualisation;
        public ObjectManipulator XSliderHandle;
        public ObjectManipulator YSliderHandle;
        public ButtonConfigHelper Toggle1D2DButton;
        public float VisualisationInterval = 0.2f;
        public DataSource DataSource;
        
        private List<DataVisualisation> visualisations = new List<DataVisualisation>();
        private List<LinkingVisualisations> linkingVisualisations = new List<LinkingVisualisations>();
        private Vector3 startPos;
        private Vector3 startRot;
        private bool isSliderOpen = false;
        private bool isSliderClosing = false;
        private string constantDimension;
        
        private SlidingAxis slidingAxis = SlidingAxis.None;
        private SlidingDimension slidingDimension = SlidingDimension.TwoD;
        
        private float[,] correlationMatrix;
        private string[] pcpOrdering;
        private int numCols;
        private bool isCreateLinksCoroutineRunning = false;
        
        private Vector3 originalLabelPos;
        private Quaternion originalLabelRot;
        
        private enum SlidingAxis
        {
            None,
            X,
            Y
        }
        
        private enum SlidingDimension
        {
            OneD,
            TwoD
        }
        
        private void Start()
        {
            XSliderHandle.OnManipulationStarted.AddListener(XSliderGrabbed);
            YSliderHandle.OnManipulationStarted.AddListener(YSliderGrabbed);
            XSliderHandle.OnManipulationEnded.AddListener(XSliderReleased);
            YSliderHandle.OnManipulationEnded.AddListener(YSliderReleased);
            
            Toggle1D2DButton.OnClick.AddListener(Toggle1D2D);
            Toggle1D2DButton.gameObject.SetActive(false);
            
            CalculateCorrelationMatrix();
        }

        private void Update()
        {
            if ((isSliderOpen && slidingAxis != SlidingAxis.None) || isSliderClosing)
            {
                ObjectManipulator slider = slidingAxis == SlidingAxis.X ? XSliderHandle : YSliderHandle;
                
                // Position main Data Visualisation
                float xPos = SlidingVisualisation.transform.localPosition.x;
                float yPos = SlidingVisualisation.transform.localPosition.y;
                SlidingVisualisation.transform.localPosition = new Vector3(xPos, yPos, slider.transform.localPosition.z);
                
                float distance = -SlidingVisualisation.transform.localPosition.z;
                int numVisualisations = visualisations.Count;
                int targetVisualisations = Mathf.FloorToInt(distance / VisualisationInterval);
                
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
                    visualisations[i].transform.localPosition = Vector3.Lerp(SlidingVisualisation.transform.localPosition, startPos, (i + 1) / (float)(targetVisualisations));
                    visualisations[i].transform.localEulerAngles = startRot;
                }
                
                if (!isCreateLinksCoroutineRunning)
                    StartCoroutine(CreateLinks());
            }            
        }
        
        private void CreateVisualisation()
        {
            GameObject go = Instantiate(Resources.Load("DataVisualisation") as GameObject);
            DataVisualisation vis = go.GetComponent<DataVisualisation>();
            visualisations.Add(vis);
            vis.VisualisationType = SlidingVisualisation.VisualisationType;
            vis.GeometryType = SlidingVisualisation.GeometryType;
            
            switch (slidingAxis)
            {
                case SlidingAxis.X:
                    vis.XDimension = SlidingVisualisation.XDimension;
                    vis.YDimension = pcpOrdering[visualisations.Count];
                    break;
                    
                case SlidingAxis.Y:
                    vis.YDimension = SlidingVisualisation.YDimension;
                    vis.XDimension = pcpOrdering[visualisations.Count];
                    break;
            }
            
            vis.Width = SlidingVisualisation.Width;
            vis.Height = SlidingVisualisation.Height;
            vis.Depth = SlidingVisualisation.Depth;
            vis.Size = SlidingVisualisation.Size;
            
            switch (slidingDimension)
            {
                case SlidingDimension.OneD:
                    SetVisualisation1D(vis);
                    break;
                    
                case SlidingDimension.TwoD:
                    SetVisualisation2D(vis);
                    break;
            }
            
            var cloneGrab = vis.gameObject.AddComponent<CloneVisualisationGrab>();
            
            vis.transform.SetParent(transform.parent);
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
                
        private void CloseSlider()
        {
            isSliderClosing = true;
            
            XSliderHandle.transform.DOLocalMoveZ(-0.0055f, 0.1f).OnComplete(() => {
                 slidingAxis = SlidingAxis.None;
                 isSliderOpen = false;
                 isSliderClosing = false;
            });
            
            YSliderHandle.transform.DOLocalMoveZ(-0.0055f, 0.1f);
            
            XSliderHandle.gameObject.SetActive(true);
            YSliderHandle.gameObject.SetActive(true);
            Toggle1D2DButton.gameObject.SetActive(false);
            
            Set1D2D(SlidingDimension.TwoD);
        }
        
        private void Toggle1D2D()
        {
            if (slidingDimension == SlidingDimension.OneD)
                Set1D2D(SlidingDimension.TwoD);
            else
                Set1D2D(SlidingDimension.OneD);
        }
        
        private void Set1D2D(SlidingDimension mode)
        {
            slidingDimension = mode;
            
            List<DataVisualisation> visualisationsToUpdate = new List<DataVisualisation>();
            visualisationsToUpdate.Add(SlidingVisualisation);
            visualisationsToUpdate.AddRange(visualisations);
            
            switch (slidingDimension)
            {
                case SlidingDimension.OneD:
                    var attributeLabel = slidingAxis == SlidingAxis.X ? SlidingVisualisation.YAxisObject.transform.Find("AttributeLabel") : SlidingVisualisation.XAxisObject.transform.Find("AttributeLabel");
                    originalLabelPos = attributeLabel.localPosition;
                    originalLabelRot = attributeLabel.localRotation;
                    foreach (var vis in visualisationsToUpdate)
                    {
                        SetVisualisation1D(vis);
                    }
                    break;
                
                case SlidingDimension.TwoD:
                    foreach (var vis in visualisationsToUpdate)
                    {
                        SetVisualisation2D(vis);
                    }
                    break;
            }
        }
        
        private void SetVisualisation1D(DataVisualisation visualisation)
        {
            Transform attributeLabel;
            
            switch (slidingAxis)
            {
                case SlidingAxis.X:
                    visualisation.XDimension = "Undefined";
                    attributeLabel = visualisation.YAxisObject.transform.Find("AttributeLabel");
                    attributeLabel.localPosition = new Vector3(0, -0.04f, 0);
                    attributeLabel.localEulerAngles = new Vector3(0, 90, 0);
                    break;
                
                case SlidingAxis.Y:
                    visualisation.YDimension = "Undefined";
                    attributeLabel = visualisation.XAxisObject.transform.Find("AttributeLabel");
                    attributeLabel.localPosition = new Vector3(0, -0.025f, 0);
                    attributeLabel.localEulerAngles = new Vector3(-90, 0, 0);
                    break;
            }
        }
        
        private void SetVisualisation2D(DataVisualisation visualisation)
        {
            Transform attributeLabel = null;
            
            switch (slidingAxis)
            {
                case SlidingAxis.X:
                    visualisation.XDimension = constantDimension;
                    attributeLabel = visualisation.YAxisObject.transform.Find("AttributeLabel");
                    break;
                
                case SlidingAxis.Y:
                    visualisation.YDimension = constantDimension;
                    attributeLabel = visualisation.XAxisObject.transform.Find("AttributeLabel");
                    break;
            }
            
            if (originalLabelPos != Vector3.zero)
            {
                attributeLabel.localPosition = originalLabelPos;
                attributeLabel.localRotation = originalLabelRot;
            }
        }
        
        // Coroutine as bandaid fix for HoloLens 2
        private IEnumerator CreateLinks()
        {
            isCreateLinksCoroutineRunning = true;
            
            List<DataVisualisation> visesToLink = new List<DataVisualisation>();
            
            visesToLink.Add(SlidingVisualisation);
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
                Destroy(linkVis.gameObject);
                linkingVisualisations.RemoveAt(i);
            }
            
            yield return null;
            
            foreach (var linkVis in linkingVisualisations)
            {
                linkVis.showLinks = true;
            }
            
            isCreateLinksCoroutineRunning = false;
        }
        
        private void XSliderGrabbed(ManipulationEventData arg0)
        {
            if (slidingAxis == SlidingAxis.None)
            {
                slidingAxis = SlidingAxis.X;
                isSliderOpen = true;
                
                startPos = SlidingVisualisation.transform.localPosition;
                startRot = SlidingVisualisation.transform.localEulerAngles;
                YSliderHandle.gameObject.SetActive(false);
                Toggle1D2DButton.gameObject.SetActive(true);
                
                constantDimension = SlidingVisualisation.XDimension;
                
                CalculatePCPOrdering();
            }
        }

        private void YSliderGrabbed(ManipulationEventData arg0)
        {
            if (slidingAxis == SlidingAxis.None)
            {
                slidingAxis = SlidingAxis.Y;
                isSliderOpen = true;
                
                startPos = SlidingVisualisation.transform.localPosition;
                startRot = SlidingVisualisation.transform.localEulerAngles;
                XSliderHandle.gameObject.SetActive(false);
                Toggle1D2DButton.gameObject.SetActive(true);
                
                constantDimension = SlidingVisualisation.YDimension;
                
                CalculatePCPOrdering();
            }
        }

        private void XSliderReleased(ManipulationEventData arg0)
        {
            XSliderHandle.gameObject.SetActive(true);
            YSliderHandle.gameObject.SetActive(true);
            
            if (visualisations.Count == 0)
                CloseSlider();
        }

        private void YSliderReleased(ManipulationEventData arg0)
        {
            XSliderHandle.gameObject.SetActive(true);
            YSliderHandle.gameObject.SetActive(true);
            
            if (visualisations.Count == 0)
                CloseSlider();
        }
        
        private void CalculateCorrelationMatrix()
        {
            DataSource dataSource = DataVisualisationManager.Instance.DataSource;
            numCols = dataSource.DimensionCount;
            correlationMatrix = new float[numCols, numCols];
            
            for (int i = 0; i < numCols; i++)
            {
                for (int j = 0; j < numCols; j++)
                {
                    var corr = Correlation(dataSource[i].Data, dataSource[j].Data);
                    correlationMatrix[i,j] = Mathf.Abs(corr);
                }
            }
        }
        
        public float Correlation(float[] array1, float[] array2)
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
        
        public void CalculatePCPOrdering()
        {
            DataSource dataSource = DataVisualisationManager.Instance.DataSource;
            
            pcpOrdering = new string[numCols];
            List<int> validIndices = new List<int>();
            for (int i = 0; i < numCols; i++)
                validIndices.Add(i);
            
            string start = "";
            switch (slidingAxis)
            {
                case SlidingAxis.X:
                    start = SlidingVisualisation.YDimension;
                    break;
                case SlidingAxis.Y:
                    start = SlidingVisualisation.XDimension;
                    break;
            }
            int row = dataSource[start].Index;
            pcpOrdering[0] = start;
            validIndices.Remove(row);
            
            for (int i = 1; i < numCols; i++)
            {
                pcpOrdering[i] = dataSource[row].Identifier;
                
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
                
                pcpOrdering[i] = dataSource[idx].Identifier;
                validIndices.Remove(idx);
            }
        }
    }
}

