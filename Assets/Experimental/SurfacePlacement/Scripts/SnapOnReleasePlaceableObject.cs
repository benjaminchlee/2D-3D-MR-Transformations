using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;

namespace Experimental.SurfacePlacement
{
    /// <summary>
    /// Places this gameobject on a compatible surface when manipulation is released
    /// </summary>
    public class SnapOnReleasePlaceableObject : PlaceableObject
    {
        private List<GameObject> allTouchedSurfaces = new List<GameObject>();
        private GameObject touchedSurface;

        public override void ManipulationStarted(ManipulationEventData eventData)
        {
            base.ManipulationStarted(eventData);

            if (isPlacedOnSurface)
            {
                ObjectLiftedFromSurface(touchedSurface);
            }
        }

        public override void ManipulationEnded(ManipulationEventData eventData)
        {
            base.ManipulationEnded(eventData);

            if (touchedSurface != null)
            {
                BeforeObjectPlacedOnSurface(touchedSurface);

                Vector3 TargetPosition = CalculatePositionOnSurface(touchedSurface);
                Quaternion TargetRotation = CalculateRotationOnSurface(touchedSurface);

                MoveToPositionAndRotation(TargetPosition, TargetRotation);

                AfterObjectPlacedOnSurface(touchedSurface);
            }
        }

        protected virtual void OnTriggerEnter(Collider collider)
        {
            if (CheckIsPlaceableSurface(collider.gameObject))
            {
                touchedSurface = collider.gameObject;

                // Keep a record of all surfaces that are currently touched in case of touching corners
                if (allTouchedSurfaces.Contains(touchedSurface))
                {
                    allTouchedSurfaces.Add(touchedSurface);
                }
            }
        }

        protected virtual void OnTriggerExit(Collider collider)
        {
            if (CheckIsPlaceableSurface(collider.gameObject))
            {
                if (allTouchedSurfaces.Contains(touchedSurface))
                {
                    allTouchedSurfaces.Remove(collider.gameObject);
                }

                // If there are still any other colliders being touched, update the reference to it
                touchedSurface = (allTouchedSurfaces.Count > 0) ? allTouchedSurfaces[0] : null;
            }
        }
    }
}
