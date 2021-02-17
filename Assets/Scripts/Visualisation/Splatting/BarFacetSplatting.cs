using DG.Tweening;
using IATK;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace SSVis
{
    public class BarFacetSplatting : BaseVisualisationSplatting
    {
        private List<DataVisualisation> facetVisualisations = new List<DataVisualisation>();
        private List<TextMeshPro> facetLabels = new List<TextMeshPro>();
        private TextMeshPro mainLabel;

        private const float maxFacetWidth = 1.25f;
        private const float maxFacetHeight = 1.25f;

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

            int numFacets;
            float[] categories = null;
            if (IsDimensionCategorical(DataVisualisation.ZDimension))
            {
                categories = DataSource[DataVisualisation.ZDimension].Data.Distinct().ToArray();
                numFacets = categories.Count();
            }
            else
            {
                numFacets = DataVisualisation.NumZBins;
            }

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
                        facet.HideAxisManipulators();
                        facet.IsPrototype = true;

                        // Set filtering to only show within the required range for each category
                        float[] data = DataSource[facet.ZDimension].Data;
                        float min = data.Min();
                        float max = data.Max();
                        facet.Visualisation.zDimension.minFilter = Mathf.Lerp(min, max, index / (float) numFacets);
                        facet.Visualisation.zDimension.maxFilter = Mathf.Lerp(min, max, (index + 1) / (float) numFacets);
                        facet.Visualisation.updateViewProperties(AbstractVisualisation.PropertyType.DimensionFiltering);

                        // Set the facet's starting position, rotation, and depth
                        float barWidth = DataVisualisation.Depth / numFacets;
                        facet.transform.position = transform.position + DataVisualisation.transform.forward * barWidth * index - DataVisualisation.transform.forward * ((DataVisualisation.Depth / 2) - (barWidth / 2));
                        facet.transform.rotation = transform.rotation;
                        facet.Scale = new Vector3(DataVisualisation.Width, DataVisualisation.Height, barWidth);
                        // We also override the bar size such that it takes up the full facet's amount pre-animation
                        facet.Visualisation.theVisualizationObject.viewList[0].BigMesh.SharedMaterial.SetFloat("_Depth", 0.5f);

                        // Calculate and animate towards the target position for this facet
                        float x = j * visWidth - (visWidth * numCols) / 2f + visWidth / 2;
                        float y = i * visHeight - (visHeight * numRows) / 2f + visHeight / 2;
                        float z = DataVisualisation.Depth / 2;
                        Vector3 targetPos = target.transform.position + x * right - up * y + z * forward;
                        facet.transform.DOMove(targetPos, 2f).SetEase(Ease.InOutCubic);
                        facet.transform.DORotate(target.transform.eulerAngles, 2f).SetEase(Ease.InOutCubic);
                        DOTween.To(() => facet.Scale, _ => facet.Scale = _, new Vector3(visWidth - 0.075f, visHeight - 0.075f, 0.0025f), 2f).SetEase(Ease.InOutCubic);  // make it a bit smaller in order to have some gaps between them

                        // Create a label and place it above
                        TextMeshPro label = new GameObject("FacetLabel").AddComponent<TextMeshPro>();
                        facetLabels.Add(label);
                        label.GetComponent<RectTransform>().sizeDelta = new Vector2(0.175f, 0.05f);
                        label.autoSizeTextContainer = false;
                        label.alignment = TextAlignmentOptions.Midline;
                        label.fontSize = 0.15f;
                        label.transform.SetParent(facet.transform);
                        label.transform.localPosition = new Vector3(0, visHeight / 2, 0);
                        label.transform.localRotation = Quaternion.identity;
                        // Set text
                        if (categories != null)
                        {
                            label.text = DataSource.getOriginalValue(categories[index], facetingDimension).ToString();
                        }
                        else
                        {
                            string range1 = DataSource.getOriginalValue(index / (float)numFacets, facetingDimension).ToString();
                            string range2 = DataSource.getOriginalValue((index + 1) / (float)numFacets, facetingDimension).ToString();
                            label.text = (range1 == range2) ? range1 : range1 + " ... " + range2;
                        }

                        index++;
                    }
                }
            }

            // Create a main label above everything that shows the faceting dimension
            mainLabel = new GameObject("FacetLabel").AddComponent<TextMeshPro>();
            facetLabels.Add(mainLabel);
            mainLabel.GetComponent<RectTransform>().sizeDelta = new Vector2(0.5f, 0.05f);
            mainLabel.autoSizeTextContainer = false;
            mainLabel.alignment = TextAlignmentOptions.Midline;
            mainLabel.fontSize = 0.25f;
            mainLabel.transform.position = target.transform.position + up * (visHeight * numRows / 2 + 0.05f) + forward * DataVisualisation.Depth / 2;
            mainLabel.transform.rotation = target.transform.rotation;
            mainLabel.text = "Faceting by " + facetingDimension;

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
            foreach (var textMesh in facetLabels)
            {
                Destroy(textMesh.gameObject);
            }
            Destroy(mainLabel.gameObject);

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

        private bool IsDimensionCategorical(string dimension)
        {
            var type = DataSource[dimension].MetaData.type;
            return (type == DataType.String || type == DataType.Date);
        }
    }
}