using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;

namespace Experimental.SurfacePlacement
{
    public class PushButtonToPlace : PlaceableObject
    {
        [Tooltip("The radius which this object checks for nearby surfaces to snap to.")] [Range(0.1f, 10)]
        public float SurfaceSearchRadius = 3f;

        private GameObject nearestSurface;

        public override void ManipulationStarted(ManipulationEventData eventData)
        {
        }

        public override void ManipulationEnded(ManipulationEventData eventData)
        {
        }

        public void PlaceOnNearestSurface()
        {
            nearestSurface = FindNearestSurface();

            if (nearestSurface != null)
            {
                Vector3 TargetPosition = CalculatePositionOnSurface(nearestSurface);
                Quaternion TargetRotation = CalculateRotationOnSurface(nearestSurface);

                MoveToPositionAndRotation(TargetPosition, TargetRotation);
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


        private Vector3 FitPositionInsideSurfaceBounds(Vector3 position, Vector3 vertex)
        {
            Vector3 localPos = transform.InverseTransformPoint(position);
            Vector3 localVertex = transform.InverseTransformPoint(vertex);
            
            // Case 1: vertex is too far to the left
            if (localVertex.x <= -0.5f)
            {
                float delta = Mathf.Abs(-0.5f - localVertex.x);
                localPos.x += delta;
            }
            // Case 2: vertex is too far to the right
            else if (0.5f <= localVertex.x)
            {
                float delta = localVertex.x - 0.5f;
                localPos.x -= delta;
            }
            // Case 3: vertex is too far to the top
            if (0.5f <= localVertex.y)
            {
                float delta = localVertex.y - 0.5f;
                localPos.y -= delta;
            }
            // Case 4: vertex is too far to the bottom
            else if (localVertex.y <= -0.5f)
            {
                float delta = Mathf.Abs(-0.5f - localVertex.y);
                localPos.y += delta;
            }
            // Case 5: vertex is behind the screen
            if (0f <= localVertex.z)
            {
                float delta = localVertex.z;
                localPos.z -= delta;
            }

            Vector3 worldPos = transform.TransformPoint(localPos);
            return worldPos;
        }
    }
}

