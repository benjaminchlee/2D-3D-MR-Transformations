using DG.Tweening;
using IATK;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.Input;

namespace Experimental.CrossDimensionalTransfer
{
    public class AxisDimensionSlider : MonoBehaviour, IMixedRealityPointerHandler
    {
        public TextMeshPro MainTextLabel;
        public int NumDimensionsOnSide = 3;
        public float LabelWidth = 0.025f;

        private DataVisualisation parentDataVisualisation;
        private DataSource dataSource;
        private List<string> dimensions;
        private Axis axisScript;
        private AbstractVisualisation.PropertyType axisDimension;
        private TextMeshPro[] textLabels;
        private int selectedDimensionIdx;

        private bool isSliding = false;
        private Vector3 slidingStartPosition;
        private int slideStartDimensionIdx;
        private Vector3 labelBasePosition;
        private float originalFontSize;

        private void Start()
        {
            // Get Data Visualisation script
            parentDataVisualisation = GetComponentInParent<DataVisualisation>();
            if (parentDataVisualisation == null)
            {
                Destroy(gameObject);
                return;
            }
            // Get DataSource and create list of dimensions
            dataSource = parentDataVisualisation.DataSource;
            dimensions = new List<string>();
            for (int i = 0; i < dataSource.DimensionCount; ++i)
                dimensions.Add(dataSource[i].Identifier);

            // Determine the direction this axis represents
            axisScript = GetComponentInParent<Axis>();
            if (axisScript.AxisDirection == 1)
                axisDimension = AbstractVisualisation.PropertyType.X;
            else if (axisScript.AxisDirection == 2)
                axisDimension = AbstractVisualisation.PropertyType.Y;
            else
                axisDimension = AbstractVisualisation.PropertyType.Z;

            // Create the labels that will be used, including the actual main label (this is placed in the middle)
            textLabels = new TextMeshPro[NumDimensionsOnSide * 2 + 1];
            for (int i = 0; i < NumDimensionsOnSide * 2 + 1; i++)
            {
                if (i == (NumDimensionsOnSide))
                {
                    textLabels[i] = MainTextLabel;
                }
                else
                {
                    GameObject go = new GameObject("AxisSliderLabel");
                    go.transform.SetParent(transform.parent);
                    TextMeshPro label = go.AddComponent<TextMeshPro>();
                    label.transform.localPosition = MainTextLabel.transform.localPosition;
                    label.transform.localRotation = MainTextLabel.transform.localRotation;
                    label.autoSizeTextContainer = false;
                    label.fontSize = MainTextLabel.fontSize;
                    label.alignment = MainTextLabel.alignment;
                    label.GetComponent<RectTransform>().pivot = MainTextLabel.rectTransform.pivot;
                    label.GetComponent<RectTransform>().sizeDelta = MainTextLabel.rectTransform.sizeDelta;
                    TextContainer container = go.AddComponent<TextContainer>();
                    container.anchorPosition = MainTextLabel.GetComponent<TextContainer>().anchorPosition;
                    container.margins = MainTextLabel.GetComponent<TextContainer>().margins;
                    container.size = MainTextLabel.GetComponent<TextContainer>().size;
                    container.pivot = MainTextLabel.GetComponent<TextContainer>().pivot;

                    textLabels[i] = label;
                }
            }

            labelBasePosition = MainTextLabel.transform.localPosition;
            originalFontSize = MainTextLabel.fontSize;

            selectedDimensionIdx = dimensions.IndexOf(GetCurrentDimension());
            SetLabelNames(selectedDimensionIdx);
            SetLabelPositions(0);
            HideLabels();
        }

        public void OnPointerDown(MixedRealityPointerEventData eventData)
        {
            isSliding = true;
            slidingStartPosition = transform.parent.InverseTransformPoint(eventData.Pointer.Position);
            slideStartDimensionIdx = selectedDimensionIdx;

            ShowLabels();
        }

