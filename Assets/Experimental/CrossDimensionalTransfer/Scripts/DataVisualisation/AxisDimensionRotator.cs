using System;
using System.Collections;
using System.Collections.Generic;
using IATK;
using Microsoft.MixedReality.Toolkit.UI;
using TMPro;
using UnityEngine;

namespace Experimental.CrossDimensionalTransfer
{
    [RequireComponent(typeof(ObjectManipulator))]
    [RequireComponent(typeof(RotationAxisConstraint))]
    public class AxisDimensionRotator : MonoBehaviour
    {
        public GameObject LabelHolder;
        public TextMeshPro PreviousText;
        public TextMeshPro CurrentText;
        public TextMeshPro NextText;

        [Range(1, 360)]
        public int DimensionSwapDegrees = 10;

        private DataVisualisation parentDataVisualisation;
        private ObjectManipulator objectManipulatorScript;
        private Axis axisScript;
        private AbstractVisualisation.PropertyType axisDimension;
        private DataSource dataSource;
        private bool isRotating = false;
        private float startDegrees;
        private List<string> dimensions;

        private void Start()
        {
            parentDataVisualisation = GetComponentInParent<DataVisualisation>();
            if (parentDataVisualisation == null || LabelHolder == null)
            {
                Destroy(gameObject);
                return;
            }

            dataSource = parentDataVisualisation.DataSource;
            dimensions = new List<string>();
            for (int i = 0; i < dataSource.DimensionCount; ++i)
                dimensions.Add(dataSource[i].Identifier);

            objectManipulatorScript = GetComponent<ObjectManipulator>();
            objectManipulatorScript.OnManipulationStarted.AddListener(RotatorGrabbed);
            objectManipulatorScript.OnManipulationEnded.AddListener(RotatorReleased);

            axisScript = GetComponentInParent<Axis>();
            if (axisScript.AxisDirection == 1)
                axisDimension = AbstractVisualisation.PropertyType.X;
            else if (axisScript.AxisDirection == 2)
                axisDimension = AbstractVisualisation.PropertyType.Y;
            else
                axisDimension = AbstractVisualisation.PropertyType.Z;

            LabelHolder.SetActive(false);
        }

        private void Update()
        {
            if (isRotating)
            {
                float angle = transform.localRotation.eulerAngles.y - startDegrees;
                if (DimensionSwapDegrees <= Mathf.Abs(angle))
                {
                    startDegrees = transform.localRotation.eulerAngles.y;

                    string newDimension;
                    if (angle < 0)
                        newDimension = GetPreviousDimension();
                    else
                        newDimension = GetNextDimension();

                    switch (axisDimension)
                    {
                        case AbstractVisualisation.PropertyType.X:
                            parentDataVisualisation.XDimension = newDimension;
                            break;
                        case AbstractVisualisation.PropertyType.Y:
                            parentDataVisualisation.YDimension = newDimension;
                            break;
                        case AbstractVisualisation.PropertyType.Z:
                            parentDataVisualisation.ZDimension = newDimension;
                            break;

                    }
                    ShowLabels();
                }
            }
        }

        private void RotatorGrabbed(ManipulationEventData eventData)
        {
            isRotating = true;
            startDegrees = transform.localRotation.eulerAngles.y;
            ShowLabels();
        }

        private void RotatorReleased(ManipulationEventData eventData)
        {
            isRotating = false;
            HideLabels();
        }

        private string GetNextDimension()
        {
            int idx = 0;
            switch (axisDimension)
            {
                case AbstractVisualisation.PropertyType.X:
                    idx = dimensions.IndexOf(parentDataVisualisation.XDimension);
                    break;
                case AbstractVisualisation.PropertyType.Y:
                    idx = dimensions.IndexOf(parentDataVisualisation.YDimension);
                    break;
                case AbstractVisualisation.PropertyType.Z:
                    idx = dimensions.IndexOf(parentDataVisualisation.ZDimension);
                    break;

            }
            return dimensions[++idx % dimensions.Count];
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

        private string GetPreviousDimension()
        {
            int idx = 0;
            switch (axisDimension)
            {
                case AbstractVisualisation.PropertyType.X:
                    idx = dimensions.IndexOf(parentDataVisualisation.XDimension);
                    break;
                case AbstractVisualisation.PropertyType.Y:
                    idx = dimensions.IndexOf(parentDataVisualisation.YDimension);
                    break;
                case AbstractVisualisation.PropertyType.Z:
                    idx = dimensions.IndexOf(parentDataVisualisation.ZDimension);
                    break;

            }
            idx--;
            if (idx < 0) idx = dimensions.Count - 1;
            return dimensions[idx];
        }

        private void ShowLabels()
        {
            LabelHolder.SetActive(true);
            NextText.text = GetNextDimension();
            CurrentText.text = GetCurrentDimension();
            PreviousText.text = GetPreviousDimension();
        }

        private void HideLabels()
        {
            LabelHolder.SetActive(false);
        }
    }
}