using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.Input;
using UnityEngine;
using TMPro;

namespace Experimental.CrossDimensionalTransfer
{
    public class TouchHighlighter : MonoBehaviour
    {
        [SerializeField]
        private NearInteractionTouchableVolume nearInteractionTouchableVolume;
        [SerializeField]
        private TouchHandler touchHandler;
        
        public Color UntouchedColor = new Color(1, 1, 1, 0);
        public Color TouchedColor = new Color(1, 1, 1, 1);
        public float TransitionTime = 0.2f;
        public string HighlightText;
        
        private void Start()
        {
            if (nearInteractionTouchableVolume == null)
            {
                if (gameObject.GetComponent<NearInteractionTouchableVolume>() == null)
                    gameObject.AddComponent<NearInteractionTouchableVolume>();
            }
            if (touchHandler == null)
            {
                if (gameObject.GetComponent<TouchHandler>() != null)
                    touchHandler = gameObject.GetComponent<TouchHandler>();
                else
                    touchHandler = gameObject.AddComponent<TouchHandler>();
            }
            
            Material material = GetComponent<Renderer>().material;
            
            GameObject textHolder = new GameObject();
            textHolder.transform.SetParent(transform);
            TextMeshPro textMesh = textHolder.AddComponent<TextMeshPro>();
            textMesh.GetComponent<RectTransform>().sizeDelta = new Vector2(0.1f, 0.05f);
            textMesh.fontSize = 0.1f;
            textMesh.alignment = TextAlignmentOptions.Midline;
            textMesh.text = HighlightText;
            textMesh.enabled = false;
            
            // touchHandler.OnTouchStarted.AddListener((e) => textMesh.enabled = true);
                        
            touchHandler.OnTouchUpdated.AddListener((e) => {
                material.DOColor(TouchedColor, TransitionTime);
                textMesh.enabled = true;
                textHolder.transform.position = e.InputData + Camera.main.transform.up * 0.05f + Camera.main.transform.right * 0.05f;
                textHolder.transform.rotation = Quaternion.LookRotation(textHolder.transform.position - Camera.main.transform.position);
            });
            
            touchHandler.OnTouchCompleted.AddListener((e) => {
                material.DOColor(UntouchedColor, TransitionTime);
                textMesh.enabled = false;
            });
        }
    }
}
