using IATK;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Microsoft.MixedReality.Toolkit.Utilities;

namespace SSVis
{
    public class ProjectionFlatteningSplatting : BaseVisualisationSplatting
    {
        // Label variables
        private List<GameObject> axisLabels = new List<GameObject>();
        private float xAxisThetaX;
        private float xAxisThetaY;
        private float xAxisThetaZ;
        private float yAxisThetaX;
        private float yAxisThetaY;
        private float yAxisThetaZ;
        private float xAxisPercentage;
        private float yAxisPercentage;
        private float zAxisPercentage;

        /// <summary>
        /// Applies the splat. This will modify the referenced Data Visualisation directly.
        /// </summary>
        public override void ApplySplat(System.Tuple<Vector3, Vector3> placementValues = null)
        {
            if (!isInitialised)
            {
                Debug.LogError("Projection Flattening: Cannot apply the splat before Initialise() has been called.");
                return;
            }

            if (DataVisualisation.XDimension == "Undefined" || DataVisualisation.YDimension == "Undefined" || DataVisualisation.ZDimension == "Undefined" || DataVisualisation.VisualisationType != AbstractVisualisation.VisualisationTypes.SCATTERPLOT)
            {
                Debug.LogError("Projection Flattening: A 3 dimensional scatterplot is required.");
                return;
            }

            // Get positions of all the data points in local space
            Vector3[] positions = Visualisation.theVisualizationObject.viewList[0].GetPositions();

            // Convert local space into world space, then into viewport space
            Camera camera = CameraCache.Main;
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

            // Before we do any modifications to the Data Visualisation, we calculate the angles of rotation between what it is now, to what it will be when fully splatted, along the three base axes (x, y, z)
            CalculateAngles();
            // We also get how long each axis is, which effects how much that axis finally contributes to the final projection
            CalculateAxisPercentages();

            // Update the points on the Data Visualisation to these positions
            DataVisualisation.ZDimension = "Undefined";
            Visualisation.theVisualizationObject.viewList[0].UpdateXPositions(xPositions);
            Visualisation.theVisualizationObject.viewList[0].UpdateYPositions(yPositions);
            Visualisation.theVisualizationObject.viewList[0].ZeroPosition(2);
            // Face towards the camera
            DataVisualisation.transform.rotation = Quaternion.LookRotation(DataVisualisation.transform.position - camera.transform.position);
            // Rescale based on bounding box corners
            Vector3 bl = camera.ViewportToWorldPoint(new Vector3(minX, minY, avgZ));
            Vector3 tl = camera.ViewportToWorldPoint(new Vector3(minX, maxY, avgZ));
            Vector3 tr = camera.ViewportToWorldPoint(new Vector3(maxX, maxY, avgZ));
            DataVisualisation.Width = Vector3.Distance(tl, tr);
            DataVisualisation.Height = Vector3.Distance(tl, bl);

            // Set labels based on the angles we calculated before
            SetAxisLabels();
        }

