using IATK;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using System;
using System.Linq;
using UnityEngine.Rendering;

public class BrushingAndLinking : MonoBehaviour {

    [SerializeField]
    public ComputeShader computeShader;
    [SerializeField]
    public Material myRenderMaterial;

    [SerializeField]
    public List<Visualisation> brushingVisualisations;
    [SerializeField]
    public List<LinkingVisualisations> brushedLinkingVisualisations;

    [SerializeField]
    public bool isBrushing;
    [SerializeField]
    public Color brushColor = Color.red;
    [SerializeField]
    [Range(0f, 1f)]
    public float brushRadius;
    [SerializeField]
    public bool showBrush = false;
    [SerializeField]
    public Vector3 OverlapBoxHalfExtents;

    [SerializeField]
    public Transform input1;
    [SerializeField]
    public Transform input2;

    [SerializeField]
    public BrushType BRUSH_TYPE;
    public enum BrushType
    {
        SPHERE = 0,
        BOXAXISALIGNED = 1,
        BOXSCREENSPACE = 2,
        OVERLAPBOX = 3
    };

    [SerializeField]
    public SelectionType SELECTION_TYPE;
    public enum SelectionType
    {
        FREE = 0,
        ADD,
        SUBTRACT
    }

    [SerializeField]
    public List<int> brushedIndices;

    [SerializeField]
    public Material debugObjectTexture;

    private int kernelComputeBrushTexture;
    private int kernelComputeBrushedIndices;

    private static RenderTexture brushedIndicesTexture;
    private static int texSize;

    private ComputeBuffer dataBuffer;
    private ComputeBuffer filteredIndicesBuffer;
    private ComputeBuffer brushedIndicesBuffer;

    private bool hasInitialised = false;
    private static bool hasFreeBrushReset = false;
    private AsyncGPUReadbackRequest brushedIndicesRequest;

    private void Start()
    {
        InitialiseShaders();
    }

    /// <summary>
    /// Initialises the indices for the kernels in the compute shader.
    /// </summary>
    private void InitialiseShaders()
    {
        computeShader = Instantiate(computeShader);

        kernelComputeBrushTexture = computeShader.FindKernel("CSMain");
        kernelComputeBrushedIndices = computeShader.FindKernel("ComputeBrushedIndicesArray");
    }

    /// <summary>
    /// Initialises the buffers and textures necessary for the brushing and linking to work.
    /// </summary>
    /// <param name="dataCount"></param>
    private void InitialiseBuffersAndTextures(int dataCount)
    {
        dataBuffer = new ComputeBuffer(dataCount, 12);
        dataBuffer.SetData(new Vector3[dataCount]);
        computeShader.SetBuffer(kernelComputeBrushTexture, "dataBuffer", dataBuffer);

        filteredIndicesBuffer = new ComputeBuffer(dataCount, 4);
        filteredIndicesBuffer.SetData(new float[dataCount]);
        computeShader.SetBuffer(kernelComputeBrushTexture, "filteredIndicesBuffer", filteredIndicesBuffer);

        brushedIndicesBuffer = new ComputeBuffer(dataCount, 4);
        brushedIndicesBuffer.SetData(Enumerable.Repeat(-1, dataCount).ToArray());
        computeShader.SetBuffer(kernelComputeBrushedIndices, "brushedIndicesBuffer", brushedIndicesBuffer);

        if (brushedIndicesTexture == null)
        {
            texSize = NextPowerOf2((int)Mathf.Sqrt(dataCount));
            brushedIndicesTexture = new RenderTexture(texSize, texSize, 24);
            brushedIndicesTexture.enableRandomWrite = true;
            brushedIndicesTexture.filterMode = FilterMode.Point;
            brushedIndicesTexture.Create();
        }

        myRenderMaterial.SetTexture("_MainTex", brushedIndicesTexture);

        computeShader.SetFloat("_size", texSize);
        computeShader.SetTexture(kernelComputeBrushTexture, "Result", brushedIndicesTexture);
        computeShader.SetTexture(kernelComputeBrushedIndices, "Result", brushedIndicesTexture);

        hasInitialised = true;
    }

    /// <summary>
    /// Updates the computebuffers with the values specific to the currently brushed visualisation.
    /// </summary>
    /// <param name="visualisation"></param>
    public void UpdateComputeBuffers(Visualisation visualisation)
    {
        if (visualisation.visualisationType == AbstractVisualisation.VisualisationTypes.SCATTERPLOT)
        {
            dataBuffer.SetData(visualisation.theVisualizationObject.viewList[0].BigMesh.getBigMeshVertices());
            computeShader.SetBuffer(kernelComputeBrushTexture, "dataBuffer", dataBuffer);

            filteredIndicesBuffer.SetData(visualisation.theVisualizationObject.viewList[0].GetFilterChannel());
            computeShader.SetBuffer(kernelComputeBrushTexture, "filteredIndicesBuffer", filteredIndicesBuffer);
        }
    }


