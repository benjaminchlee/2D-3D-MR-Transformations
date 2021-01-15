using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using TMPro;
using UnityEngine;

namespace SSVis
{
    public enum RadialSegmentProperty
    {
        None,
        XDimension,
        YDimension,
        ZDimension,
        Colour,
        ColourDimension,
        Size,
        SizeDimension
    }

    [RequireComponent(typeof(NearInteractionGrabbable))]
    public class VisualisationRadialMenu : MonoBehaviour, IMixedRealityPointerHandler
    {
        public DataVisualisation DataVisualisation;

        public List<RadialSegmentProperty> RadialProperties = new List<RadialSegmentProperty>();
        public float PropertySelectionDistanceThreshold = 0.04f;

        private Vector3 startPos;
        private RadialSegmentProperty selectedProperty;
        private object selectedValue;
        private float propertyAngleSize;
        private bool isValuesSelectionOpen = false;

        private List<TextMeshPro> propertyTextLabels = new List<TextMeshPro>();
        private List<TextMeshPro> valueTextLabels = new List<TextMeshPro>();

        private GameObject radialMenuHolder;

        private void Start()
        {
            propertyAngleSize = 360f / RadialProperties.Count;
            radialMenuHolder = new GameObject("Radial Menu");
        }

        public void OnPointerClicked(MixedRealityPointerEventData eventData)
        {
        }

        public void OnPointerDown(MixedRealityPointerEventData eventData)
        {
            if (eventData.Handedness != Handedness.None)
                startPos = HandInputManager.Instance.GetJointTransform(eventData.Handedness, TrackedHandJoint.IndexTip).position;
        }

        public void OnPointerDragged(MixedRealityPointerEventData eventData)
        {
            if (eventData.Handedness == Handedness.None)
                return;

            Vector3 currentPos = HandInputManager.Instance.GetJointTransform(eventData.Handedness, TrackedHandJoint.IndexTip).position;
            float distance = Vector3.Distance(currentPos, startPos);

            // Determine angle by converting to screen space
            Vector3 startScreenPos = Camera.main.WorldToScreenPoint(startPos, Camera.MonoOrStereoscopicEye.Mono);
            Vector3 currentScreenPos = Camera.main.WorldToScreenPoint(currentPos, Camera.MonoOrStereoscopicEye.Mono);

            float angle = Vector3.Angle(Vector3.up, (currentScreenPos - startScreenPos));
            // If the angle is meant to be an obtuse angle (going clockwise), fix it
            if (currentScreenPos.x < startScreenPos.x)
                angle = 360 - angle;

            // Position and rotate radial menu
            radialMenuHolder.transform.position = Camera.main.ScreenToWorldPoint(new Vector3(startScreenPos.x, startScreenPos.y, 0.45f));
            radialMenuHolder.transform.rotation = Quaternion.LookRotation(radialMenuHolder.transform.position - Camera.main.transform.position);


            // Determine which segment is currently being hovered over
            int propertyIdx = Mathf.FloorToInt(angle / propertyAngleSize);
            selectedProperty = RadialProperties[propertyIdx];

            DrawPropertyTextLabels(propertyIdx);

            // Only show the value options when the hovered distance is far enough from the start position
            if (PropertySelectionDistanceThreshold <= distance)
            {
                object[] values = GetPropertyValues(selectedProperty);
                int numValues = values.Length;
                float valueAngleSize = propertyAngleSize / numValues;
                int valueIdx = Mathf.FloorToInt((angle - (propertyIdx * propertyAngleSize)) / valueAngleSize);
                selectedValue = values[valueIdx];

                DrawValueTextLabels(values, propertyIdx, valueIdx);
            }
            else
            {
                selectedValue = null;

                HideValueTextLabels();
            }
        }

