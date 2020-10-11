using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using DG.Tweening;


namespace IATK
{
    [ExecuteInEditMode]
    public class ScatterplotVisualisation : AbstractVisualisation 
    {
        #region Method overrides
        
        public override void CreateVisualisation()
        {
            string savedName = name;
            
            foreach (View v in viewList)
            {
                DestroyImmediate(v.gameObject);
            }

            viewList.Clear();

            // Create the configuration object
            if (creationConfiguration == null)
                creationConfiguration = new CreationConfiguration()
                {
                    XDimension = visualisationReference.xDimension.Attribute,
                    YDimension = visualisationReference.yDimension.Attribute,
                    ZDimension = visualisationReference.zDimension.Attribute,
                    Geometry = visualisationReference.geometry
                };
            else
            {
                creationConfiguration.Geometry = visualisationReference.geometry;
                creationConfiguration.XDimension = visualisationReference.xDimension.Attribute;
                creationConfiguration.YDimension = visualisationReference.yDimension.Attribute;
                creationConfiguration.ZDimension = visualisationReference.zDimension.Attribute;
                creationConfiguration.LinkingDimension = visualisationReference.linkingDimension;
                creationConfiguration.Size = visualisationReference.size;
                creationConfiguration.MinSize = visualisationReference.minSize;
                creationConfiguration.MaxSize = visualisationReference.maxSize;
                creationConfiguration.colour = visualisationReference.colour;
                creationConfiguration.VisualisationWidth = visualisationReference.width;
                creationConfiguration.VisualisationHeight = visualisationReference.height;
                creationConfiguration.VisualisationDepth = visualisationReference.depth;
            }

            // Create the visualisation view (i.e., the mesh with visualisation marks)
            View view = CreateSimpleVisualisation(creationConfiguration);
            
            if (view != null)
            {
                view.transform.localPosition = Vector3.zero;
                view.transform.SetParent(transform, false);
                view.onViewChangeEvent += UpdateVisualisation;   // Receive notifications when the view configuration changes
                view.transform.localScale = new Vector3(
                    visualisationReference.width,
                    visualisationReference.height,
                    visualisationReference.depth
                );
                
                viewList.Add(view);
                                
                // Set visual properties for all views (in most cases the scatterplot should only have 1 view)
                for (int i = 0; i < viewList.Count; i++)
                {
                    var thisView = viewList[i];
                    
                    // Set colours of the view's points based on visualisation configuration
                    // Continuous colour dimension
                    if (visualisationReference.colourDimension != "Undefined")
                    {
                        thisView.SetColors(mapColoursContinuous(visualisationReference.dataSource[visualisationReference.colourDimension].Data));
                    }
                    // Discrete colour palette dimension
                    else if (visualisationReference.colorPaletteDimension != "Undefined")
                    {
                        thisView.SetColors(mapColoursPalette(visualisationReference.dataSource[visualisationReference.colorPaletteDimension].Data, visualisationReference.coloursPalette));
                    }
                    // Uniform colour
                    else
                    {
                        Color colourToSet = (visualisationReference.colour != null) ? visualisationReference.colour : Color.white;
                        Color[] colours = thisView.GetColors();
                        for (int j = 0; j < colours.Length; j++)
                        {
                            colours[j] = colourToSet;
                        }
                        thisView.SetColors(colours);
                    }
                    
                    thisView.SetSize(visualisationReference.size);
                    thisView.SetMinSize(visualisationReference.minSize);
                    thisView.SetMaxSize(visualisationReference.maxSize);
                    if (visualisationReference.sizeDimension != "Undefined")
                        thisView.SetSizeChannel(visualisationReference.dataSource[visualisationReference.sizeDimension].Data);

                    thisView.SetMinNormX(visualisationReference.xDimension.minScale);
                    thisView.SetMaxNormX(visualisationReference.xDimension.maxScale);
                    thisView.SetMinNormY(visualisationReference.yDimension.minScale);
                    thisView.SetMaxNormY(visualisationReference.yDimension.maxScale);
                    thisView.SetMinNormZ(visualisationReference.zDimension.minScale);
                    thisView.SetMaxNormZ(visualisationReference.zDimension.maxScale);

                    thisView.SetMinX(visualisationReference.xDimension.minFilter);
                    thisView.SetMaxX(visualisationReference.xDimension.maxFilter);
                    thisView.SetMinY(visualisationReference.yDimension.minFilter);
                    thisView.SetMaxY(visualisationReference.yDimension.maxFilter);
                    thisView.SetMinZ(visualisationReference.zDimension.minFilter);
                    thisView.SetMaxZ(visualisationReference.zDimension.maxFilter);
                }
            }
            
            name = savedName;
        }

