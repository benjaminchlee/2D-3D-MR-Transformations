using DG.Tweening;
using Microsoft.MixedReality.Toolkit.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Experimental.SurfaceBasedVisualisations
{
    /// <summary>
    /// A prototype surface based visualisation which slides apart separate groups in a 3D bar chart.
    /// </summary>
    public class SlidingBarChartSBV : MonoBehaviour
    {
        public GameObject[] SlidingBarGroups;
        public GridObjectCollection GridObjectCollectionScript;
        public GameObject[] Bars;

        private Vector3[] slidingBarGroupOriginalPositions;
        private Vector3[] slidingBarGroupTargetPositions;
        private int groupCount;
        private BoxCollider boxCollider;
        private Vector3 originalColliderCenter;
        private Vector3 originalColliderSize;

        private void Start()
        {
            groupCount = SlidingBarGroups.Length;
            slidingBarGroupOriginalPositions = new Vector3[groupCount];
            slidingBarGroupTargetPositions = new Vector3[groupCount];

            for (int i = 0; i < SlidingBarGroups.Length; i++)
            {
                slidingBarGroupOriginalPositions[i] = SlidingBarGroups[i].transform.localPosition;
            }

            Random.InitState((int)(Time.timeSinceLevelLoad * 1000));
            foreach (var bar in Bars)
            {
                float height = Random.Range(0.03f, 0.15f);

                Vector3 position = bar.transform.localPosition;
                Vector3 scale = bar.transform.localScale;
                
                position.y = height / 2;
                scale.y = height;

                bar.transform.localPosition = position;
                bar.transform.localScale = scale;
            }

            boxCollider = GetComponent<BoxCollider>();
            originalColliderCenter = boxCollider.center;
            originalColliderSize = boxCollider.size;
        }

        public void ExpandGroups()
        {
            float Width = 0.6f;
            float Height = 0.35f;

            int index = 0;
            int numRows = (int)Mathf.Sqrt(groupCount);
            int numCols = (int)Mathf.Ceil(groupCount / (float)numRows);

            // Get the distance from the edge to the centre-point (tentatively called etcp) of each subchart
            float xDelta = Width / (numCols * 2);
            float yDelta = Height / (numRows * 2);

            float w = Width / numCols;
            float h = Height / numRows;

            for (int i = 0; i < numRows; i++)
            {
                for (int j = 0; j < numCols; j++)
                {
                    if (index < groupCount)
                    {
                        float x = (-(Width / 2) + xDelta) + 2 * xDelta * j;
                        float y = ((Height / 2) - yDelta) - 2 * yDelta * i;
                        float z = 0;
                        
                        slidingBarGroupTargetPositions[index] = new Vector3(x, y, z);
                    }
                    index++;
                }
            }

            FitColliderToChildren();
            
            for (int i = 0; i < SlidingBarGroups.Length; i++)
            {
                SlidingBarGroups[i].transform.DOLocalMove(slidingBarGroupTargetPositions[i], 0.5f).SetEase(Ease.OutQuint);
            }
        }

        public void CloseGroups()
        {
            for (int i = 0; i < SlidingBarGroups.Length; i++)
            {
                SlidingBarGroups[i].transform.DOLocalMove(slidingBarGroupOriginalPositions[i], 0.5f).SetEase(Ease.OutQuint);
            }

            boxCollider.center = originalColliderCenter;
            boxCollider.size = originalColliderSize;
        }

        private void FitColliderToChildren()
        {
            for (int i = 0; i < SlidingBarGroups.Length; i++)
            {
                SlidingBarGroups[i].transform.localPosition = slidingBarGroupTargetPositions[i];
            }
            
            bool hasBounds = false;
            Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);

            foreach (GameObject group in SlidingBarGroups)
            {
                foreach (Transform child in group.transform)
                {
                    Renderer childRenderer = child.GetComponent<Renderer>();

                    if (childRenderer != null)
                    {
                        if (hasBounds) {
                            bounds.Encapsulate(childRenderer.bounds);
                        }
                        else {
                            bounds = childRenderer.bounds;
                            hasBounds = true;
                        }
                    }
                }                
            }

            boxCollider.center = bounds.center - transform.position;
            boxCollider.size = bounds.size;
            
            for (int i = 0; i < SlidingBarGroups.Length; i++)
            {
                SlidingBarGroups[i].transform.localPosition = slidingBarGroupOriginalPositions[i];
            }
        }
    }
}