        public void OnPointerUp(MixedRealityPointerEventData eventData)
        {
            if (selectedProperty != RadialSegmentProperty.None && selectedValue != null)
            {
                if (DataVisualisation != null)
                    SetDataVisualisationPropertyAndValue(selectedProperty, selectedValue);
            }

            selectedProperty = RadialSegmentProperty.None;
            selectedValue = null;

            HidePropertyTextLabels();
            HideValueTextLabels();
        }

        private void SetDataVisualisationPropertyAndValue(RadialSegmentProperty property, object value)
        {
            switch (property)
            {
                case RadialSegmentProperty.XDimension:
                    DataVisualisation.XDimension = value.ToString();
                    break;

                case RadialSegmentProperty.YDimension:
                    DataVisualisation.YDimension = value.ToString();
                    break;

                case RadialSegmentProperty.ZDimension:
                    DataVisualisation.ZDimension = value.ToString();
                    break;

                case RadialSegmentProperty.ColourDimension:
                    DataVisualisation.ColourByDimension = value.ToString();
                    break;

                case RadialSegmentProperty.SizeDimension:
                    DataVisualisation.SizeByDimension = value.ToString();
                    break;

                case RadialSegmentProperty.Colour:
                    Color color;
                    if (value.ToString() == "Red") color = Color.red;
                    else if (value.ToString() == "Yellow") color = Color.yellow;
                    else if (value.ToString() == "Green") color = Color.green;
                    else if (value.ToString() == "Blue") color = Color.blue;
                    else if (value.ToString() == "Magenta") color = Color.magenta;
                    else if (value.ToString() == "Grey") color = Color.grey;
                    else if (value.ToString() == "Black") color = Color.black;
                    else if (value.ToString() == "White") color = Color.white;
                    else color = Color.white;
                    DataVisualisation.Colour = color;
                    break;

                case RadialSegmentProperty.Size:
                    DataVisualisation.Size = (float)value;
                    break;

                default:
                    break;
            }
        }

        private void DrawPropertyTextLabels(int selectedIndex = -1)
        {
            if (propertyTextLabels.Count < RadialProperties.Count)
            {
                int labelsToAdd = RadialProperties.Count - propertyTextLabels.Count;
                for (int i = 0; i < labelsToAdd; i++)
                {
                    TextMeshPro t = new GameObject("Property Label").AddComponent<TextMeshPro>();
                    t.GetComponent<RectTransform>().sizeDelta = new Vector2(0.175f, 0.05f);
                    t.autoSizeTextContainer = false;
                    t.fontSize = 0.1f;
                    t.alignment = TextAlignmentOptions.Midline;
                    t.transform.SetParent(radialMenuHolder.transform);
                    t.transform.localRotation = Quaternion.identity;
                    propertyTextLabels.Add(t);
                }
            }

            for (int i = 0; i < RadialProperties.Count; i++)
            {
                TextMeshPro textMesh = propertyTextLabels[i];
                textMesh.enabled = true;
                textMesh.text = RadialProperties[i].ToString();
                if (selectedIndex == i)
                    textMesh.fontStyle = FontStyles.Bold;
                else
                    textMesh.fontStyle = FontStyles.Normal;

                Vector2 pos = CalculatePositionOnCircle((i + 0.5f) * propertyAngleSize, PropertySelectionDistanceThreshold);
                textMesh.transform.localPosition = new Vector3(pos.x, pos.y, 0);
            }
        }

