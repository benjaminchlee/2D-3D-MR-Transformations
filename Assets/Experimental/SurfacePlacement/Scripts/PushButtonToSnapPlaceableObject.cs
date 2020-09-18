using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;

namespace Experimental.SurfacePlacement
{
    /// <summary>
    /// Places this gameobject on the nearest compatible surface when a button is pushed
    /// </summary>
    public class PushButtonToSnapPlaceableObject: PlaceableObject
    {
        [Tooltip("The radius which this object checks for nearby surfaces to snap to.")] [Range(0.1f, 10)]
        public float SurfaceSearchRadius = 3f;

        private GameObject nearestSurface;

        public override void ManipulationStarted(ManipulationEventData eventData)
        {
            base.ManipulationEnded(eventData);

            if (isPlacedOnSurface)
            {
                ObjectLiftedFromSurface(nearestSurface);
            }
        }

        public void PlaceOnNearestSurface()
        {
            nearestSurface = FindNearestSurface();

            if (nearestSurface != null)
            {
                BeforeObjectPlacedOnSurface(nearestSurface);

                Vector3 TargetPosition = CalculatePositionOnSurface(nearestSurface);
                Quaternion TargetRotation = CalculateRotationOnSurface(nearestSurface);

                MoveToPositionAndRotation(TargetPosition, TargetRotation);
                
                AfterObjectPlacedOnSurface(nearestSurface);
            }
        }

        private GameObject FindNearestSurface()
        {
            GameObject[] placeableSurfaces = GameObject.FindGameObjectsWithTag("PlaceableSurface");

            if (placeableSurfaces.Length == 0)
            {
                Debug.LogError("No surfaces in the scene exist.");
                return null;
            }

            GameObject nearest = null;
            float nearestDistance = Mathf.Infinity;

            foreach (var surface in placeableSurfaces)
            {
                Vector3 closestPoint = surface.GetComponent<Collider>().ClosestPoint(gameObject.transform.position);
                float distance = Vector3.Distance(closestPoint, gameObject.transform.position);

                if (distance < SurfaceSearchRadius && distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = surface;
                }
            }

            return nearest;
        }
    }
}