        public override void UpdateVisualisation(PropertyType propertyType){

            if (viewList.Count == 0)
                CreateVisualisation();

            // Update creation configuration properties to most up-to-date values
            creationConfiguration.XDimension = visualisationReference.xDimension.Attribute;
            creationConfiguration.YDimension = visualisationReference.yDimension.Attribute;
            creationConfiguration.ZDimension = visualisationReference.zDimension.Attribute;
            creationConfiguration.ColourDimension = visualisationReference.colourDimension;
            creationConfiguration.colourKeys = visualisationReference.dimensionColour;
            creationConfiguration.colour = visualisationReference.colour;
            creationConfiguration.SizeDimension = visualisationReference.sizeDimension;  
            creationConfiguration.Size = visualisationReference.size;
            creationConfiguration.MinSize = visualisationReference.minSize;
            creationConfiguration.MaxSize = visualisationReference.maxSize;     
            creationConfiguration.LinkingDimension = visualisationReference.linkingDimension;
            creationConfiguration.Geometry = visualisationReference.geometry;
            creationConfiguration.VisualisationWidth = visualisationReference.width;
            creationConfiguration.VisualisationHeight = visualisationReference.height;
            creationConfiguration.VisualisationDepth = visualisationReference.depth;
            
            for (int a = 0; a < viewList.Count; a++)
            {
                var thisView = viewList[a];
                
                switch (propertyType)
                {
                    case AbstractVisualisation.PropertyType.X:
                        // Update position of points depending if a dimension is given or not
                        if (visualisationReference.xDimension.Attribute.Equals("Undefined"))
                            thisView.ZeroPosition(0);
                        else
                            thisView.UpdateXPositions(visualisationReference.dataSource[visualisationReference.xDimension.Attribute].Data);
                        // Trigger an animation (tween) to new positions
                        thisView.TweenPosition();                        
                        break;
                        
                    case AbstractVisualisation.PropertyType.Y:
                        if (visualisationReference.yDimension.Attribute.Equals("Undefined"))
                            thisView.ZeroPosition(1);
                        else
                            thisView.UpdateYPositions(visualisationReference.dataSource[visualisationReference.yDimension.Attribute].Data);
                        thisView.TweenPosition();
                        break;
                        
                    case AbstractVisualisation.PropertyType.Z:
                        if (visualisationReference.zDimension.Attribute.Equals("Undefined"))
                            thisView.ZeroPosition(2);
                        else
                            thisView.UpdateZPositions(visualisationReference.dataSource[visualisationReference.zDimension.Attribute].Data);
                        thisView.TweenPosition();                        
                        break;
                        
                    case AbstractVisualisation.PropertyType.Colour:
                        // Continuous colour dimension
                        if (visualisationReference.colourDimension != "Undefined")
                        {
                            thisView.SetColors(mapColoursContinuous(visualisationReference.dataSource[visualisationReference.colourDimension].Data));
                        }
                        // Discrete colour palette dimension
                        else if (visualisationReference.colorPaletteDimension != "Undefined")
                        {
                            thisView.SetColors(mapColoursPalette(visualisationReference.dataSource[visualisationReference.colorPaletteDimension].Data, visualisationReference.coloursPalette));
                        }
                        // Uniform colour
                        else
                        {
                            Color colourToSet = (visualisationReference.colour != null) ? visualisationReference.colour : Color.white;
                            Color[] colours = thisView.GetColors();
                            for (int j = 0; j < colours.Length; j++)
                            {
                                colours[j] = colourToSet;
                            }
                            thisView.SetColors(colours);
                        }
                        break;
                        
                    case AbstractVisualisation.PropertyType.Size:
                        if (visualisationReference.sizeDimension != "Undefined")
                            thisView.SetSizeChannel(visualisationReference.dataSource[visualisationReference.sizeDimension].Data);
                        else
                            thisView.SetSizeChannel(Enumerable.Repeat(0f, visualisationReference.dataSource[0].Data.Length).ToArray());
                        viewList[0].TweenSize();
                        break;

                    case PropertyType.SizeValues:
                        thisView.SetSize(visualisationReference.size);
                        thisView.SetMinSize(visualisationReference.minSize);        // Data is normalised
                        thisView.SetMaxSize(visualisationReference.maxSize);
                        break;
                        
                    case AbstractVisualisation.PropertyType.LinkingDimension:
                        // Recreate the visualisation because the mesh properties have changed 
                        CreateVisualisation(); 
                        break;

                    case AbstractVisualisation.PropertyType.GeometryType:
                        // Recreate the visualisation because the mesh properties have changed 
                        CreateVisualisation(); 
                        break;

                    case AbstractVisualisation.PropertyType.Scaling:
                        thisView.SetMinNormX(visualisationReference.xDimension.minScale);
                        thisView.SetMaxNormX(visualisationReference.xDimension.maxScale);
                        thisView.SetMinNormY(visualisationReference.yDimension.minScale);
                        thisView.SetMaxNormY(visualisationReference.yDimension.maxScale);
                        thisView.SetMinNormZ(visualisationReference.zDimension.minScale);
                        thisView.SetMaxNormZ(visualisationReference.zDimension.maxScale);
                        break;

                    case AbstractVisualisation.PropertyType.DimensionFiltering:
                        thisView.SetMinX(visualisationReference.xDimension.minFilter);
                        thisView.SetMaxX(visualisationReference.xDimension.maxFilter);
                        thisView.SetMinY(visualisationReference.yDimension.minFilter);
                        thisView.SetMaxY(visualisationReference.yDimension.maxFilter);
                        thisView.SetMinZ(visualisationReference.zDimension.minFilter);
                        thisView.SetMaxZ(visualisationReference.zDimension.maxFilter);
                        break;
                        
                    case AbstractVisualisation.PropertyType.AttributeFiltering:
                        if (visualisationReference.attributeFilters != null)
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
                            
                            // Map the filtered attribute into the normal channel of the bigmesh
                            thisView.SetFilterChannel(isFiltered);
                        }
                        break;
                        
                    case PropertyType.VisualisationType:                       
                        break;
                        
                    case PropertyType.BlendDestinationMode:
                        float bmds = (int)(System.Enum.Parse(typeof(UnityEngine.Rendering.BlendMode), visualisationReference.blendingModeDestination));
                        thisView.SetBlendindDestinationMode(bmds);
                        break;
                        
                    case PropertyType.BlendSourceMode:
                        float bmd = (int)(System.Enum.Parse(typeof(UnityEngine.Rendering.BlendMode), visualisationReference.blendingModeSource));
                        thisView.SetBlendingSourceMode(bmd);

                        break;
                    
                    case PropertyType.VisualisationSize:
                        thisView.transform.localScale = new Vector3(
                            visualisationReference.width,
                            visualisationReference.height,
                            visualisationReference.depth
                        );
                        break;
                    
                    default:
                        break;
                }
            }
            
