using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Experimental.SurfacePlacement
{
    /// <summary>
    /// Places this gameobject on a compatible surface based on proximity during manipulation
    /// </summary>
    public class SnapOnProximityPlaceableObject : PlaceableObject
    {
        [Tooltip("The radius which this object checks for nearby surfaces to snap to when being manipulated.")] [Range(0, 2)]
        public float SurfaceSearchRadius = 0.05f;

        private GameObject nearestSurface;
        
        private void Update()
        {
            if (isBeingManipulated)
            {
                PlaceOnNearestSurface();
            }
        }

        public void PlaceOnNearestSurface()
        {
            nearestSurface = FindNearestSurface();

            if (nearestSurface != null)
            {
                Vector3 TargetPosition = CalculatePositionOnSurface(nearestSurface);
                Quaternion TargetRotation = CalculateRotationOnSurface(nearestSurface);

                MoveToPositionAndRotation(TargetPosition, TargetRotation);
                SetPlacedOnSurface(nearestSurface);
            }
            else
            {
                SetLiftedFromSurface(nearestSurface);
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
                Vector3 closestPoint = surface.GetComponent<Collider>().ClosestPoint(manipulationPointer.Position);
                float distance = Vector3.Distance(closestPoint, manipulationPointer.Position);

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