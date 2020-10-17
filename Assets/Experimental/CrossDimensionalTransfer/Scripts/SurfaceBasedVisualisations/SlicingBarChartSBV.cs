using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace Experimental.CrossDimensionalTransfer
{
    public class SlicingBarChartSBV : MonoBehaviour
    {
        public GameObject[] BarGroups;
        public GameObject[] Bars;
        public TextMeshPro BarGroupHighlightText;
        
        private BoxCollider boxCollider;
        private float boxColliderDepthSize;
        private int numBarGroups;
        private float barGroupDepthSize;
        private bool isSlicing = false;
        private int groupCount;
        private GameObject slicingSurface;
        private int highlightedBarGroup = -1;

        private void Start()
        {
            boxCollider = GetComponent<BoxCollider>();
            boxColliderDepthSize = boxCollider.size.z;
            numBarGroups = BarGroups.Length;
            barGroupDepthSize = boxColliderDepthSize / numBarGroups;
            
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
        }

        private void Update()
        {
            if (isSlicing)
            {
                float depth = CalculateDepthOnSurface();
                depth = depth + boxColliderDepthSize / 2f;
                int idx = numBarGroups - Mathf.FloorToInt(depth / barGroupDepthSize) - 1;
                
                SetHighlightedGroup(idx);
            }
        }
        
        private float CalculateDepthOnSurface()
        {
            if (slicingSurface == null)
                return 0;
                        
            var worldToLocalMatrix = Matrix4x4.TRS(slicingSurface.transform.position, slicingSurface.transform.rotation, Vector3.one).inverse;
            return worldToLocalMatrix.MultiplyPoint3x4(transform.position).z;
        }
        
        private void SetHighlightedGroup(int index)
        {
            if (index < 0 || index > numBarGroups - 1)
            {
                highlightedBarGroup = -1;
                BarGroupHighlightText.text = "";
            }
            
            else if (highlightedBarGroup != index)
            {
                Debug.Log(index);
                highlightedBarGroup = index;
                
                StringBuilder sb = new StringBuilder();
                sb.Append(string.Format("Bar group {0} selected\n", index));
                
                int i = 0;
                foreach (Transform bar in BarGroups[index].transform)
                {
                    sb.Append(string.Format("Bar #{0}: {1}\n", i, bar.localScale.y));
                    i++;
                }
                
                BarGroupHighlightText.text = sb.ToString();
            }
        }
                
        public void StartSlicing(PlacedObjectEventData eventData)
        {
            isSlicing = true;
            slicingSurface = eventData.Surface;
        }
        
        
        public void EndSlicing(PlacedObjectEventData eventData)
        {
            isSlicing = false;
            slicingSurface = null;
        }
    }
}
