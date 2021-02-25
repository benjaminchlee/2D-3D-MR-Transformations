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

        #region Pre-calculated transformation constants

        /// <summary>
        /// The axis that is being cracked. This will likely require a full re-calculation of the following variables
        /// </summary>
        private AxisDirection crackingDimension = AxisDirection.None;
        /// <summary>
        /// The total number of facets/slices that is to be created
        /// </summary>
        private int totalFacets;
        /// <summary>
        /// The name of the dimension that is to be cracked by
        /// </summary>
        private string facetingDimension;
        /// <summary>
        /// An array of the original data
        /// </summary>
        private float[] data;
        /// <summary>
        /// The minimum and maximum float values in the dataset
        /// </summary>
        private float min, max;
        /// <summary>
        /// The number of rows and columns that make up a grid of small multiples on the surface
        /// </summary>
        private int numRows, numCols;
        /// <summary>
        /// The calculated width and height of the small multiples when they are on the surface
        /// </summary>
        private float visWidth, visHeight;
        /// <summary>
        /// The spacing betwen each small multiple when on the surface
        /// </summary>
        private float xDelta, yDelta;
        /// <summary>
        /// The physical width of each bar in the bar chart
        /// </summary>
        private float barWidth;

        #endregion

        private int numCracked = 0;
        private bool crackingFromZero = true;
        private int previousNumCracked;
        private bool previousCrackingFromZero;

        private Vector3 originalScale;

        public override void Initialise(DataSource dataSource, DataVisualisation dataVisualisation, Visualisation visualisation)
        {
            base.Initialise(dataSource, dataVisualisation, visualisation);


        }

        private bool CheckForCrackingDimensionChange(GameObject nearestSurface)
        {
            // Get the axis that is "sticking into" the surface
            float dot = Vector3.Dot(DataVisualisation.transform.forward, nearestSurface.transform.forward);
            AxisDirection newDirection = (-0.5f < dot && dot < 0.5f) ? AxisDirection.X : AxisDirection.Z;

            // Depending on the newDirection we just found, determine if the visualisation is contacting the surface from the 0 or 1 side. The result is inverted between the two different dictions
            if (newDirection == AxisDirection.X)    dot = Vector3.Dot(DataVisualisation.transform.right, nearestSurface.transform.forward);
            bool newCrackingFromZero = (dot < 0);
            
            if (newDirection != crackingDimension || newCrackingFromZero != crackingFromZero)
            {
                crackingDimension = newDirection;
                crackingFromZero = newCrackingFromZero;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Precomputes fixed variables based on the set crackingDimension and Data Visualisation
        /// </summary>
        private void CalculateCrackingProperties()
        {
            // Precompute fixed variables based off the given Data Visualisation
            facetingDimension = (crackingDimension == AxisDirection.X) ? DataVisualisation.XDimension : DataVisualisation.ZDimension;

            if (IsDimensionCategorical(facetingDimension))
            {
                if (crackingDimension == AxisDirection.X)
                    totalFacets = DataSource[DataVisualisation.XDimension].Data.Distinct().Count();
                else
                    totalFacets = DataSource[DataVisualisation.ZDimension].Data.Distinct().Count();
            }
            else
            {
                totalFacets = DataVisualisation.NumZBins;
            }
            facetVisualisations = new DataVisualisation[totalFacets];

            numRows = (int)Mathf.Sqrt(totalFacets);
            numCols = (int)Mathf.Ceil(totalFacets / (float)numRows);
            visWidth = Mathf.Min(maxFacetWidth / numCols, (crackingDimension == AxisDirection.X) ? DataVisualisation.Depth : DataVisualisation.Width);
            visHeight = Mathf.Min(maxFacetHeight / numRows, DataVisualisation.Height);
            xDelta = Mathf.Min(maxFacetWidth, visWidth * numCols) / (numCols * 2);
            yDelta = Mathf.Min(maxFacetHeight, visHeight * numCols) / (numRows * 2);

            data = DataSource[facetingDimension].Data;
            min = data.Min();
            max = data.Max();
            barWidth = Mathf.Abs((crackingDimension == AxisDirection.X) ? DataVisualisation.Width : DataVisualisation.Depth) / totalFacets;
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

            // Check if the dimension has changed between x and z
            if (CheckForCrackingDimensionChange(nearestSurface))
            {
                // If it has, we need to reset the visualisation entirely before we do anything else
                for (int i = 0; i < totalFacets; i++)
                {
                    if (facetVisualisations[i] != null)
                    {
                        RemoveFacet(i);
                    }
                }

                Visualisation.xDimension.minFilter = 0;
                Visualisation.xDimension.maxFilter = 1;
                Visualisation.zDimension.minFilter = 0;
                Visualisation.zDimension.maxFilter = 1;
                previousNumCracked = 0;

                // Calculate new fixed properties specific to this cracking dimension
                CalculateCrackingProperties();
            }

            CalculateNumCrackedFacets(nearestSurface);

            // If the number of facets we need has decreased, we destroy the appropriate number of facets and restore part (or all) of the original data visualisation
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

            if (crackingDimension == AxisDirection.X)
            {
                if (crackingFromZero)
                {
                    Visualisation.xDimension.minFilter = Mathf.Lerp(min, max, numCracked / (float) totalFacets);
                    Visualisation.xDimension.maxFilter = 1;
                }
                else
                {
                    Visualisation.xDimension.minFilter = 0;
                    Visualisation.xDimension.maxFilter = Mathf.Lerp(max, min, numCracked / (float) totalFacets);
                }
            }
            else
            {
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

            Visualisation.xDimension.minFilter = 0;
            Visualisation.xDimension.maxFilter = 1;
            Visualisation.zDimension.minFilter = 0;
            Visualisation.zDimension.maxFilter = 1;
            Visualisation.updateViewProperties(AbstractVisualisation.PropertyType.DimensionFiltering);

            Destroy(this);
        }

        private void CalculateNumCrackedFacets(GameObject nearestSurface)
        {
            // Do overlap boxes along the different segments of the barchart to see which ones are colliding with the surface
            Vector3 centre = DataVisualisation.transform.position;
            // Different values depending on if it's cracking along the x or z dimension
            Vector3 forward, offset, halfExtents;
            if (crackingDimension == AxisDirection.X)
            {
                forward = DataVisualisation.transform.right;
                offset = forward * ((DataVisualisation.Width / 2) - barWidth / 2);
                halfExtents = new Vector3(barWidth, Mathf.Abs(DataVisualisation.Height), Mathf.Abs(DataVisualisation.Depth)) / 2f;
            }
            else
            {
                forward = DataVisualisation.transform.forward;
                offset = forward * ((DataVisualisation.Depth / 2) - barWidth / 2);
                halfExtents = new Vector3(Mathf.Abs(DataVisualisation.Width), Mathf.Abs(DataVisualisation.Height), barWidth) / 2f;
            }

            numCracked = 0;

            // We have to create a "dummy" version of the nearest surface, because for some reason OverlapBox doesn't work on surfaces generated by the Scene Understanding API
            // Create it from scratch to ensure as little baggage as possible
            GameObject dummyWall = new GameObject();
            dummyWall.layer = LayerMask.NameToLayer("SceneWall");
            dummyWall.transform.position = nearestSurface.transform.position;
            dummyWall.transform.rotation = nearestSurface.transform.rotation;
            dummyWall.transform.localScale = nearestSurface.transform.localScale;
            BoxCollider coll = dummyWall.AddComponent<BoxCollider>();
            coll.isTrigger = true;
            coll.size = nearestSurface.GetComponent<BoxCollider>().size;
            coll.center = nearestSurface.GetComponent<BoxCollider>().center;
            Rigidbody rb = dummyWall.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            Physics.SyncTransforms();
            
            if (crackingFromZero)
            {
                for (int i = 0; i < totalFacets; i++)
                {
                    if (IsSectionOverlapping(dummyWall, i, centre, forward, offset, halfExtents))
                        numCracked = i + 1;
                }
            }
            else
            {
                int j = 0;
                for (int i = totalFacets - 1; i >= 0; i--)
                {
                    if (IsSectionOverlapping(dummyWall, i, centre, forward, offset, halfExtents))
                        numCracked = j + 1;
                    j++;
                }
            }
    
            Destroy(dummyWall);
        }

        private void AddFacet(int index, int row, int col, GameObject target, Vector3 right, Vector3 up, Vector3 forward)
        {
            DataVisualisation facet = facetVisualisations[index];

            // Create the facet object if it does not yet exist
            if (facet == null)
            {
                if (crackingDimension == AxisDirection.X)
                {
                    facet = DataVisualisationManager.Instance.CreateDataVisualisation(DataVisualisation.DataSource, AbstractVisualisation.VisualisationTypes.BAR, AbstractVisualisation.GeometryType.Bars,
                                                                                        xDimension: DataVisualisation.ZDimension, yDimension: DataVisualisation.YDimension, zDimension: DataVisualisation.XDimension,
                                                                                        numXBins: DataVisualisation.NumXBins, numZBins: DataVisualisation.NumZBins, barAggregation: DataVisualisation.BarAggregation
                                                                                        );
                }
                else
                {
                    facet = DataVisualisationManager.Instance.CloneDataVisualisation(DataVisualisation);
                }
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
            DOTween.To(() => facet.Scale, _ => facet.Scale = _, new Vector3(visWidth - 0.075f, visHeight - 0.075f, 0.0025f), animationTime).SetEase(Ease.InOutCubic);  // make it a bit smaller in order to have some gaps between them=
        }

        private void RemoveFacet(int index)
        {
            DataVisualisation facet = facetVisualisations[index];
            if (facet == null)
                return;

            Vector3 targetPos;
            if (crackingDimension == AxisDirection.X)
                targetPos = new Vector3(0, 0, barWidth * index - DataVisualisation.Width / 2);
            else
                targetPos = new Vector3(0, 0, barWidth * index - DataVisualisation.Depth / 2);
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
            var overlapping = Physics.OverlapBox(centre + forward * i * barWidth - offset, halfExtents, DataVisualisation.transform.rotation, LayerMask.GetMask("SceneWall"), QueryTriggerInteraction.Collide);
            return overlapping.Select(x => x.gameObject).Contains(nearestSurface);
        }

        private bool IsDimensionCategorical(string dimension)
        {
            var type = DataSource[dimension].MetaData.type;
            return (type == DataType.String || type == DataType.Date);
        }
    }
}
