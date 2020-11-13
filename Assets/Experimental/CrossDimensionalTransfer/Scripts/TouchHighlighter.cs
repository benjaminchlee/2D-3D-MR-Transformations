using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.Input;
using UnityEngine;
using TMPro;
using Microsoft.MixedReality.Toolkit.UI;

namespace Experimental.CrossDimensionalTransfer
{
    public class TouchHighlighter : MonoBehaviour
    {
        [SerializeField]
        private ObjectManipulator objectManipulator;
        [SerializeField]
        private NearInteractionTouchableVolume nearInteractionTouchableVolume;
        [SerializeField]
        private TouchHandler touchHandler;
        
        public Color UntouchedColor = new Color(1, 1, 1, 0);
        public Color TouchedColor = new Color(1, 1, 1, 1);
        public float TransitionTime = 0.2f;
        public string HighlightText;
        
        private bool isManipulating = false;
        private GameObject textHolder;
        private Transform pointer;
        
        private void Start()
        {
            if (objectManipulator == null)
            {
                if (gameObject.GetComponent<ObjectManipulator>() != null)
                    objectManipulator = gameObject.GetComponent<ObjectManipulator>();
                else
                    objectManipulator = gameObject.AddComponent<ObjectManipulator>();
            }
            
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
            
            textHolder = new GameObject();
            textHolder.transform.SetParent(transform);
            TextMeshPro textMesh = textHolder.AddComponent<TextMeshPro>();
            textMesh.GetComponent<RectTransform>().sizeDelta = new Vector2(0.1f, 0.05f);
            textMesh.fontSize = 0.1f;
            textMesh.alignment = TextAlignmentOptions.Midline;
            textMesh.text = HighlightText;
            textMesh.enabled = false;
            
            objectManipulator.OnManipulationStarted.AddListener((e) => {
                material.DOColor(TouchedColor, TransitionTime);
                textMesh.enabled = true;
                isManipulating = true;
                pointer = (e.Pointer as MonoBehaviour).transform;
            });
            
            objectManipulator.OnManipulationEnded.AddListener((e) =>
            {
                isManipulating = false;
                material.DOColor(UntouchedColor, TransitionTime);
                textMesh.enabled = false;
            });
            
            touchHandler.OnTouchUpdated.AddListener((e) => {
                material.DOColor(TouchedColor, TransitionTime);
                textMesh.enabled = true;
                SetTextPosition(e.InputData);
            });
            
            touchHandler.OnTouchCompleted.AddListener((e) => {
                if (!isManipulating)
                {
                    material.DOColor(UntouchedColor, TransitionTime);
                    textMesh.enabled = false;
                }
            });
        }
        
        private void Update()
        {
            if (isManipulating)
            {
                SetTextPosition(pointer.position);
            }
        }
        
        private void SetTextPosition(Vector3 pos)
        {
            textHolder.transform.position = pos + Camera.main.transform.up * 0.05f + Camera.main.transform.right * 0.05f;
            textHolder.transform.rotation = Quaternion.LookRotation(textHolder.transform.position - Camera.main.transform.position);
        }
    }
}