            if (visualisationReference.geometry != GeometryType.Undefined)
                SerializeViewConfiguration(creationConfiguration);

            // Handle the axes objects of this visualisation
            UpdateVisualisationAxes(propertyType);
        }

        public override void SaveConfiguration(){}

        public override void LoadConfiguration(){}

        #endregion // Function overrides

        protected void UpdateVisualisationAxes(AbstractVisualisation.PropertyType propertyType)
        {
            switch (propertyType)
            {
                case AbstractVisualisation.PropertyType.X:
                    // Axis deletion
                    if (visualisationReference.xDimension.Attribute == "Undefined" && X_AXIS != null)
                    {
                        DestroyImmediate(X_AXIS);
                    }
                    // Axis updating
                    else if (X_AXIS != null)
                    {
                        Axis a = X_AXIS.GetComponent<Axis>();
                        a.Initialise(visualisationReference.dataSource, visualisationReference.xDimension, visualisationReference);
                        BindMinMaxAxisValues(a, visualisationReference.xDimension);
                    }
                    // Axis creation
                    else if (visualisationReference.xDimension.Attribute != "Undefined")
                    {
                        Vector3 pos = Vector3.zero;
                        pos.y = -0.02f;
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
                        a.Initialise(visualisationReference.dataSource, visualisationReference.yDimension, visualisationReference);
                        BindMinMaxAxisValues(a, visualisationReference.yDimension);
                    }
                    else if (visualisationReference.yDimension.Attribute != "Undefined")
                    {
                        Vector3 pos = Vector3.zero;
                        pos.x = -0.02f;
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
                        a.Initialise(visualisationReference.dataSource, visualisationReference.zDimension, visualisationReference);
                        BindMinMaxAxisValues(Z_AXIS.GetComponent<Axis>(), visualisationReference.zDimension);
                    }
                    else if (visualisationReference.zDimension.Attribute != "Undefined")
                    {
                        Vector3 pos = Vector3.zero;
                        pos.y = -0.02f;
                        pos.x = -0.02f;
                        Z_AXIS = CreateAxis(propertyType, visualisationReference.zDimension, pos, new Vector3(90f, 0f, 0f), 2);
                    }
                    break;

                case AbstractVisualisation.PropertyType.DimensionFiltering:
                    if (visualisationReference.xDimension.Attribute != "Undefined")
                    {
                        BindMinMaxAxisValues(X_AXIS.GetComponent<Axis>(), visualisationReference.xDimension);
                    }
                    if (visualisationReference.yDimension.Attribute != "Undefined")
                    {
                        BindMinMaxAxisValues(Y_AXIS.GetComponent<Axis>(), visualisationReference.yDimension);
                    }
                    if (visualisationReference.zDimension.Attribute != "Undefined")
                    {
                        BindMinMaxAxisValues(Z_AXIS.GetComponent<Axis>(), visualisationReference.zDimension);
                    }
                    break;
                    
                case AbstractVisualisation.PropertyType.Scaling:
                    if (visualisationReference.xDimension.Attribute != "Undefined")
                    {
                        BindMinMaxAxisValues(X_AXIS.GetComponent<Axis>(), visualisationReference.xDimension);
                    }
                    if (visualisationReference.yDimension.Attribute != "Undefined")
                    {
                        BindMinMaxAxisValues(Y_AXIS.GetComponent<Axis>(), visualisationReference.yDimension);
                    }
                    if (visualisationReference.zDimension.Attribute != "Undefined")
                    {
                        BindMinMaxAxisValues(Z_AXIS.GetComponent<Axis>(), visualisationReference.zDimension);
                    }
                    break;
                
                case AbstractVisualisation.PropertyType.VisualisationSize:
                    if (X_AXIS != null)
                    {
                        X_AXIS.GetComponent<Axis>().UpdateLength(visualisationReference.width);
                    }
                    if (Y_AXIS != null)
                    {
                        Y_AXIS.GetComponent<Axis>().UpdateLength(visualisationReference.height);
                    }
                    if (Z_AXIS != null)
                    {
                        Z_AXIS.GetComponent<Axis>().UpdateLength(visualisationReference.depth);
                    }
                    break;
                    
                default:
                    break;
            }
        }
        
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

