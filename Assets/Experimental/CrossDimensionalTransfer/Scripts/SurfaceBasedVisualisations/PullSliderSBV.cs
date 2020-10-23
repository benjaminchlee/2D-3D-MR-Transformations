using DG.Tweening;
using IATK;
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
        public Transform SliderHandle;
        public float VisualisationInterval = 0.2f;
        public DataSource DataSource;
        
        private List<Visualisation> visualisations;
        
        private void Start()
        {
            visualisations = new List<Visualisation>();
            visualisations.Add(GetComponentInChildren<Visualisation>());
        }
        
        private void Update()
        {
            float distance = -SliderHandle.transform.localPosition.z;
            
            int currentVisualisations = visualisations.Count;
            int targetVisualisations = Mathf.FloorToInt(distance / VisualisationInterval) + 1;
            
            // Add visualisations
            if (currentVisualisations < targetVisualisations)
            {
                for (int i = 0; i < targetVisualisations - currentVisualisations; i++)
                {
                    CreateVisualisation();
                }
            }
            else if (currentVisualisations > targetVisualisations)
            {
                for (int i = 0; i < currentVisualisations - targetVisualisations; i++)
                {
                    DeleteVisualisation();
                }
            }
            
            // Position visualisations
            for (int i = 0; i < visualisations.Count; i++)
            {
                visualisations[i].transform.localPosition = new Vector3(0, 0, -Mathf.Lerp(distance, 0, i/(float)targetVisualisations));
            }
        }
        
        
        private void CreateVisualisation()
        {
            var go = new GameObject();
            go.transform.SetParent(transform);
            var vis = go.AddComponent<Visualisation>();
            visualisations.Add(vis);
            
            vis.colourDimension = "Undefined";
            vis.colorPaletteDimension = "Undefined";
            vis.sizeDimension = "Undefined";
            vis.linkingDimension = "Undefined";
            
            vis.dataSource = DataSource;
            vis.geometry = AbstractVisualisation.GeometryType.Points;
            vis.visualisationType = AbstractVisualisation.VisualisationTypes.SCATTERPLOT;
            vis.xDimension = DataSource[Mathf.FloorToInt(Random.Range(0, DataSource.DimensionCount))].Identifier;
            vis.yDimension = DataSource[Mathf.FloorToInt(Random.Range(0, DataSource.DimensionCount))].Identifier;
            vis.width = 0.2f;
            vis.height = 0.2f;
            vis.depth = 0.2f;
            vis.size = 0.1f;
            
            vis.updateViewProperties(AbstractVisualisation.PropertyType.VisualisationType);
            vis.updateViewProperties(AbstractVisualisation.PropertyType.GeometryType);
            vis.updateViewProperties(AbstractVisualisation.PropertyType.X);
            vis.updateViewProperties(AbstractVisualisation.PropertyType.Y);
            vis.updateViewProperties(AbstractVisualisation.PropertyType.Size);
            vis.updateViewProperties(AbstractVisualisation.PropertyType.Scaling);
            
            
            // theVisualizationObject.UpdateVisualisation(AbstractVisualisation.PropertyType.AttributeFiltering);
            // theVisualizationObject.UpdateVisualisation(AbstractVisualisation.PropertyType.BlendDestinationMode);
            // theVisualizationObject.UpdateVisualisation(AbstractVisualisation.PropertyType.BlendSourceMode);
            // theVisualizationObject.UpdateVisualisation(AbstractVisualisation.PropertyType.Colour);
            // theVisualizationObject.UpdateVisualisation(AbstractVisualisation.PropertyType.DimensionChange);
            // theVisualizationObject.UpdateVisualisation(AbstractVisualisation.PropertyType.DimensionFiltering);
            // theVisualizationObject.UpdateVisualisation(AbstractVisualisation.PropertyType.LinkingDimension);
            // theVisualizationObject.UpdateVisualisation(AbstractVisualisation.PropertyType.None);
            // theVisualizationObject.UpdateVisualisation(AbstractVisualisation.PropertyType.Scaling);
            // theVisualizationObject.UpdateVisualisation(AbstractVisualisation.PropertyType.Size);
            // theVisualizationObject.UpdateVisualisation(AbstractVisualisation.PropertyType.SizeValues);
            // theVisualizationObject.UpdateVisualisation(AbstractVisualisation.PropertyType.VisualisationType);
            // theVisualizationObject.UpdateVisualisation(AbstractVisualisation.PropertyType.VisualisationSize);
        }
        
        private void DeleteVisualisation()
        {
            var vis = visualisations[visualisations.Count - 1];
            visualisations.RemoveAt(visualisations.Count - 1);
            Destroy(vis.gameObject);
        }
    }
}