        private void DrawValueTextLabels(object[] values, int propertyIndex, int selectedIndex = -1)
        {
            if (valueTextLabels.Count < values.Length)
            {
                int labelsToAdd = values.Length - valueTextLabels.Count;
                for (int i = 0; i < labelsToAdd; i++)
                {
                    TextMeshPro t = new GameObject("Value Label").AddComponent<TextMeshPro>();
                    t.GetComponent<RectTransform>().sizeDelta = new Vector2(0.175f, 0.05f);
                    t.autoSizeTextContainer = false;
                    t.alignment = TextAlignmentOptions.Midline;
                    t.transform.SetParent(radialMenuHolder.transform);
                    t.transform.localRotation = Quaternion.identity;
                    valueTextLabels.Add(t);
                }
            }
            else if (valueTextLabels.Count > values.Length)
            {
                int labelsToRemove = valueTextLabels.Count - values.Length;
                for (int i = 0; i < labelsToRemove; i++)
                {
                    TextMeshPro t = valueTextLabels[0];
                    valueTextLabels.RemoveAt(0);
                    Destroy(t.gameObject);
                }
            }

            float valueAngleSize = propertyAngleSize / values.Length;

            for (int i = 0; i < values.Length; i++)
            {
                TextMeshPro textMesh = valueTextLabels[i];
                textMesh.enabled = true;
                textMesh.text = values[i].ToString();
                if (selectedIndex == i)
                {
                    textMesh.fontSize = 0.1f;
                    textMesh.fontStyle = FontStyles.Bold;
                }
                else
                {
                    textMesh.fontSize = 0.04f;
                    textMesh.fontStyle = FontStyles.Normal;
                }

                Vector2 pos = CalculatePositionOnCircle((propertyIndex * propertyAngleSize) + (i * valueAngleSize), PropertySelectionDistanceThreshold + 0.06f);
                textMesh.transform.localPosition = new Vector3(pos.x, pos.y, 0);
            }
        }

        private void HidePropertyTextLabels()
        {
            for (int i = 0; i < propertyTextLabels.Count; i++)
            {
                TextMeshPro textMesh = propertyTextLabels[i];
                Destroy(textMesh.gameObject);
            }
            propertyTextLabels.Clear();
        }

        private void HideValueTextLabels()
        {
            for (int i = 0; i < valueTextLabels.Count; i++)
            {
                TextMeshPro textMesh = valueTextLabels[i];
                Destroy(textMesh.gameObject);
            }
            valueTextLabels.Clear();
        }

        private Vector2 CalculatePositionOnCircle(float angle, float radius)
        {
            angle += 270;
            if (360 < angle)
                angle -= 360;
            angle = 360 - angle;
            angle = Mathf.Deg2Rad * angle;

            float x = radius * Mathf.Cos(angle);
            float y = radius * Mathf.Sin(angle);
            return new Vector2(x, y);
        }

        private int GetNumPropertyValues(RadialSegmentProperty property)
        {
            if (property == RadialSegmentProperty.None)
                return 0;

            return GetPropertyValues(property).Length;
        }

        private object[] GetPropertyValues(RadialSegmentProperty property)
        {
            switch (property)
            {
                case RadialSegmentProperty.XDimension:
                case RadialSegmentProperty.YDimension:
                case RadialSegmentProperty.ZDimension:
                case RadialSegmentProperty.ColourDimension:
                case RadialSegmentProperty.SizeDimension:
                    var dataSource = DataVisualisationManager.Instance.DataSource;
                    string[] dimensions = new string[dataSource.DimensionCount + 1];
                    dimensions[0] = "Undefined";
                    for (int i = 0; i < dataSource.DimensionCount; i++)
                    {
                        dimensions[i + 1] = dataSource[i].Identifier;
                    }
                    return dimensions;

                case RadialSegmentProperty.Colour:
                    string[] colours = new string[]
                    {
                        "Red",
                        "Yellow",
                        "Green",
                        "Blue",
                        "Magenta",
                        "Grey",
                        "Black",
                        "White"
                    };
                    return colours;

                case RadialSegmentProperty.Size:
                    object[] sizes = new object[]
                    {
                        0.025f,
                        0.05f,
                        0.075f,
                        0.1f,
                        0.125f,
                        0.15f,
                        0.175f,
                        0.2f
                    };
                    return sizes;

                default:
                    return null;
            }
        }

        private void OnDisable()
        {
            HidePropertyTextLabels();
            HideValueTextLabels();
        }

        private void OnDestroy()
        {
            HidePropertyTextLabels();
            HideValueTextLabels();
        }
    }
}
