using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Experimental.CrossDimensionalTransfer
{
    public class InteractionButtonsSBV : MonoBehaviour
    {
        public PushButtonToSnapPlaceableObject interactionButtons;
        
        private Vector3 originalPosition;
        private Quaternion originalRotation;
        
        private void Start()
        {
        }
        
        public void MoveInteractionButtonsToSurface()
        {
            originalPosition = interactionButtons.transform.localPosition;
            originalRotation = interactionButtons.transform.localRotation;
            
            interactionButtons.PlaceOnNearestSurface();
        }
        
        public void ResetInteractionButtons()
        {
            interactionButtons.transform.DOLocalMove(originalPosition, 0.5f).SetEase(Ease.OutQuint);
            interactionButtons.transform.DOLocalRotate(originalRotation.eulerAngles, 0.5f).SetEase(Ease.OutQuint);
        }
    }
}