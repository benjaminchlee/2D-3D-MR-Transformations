using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using IATK;
using UnityEngine;

namespace SSVis
{
    public class BarChartCrackingContinuous : BaseVisualisationContinuous
    {
        private DataVisualisation[] facetVisualisations;

        private const float maxFacetWidth = 1.25f;
        private const float maxFacetHeight = 1.25f;
        private const float animationTime = 0.25f;

        private int totalFacets;
        private string facetingDimension;
        private int numRows, numCols;
        private float visWidth, visHeight;
        private float xDelta, yDelta;
        private float[] data;
        private float min, max;
        private float barWidth;


        private int numCracked = 0;
        private bool crackingFromZero = true;
        private int previousNumCracked;
        private bool previousCrackingFromZero;

        private Vector3 originalScale;

        public override void Initialise(DataSource dataSource, DataVisualisation dataVisualisation, Visualisation visualisation)
        {
            base.Initialise(dataSource, dataVisualisation, visualisation);

            float[] categories = null;
            if (IsDimensionCategorical(DataVisualisation.ZDimension))
            {
                categories = DataSource[DataVisualisation.ZDimension].Data.Distinct().ToArray();
                totalFacets = categories.Count();
            }
            else
            {
                totalFacets = DataVisualisation.NumZBins;
            }
            facetVisualisations = new DataVisualisation[totalFacets];

            // Precompute fixed variables based off the given Data Visualisation
            facetingDimension = DataVisualisation.ZDimension;
            numRows = (int)Mathf.Sqrt(totalFacets);
            numCols = (int)Mathf.Ceil(totalFacets / (float)numRows);
            visWidth = Mathf.Min(maxFacetWidth / numCols, DataVisualisation.Width);
            visHeight = Mathf.Min(maxFacetHeight / numRows, DataVisualisation.Height);
            xDelta = Mathf.Min(maxFacetWidth, visWidth * numCols) / (numCols * 2);
            yDelta = Mathf.Min(maxFacetHeight, visHeight * numCols) / (numRows * 2);

            data = DataSource[facetingDimension].Data;
            min = data.Min();
            max = data.Max();
            barWidth = DataVisualisation.Depth / totalFacets;
        }

        public override void UpdateContinuous(GameObject nearestSurface, System.Tuple<Vector3, Vector3> placementValues = null)
        {
            if (!isInitialised)
            {
                Debug.LogError("Bar Cracking Continuous: Cannot apply the continuous transform before Initialise() has been called.");
                return;
            }

            if (DataVisualisation.XDimension == "Undefined" || DataVisualisation.ZDimension == "Undefined" || DataVisualisation.VisualisationType != IATK.AbstractVisualisation.VisualisationTypes.BAR)
            {
                Debug.LogError("Bar Cracking Continuous: A 3 dimensional bar chart is required.");
                return;
            }

            // Calculate how many facets to crack in the visualisation
            CalculateNumCrackedFacets(nearestSurface);

            // If the direction at which the bar chart is cracked has swapped, we need to reset the visualisation entirely before we do anything else
            if (crackingFromZero != previousCrackingFromZero)
            {
                for (int i = 0; i < totalFacets; i++)
                {
                    if (facetVisualisations[i] != null)
                    {
                        RemoveFacet(i);
                    }
                }

                Visualisation.zDimension.minFilter = 0;
                Visualisation.zDimension.maxFilter = 1;

                previousNumCracked = 0;
            }

            // If the number of facets we need has decreased, we destroy existing facets and restore the original data visualisation
            if (numCracked < previousNumCracked)
            {
                if (crackingFromZero)
                {
                    for (int i = numCracked; i < previousNumCracked; i++)
                    {
                        RemoveFacet(i);
                    }
                }
                else
                {
                    for (int i = numCracked; i < previousNumCracked; i++)
                    {
                        RemoveFacet(totalFacets - 1 - i);
                    }
                }
            }
            // Otherwise it has increased, and we need to create facets and shrink the original data visualisation
            else
            {
                if (placementValues == null)
                    placementValues = new System.Tuple<Vector3, Vector3>(Vector3.zero, Vector3.zero);
                // Create temporary gameobject which will make it easier to position facets
                GameObject target = new GameObject();
                target.transform.position = placementValues.Item1;
                target.transform.rotation = nearestSurface.transform.rotation; // Force use the surface's rotation
                Vector3 up = target.transform.up;
                Vector3 right = target.transform.right;
                Vector3 forward = target.transform.forward;

                int index = crackingFromZero ? 0 : totalFacets - 1;
                int created = 0;

                for (int i = 0; i < numRows; i++)
                {
                    for (int j = 0; j < numCols; j++)
                    {
                        if (created < numCracked)
                        {
                            if (facetVisualisations[index] == null || !facetVisualisations[index].gameObject.activeSelf)
                            {
                                AddFacet(index, i, j, target, right, up, forward);
                            }

                            index += crackingFromZero ? 1 : -1;
                            created++;
                        }
                    }
                }

                Destroy(target);
            }

            if (crackingFromZero)
            {
                Visualisation.zDimension.minFilter = Mathf.Lerp(min, max, numCracked / (float) totalFacets);
                Visualisation.zDimension.maxFilter = 1;
            }
            else
            {
                Visualisation.zDimension.minFilter = 0;
                Visualisation.zDimension.maxFilter = Mathf.Lerp(max, min, numCracked / (float) totalFacets);
            }

            // Apply visualisation changes
            Visualisation.updateViewProperties(AbstractVisualisation.PropertyType.DimensionFiltering);

            previousNumCracked = numCracked;
            previousCrackingFromZero = crackingFromZero;
        }

