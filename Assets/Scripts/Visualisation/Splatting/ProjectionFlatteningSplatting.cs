using IATK;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SSVis
{
    public class ProjectionFlatteningSplatting : MonoBehaviour
    {
        private DataSource DataSource;
        private DataVisualisation DataVisualisation;
        private Visualisation Visualisation;

        public void Initialise(DataSource dataSource, DataVisualisation dataVisualisation, Visualisation visualisation)
        {
            this.DataSource = dataSource;
            this.DataVisualisation = dataVisualisation;
            this.Visualisation = visualisation;
        }

        public void ApplySplat()
        {
            if (DataVisualisation.XDimension == "Undefined" || DataVisualisation.YDimension == "Undefined" || DataVisualisation.ZDimension == "Undefined")
            {
                Debug.LogError("Projection flattening requires a 3 dimensional visualisation.");
                return;
            }

            // Get positions of all the data points in local space
            Vector3[] positions = Visualisation.theVisualizationObject.viewList[0].GetPositions();

            // Convert local space into world space, then into viewport space
            Camera camera = Camera.main;
            for (int i = 0; i < DataSource.DataCount; i++)
            {
                Vector3 pos = Visualisation.theVisualizationObject.viewList[0].transform.TransformPoint(positions[i]);
                pos = camera.WorldToViewportPoint(pos, Camera.MonoOrStereoscopicEye.Mono);
                positions[i] = pos;
            }

            // Determine the four corners of the Data Visualisation by calculating the bounding box of the 8 corners of the cube in viewport space
            Vector3[] corners = GetColliderVertexPositions(DataVisualisation.GetComponent<BoxCollider>());
            for (int i = 0; i < corners.Length; i++)
            {
                corners[i] = camera.WorldToViewportPoint(corners[i]);
            }

            float minX = corners.Min(c => c.x);
            float maxX = corners.Max(c => c.x);
            float minY = corners.Min(c => c.y);
            float maxY = corners.Max(c => c.y);
            float avgZ = corners.Average(c => c.z);

            // Normalise data point positions based on these corners
            float[] xPositions = new float[DataSource.DataCount];
            float[] yPositions = new float[DataSource.DataCount];
            for (int i = 0; i < DataSource.DataCount; i++)
            {
                Vector3 pos = positions[i];
                xPositions[i] = NormaliseValue(pos.x, minX, maxX);
                yPositions[i] = NormaliseValue(pos.y, minY, maxY);
            }

            // Update the points on the Data Visualisation to these positions
            DataVisualisation.ZDimension = "Undefined";
            Visualisation.theVisualizationObject.viewList[0].UpdateXPositions(xPositions);
            Visualisation.theVisualizationObject.viewList[0].UpdateYPositions(yPositions);
            Visualisation.theVisualizationObject.viewList[0].ZeroPosition(2);
            // Face towards the camera
            DataVisualisation.transform.rotation = Quaternion.LookRotation(DataVisualisation.transform.position - Camera.main.transform.position);
            // Rescale based on bounding box corners
            Vector3 bl = camera.ViewportToWorldPoint(new Vector3(minX, minY, avgZ));
            Vector3 tl = camera.ViewportToWorldPoint(new Vector3(minX, maxY, avgZ));
            Vector3 tr = camera.ViewportToWorldPoint(new Vector3(maxX, maxY, avgZ));
            DataVisualisation.Width = Vector3.Distance(tl, tr);
            DataVisualisation.Height = Vector3.Distance(tl, bl);
        }

        private Vector3[] GetColliderVertexPositions (BoxCollider b)
        {
            var vertices = new Vector3[8];

            vertices[0] = b.transform.TransformPoint(b.center + new Vector3(-b.size.x, -b.size.y, -b.size.z) * 0.5f);
            vertices[1] = b.transform.TransformPoint(b.center + new Vector3(b.size.x, -b.size.y, -b.size.z) * 0.5f);
            vertices[2] = b.transform.TransformPoint(b.center + new Vector3(b.size.x, -b.size.y, b.size.z) * 0.5f);
            vertices[3] = b.transform.TransformPoint(b.center + new Vector3(-b.size.x, -b.size.y, b.size.z) * 0.5f);
            vertices[4] = b.transform.TransformPoint(b.center + new Vector3(-b.size.x, b.size.y, -b.size.z) * 0.5f);
            vertices[5] = b.transform.TransformPoint(b.center + new Vector3(b.size.x, b.size.y, -b.size.z) * 0.5f);
            vertices[6] = b.transform.TransformPoint(b.center + new Vector3(b.size.x, b.size.y, b.size.z) * 0.5f);
            vertices[7] = b.transform.TransformPoint(b.center + new Vector3(-b.size.x, b.size.y, b.size.z) * 0.5f);

            return vertices;
        }

        private float NormaliseValue(float value, float i0, float i1, float j0 = 0, float j1 = 1)
        {
            float L = (j0 - j1) / (i0 - i1);
            return (j0 - (L * i0) + (L * value));
        }
    }
}
