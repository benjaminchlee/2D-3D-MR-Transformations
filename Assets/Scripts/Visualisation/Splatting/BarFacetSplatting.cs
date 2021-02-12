using DG.Tweening;
using IATK;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SSVis
{
    public class BarFacetSplatting : BaseVisualisationSplatting
    {
        private List<DataVisualisation> facetVisualisations = new List<DataVisualisation>();

        private const float maxFacetWidth = 1;
        private const float maxFacetHeight = 1;

        private Vector3 originalScale;

        public override void ApplySplat(System.Tuple<Vector3, Vector3> placementValues = null)
        {
            if (!isInitialised)
            {
                Debug.LogError("Bar Facet Splatting: Cannot apply the splat before Initialise() has been called.");
                return;
            }

            if (DataVisualisation.XDimension == "Undefined" || DataVisualisation.ZDimension == "Undefined" || DataVisualisation.VisualisationType != IATK.AbstractVisualisation.VisualisationTypes.BAR)
            {
                Debug.LogError("Bar Facet Splatting: A 3 dimensional bar chart is required.");
                return;
            }

            if (placementValues == null)
                placementValues = new System.Tuple<Vector3, Vector3>(Vector3.zero, Vector3.zero);

            GameObject target = new GameObject();
            target.transform.position = placementValues.Item1;
            target.transform.eulerAngles = placementValues.Item2;
            Vector3 up = target.transform.up;
            Vector3 right = target.transform.right;
            Vector3 forward = target.transform.forward;

            int numFacets = DataVisualisation.NumZBins;
            string facetingDimension = DataVisualisation.ZDimension;
            int numRows = (int)Mathf.Sqrt(numFacets);
            int numCols = (int)Mathf.Ceil(numFacets / (float)numRows);

            float visWidth = Mathf.Min(maxFacetWidth / numCols, DataVisualisation.Width);
            float visHeight = Mathf.Min(maxFacetHeight / numRows, DataVisualisation.Height);


            float xDelta = Mathf.Min(maxFacetWidth, visWidth * numCols) / (numCols * 2);
            float yDelta = Mathf.Min(maxFacetHeight, visHeight * numCols) / (numRows * 2);

            int index = 0;
            for (int i = 0; i < numRows; i++)
            {
                for (int j = 0; j < numCols; j++)
                {
                    if (index < numFacets)
                    {
                        var facet = DataVisualisationManager.Instance.CloneDataVisualisation(DataVisualisation);
                        facetVisualisations.Add(facet);
                        facet.SetZAxisVisibility(false);
                        facet.IsPrototype = true;

                        // Set filtering
                        AttributeFilter filter = new AttributeFilter();
                        filter.Attribute = facetingDimension;
                        filter.minFilter = Mathf.Max(index / (float)numFacets - 0.001f, 0);
                        filter.maxFilter = Mathf.Min((index + 1) / (float)numFacets + 0.001f, 1);
                        facet.Visualisation.attributeFilters = new AttributeFilter[] { filter };
                        facet.Visualisation.updateViewProperties(AbstractVisualisation.PropertyType.AttributeFiltering);

                        // Set to the starting position and rotation
                        facet.transform.position = transform.position;
                        facet.transform.rotation = transform.rotation;

                        // Calculate and animate towards the target position for this facet
                        float x = j * visWidth - (visWidth * numCols) / 2f + visWidth / 2;
                        float y = i * visHeight - (visHeight * numRows) / 2f + visHeight / 2;
                        float z = DataVisualisation.Depth / 2;
                        Vector3 targetPos = target.transform.position + x * right - up * y + z * forward;
                        facet.transform.DOMove(targetPos, 0.1f);
                        facet.transform.DORotate(target.transform.eulerAngles, 0.1f);

                        // Set scale
                        facet.Scale = new Vector3(visWidth, visHeight, 0.0025f);

                        index++;
                    }
                }

            }

            // Scale up the original visualisation such that its collider overlaps with the entire facet grid
            originalScale = DataVisualisation.Scale;
            DataVisualisation.Scale = new Vector3(visWidth * numCols, visHeight * numRows, 0.1f);

            // Change the layer on the original visualisation's collider to prioritise the others
            DataVisualisation.gameObject.layer = LayerMask.NameToLayer("Back Trigger Layer");

            ToggleDataVisualisationVisiblity(false);
            Destroy(target);
        }

        public override void DestroyThisSplat()
        {
            foreach (var facet in facetVisualisations)
            {
                Destroy(facet.gameObject);
            }

            DataVisualisation.Scale = originalScale;
            DataVisualisation.gameObject.layer = LayerMask.NameToLayer("Default");
            ToggleDataVisualisationVisiblity(true);
        }

        private void ToggleDataVisualisationVisiblity(bool visible)
        {
            DataVisualisation.SetXAxisVisibility(visible);
            DataVisualisation.SetYAxisVisibility(visible);
            DataVisualisation.SetZAxisVisibility(visible);
            if (visible) DataVisualisation.ShowAxisManipulators();
            else DataVisualisation.HideAxisManipulators();
            DataVisualisation.Visualisation.theVisualizationObject.viewList[0].gameObject.SetActive(visible);
        }
    }
}