    /// <summary>
    /// Finds the next power of 2 for a given number.
    /// </summary>
    /// <param name="number"></param>
    /// <returns></returns>
    private int NextPowerOf2(int number)
    {
        int pos = 0;

        while (number > 0)
        {
            pos++;
            number = number >> 1;
        }
        return (int)Mathf.Pow(2, pos);
    }

    public void Update()
    {
        if (isBrushing && brushingVisualisations.Count > 0 && input1 != null && input2 != null)
        {
            if (hasInitialised)
            {
                UpdateBrushTexture();

                UpdateBrushedIndices();
            }
            else
            {
                InitialiseBuffersAndTextures(brushingVisualisations[0].dataSource.DataCount);
            }
        }

    }

    /// <summary>
    /// Returns a list with all indices that are brushed
    /// </summary>
    /// <returns></returns>
    public List<int> GetBrushedIndices()
    {
        UpdateBrushedIndices();
        List<int> indicesBrushed = new List<int>();

        for (int i = 0; i < brushedIndices.Count; i++)
        {
            if (brushedIndices[i] > 0)
                indicesBrushed.Add(i);
        }

        return indicesBrushed;
    }

    /// <summary>
    /// Updates the brushedIndicesTexture using the visualisations set in the brushingVisualisations list.
    /// </summary>
    private void UpdateBrushTexture()
    {
        Vector3 projectedPointer1;
        Vector3 projectedPointer2;

        computeShader.SetInt("BrushMode", (int)BRUSH_TYPE);
        computeShader.SetInt("SelectionMode", (int)SELECTION_TYPE);

        for (int i = 0; i < brushingVisualisations.Count; i++)
        {
            var vis = brushingVisualisations[i];
            if (vis == null)
            {
                brushingVisualisations.RemoveAt(i);
                i--;
                continue;
            }

            UpdateComputeBuffers(vis);

            switch (BRUSH_TYPE)
            {
                case BrushType.SPHERE:
                    projectedPointer1 = vis.transform.InverseTransformPoint(input1.transform.position);

                    computeShader.SetFloats("pointer1", projectedPointer1.x, projectedPointer1.y, projectedPointer1.z);
                    break;

                case BrushType.BOXAXISALIGNED:
                    projectedPointer1 = vis.transform.InverseTransformPoint(input1.transform.position);
                    projectedPointer2 = vis.transform.InverseTransformPoint(input2.transform.position);

                    computeShader.SetFloats("pointer1", projectedPointer1.x, projectedPointer1.y, projectedPointer1.z);
                    computeShader.SetFloats("pointer2", projectedPointer2.x, projectedPointer2.y, projectedPointer2.z);
                    break;

                case BrushType.BOXSCREENSPACE:
                    projectedPointer1 = vis.transform.InverseTransformPoint(input1.transform.position);
                    projectedPointer2 = vis.transform.InverseTransformPoint(input2.transform.position);

                    computeShader.SetFloats("pointer1", projectedPointer1.x, projectedPointer1.y, projectedPointer1.z);
                    computeShader.SetFloats("pointer2", projectedPointer2.x, projectedPointer2.y, projectedPointer2.z);
                    computeShader.SetMatrix("LocalToWorldMatrix", vis.transform.localToWorldMatrix);
                    computeShader.SetMatrix("WorldToClipMatrix", (Camera.main.projectionMatrix * Camera.main.worldToCameraMatrix));
                    break;

                case BrushType.OVERLAPBOX:
                    computeShader.SetFloats("OverlapBoxHalfExtents", OverlapBoxHalfExtents.x, OverlapBoxHalfExtents.y, OverlapBoxHalfExtents.z);
                    computeShader.SetMatrix("LocalToWorldMatrix", vis.transform.localToWorldMatrix);
                    computeShader.SetMatrix("OverlapBoxWorldToLocalMatrix", input1.worldToLocalMatrix);
                    break;

                default:
                    break;
            }

            //set the filters and normalisation values of the brushing visualisation to the computer shader
            computeShader.SetFloat("_MinNormX", vis.xDimension.minScale);
            computeShader.SetFloat("_MaxNormX", vis.xDimension.maxScale);
            computeShader.SetFloat("_MinNormY", vis.yDimension.minScale);
            computeShader.SetFloat("_MaxNormY", vis.yDimension.maxScale);
            computeShader.SetFloat("_MinNormZ", vis.zDimension.minScale);
            computeShader.SetFloat("_MaxNormZ", vis.zDimension.maxScale);

            computeShader.SetFloat("_MinX", vis.xDimension.minFilter);
            computeShader.SetFloat("_MaxX", vis.xDimension.maxFilter);
            computeShader.SetFloat("_MinY", vis.yDimension.minFilter);
            computeShader.SetFloat("_MaxY", vis.yDimension.maxFilter);
            computeShader.SetFloat("_MinZ", vis.zDimension.minFilter);
            computeShader.SetFloat("_MaxZ", vis.zDimension.maxFilter);

            computeShader.SetFloat("RadiusSphere", brushRadius);

            computeShader.SetFloat("width", vis.width);
            computeShader.SetFloat("height", vis.height);
            computeShader.SetFloat("depth", vis.depth);

            // Tell the shader whether or not the visualisation's points have already been reset by a previous brush, required to allow for
            // multiple visualisations to be brushed with the free selection tool
            if (SELECTION_TYPE == SelectionType.FREE)
                computeShader.SetBool("HasFreeBrushReset", hasFreeBrushReset);

            // Run the compute shader
            computeShader.Dispatch(kernelComputeBrushTexture, Mathf.CeilToInt(texSize / 32f), Mathf.CeilToInt(texSize / 32f), 1);

            foreach (var view in vis.theVisualizationObject.viewList)
            {
                view.BigMesh.SharedMaterial.SetTexture("_BrushedTexture", brushedIndicesTexture);
                view.BigMesh.SharedMaterial.SetFloat("_DataWidth", texSize);
                view.BigMesh.SharedMaterial.SetFloat("_DataHeight", texSize);
                view.BigMesh.SharedMaterial.SetFloat("_ShowBrush", Convert.ToSingle(showBrush));
                view.BigMesh.SharedMaterial.SetColor("_BrushColor", brushColor);
            }
            // Now that we are the first BrushingAndLinking computeshader to have reset the brushed indices texture this frame, we set this to true so
            // that no other computeshaders reset it any more
            hasFreeBrushReset = true;
        }

        for (int i = 0; i < brushedLinkingVisualisations.Count; i++)
        {
            var linkingVis = brushedLinkingVisualisations[i];
            if (linkingVis == null)
            {
                brushedLinkingVisualisations.RemoveAt(i);
                i--;
                continue;
            }

            linkingVis.View.BigMesh.SharedMaterial.SetTexture("_BrushedTexture", brushedIndicesTexture);
            linkingVis.View.BigMesh.SharedMaterial.SetFloat("_DataWidth", texSize);
            linkingVis.View.BigMesh.SharedMaterial.SetFloat("_DataHeight", texSize);
            linkingVis.View.BigMesh.SharedMaterial.SetFloat("_ShowBrush", Convert.ToSingle(showBrush));
            linkingVis.View.BigMesh.SharedMaterial.SetColor("_BrushColor", brushColor);
        }
    }

