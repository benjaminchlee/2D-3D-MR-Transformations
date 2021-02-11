using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace IATK
{
    public enum BarAggregation
    {
        None,
        Count,
        Sum,
        Average,
        Median,
        Min,
        Max
    }

    [ExecuteInEditMode]
    public class BarVisualisation : AbstractVisualisation
    {
        public override void CreateVisualisation()
        {
            string savedName = name;

            // Destroy all old views
            foreach (View v in viewList)
            {
                DestroyImmediate(v.gameObject);
            }
            viewList.Clear();

            // Create axes dictionary to pass to the CreationConfiguration
            Dictionary<CreationConfiguration.Axis, string> axes = new Dictionary<CreationConfiguration.Axis, string>();
            if (visualisationReference.xDimension.Attribute != "" && visualisationReference.xDimension.Attribute != "Undefined")
            {
                axes.Add(CreationConfiguration.Axis.X, visualisationReference.xDimension.Attribute);
            }
            if (visualisationReference.yDimension.Attribute != "" && visualisationReference.yDimension.Attribute != "Undefined")
            {
                axes.Add(CreationConfiguration.Axis.Y, visualisationReference.yDimension.Attribute);
            }
            if (visualisationReference.zDimension.Attribute != "" && visualisationReference.zDimension.Attribute != "Undefined")
            {
                axes.Add(CreationConfiguration.Axis.Z, visualisationReference.zDimension.Attribute);
            }

            // Create new CreationConfiguration object
            if (creationConfiguration == null)
                creationConfiguration = new CreationConfiguration(visualisationReference.geometry, axes);
            else
            {
                creationConfiguration.Axies = axes;
                creationConfiguration.Geometry = visualisationReference.geometry;
                creationConfiguration.LinkingDimension = visualisationReference.linkingDimension;
                creationConfiguration.Size = visualisationReference.size;
                creationConfiguration.MinSize = visualisationReference.minSize;
                creationConfiguration.MaxSize = visualisationReference.maxSize;
                creationConfiguration.colour = visualisationReference.colour;
            }

            View view = CreateSimpleVisualisation(creationConfiguration);

            if (view != null)
            {
                view.transform.localPosition = Vector3.zero;
                view.transform.SetParent(transform, false);
                view.onViewChangeEvent += UpdateVisualisation;   // Receive notifications when the view configuration changes

                viewList.Add(view);
            }

            if (viewList.Count > 0 && visualisationReference.colourDimension != "Undefined")
            {
                for (int i = 0; i < viewList.Count; i++)
                {
                    viewList[i].SetColors(mapColoursContinuous(visualisationReference.dataSource[visualisationReference.colourDimension].Data));
                }
            }
            else if (viewList.Count > 0 && visualisationReference.colorPaletteDimension != "Undefined")
            {
                for (int i = 0; i < viewList.Count; i++)
                {
                    viewList[i].SetColors(mapColoursPalette(visualisationReference.dataSource[visualisationReference.colorPaletteDimension].Data, visualisationReference.coloursPalette));
                }
            }
            else if (viewList.Count > 0 && visualisationReference.colour != null)
            {
                for (int i = 0; i < viewList.Count; i++)
                {
                    Color[] colours = viewList[i].GetColors();
                    for (int j = 0; j < colours.Length; ++j)
                    {
                        colours[j] = visualisationReference.colour;
                    }
                    viewList[i].SetColors(colours);
                }
            }


            if (viewList.Count > 0)
            {
                for (int i = 0; i < viewList.Count; i++)
                {
                    viewList[i].SetSize(visualisationReference.size);
                    viewList[i].SetMinSize(visualisationReference.minSize);
                    viewList[i].SetMaxSize(visualisationReference.maxSize);

                    viewList[i].SetMinNormX(visualisationReference.xDimension.minScale);
                    viewList[i].SetMaxNormX(visualisationReference.xDimension.maxScale);
                    viewList[i].SetMinNormY(visualisationReference.yDimension.minScale);
                    viewList[i].SetMaxNormY(visualisationReference.yDimension.maxScale);
                    viewList[i].SetMinNormZ(visualisationReference.zDimension.minScale);
                    viewList[i].SetMaxNormZ(visualisationReference.zDimension.maxScale);

                    viewList[i].SetMinX(visualisationReference.xDimension.minFilter);
                    viewList[i].SetMaxX(visualisationReference.xDimension.maxFilter);
                    viewList[i].SetMinY(visualisationReference.yDimension.minFilter);
                    viewList[i].SetMaxY(visualisationReference.yDimension.maxFilter);
                    viewList[i].SetMinZ(visualisationReference.zDimension.minFilter);
                    viewList[i].SetMaxZ(visualisationReference.zDimension.maxFilter);
                }
            }

            if (viewList.Count > 0 && visualisationReference.sizeDimension != "Undefined")
            {
                for (int i = 0; i < viewList.Count; i++)
                {
                    viewList[i].SetSizeChannel(visualisationReference.dataSource[visualisationReference.sizeDimension].Data);
                }
            }

            name = savedName;
        }

        public override void UpdateVisualisation(PropertyType propertyType){

            if (viewList.Count == 0 || creationConfiguration == null)
                CreateVisualisation();

            if (viewList.Count != 0)
                switch (propertyType)
                {
                    case AbstractVisualisation.PropertyType.X:
                        if (visualisationReference.xDimension.Attribute.Equals("Undefined"))
                        {
                            viewList[0].ZeroPosition(0);
                        }
                        else
                        {
                            float[] xPositions = SetBinnedDataDimension(visualisationReference.dataSource[visualisationReference.xDimension.Attribute].Data, visualisationReference.numXBins, IsDimensionCategorical(visualisationReference.xDimension.Attribute));
                            viewList[0].UpdateXPositions(xPositions);
                        }

                        UpdateVisualisation(PropertyType.Y);

                        if (creationConfiguration.Axies.ContainsKey(CreationConfiguration.Axis.X))
                            creationConfiguration.Axies[CreationConfiguration.Axis.X] = visualisationReference.xDimension.Attribute;
                        else
                            creationConfiguration.Axies.Add(CreationConfiguration.Axis.X, visualisationReference.xDimension.Attribute);
                        break;

                    case AbstractVisualisation.PropertyType.Y:
                        float[] yPositions;
                        if (visualisationReference.yDimension.Attribute.Equals("Undefined"))
                        {
                            yPositions = SetAggregatedDimension(null, BarAggregation.Count);

                        }
                        // If the aggregation type is not set, just use the raw position
                        else if ((BarAggregation)Enum.Parse(typeof(BarAggregation), visualisationReference.barAggregation) == BarAggregation.None)
                        {
                            yPositions = visualisationReference.dataSource[visualisationReference.yDimension.Attribute].Data;
                        }
                        else
                        {
                            yPositions = SetAggregatedDimension(visualisationReference.dataSource[visualisationReference.yDimension.Attribute].Data, (BarAggregation)Enum.Parse(typeof(BarAggregation), visualisationReference.barAggregation));
                        }

                        viewList[0].UpdateYPositions(yPositions);
                        viewList[0].TweenPosition();

                        if (creationConfiguration.Axies.ContainsKey(CreationConfiguration.Axis.Y))
                            creationConfiguration.Axies[CreationConfiguration.Axis.Y] = visualisationReference.yDimension.Attribute;
                        else
                            creationConfiguration.Axies.Add(CreationConfiguration.Axis.Y, visualisationReference.yDimension.Attribute);
                        break;

                    case AbstractVisualisation.PropertyType.Z:
                        if (visualisationReference.zDimension.Attribute.Equals("Undefined"))
                        {
                            viewList[0].ZeroPosition(2);
                        }
                        else
                        {
                            float[] zPositions = SetBinnedDataDimension(visualisationReference.dataSource[visualisationReference.zDimension.Attribute].Data, visualisationReference.numZBins, IsDimensionCategorical(visualisationReference.zDimension.Attribute));
                            viewList[0].UpdateZPositions(zPositions);
                        }

                        UpdateVisualisation(PropertyType.Y);

                        if (creationConfiguration.Axies.ContainsKey(CreationConfiguration.Axis.Z))
                            creationConfiguration.Axies[CreationConfiguration.Axis.Z] = visualisationReference.zDimension.Attribute;
                        else
                            creationConfiguration.Axies.Add(CreationConfiguration.Axis.Z, visualisationReference.zDimension.Attribute);
                        break;

                    case AbstractVisualisation.PropertyType.Colour:
                        if (visualisationReference.colourDimension != "Undefined")
                        {
                            for (int i = 0; i < viewList.Count; i++)
                                viewList[i].SetColors(mapColoursContinuous(visualisationReference.dataSource[visualisationReference.colourDimension].Data));
                        }
                        else if (visualisationReference.colorPaletteDimension != "Undefined")
                        {
                            for (int i = 0; i < viewList.Count; i++)
                            {
                                viewList[i].SetColors(mapColoursPalette(visualisationReference.dataSource[visualisationReference.colorPaletteDimension].Data, visualisationReference.coloursPalette));
                            }
                        }
                        else
                        {
                            for (int i = 0; i < viewList.Count; i++)
                            {
                                Color[] colours = viewList[0].GetColors();
                                for (int j = 0; j < colours.Length; ++j)
                                {
                                    colours[j] = visualisationReference.colour;
                                }
                                viewList[i].SetColors(colours);
                            }

                        }

                        creationConfiguration.ColourDimension = visualisationReference.colourDimension;
                        creationConfiguration.colourKeys = visualisationReference.dimensionColour;
                        creationConfiguration.colour = visualisationReference.colour;

                        break;
                    case AbstractVisualisation.PropertyType.Size:
                        {
                            for (int i = 0; i < viewList.Count; i++)
                            {
                                if (visualisationReference.sizeDimension != "Undefined")
                                {
                                    viewList[i].SetSizeChannel(visualisationReference.dataSource[visualisationReference.sizeDimension].Data);
                                }
                                else
                                {
                                    viewList[i].SetSizeChannel(new float[visualisationReference.dataSource.DataCount]);
                                }
                            }
                            creationConfiguration.SizeDimension = visualisationReference.sizeDimension;
                            viewList[0].TweenSize();

                            break;

                        }
                    case PropertyType.SizeValues:
                        for (int i = 0; i < viewList.Count; i++)
                        {
                            if (visualisationReference.xDimension.Attribute.Equals("Undefined"))
                                viewList[i].BigMesh.SharedMaterial.SetFloat("_Width", 0.01f);
                            else
                                viewList[i].BigMesh.SharedMaterial.SetFloat("_Width", 1 / (float)visualisationReference.numXBins / 2f);

                            if (visualisationReference.zDimension.Attribute.Equals("Undefined"))
                                viewList[i].BigMesh.SharedMaterial.SetFloat("_Depth", 0.01f);
                            else
                                viewList[i].BigMesh.SharedMaterial.SetFloat("_Depth", 1 / (float)visualisationReference.numZBins / 2f);
                        }
                        creationConfiguration.Size = visualisationReference.size;
                        creationConfiguration.MinSize = visualisationReference.minSize;
                        creationConfiguration.MaxSize = visualisationReference.maxSize;
                        break;

                    case AbstractVisualisation.PropertyType.LinkingDimension:
                        creationConfiguration.LinkingDimension = visualisationReference.linkingDimension;

                        CreateVisualisation(); // needs to recreate the visualsiation because the mesh properties have changed
                        rescaleViews();
                        break;

                    case AbstractVisualisation.PropertyType.GeometryType:
                        creationConfiguration.Geometry = visualisationReference.geometry;
                        CreateVisualisation(); // needs to recreate the visualsiation because the mesh properties have changed
                        rescaleViews();
                        break;

                    case AbstractVisualisation.PropertyType.DimensionScaling:

                        for (int i = 0; i < viewList.Count; i++)
                        {
                            viewList[i].SetMinNormX(visualisationReference.xDimension.minScale);
                            viewList[i].SetMaxNormX(visualisationReference.xDimension.maxScale);
                            viewList[i].SetMinNormY(visualisationReference.yDimension.minScale);
                            viewList[i].SetMaxNormY(visualisationReference.yDimension.maxScale);
                            viewList[i].SetMinNormZ(visualisationReference.zDimension.minScale);
                            viewList[i].SetMaxNormZ(visualisationReference.zDimension.maxScale);
                        }

                        // TODO: Move visualsiation size from Scaling to its own PropertyType
                        creationConfiguration.VisualisationWidth = visualisationReference.width;
                        creationConfiguration.VisualisationHeight = visualisationReference.height;
                        creationConfiguration.VisualisationDepth = visualisationReference.depth;
                        break;

                    case AbstractVisualisation.PropertyType.DimensionFiltering:
                        for (int i = 0; i < viewList.Count; i++)
                        {
                            viewList[i].SetMinX(visualisationReference.xDimension.minFilter);
                            viewList[i].SetMaxX(visualisationReference.xDimension.maxFilter);
                            viewList[i].SetMinY(visualisationReference.yDimension.minFilter);
                            viewList[i].SetMaxY(visualisationReference.yDimension.maxFilter);
                            viewList[i].SetMinZ(visualisationReference.zDimension.minFilter);
                            viewList[i].SetMaxZ(visualisationReference.zDimension.maxFilter);
                        }
                        break;
                    case AbstractVisualisation.PropertyType.AttributeFiltering:
                        if (visualisationReference.attributeFilters!=null)
                        {
                            foreach (var viewElement in viewList)
                            {
                                float[] isFiltered = new float[visualisationReference.dataSource.DataCount];
                                for (int i = 0; i < visualisationReference.dataSource.DimensionCount; i++)
                                {
                                    foreach (AttributeFilter attrFilter in visualisationReference.attributeFilters)
                                    {
                                        if (attrFilter.Attribute == visualisationReference.dataSource[i].Identifier)
                                        {
                                            float minFilteringValue = UtilMath.normaliseValue(attrFilter.minFilter, 0f, 1f, attrFilter.minScale, attrFilter.maxScale);
                                            float maxFilteringValue = UtilMath.normaliseValue(attrFilter.maxFilter, 0f, 1f, attrFilter.minScale, attrFilter.maxScale);

                                            for (int j = 0; j < isFiltered.Length; j++)
                                            {
                                                isFiltered[j] = (visualisationReference.dataSource[i].Data[j] < minFilteringValue || visualisationReference.dataSource[i].Data[j] > maxFilteringValue) ? 1.0f : isFiltered[j];
                                            }
                                        }
                                    }
                                }
                                // map the filtered attribute into the normal channel of the bigmesh
                                foreach (View v in viewList)
                                {
                                    v.SetFilterChannel(isFiltered);
                                }
                            }
                        }
                        break;
                    case PropertyType.VisualisationType:
                        break;
                    case PropertyType.BlendDestinationMode:
                        float bmds = (int)(System.Enum.Parse(typeof(UnityEngine.Rendering.BlendMode), visualisationReference.blendingModeDestination));
                        for (int i = 0; i < viewList.Count; i++)
                        {
                            viewList[i].SetBlendindDestinationMode(bmds);
                        }

                            break;
                    case PropertyType.BlendSourceMode:
                        float bmd = (int)(System.Enum.Parse(typeof(UnityEngine.Rendering.BlendMode), visualisationReference.blendingModeSource));
                        for (int i = 0; i < viewList.Count; i++)
                        {
                            viewList[i].SetBlendingSourceMode(bmd);
                        }

                        break;

                    case PropertyType.AggregationType:
                        UpdateVisualisation(PropertyType.Y);
                        break;

                    case PropertyType.NumXBins:
                        UpdateVisualisation(PropertyType.X);
                        UpdateVisualisation(PropertyType.SizeValues);
                        break;

                    case PropertyType.NumZBins:
                        UpdateVisualisation(PropertyType.Z);
                        UpdateVisualisation(PropertyType.SizeValues);
                        break;

                    default:
                        break;
                }

            if (visualisationReference.geometry != GeometryType.Undefined)// || visualisationType == VisualisationTypes.PARALLEL_COORDINATES)
            SerializeViewConfiguration(creationConfiguration);

            //Update any label on the corresponding axes
            UpdateVisualisationAxes(propertyType);

            rescaleViews();
        }

        public void UpdateVisualisationAxes(AbstractVisualisation.PropertyType propertyType)
        {
            switch (propertyType)
            {
                case AbstractVisualisation.PropertyType.X:
                    if (visualisationReference.xDimension.Attribute == "Undefined" && X_AXIS != null)
                    {
                        DestroyImmediate(X_AXIS);
                    }
                    else if (X_AXIS != null)
                    {
                        Axis a = X_AXIS.GetComponent<Axis>();
                        a.Initialise(visualisationReference.dataSource, visualisationReference.xDimension, visualisationReference, (int)propertyType);
                    }
                    else if (visualisationReference.xDimension.Attribute != "Undefined")
                    {
                        Vector3 pos = Vector3.zero;
                        //pos.y = -0.025f;
                        X_AXIS = CreateAxis(propertyType, visualisationReference.xDimension, pos, new Vector3(0f, 0f, 0f), 0);
                    }
                    break;
                case AbstractVisualisation.PropertyType.Y:
                    if (visualisationReference.yDimension.Attribute == "Undefined" && Y_AXIS != null)
                    {
                        DestroyImmediate(Y_AXIS);
                    }
                    else if (Y_AXIS != null)
                    {
                        Axis a = Y_AXIS.GetComponent<Axis>();
                        a.Initialise(visualisationReference.dataSource, visualisationReference.yDimension, visualisationReference, (int)propertyType);
                    }
                    else if (visualisationReference.yDimension.Attribute != "Undefined")
                    {
                        Vector3 pos = Vector3.zero;
                        //pos.x = -0.025f;
                        Y_AXIS = CreateAxis(propertyType, visualisationReference.yDimension, pos, new Vector3(0f, 0f, 0f), 1);
                    }
                    break;
                case AbstractVisualisation.PropertyType.Z:
                    if (visualisationReference.zDimension.Attribute == "Undefined" && Z_AXIS != null)
                    {
                        DestroyImmediate(Z_AXIS);
                    }
                    else if (Z_AXIS != null)
                    {
                        Axis a = Z_AXIS.GetComponent<Axis>();
                        a.Initialise(visualisationReference.dataSource, visualisationReference.zDimension, visualisationReference, (int)propertyType);
                    }
                    else if (visualisationReference.zDimension.Attribute != "Undefined")
                    {
                        Vector3 pos = Vector3.zero;
                        //pos.y = -0.025f;
                        //pos.x = -0.025f;
                        Z_AXIS = CreateAxis(propertyType, visualisationReference.zDimension, pos, new Vector3(90f, 0f, 0f), 2);
                    }
                    break;

                case AbstractVisualisation.PropertyType.DimensionFiltering:
                    if (visualisationReference.xDimension.Attribute != "Undefined")
                    {
                        X_AXIS.GetComponent<Axis>().UpdateAxisRanges();
                    }
                    if (visualisationReference.yDimension.Attribute != "Undefined")
                    {
                        Y_AXIS.GetComponent<Axis>().UpdateAxisRanges();
                    }
                    if (visualisationReference.zDimension.Attribute != "Undefined")
                    {
                        Z_AXIS.GetComponent<Axis>().UpdateAxisRanges();
                    }
                    break;

                case AbstractVisualisation.PropertyType.DimensionScaling:
                    if (visualisationReference.xDimension.Attribute != "Undefined")
                    {
                        X_AXIS.GetComponent<Axis>().UpdateAxisRanges();
                    }
                    if (visualisationReference.yDimension.Attribute != "Undefined")
                    {
                        Y_AXIS.GetComponent<Axis>().UpdateAxisRanges();
                    }
                    if (visualisationReference.zDimension.Attribute != "Undefined")
                    {
                        Z_AXIS.GetComponent<Axis>().UpdateAxisRanges();
                    }
                    break;

                case AbstractVisualisation.PropertyType.VisualisationScale:
                    if (visualisationReference.xDimension.Attribute != "Undefined")
                    {
                        if (X_AXIS == null)
                            UpdateVisualisationAxes(PropertyType.X);

                        Axis axis = X_AXIS.GetComponent<Axis>();
                        axis.UpdateAxisAttributeAndLength(axis.AttributeFilter, visualisationReference.width);
                    }
                    if (visualisationReference.yDimension.Attribute != "Undefined")
                    {
                        if (Y_AXIS == null)
                            UpdateVisualisationAxes(PropertyType.Y);

                        Axis axis = Y_AXIS.GetComponent<Axis>();
                        axis.UpdateAxisAttributeAndLength(axis.AttributeFilter, visualisationReference.height);
                    }
                    if (visualisationReference.zDimension.Attribute != "Undefined")
                    {
                        if (Z_AXIS == null)
                            UpdateVisualisationAxes(PropertyType.Z);

                        Axis axis = Z_AXIS.GetComponent<Axis>();
                        axis.UpdateAxisAttributeAndLength(axis.AttributeFilter, visualisationReference.depth);
                    }
                    rescaleViews();
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// Gets the axies.
        /// </summary>
        /// <returns>The axies.</returns>
        /// <param name="axies">Axies.</param>
        protected string getAxis(Dictionary<CreationConfiguration.Axis, string> axies, CreationConfiguration.Axis axis)
        {

            string axes = null;
            string retVal = "";
            if (axies.TryGetValue(axis, out axes))
                retVal = axes;

            return retVal;
        }

        /// <summary>
        /// Rescales the views in this scatterplot to the width, height, and depth values in the visualisationReference
        /// </summary>
        protected void rescaleViews()
        {
            foreach (View view in viewList)
            {
                view.transform.localScale = new Vector3(
                    visualisationReference.width,
                    visualisationReference.height,
                    visualisationReference.depth
                );
            }
        }

        public override void SaveConfiguration(){}

        public override void LoadConfiguration(){}

        /// <summary>
        /// Maps the colours.
        /// </summary>
        /// <returns>The colours.</returns>
        /// <param name="data">Data.</param>
        public override Color[] mapColoursContinuous(float[] data)
        {
            Color[] colours = new Color[data.Length];

            for (int i = 0; i < data.Length; ++i)
            {
                colours[i] = visualisationReference.dimensionColour.Evaluate(data[i]);
            }

            return colours;
        }

        public Color[] mapColoursPalette(float[] data, Color[] palette)
        {
            Color[] colours = new Color[data.Length];

            float[] uniqueValues = data.Distinct().ToArray();

            for (int i = 0; i < data.Length; i++)
            {
                int indexColor = Array.IndexOf(uniqueValues, data[i]);
                colours[i] = palette[indexColor];
            }

            return colours;
        }

        // ******************************
        // SPECIFIC VISUALISATION METHODS
        // ******************************

        private View CreateSimpleVisualisation(CreationConfiguration configuration)
        {
            if (visualisationReference.dataSource != null)
            {
                if (!visualisationReference.dataSource.IsLoaded) visualisationReference.dataSource.load();

                ViewBuilder builder = new ViewBuilder(geometryToMeshTopology(GeometryType.Bars), "Bar Visualisation");

                DataSource.DimensionData xDimensionData = visualisationReference.dataSource[getAxis(configuration.Axies, CreationConfiguration.Axis.X)];
                DataSource.DimensionData yDimensionData = visualisationReference.dataSource[getAxis(configuration.Axies, CreationConfiguration.Axis.Y)];
                DataSource.DimensionData zDimensionData = visualisationReference.dataSource[getAxis(configuration.Axies, CreationConfiguration.Axis.Z)];

                if ((xDimensionData != null) ||
                    (zDimensionData != null))
                {
                    builder.initialiseDataView(visualisationReference.dataSource.DataCount);

                    if (xDimensionData != null)
                    {
                        builder.setBinnedDataDimension(xDimensionData.Data, ViewBuilder.VIEW_DIMENSION.X, visualisationReference.numXBins, IsDimensionCategorical(xDimensionData.Identifier));
                    }
                    if (zDimensionData != null)
                    {
                        builder.setBinnedDataDimension(zDimensionData.Data, ViewBuilder.VIEW_DIMENSION.Z, visualisationReference.numZBins, IsDimensionCategorical(zDimensionData.Identifier));
                    }
                    // If no y dimension was set, set aggregation to count
                    if (yDimensionData == null)
                    {
                        builder.setAggregatedDimension(null, ViewBuilder.VIEW_DIMENSION.Y, BarAggregation.Count);
                    }
                    // If the aggregation type is not set, just use the raw position
                    else if ((BarAggregation)Enum.Parse(typeof(BarAggregation), visualisationReference.barAggregation) == BarAggregation.None)
                    {
                        builder.setDataDimension(yDimensionData.Data, ViewBuilder.VIEW_DIMENSION.Y);
                    }
                    else
                    {
                        builder.setAggregatedDimension(yDimensionData.Data, ViewBuilder.VIEW_DIMENSION.Y, (BarAggregation)Enum.Parse(typeof(BarAggregation), visualisationReference.barAggregation));
                    }

                    //destroy the view to clean the big mesh
                    destroyView();

                    //return the appropriate geometry view
                    return ApplyGeometryAndRendering(creationConfiguration, ref builder);
                }

            }

            return null;

        }

        private bool IsDimensionCategorical(string dimension)
        {
            var type = visualisationReference.dataSource[dimension].MetaData.type;
            return (type == DataType.String || type == DataType.Date);
        }

        /// <summary>
        /// Creates an array of positions that are binned
        /// </summary>
        /// <param name="data"></param>
        /// <param name="dimension"></param>
        /// <param name="numBins"></param>
        /// <param name="isCategorical"></param>
        /// <returns></returns>
        public float[] SetBinnedDataDimension(float[] data, int numBins, bool isCategorical = false)
        {
            DiscreteBinner binner = new DiscreteBinner();
            // If the dimension is categorical, numBins is fixed to the number of distinct values in it
            if (isCategorical)
                numBins = data.Distinct().Count();
            binner.MakeIntervals(data, numBins);

            float[] positions = new float[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                // Position such that the centre of the bar is never at 0 or 1
                float value = binner.Bin(data[i]);
                value = (value * 2 + 1) / (float)(numBins * 2);
                positions[i] = value;
            }

            return positions;
        }

        /// <summary>
        /// Creates an array of positions that are aggregated based on the given aggregation type.
        /// This MUST be called AFTER each time the other dimensions change.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="dimension"></param>
        /// <param name="aggregation"></param>
        /// <returns></returns>
        public float[] SetAggregatedDimension(float[] yData, BarAggregation aggregation)
        {
            // Extract list versions of the position values for the x and z dimensions
            Vector3[] bigMeshVertices = viewList[0].BigMesh.getBigMeshVertices();
            float[] xData = new float[bigMeshVertices.Length];
            float[] zData = new float[bigMeshVertices.Length];
            for (int i = 0; i < visualisationReference.dataSource.DataCount; i++)
            {
                xData[i] = bigMeshVertices[i].x;
                zData[i] = bigMeshVertices[i].z;
            }

            // Get the unique "categories" of the x and z dimensions (these are technically floats)
            var xCategories = xData.Distinct();
            var zCategories = zData.Distinct();

            // LAZY HACK: Set a value in the mesh's normal.y value to designate whether to show or hide the point to prevent z-fighting and mass lag
            float[] masterBars = new float[visualisationReference.dataSource.DataCount];

            // Create a dictionary that will store the values assocatied with each (x, z) pairs of aggregating values (x bins * z bins = n lists)
            Dictionary<float, Dictionary<float, List<float>>> aggGroups = new Dictionary<float, Dictionary<float, List<float>>>();
            // Iterate through each position and assign the data values to the respective (x, z) pair
            for (int i = 0; i < visualisationReference.dataSource.DataCount; i++)
            {
                Dictionary<float, List<float>> innerDict;
                if (!aggGroups.TryGetValue(xData[i], out innerDict))
                {
                    innerDict = new Dictionary<float, List<float>>();
                    aggGroups[xData[i]] = innerDict;
                }

                List<float> innerList;
                if (!innerDict.TryGetValue(zData[i], out innerList))
                {
                    innerList = new List<float>();
                    innerDict[zData[i]] = innerList;
                    masterBars[i] = 1;
                }

                // If the aggregation type is count, we don't need to use the y axis values
                if (aggregation == BarAggregation.Count || yData == null)
                    innerList.Add(0);
                else
                    innerList.Add(yData[i]);
            }

            // LAZY HACK: Send the master values to the mesh now
            viewList[0].BigMesh.MapUVChannel(0, 1, masterBars);

            // Create another dictionary that will store the aggregated value for each (x, z) pair group
            float max = 0;
            Dictionary<float, Dictionary<float, float>> aggregatedValues = new Dictionary<float, Dictionary<float, float>>();
            foreach (float xCategory in xCategories)
            {
                foreach (float zCategory in zCategories)
                {
                    // Calculate final aggregated value
                    if (!aggGroups[xCategory].ContainsKey(zCategory))
                        continue;

                    List<float> values = aggGroups[xCategory][zCategory];
                    float aggregated = 0;
                    switch (aggregation)
                    {
                        case BarAggregation.Count:
                            aggregated = values.Count;
                            break;
                        case BarAggregation.Average:
                            aggregated = values.Average();
                            break;
                        case BarAggregation.Sum:
                            aggregated = values.Sum();
                            break;
                        case BarAggregation.Median:
                            values.Sort();
                            float mid = (values.Count - 1) / 2f;
                            aggregated = (values[(int)(mid)] + values[(int)(mid + 0.5f)]) / 2;
                            break;
                        case BarAggregation.Min:
                            aggregated = values.Min();
                            break;
                        case BarAggregation.Max:
                            aggregated = values.Max();
                            break;
                    }

                    // Set value
                    Dictionary<float, float> innerDict;
                    if (!aggregatedValues.TryGetValue(xCategory, out innerDict))
                    {
                        innerDict = new Dictionary<float, float>();
                        aggregatedValues[xCategory] = innerDict;
                    }
                    innerDict[zCategory] = aggregated;

                    // We need to normalise back into 0..1 for these specific aggregations, so we collect the max value
                    if (aggregation == BarAggregation.Count || aggregation == BarAggregation.Sum)
                    {
                        if (max < aggregated)
                            max = aggregated;
                    }
                }
            }

            // Set y position based on newly aggregated values
            float[] positions = new float[visualisationReference.dataSource.DataCount];
            for (int i = 0; i < visualisationReference.dataSource.DataCount; i++)
            {
                // For specific aggregations, normalise
                if (aggregation == BarAggregation.Count || aggregation == BarAggregation.Sum)
                {
                    positions[i] = UtilMath.normaliseValue(aggregatedValues[xData[i]][zData[i]], 0, max, 0, 1);
                }
                else
                {
                    positions[i] = aggregatedValues[xData[i]][zData[i]];
                }
            }

            return positions;
        }


        // *************************************************************
        // ********************  UNITY METHODS  ************************
        // *************************************************************

    }

}