        public void OnPointerDragged(MixedRealityPointerEventData eventData)
        {
            if (isSliding)
            {
                Vector3 thisPos = transform.parent.InverseTransformPoint(eventData.Pointer.Position);
                float distance = thisPos.x - slidingStartPosition.x;
                int dimensionOffset = Mathf.FloorToInt(Mathf.Abs(distance) / LabelWidth);
                if (distance < 0) dimensionOffset *= -1;
                int newDimensionIdx = (slideStartDimensionIdx - dimensionOffset) % dimensions.Count;
                if (newDimensionIdx < 0) newDimensionIdx += dimensions.Count;
                // If the selected dimension has changed
                if (newDimensionIdx != selectedDimensionIdx)
                {
                    SetLabelNames(newDimensionIdx);
                }

                // Position the labels
                float offset = distance % LabelWidth;
                SetLabelPositions(offset);
            }
        }

        public void OnPointerUp(MixedRealityPointerEventData eventData)
        {
            if (isSliding)
            {
                SetLabelPositions(0);
                HideLabels();
            }

            isSliding = false;
        }

        public void OnPointerClicked(MixedRealityPointerEventData eventData) {}

        private void SetLabelPositions(float offset)
        {
            int labelIdx = 0;
            float posRange = NumDimensionsOnSide * LabelWidth + LabelWidth;

            for (int i = -NumDimensionsOnSide; i <= NumDimensionsOnSide; i++)
            {
                TextMeshPro label = textLabels[labelIdx];
                Vector3 pos = labelBasePosition;
                pos.x = i * LabelWidth + offset;
                label.transform.localPosition = pos;

                // Adjust the size of the text based on the translated position
                label.fontSize = NormaliseValue(posRange - Mathf.Abs(pos.x), 0, posRange, 0, originalFontSize);
                if (label == MainTextLabel)
                {
                    label.fontStyle = FontStyles.Bold;
                    label.fontSize = label.fontSize * 1.2f;
                }

                labelIdx++;
            }
        }

        private void SetLabelNames(int newMainIdx)
        {
            // If dimension has changed, update the parent Data Visualisation
            if (GetCurrentDimension() != dimensions[newMainIdx])
            {
                switch (axisDimension)
                {
                    case AbstractVisualisation.PropertyType.X:
                        parentDataVisualisation.XDimension = dimensions[newMainIdx];
                        break;
                    case AbstractVisualisation.PropertyType.Y:
                        parentDataVisualisation.YDimension = dimensions[newMainIdx];
                        break;
                    case AbstractVisualisation.PropertyType.Z:
                        parentDataVisualisation.ZDimension = dimensions[newMainIdx];
                        break;
                }
                selectedDimensionIdx = newMainIdx;
            }

            // Update the text of the other labels
            int labelIdx = 0;
            for (int i = -NumDimensionsOnSide; i < 0; i++) // Negative side
            {
                TextMeshPro label = textLabels[labelIdx];
                int j = (newMainIdx + i) % dimensions.Count;
                if (j < 0) j += dimensions.Count;
                label.text = dimensions[j];
                labelIdx++;
            }
            labelIdx++; // Skip the main label that is already part of the Axis script
            for (int i = 1; i <= NumDimensionsOnSide; i++) // Positive side
            {
                TextMeshPro label = textLabels[labelIdx];
                int j = (newMainIdx + i) % dimensions.Count;
                label.text = dimensions[j];
                labelIdx++;
            }
        }

        private void ShowLabels()
        {
            for (int i = 0; i < textLabels.Length; i++)
            {
                textLabels[i].gameObject.SetActive(true);
            }
        }

        private void HideLabels()
        {
            for (int i = 0; i < textLabels.Length; i++)
            {
                if (i != NumDimensionsOnSide)
                {
                    textLabels[i].gameObject.SetActive(false);
                }
            }

            MainTextLabel.fontStyle = FontStyles.Normal;
            MainTextLabel.fontSize = originalFontSize;
        }

        private string GetCurrentDimension()
        {
            switch (axisDimension)
            {
                case AbstractVisualisation.PropertyType.X:
                    return parentDataVisualisation.XDimension;
                case AbstractVisualisation.PropertyType.Y:
                    return parentDataVisualisation.YDimension;
                case AbstractVisualisation.PropertyType.Z:
                    return parentDataVisualisation.ZDimension;
            }
            return "";
        }


        private float NormaliseValue(float value, float i0, float i1, float j0 = 0, float j1 = 1)
        {
            float L = (j0 - j1) / (i0 - i1);
            return (j0 - (L * i0) + (L * value));
        }
    }
}