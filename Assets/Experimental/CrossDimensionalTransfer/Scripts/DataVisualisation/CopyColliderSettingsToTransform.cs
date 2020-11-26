using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Experimental.CrossDimensionalTransfer
{
    public class CopyColliderSettingsToTransform : MonoBehaviour
    {
        public BoxCollider SourceBoxCollider;
        public Transform TargetTransform;
        public bool CopyLocalScale = false;
        
        private void Update()
        {
            if (CopyLocalScale)
            {
                TargetTransform.localPosition = SourceBoxCollider.center;
                TargetTransform.localScale = SourceBoxCollider.size;
            }
            else
            {
                TargetTransform.localPosition = SourceBoxCollider.center;
                var parent = TargetTransform.parent;
                TargetTransform.parent = null;
                TargetTransform.localScale = SourceBoxCollider.size;
                TargetTransform.parent = parent;
            }
            
        }
    }    
}