    // This is here so that the free brushing can work with multiple BrushingAndLinking scripts
    // We set the boolean to false at the end of the update loop to reset its value for the first BrushingAndLinking script in the following frame
    private void LateUpdate()
    {
        hasFreeBrushReset = false;
    }

    /// <summary>
    /// Updates the brushedIndices list with the currently brushed indices. A value of 1 represents brushed, -1 represents not brushed (boolean values are not supported).
    /// </summary>
    private void UpdateBrushedIndices()
    {
        // Wait for request to finish
        if (brushedIndicesRequest.done)
        {
            // Get values from request
            if (!brushedIndicesRequest.hasError)
            {
                brushedIndices = brushedIndicesRequest.GetData<int>().ToList();
            }

            // Dispatch again
            computeShader.Dispatch(kernelComputeBrushedIndices, Mathf.CeilToInt(brushedIndicesBuffer.count / 64f), 1, 1);
            brushedIndicesRequest = AsyncGPUReadback.Request(brushedIndicesBuffer);
        }
    }

    /// <summary>
    /// Releases the buffers on the graphics card.
    /// </summary>
    private void OnDestroy()
    {
        if (dataBuffer != null)
            dataBuffer.Release();

        if (filteredIndicesBuffer != null)
            filteredIndicesBuffer.Release();

        if (brushedIndicesBuffer != null)
            brushedIndicesBuffer.Release();
    }

    private void OnApplicationQuit()
    {
        if (dataBuffer != null)
            dataBuffer.Release();

        if (filteredIndicesBuffer != null)
            filteredIndicesBuffer.Release();

        if (brushedIndicesBuffer != null)
            brushedIndicesBuffer.Release();
    }
}