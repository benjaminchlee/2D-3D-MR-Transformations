using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SSVis
{
    public class VolumeRenderingSurfaceSlicer : MonoBehaviour
    {
        public VolumeRendering.VolumeRendering VolumeRendering;

        private GameObject nearestSurface;

        private void Update()
        {
            Collider[] overlapping = Physics.OverlapBox(transform.position, transform.localScale / 2, transform.rotation);

            if (overlapping.Length > 0)
            {
                // Get the largest sized collider that's tagged as a SceneWall
                var sceneWalls = overlapping.Where(x => x.gameObject.tag == "SceneWall");
                if (sceneWalls.Count() > 0)
                {
                    nearestSurface = sceneWalls.OrderByDescending(x => x.transform.localScale.x * x.transform.localScale.y).First().gameObject;

                    UpdateSliceRanges();
                }
                else
                {
                    ResetSliceRanges();
                }
            }
            else
            {
                ResetSliceRanges();
            }
        }

        private void UpdateSliceRanges()
        {
            // Get local positions of all 8 corners
            Vector3 size = Vector3.one;
            Vector3[] verts = new Vector3[8];
            verts[0] = nearestSurface.transform.InverseTransformPoint(transform.TransformPoint(new Vector3(size.x, size.y, size.z) * 0.5f));
            verts[1] = nearestSurface.transform.InverseTransformPoint(transform.TransformPoint(new Vector3(size.x, size.y, -size.z) * 0.5f));
            verts[2] = nearestSurface.transform.InverseTransformPoint(transform.TransformPoint(new Vector3(size.x, -size.y, size.z) * 0.5f));
            verts[3] = nearestSurface.transform.InverseTransformPoint(transform.TransformPoint(new Vector3(size.x, -size.y, -size.z) * 0.5f));
            verts[4] = nearestSurface.transform.InverseTransformPoint(transform.TransformPoint(new Vector3(-size.x, size.y, size.z) * 0.5f));
            verts[5] = nearestSurface.transform.InverseTransformPoint(transform.TransformPoint(new Vector3(-size.x, size.y, -size.z) * 0.5f));
            verts[6] = nearestSurface.transform.InverseTransformPoint(transform.TransformPoint(new Vector3(-size.x, -size.y, size.z) * 0.5f));
            verts[7] = nearestSurface.transform.InverseTransformPoint(transform.TransformPoint(new Vector3(-size.x, -size.y, -size.z) * 0.5f));

            // Get the axis aligned min and max size
            float min = verts.Min(x => x.z);
            float max = verts.Max(x => x.z);

            float minSlice = NormaliseValue(0, min, max, 0, 1) - 0.025f;
            float maxSlice = minSlice + 0.05f;

            VolumeRendering.sliceZMin = minSlice;
            VolumeRendering.sliceZMax = maxSlice;
            VolumeRendering.SlicingPlane = nearestSurface.transform;
        }

        private void ResetSliceRanges()
        {
            VolumeRendering.sliceZMin = 0;
            VolumeRendering.sliceZMax = 1;
            VolumeRendering.SlicingPlane = null;
        }

        private float NormaliseValue(float value, float minVal, float maxVal, float minRange, float maxRange)
        {
            return (((value - minVal) / (maxVal - minVal)) *
                    (maxRange - minRange) + minRange);
        }
    }
}