        #region Scatterplot specific functions

        private View CreateSimpleVisualisation(CreationConfiguration configuration)
        {
            if (visualisationReference.dataSource != null)
            {
                if (!visualisationReference.dataSource.IsLoaded)
                    visualisationReference.dataSource.load();

                ViewBuilder builder = new ViewBuilder(geometryToMeshTopology(configuration.Geometry), "Simple Visualisation");

                string xDimension = configuration.XDimension;
                string yDimension = configuration.YDimension;
                string zDimension = configuration.ZDimension;

                if ((visualisationReference.dataSource[xDimension] != null) ||
                    (visualisationReference.dataSource[yDimension] != null) ||
                    (visualisationReference.dataSource[zDimension] != null))
                {
                    builder.initialiseDataView(visualisationReference.dataSource.DataCount);

                    if (visualisationReference.dataSource[xDimension] != null)
                        builder.setDataDimension(visualisationReference.dataSource[xDimension].Data, ViewBuilder.VIEW_DIMENSION.X);
                    if (visualisationReference.dataSource[yDimension] != null)
                        builder.setDataDimension(visualisationReference.dataSource[yDimension].Data, ViewBuilder.VIEW_DIMENSION.Y);
                    if (visualisationReference.dataSource[zDimension] != null)
                        builder.setDataDimension(visualisationReference.dataSource[zDimension].Data, ViewBuilder.VIEW_DIMENSION.Z);

                    // Destroy the view to clean the big mesh
                    DestroyView();

                    // Return the appropriate geometry view
                    return ApplyGeometryAndRendering(creationConfiguration, ref builder);
                }

            }

            return null;
        }
        
        #endregion // Scatterplot specific functions
    }
}