        private void UpdateVisualisationFilters()
        {

        }

        public override void DestroyThisContinuous()
        {
            // Animate any remaining facets back to the Data Visualisation
            for (int i = 0; i < totalFacets; i++)
            {
                if (facetVisualisations[i] != null)
                {
                    RemoveFacet(i);
                    Destroy(facetVisualisations[i].gameObject, animationTime);
                }
            }

            Visualisation.zDimension.minFilter = 0;
            Visualisation.zDimension.maxFilter = 1;
            Visualisation.updateViewProperties(AbstractVisualisation.PropertyType.DimensionFiltering);

            Destroy(this);
        }

        private void CalculateNumCrackedFacets(GameObject nearestSurface)
        {
            // Determine if the visualisation is contacting the surface from the 0 or 1 side
            float dot = Vector3.Dot(nearestSurface.transform.forward, DataVisualisation.transform.forward);
            crackingFromZero = (dot < 0);

            // Do overlap boxes along the different segments of the barchart to see which ones are colliding with the surface
            Vector3 centre = DataVisualisation.transform.position;
            Vector3 forward = DataVisualisation.transform.forward;
            Vector3 offset = forward * ((DataVisualisation.Depth / 2) - barWidth / 2);
            Vector3 halfExtents = new Vector3(DataVisualisation.Width, DataVisualisation.Height, barWidth) / 2f;
            numCracked = 0;

            if (crackingFromZero)
            {
                for (int i = 0; i < totalFacets; i++)
                {
                    if (IsSectionOverlapping(nearestSurface, i, centre, forward, offset, halfExtents))
                        numCracked = i + 1;
                }
            }
            else
            {
                int j = 0;
                for (int i = totalFacets - 1; i >= 0; i--)
                {
                    if (IsSectionOverlapping(nearestSurface, i, centre, forward, offset, halfExtents))
                        numCracked = j + 1;
                    j++;
                }
            }
        }

        private void AddFacet(int index, int row, int col, GameObject target, Vector3 right, Vector3 up, Vector3 forward)
        {
            DataVisualisation facet = facetVisualisations[index];

            // Create the facet object if it does not yet exist
            if (facet == null)
            {
                facet = DataVisualisationManager.Instance.CloneDataVisualisation(DataVisualisation);
                facetVisualisations[index] = facet;
                facet.SetZAxisVisibility(false);
                facet.HideAxisManipulators();
                facet.IsPrototype = true;

                // Set filtering to only show within the required range
                facet.Visualisation.zDimension.minFilter = Mathf.Lerp(min, max, index / (float) totalFacets);
                facet.Visualisation.zDimension.maxFilter = Mathf.Lerp(min, max, (index + 1) / (float) totalFacets);
                facet.Visualisation.updateViewProperties(AbstractVisualisation.PropertyType.DimensionFiltering);

            }
            // Otherwise we just use a previous one that has already been set up
            else
            {
                facet.gameObject.SetActive(true);
            }

            // Set the facet's starting position, rotation, and depth
            facet.transform.position = transform.position + DataVisualisation.transform.forward * barWidth * index - DataVisualisation.transform.forward * ((DataVisualisation.Depth / 2) - (barWidth / 2));
            facet.transform.rotation = transform.rotation;
            facet.Scale = new Vector3(DataVisualisation.Width, DataVisualisation.Height, barWidth);
            // Override the bar size such that it takes up the full facet's amount pre-animation
            facet.Visualisation.theVisualizationObject.viewList[0].BigMesh.SharedMaterial.SetFloat("_Depth", 0.5f);

            // Calculate and animate towards the target position for this facet
            float x = col * visWidth - (visWidth * numCols) / 2f + visWidth / 2;
            float y = row * visHeight - (visHeight * numRows) / 2f + visHeight / 2;
            float z = DataVisualisation.Depth / 2;
            Vector3 targetPos = target.transform.position + x * right - up * y + z * forward;
            facet.transform.DOMove(targetPos, animationTime).SetEase(Ease.InOutCubic);
            facet.transform.DORotate(target.transform.eulerAngles, animationTime).SetEase(Ease.InOutCubic);
            DOTween.To(() => facet.Scale, _ => facet.Scale = _, new Vector3(visWidth - 0.075f, visHeight - 0.075f, 0.0025f), animationTime).SetEase(Ease.InOutCubic);  // make it a bit smaller in order to have some gaps between them
        }

        private void RemoveFacet(int index)
        {
            DataVisualisation facet = facetVisualisations[index];
            if (facet == null)
                return;

            Vector3 targetPos = new Vector3(0, 0, barWidth * index - DataVisualisation.Depth / 2);
            Vector3 targetRot = Vector3.zero;

            facet.transform.SetParent(DataVisualisation.transform);
            facet.transform.DOLocalMove(targetPos, animationTime).SetEase(Ease.InOutCubic);
            facet.transform.DOLocalRotate(targetRot, animationTime).SetEase(Ease.InOutCubic).OnComplete(() => {
                // When animation is complete, disable this facet and unparent it
                facet.gameObject.SetActive(false);
                facet.transform.parent = null;
            });
        }

        private bool IsSectionOverlapping(GameObject nearestSurface, int i, Vector3 centre, Vector3 forward, Vector3 offset, Vector3 halfExtents)
        {
            var overlapping = Physics.OverlapBox(centre + forward * i * barWidth - offset, halfExtents, DataVisualisation.transform.rotation);
            return overlapping.Select(x => x.gameObject).Contains(nearestSurface);
        }

        private bool IsDimensionCategorical(string dimension)
        {
            var type = DataSource[dimension].MetaData.type;
            return (type == DataType.String || type == DataType.Date);
        }
    }
}