        public override void DestroyThisSplat()
        {
            // Remove the axis labels
            foreach (var go in axisLabels)
            {
                Destroy(go);
            }

            // Force the Data Visualisation to reset the positions of the points
            DataVisualisation.XDimension = DataVisualisation.XDimension;
            DataVisualisation.YDimension = DataVisualisation.YDimension;
            DataVisualisation.ZDimension = DataVisualisation.ZDimension;

            // Destroy this script
            Destroy(this);
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

        private void CalculateAngles()
        {
            Vector3 cameraUp = Vector3.up;
            Vector3 cameraForward = Vector3.ProjectOnPlane(CameraCache.Main.transform.forward, cameraUp);
            Vector3 cameraBackward = -cameraForward;
            Vector3 cameraRight = Vector3.ProjectOnPlane(CameraCache.Main.transform.right, cameraUp);

            // Calculate the angles for each axis based on how much they contribute to the x or y dimension (i.e. the more an axis is parallel to the camera's plane)
            xAxisThetaX = Vector3.SignedAngle(cameraForward, Vector3.ProjectOnPlane(DataVisualisation.transform.right, cameraUp), cameraUp);
            xAxisThetaY = Vector3.SignedAngle(cameraForward, Vector3.ProjectOnPlane(DataVisualisation.transform.up, cameraUp), cameraUp);
            xAxisThetaZ = Vector3.SignedAngle(cameraForward, Vector3.ProjectOnPlane(DataVisualisation.transform.forward, cameraUp), cameraUp);

            yAxisThetaX = Vector3.SignedAngle(cameraBackward, Vector3.ProjectOnPlane(DataVisualisation.transform.right, cameraRight), cameraRight);
            yAxisThetaY = Vector3.SignedAngle(cameraBackward, Vector3.ProjectOnPlane(DataVisualisation.transform.up, cameraRight), cameraRight);
            yAxisThetaZ = Vector3.SignedAngle(cameraBackward, Vector3.ProjectOnPlane(DataVisualisation.transform.forward, cameraRight), cameraRight);

            // Convert to radians
            xAxisThetaX *= Mathf.Deg2Rad;
            xAxisThetaY *= Mathf.Deg2Rad;
            xAxisThetaZ *= Mathf.Deg2Rad;
            yAxisThetaX *= Mathf.Deg2Rad;
            yAxisThetaY *= Mathf.Deg2Rad;
            yAxisThetaZ *= Mathf.Deg2Rad;
        }

        private void CalculateAxisPercentages()
        {
            // Get the highest value between width/height/depth so that we can use this to scale the labels based on its axis length
            float longestAxis = Mathf.Max(new float[] { Mathf.Abs(DataVisualisation.Width), Mathf.Abs(DataVisualisation.Height), Mathf.Abs(DataVisualisation.Depth) });
            xAxisPercentage = Mathf.Abs(DataVisualisation.Width) / longestAxis;
            yAxisPercentage = Mathf.Abs(DataVisualisation.Height) / longestAxis;
            zAxisPercentage = Mathf.Abs(DataVisualisation.Depth) / longestAxis;
        }

        private void SetAxisLabels()
        {

            // Find the original axis labels which we can use to position our new labels
            var xAxisLabel = DataVisualisation.Visualisation.theVisualizationObject.X_AXIS.transform.Find("AttributeLabel").GetComponent<TextMeshPro>();
            var yAxisLabel = DataVisualisation.Visualisation.theVisualizationObject.Y_AXIS.transform.Find("AttributeLabel").GetComponent<TextMeshPro>();
            xAxisLabel.text = "";
            yAxisLabel.text = "";

            // Create the first label which will act as the "prefab" for the others
            GameObject x1 = new GameObject();
            TextMeshPro x1tm = x1.AddComponent<TextMeshPro>();
            x1tm.GetComponent<RectTransform>().sizeDelta = new Vector2(0.05f, 0.05f);
            x1tm.autoSizeTextContainer = false;
            x1tm.fontSize = 0.15f;
            x1tm.alignment = TextAlignmentOptions.Midline;
            LineRenderer x1lr = x1.AddComponent<LineRenderer>();
            x1lr.startWidth = 0.0025f;
            x1lr.endWidth = 0.0025f;
            x1lr.startColor = Color.red;
            x1lr.endColor = Color.red;
            x1lr.useWorldSpace = false;
            x1lr.generateLightingData = true;
            x1lr.material = (Material) Resources.Load("LineMaterial");
            x1lr.SetPositions(new Vector3[] { new Vector3(0, -0.01f, 0), Vector3.zero });

            // Set the x axis labels
            GameObject x2 = Instantiate(x1);
            GameObject x3 = Instantiate(x1);
            x1.transform.SetParent(xAxisLabel.transform);
            x2.transform.SetParent(xAxisLabel.transform);
            x3.transform.SetParent(xAxisLabel.transform);
            x1.transform.localPosition = new Vector3(0, -0.075f + -0.02f, 0);
            x2.transform.localPosition = new Vector3(0, -0.075f + -0.05f, 0);
            x3.transform.localPosition = new Vector3(0, -0.075f + -0.08f, 0);
            x1.transform.localRotation = Quaternion.identity;
            x2.transform.localRotation = Quaternion.identity;
            x3.transform.localRotation = Quaternion.identity;
            x1tm.text = "X'";
            x2.GetComponent<TextMeshPro>().text = "Y'";
            x3.GetComponent<TextMeshPro>().text = "Z'";
            x1lr.SetPosition(1, new Vector3(Mathf.Sin(xAxisThetaX) * 0.05f * xAxisPercentage, -0.01f, 0));
            x2.GetComponent<LineRenderer>().SetPosition(1, new Vector3(Mathf.Sin(xAxisThetaY) * 0.05f * yAxisPercentage, -0.01f, 0));
            x3.GetComponent<LineRenderer>().SetPosition(1, new Vector3(Mathf.Sin(xAxisThetaZ) * 0.05f * zAxisPercentage, -0.01f, 0));

            // Set the y axis labels
            GameObject y1 = Instantiate(x1);
            GameObject y2 = Instantiate(x1);
            GameObject y3 = Instantiate(x1);
            y1.transform.SetParent(yAxisLabel.transform);
            y2.transform.SetParent(yAxisLabel.transform);
            y3.transform.SetParent(yAxisLabel.transform);
            y1.transform.localPosition = new Vector3(0.03f, 0.075f, 0);
            y2.transform.localPosition = new Vector3(0f, 0.075f, 0);
            y3.transform.localPosition = new Vector3(-0.03f, 0.075f, 0);
            y1.transform.localEulerAngles = new Vector3(0, 0, -90);
            y2.transform.localEulerAngles = new Vector3(0, 0, -90);
            y3.transform.localEulerAngles = new Vector3(0, 0, -90);
            y1.GetComponent<TextMeshPro>().text = "X'";
            y2.GetComponent<TextMeshPro>().text = "Y'";
            y3.GetComponent<TextMeshPro>().text = "Z'";
            y1.GetComponent<LineRenderer>().SetPosition(1, new Vector3(Mathf.Sin(yAxisThetaX) * 0.05f * xAxisPercentage, -0.01f, 0));
            y2.GetComponent<LineRenderer>().SetPosition(1, new Vector3(Mathf.Sin(yAxisThetaY) * 0.05f * yAxisPercentage, -0.01f, 0));
            y3.GetComponent<LineRenderer>().SetPosition(1, new Vector3(Mathf.Sin(yAxisThetaZ) * 0.05f * zAxisPercentage, -0.01f, 0));

            // Store references to these labels
            axisLabels.Add(x1);
            axisLabels.Add(x2);
            axisLabels.Add(x3);
            axisLabels.Add(y1);
            axisLabels.Add(y2);
            axisLabels.Add(y3);
        }

        private float NormaliseValue(float value, float i0, float i1, float j0 = 0, float j1 = 1)
        {
            float L = (j0 - j1) / (i0 - i1);
            return (j0 - (L * i0) + (L * value));
        }
    }
}
