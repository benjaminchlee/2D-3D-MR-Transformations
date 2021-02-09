using IATK;
using Microsoft.MixedReality.Toolkit.Utilities;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace SSVis
{
    public class DetailsOnDemand : MonoBehaviour
    {
        public ComputeShader IndexDistancesComputeShader;
        public Handedness HandToUse;

        private int kernelComputeDistancesArray;

        private ComputeBuffer dataBuffer;
        private ComputeBuffer filteredIndicesBuffer;
        private ComputeBuffer distancesBuffer;

        private Transform inspectionPoint;
        private bool isDetailsOnDemandEnabled = false;

        private Visualisation visualisationToInspect;

        private AsyncGPUReadbackRequest computeDistancesRequest;
        private NativeArray<float> distancesArray;

        private TextMeshPro detailsOnDemandLabel;
        private LineRenderer detailsOnDemandLine;

        private void Start()
        {
            IndexDistancesComputeShader = Instantiate(IndexDistancesComputeShader);

            kernelComputeDistancesArray = IndexDistancesComputeShader.FindKernel("ComputeDistancesArray");

            Visualisation.OnUpdateViewAction += (e) => {
                if (isDetailsOnDemandEnabled)
                {
                    UpdateComputeShaderWithVisualisationData(visualisationToInspect);
                }
            };

            InitialiseDetailsOnDemandLabel();
            InitialiseDetailsOnDemandLine();
        }

        private void InitialiseBuffers(int dataCount)
        {
            ReleaseBuffers();

            dataBuffer = new ComputeBuffer(dataCount, 12);
            filteredIndicesBuffer = new ComputeBuffer(dataCount, 4);
            distancesBuffer = new ComputeBuffer(dataCount, 4);
            distancesBuffer.SetData(new float[dataCount]);
            IndexDistancesComputeShader.SetBuffer(kernelComputeDistancesArray, "distancesBuffer", distancesBuffer);
        }

        private void UpdateComputeShaderWithVisualisationData(Visualisation visualisation)
        {
            if (visualisation.visualisationType == AbstractVisualisation.VisualisationTypes.SCATTERPLOT)
            {
                dataBuffer.SetData(visualisation.theVisualizationObject.viewList[0].BigMesh.getBigMeshVertices());
                IndexDistancesComputeShader.SetBuffer(kernelComputeDistancesArray, "dataBuffer", dataBuffer);

                filteredIndicesBuffer.SetData(visualisation.theVisualizationObject.viewList[0].GetFilterChannel());
                IndexDistancesComputeShader.SetBuffer(kernelComputeDistancesArray, "filteredIndicesBuffer", filteredIndicesBuffer);

                IndexDistancesComputeShader.SetFloat("_MinNormX", visualisation.xDimension.minScale);
                IndexDistancesComputeShader.SetFloat("_MaxNormX", visualisation.xDimension.maxScale);
                IndexDistancesComputeShader.SetFloat("_MinNormY", visualisation.yDimension.minScale);
                IndexDistancesComputeShader.SetFloat("_MaxNormY", visualisation.yDimension.maxScale);
                IndexDistancesComputeShader.SetFloat("_MinNormZ", visualisation.zDimension.minScale);
                IndexDistancesComputeShader.SetFloat("_MaxNormZ", visualisation.zDimension.maxScale);

                IndexDistancesComputeShader.SetFloat("_MinX", visualisation.xDimension.minFilter);
                IndexDistancesComputeShader.SetFloat("_MaxX", visualisation.xDimension.maxFilter);
                IndexDistancesComputeShader.SetFloat("_MinY", visualisation.yDimension.minFilter);
                IndexDistancesComputeShader.SetFloat("_MaxY", visualisation.yDimension.maxFilter);
                IndexDistancesComputeShader.SetFloat("_MinZ", visualisation.zDimension.minFilter);
                IndexDistancesComputeShader.SetFloat("_MaxZ", visualisation.zDimension.maxFilter);

                IndexDistancesComputeShader.SetFloat("width", visualisation.width);
                IndexDistancesComputeShader.SetFloat("height", visualisation.height);
                IndexDistancesComputeShader.SetFloat("depth", visualisation.depth);
            }
        }

        private void Update()
        {
            // #if UNITY_EDITOR
            // if (HandInputManager.Instance.IsHandTracked(HandToUse))
            // #else
            if (HandInputManager.Instance.IsHandIndexPointing(HandToUse))
            // #endif
            {
                if (inspectionPoint == null)
                {
                    inspectionPoint = HandInputManager.Instance.GetJointTransform(HandToUse, TrackedHandJoint.IndexTip);
                }

                // Find the nearest visualisation to the inspection point
                var nearestVisualisation = GetNearestVisualisation();

                // If no visualisation was found, don't proceed any further
                if (nearestVisualisation == null)
                    return;

                // If this visualisation has changed, or if it is the first time running, update the compute shader with its data
                // Also run if it has an extrusion script associated with it, as these change the visualisation's points as well
                if (nearestVisualisation != visualisationToInspect || nearestVisualisation.GetComponent<BaseVisualisationExtrusion>() != null)
                {
                    visualisationToInspect = nearestVisualisation;
                    InitialiseBuffers(visualisationToInspect.dataSource.DataCount);
                    UpdateComputeShaderWithVisualisationData(visualisationToInspect);
                    isDetailsOnDemandEnabled = true;
                }

                // Check if the previous GPU request has completed
                if (computeDistancesRequest.done)
                {
                    // Retrieve the list from the compute shader
                    if (!computeDistancesRequest.hasError)
                    {
                        distancesArray = computeDistancesRequest.GetData<float>();
                        DisplayDetailsOnDemandInfo();
                    }

                    // Set position of the inspection pointer on the compute shader
                    Vector3 inversePosition = visualisationToInspect.transform.InverseTransformPoint(inspectionPoint.position);
                    IndexDistancesComputeShader.SetFloats("pointer", inversePosition.x, inversePosition.y, inversePosition.z);

                    // Execute the kernel on the compute shader
                    IndexDistancesComputeShader.Dispatch(kernelComputeDistancesArray, Mathf.CeilToInt(dataBuffer.count / 64f), 1, 1);
                    computeDistancesRequest = AsyncGPUReadback.Request(distancesBuffer);
                }

            }
            else if (isDetailsOnDemandEnabled)
            {
                visualisationToInspect = null;
                isDetailsOnDemandEnabled = false;
                ReleaseBuffers();

                detailsOnDemandLabel.enabled = false;
                detailsOnDemandLine.enabled = false;
            }
        }

        private Visualisation GetNearestVisualisation()
        {
            var visualisations = GameObject.FindGameObjectsWithTag("DataVisualisation");

            float minDistance = Mathf.Infinity;
            Visualisation nearestVisualisation = null;

            foreach (var visualisation in visualisations)
            {
                BoxCollider collider = visualisation.GetComponent<BoxCollider>();
                Vector3 nearestPoint = collider.ClosestPoint(inspectionPoint.position);
                float distance = Vector3.Distance(nearestPoint, inspectionPoint.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestVisualisation = visualisation.GetComponentInChildren<Visualisation>();
                }
            }

            return nearestVisualisation;
        }

        private void InitialiseDetailsOnDemandLabel()
        {
            GameObject go = new GameObject(HandToUse.ToString() + "DetailsOnDemandLabel");
            detailsOnDemandLabel = go.AddComponent<TextMeshPro>();
            detailsOnDemandLabel.GetComponent<RectTransform>().sizeDelta = new Vector2(0.175f, 0.05f);
            detailsOnDemandLabel.autoSizeTextContainer = false;
            detailsOnDemandLabel.fontSize = 0.04f;
            detailsOnDemandLabel.alignment = TextAlignmentOptions.Midline;

            detailsOnDemandLabel.enabled = false;
        }

        private void InitialiseDetailsOnDemandLine()
        {
            GameObject go = new GameObject(HandToUse.ToString() + "DetailsOnDemandLine");
            detailsOnDemandLine = go.AddComponent<LineRenderer>();
            detailsOnDemandLine.startWidth = 0.0015f;
            detailsOnDemandLine.endWidth = 0.0015f;
            detailsOnDemandLine.startColor = Color.red;
            detailsOnDemandLine.endColor = Color.red;
            detailsOnDemandLine.useWorldSpace = true;
            detailsOnDemandLine.generateLightingData = true;
            detailsOnDemandLine.material = (Material) Resources.Load("LineMaterial");

            detailsOnDemandLine.enabled = false;
        }

        private void DisplayDetailsOnDemandInfo()
        {
            // Get a list of the closest indices
            List<int> nearestIndices = new List<int>();
            float minDistance = 0.25f;     // This acts as a way to filter out points which are way too far away

            for (int i = 0; i < distancesArray.Length; i++)
            {
                // Distances marked with a -1 are those which are filtered
                if (distancesArray[i] == -1)
                    break;

                if (distancesArray[i] < minDistance)
                {
                    nearestIndices.Clear();
                    nearestIndices.Add(i);
                    minDistance = distancesArray[i];
                }
                else if (IsFloatEqual(distancesArray[i], minDistance, 0.0001f))
                {
                    nearestIndices.Add(i);
                }
            }

            // If there are indices, display a floating label
            if (nearestIndices.Count > 0)
            {
                // Get position of the original point
                Vector3 originalPos = visualisationToInspect.theVisualizationObject.viewList[0].BigMesh.getBigMeshVertices()[nearestIndices[0]];

                // Normalise it based on visualisation properties
                float minNormX = visualisationToInspect.xDimension.minScale;
                float maxNormX = visualisationToInspect.xDimension.maxScale;
                float minNormY = visualisationToInspect.yDimension.minScale;
                float maxNormY = visualisationToInspect.yDimension.maxScale;
                float minNormZ = visualisationToInspect.zDimension.minScale;
                float maxNormZ = visualisationToInspect.zDimension.maxScale;
                float width = visualisationToInspect.width;
                float height = visualisationToInspect.height;
                float depth = visualisationToInspect.depth;

                float normX = NormaliseValue(originalPos.x, minNormX, maxNormX, 0, width);
                float normY = NormaliseValue(originalPos.y, minNormY, maxNormY, 0, height);
                float normZ = NormaliseValue(originalPos.z, minNormZ, maxNormZ, 0, depth);
                Vector3 normalisedPos = new Vector3(normX, normY, normZ);

                // Convert to world space
                Vector3 worldPos = visualisationToInspect.transform.TransformPoint(normalisedPos);

                // Draw label at the point
                detailsOnDemandLabel.transform.position = worldPos + Camera.main.transform.right * 0.01f + Camera.main.transform.up * 0.01f;
                detailsOnDemandLabel.transform.rotation = Quaternion.LookRotation(worldPos - Camera.main.transform.position, Vector3.up);
                SetDetailsOnDemandLabelText(nearestIndices);

                // Draw line between inspection point and index point
                detailsOnDemandLine.SetPositions(new [] {inspectionPoint.position, worldPos});

                detailsOnDemandLabel.enabled = true;
                detailsOnDemandLine.enabled = true;
            }
            else
            {
                detailsOnDemandLabel.enabled = false;
                detailsOnDemandLine.enabled = false;
            }
        }

        private void SetDetailsOnDemandLabelText(List<int> indices)
        {
            var visualisation = visualisationToInspect;
            var dataSource = visualisation.dataSource;

            StringBuilder stringBuilder = new StringBuilder();

            if (visualisation.xDimension.Attribute != "Undefined") stringBuilder.AppendFormat("<b>{0} (x):</b> {1}\n", visualisation.xDimension.Attribute, dataSource.getOriginalValue(indices[0], visualisation.xDimension.Attribute));
            if (visualisation.yDimension.Attribute != "Undefined") stringBuilder.AppendFormat("<b>{0} (y):</b> {1}\n", visualisation.yDimension.Attribute, dataSource.getOriginalValue(indices[0], visualisation.yDimension.Attribute));
            if (visualisation.zDimension.Attribute != "Undefined") stringBuilder.AppendFormat("<b>{0} (z):</b> {1}\n", visualisation.zDimension.Attribute, dataSource.getOriginalValue(indices[0], visualisation.zDimension.Attribute));

            stringBuilder.Append("--------\n");

            if (indices.Count > 1)
            {
                stringBuilder.AppendFormat("<i>{0} stacked points</i>", indices.Count);
            }
            else
            {
                for (int i = 0; i < dataSource.DimensionCount; i++)
                {
                    string dimension = dataSource[i].Identifier;
                    object value = dataSource.getOriginalValue(indices[0], dimension);
                    if (dataSource[dimension].MetaData.type == DataType.Float && value.ToString().Length > 4)
                    {
                        stringBuilder.AppendFormat("<b>{0}:</b> {1}\n", dimension, ((float)value).ToString("#,##0.00"));
                    }
                    else
                    {
                        stringBuilder.AppendFormat("<b>{0}:</b> {1}\n", dimension, value);
                    }
                }
            }

            detailsOnDemandLabel.text = stringBuilder.ToString();
        }

        private float NormaliseValue(float value, float min1, float max1, float min2, float max2)
        {
            float i = (min2 - max2) / (min1 - max1);
            return (min2 - (i * min1) + (i * value));
        }

        private bool IsFloatEqual(float value1, float value2, float tolerance)
        {
            return (Mathf.Abs(value1 - value2) < tolerance);
        }

        private void ReleaseBuffers()
        {
            if (dataBuffer != null)
                dataBuffer.Release();

            if (filteredIndicesBuffer != null)
                filteredIndicesBuffer.Release();

            if (distancesBuffer != null)
                distancesBuffer.Release();
        }

        private void OnDisable()
        {
            ReleaseBuffers();
        }

        private void OnDestroy()
        {
            ReleaseBuffers();

            if (detailsOnDemandLabel != null)
                Destroy(detailsOnDemandLabel.gameObject);

            if (detailsOnDemandLine != null)
                Destroy(detailsOnDemandLine.gameObject);
        }
    }
}