using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.Events;
using IATK;
using System.Linq;
using System;

namespace IATK
{
    [ExecuteInEditMode]
    public class Axis : MonoBehaviour
    {
        #region Public/Inspector variables

        [Header("Child GameObject references")]
        [SerializeField]
        [Tooltip("The tip (cone) of the axis.")]
        private Transform axisTip;
        [SerializeField]
        [Tooltip("The rod (cylinder) of the axis.")]
        private Transform axisRod;
        [SerializeField]
        [Tooltip("The main attribute label of this axis.")]
        private TextMeshPro attributeLabel;
        [SerializeField]
        [Tooltip("The GameObject which holds all of the axis value labels.")]
        private GameObject axisTickLabelHolder;
        [SerializeField]
        [Tooltip("The base axis tick label to duplicate and use.")]
        private GameObject axisTickLabelPrefab;
        [SerializeField]
        [Tooltip("The minimum normaliser handle.")]
        private Transform minNormaliserObject;
        [SerializeField]
        [Tooltip("The maximum normaliser handle.")]
        private Transform maxNormaliserObject;
        [SerializeField]
        [Tooltip("The minimum filter handle.")]
        private Transform minFilterObject;
        [SerializeField]
        [Tooltip("The maximum normaliser handle.")]
        private Transform maxFilterObject;
        [Header("Axis Visual Properties")]
        [SerializeField]
        [Tooltip("The maximum amount of spacing that each axis tick label should have between each other.")]
        private float AxisTickSpacing = 0.075f;

        [HideInInspector]
        public AttributeFilter AttributeFilter;
        [HideInInspector]
        public float Length = 1.0f;
        [HideInInspector]
        public float MinNormaliser;
        [HideInInspector]
        public float MaxNormaliser;
        [HideInInspector]
        public HashSet<Axis> ConnectedAxis = new HashSet<Axis>();
        [HideInInspector]
        public int SourceIndex = -1;
        [HideInInspector]
        public int AxisDirection = 0;

        #endregion

        #region Private variables

        private Visualisation visualisationReference;
        private DataSource dataSource;
        private List<GameObject> axisTickLabels = new List<GameObject>();
        private bool isInitialised = false;

        #endregion

        /// <summary>
        /// Initialises the axis.
        /// </summary>
        /// <param name="srcData"></param>
        /// <param name="attributeFilter"></param>
        /// <param name="visualisation"></param>
        public void Initialise(DataSource srcData, AttributeFilter attributeFilter, Visualisation visualisation, int direction)
        {
            isInitialised = false;

            AttributeFilter = attributeFilter;
            dataSource = srcData;

            int idx = Array.IndexOf(srcData.Select(m => m.Identifier).ToArray(), attributeFilter.Attribute);
            SourceIndex = idx;
            name = "Axis " + srcData[idx].Identifier;

            visualisationReference = visualisation;
            axisTickLabelPrefab.GetComponentInChildren<TextMeshPro>().text = "";

            SetDirection(direction);

            isInitialised = true;
        }

        /// <summary>
        /// Sets the direction (dimension) that this axis represents
        /// </summary>
        /// <param name="direction">X=1, Y=2, Z=3</param>
        private void SetDirection(int direction)
        {
            AxisDirection = direction;
            switch (direction)
            {
                case 1:
                    // Fix the alignment of the axis tick labels
                    foreach (Transform child in axisTickLabelHolder.GetComponentsInChildren<Transform>(true))
                    {
                        if (child.gameObject.name.Contains("Text"))
                        {
                            TextMeshPro labelText = child.GetComponent<TextMeshPro>();
                            labelText.alignment = TextAlignmentOptions.MidlineLeft;
                            labelText.rectTransform.pivot = new Vector2(0, 0.5f);
                        }
                        else if (child.gameObject.name.Contains("Tick"))
                        {
                            SetXLocalPosition(child, -child.localPosition.x);
                        }
                    }
                    transform.localEulerAngles = new Vector3(0, 0, -90);
                    SetXLocalPosition(axisTickLabelHolder.transform, 0);
                    attributeLabel.alignment = TextAlignmentOptions.Top;
                    attributeLabel.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    UpdateAxisAttributeAndLength(AttributeFilter, visualisationReference.width);
                    break;

                case 2:
                    transform.localEulerAngles = new Vector3(0, 0, 0);
                    SetXLocalPosition(minNormaliserObject, -minNormaliserObject.transform.localPosition.x);
                    SetXLocalPosition(maxNormaliserObject, -maxNormaliserObject.transform.localPosition.x);
                    minNormaliserObject.localEulerAngles = new Vector3(90, 90, 0);
                    maxNormaliserObject.localEulerAngles = new Vector3(90, 90, 0);
                    UpdateAxisAttributeAndLength(AttributeFilter, visualisationReference.height);
                    break;

                case 3:
                    transform.localEulerAngles = new Vector3(90, 0, 0);
                    SetXLocalPosition(minNormaliserObject, -minNormaliserObject.transform.localPosition.x);
                    SetXLocalPosition(maxNormaliserObject, -maxNormaliserObject.transform.localPosition.x);
                    minNormaliserObject.localEulerAngles = new Vector3(90, 90, 0);
                    maxNormaliserObject.localEulerAngles = new Vector3(90, 90, 0);
                    UpdateAxisAttributeAndLength(AttributeFilter, visualisationReference.depth);
                    break;
            }
        }

        /// <summary>
        /// Updates the labels to reflect the new attribute and the length of this axis. Call this whenever the length of the axis changes
        /// </summary>
        /// <param name="attribute"></param>
        /// <param name="length"></param>
        public void UpdateAxisAttributeAndLength(AttributeFilter attributeFilter, float length)
        {
            // If nothing has changed, then don't update anything
            if (isInitialised && attributeFilter.Attribute == attributeLabel.text && Length == length)
                return;

            // Attribute
            AttributeFilter = attributeFilter;
            attributeLabel.text = AttributeFilter.Attribute;
            SetYLocalPosition(attributeLabel.transform, length * 0.5f);

            // Length
            Length = length;
            axisRod.localScale = new Vector3(axisRod.localScale.x, Length, axisRod.localScale.z);
            axisTip.localPosition = new Vector3(axisTip.localPosition.x, Length, axisTip.localPosition.z);
            axisTip.localEulerAngles = new Vector3(length >= 0 ? 0 : 180, -45, 0);

            UpdateAxisTickLabels();
        }

        /// <summary>
        /// Updates the labels to reflect the new min and max normalisers and filters of this axis.
        /// </summary>
        /// <param name="dimensionFilter"></param>
        public void UpdateAxisRanges()
        {
            SetMinFilter(AttributeFilter.minFilter);
            SetMaxFilter(AttributeFilter.maxFilter);
            SetMinNormalizer(AttributeFilter.minScale);
            SetMaxNormalizer(AttributeFilter.maxScale);

            UpdateAxisTickLabels();
        }

        /// <summary>
        /// Updates the text of the tick labels on this axis.
        /// </summary>
        private void UpdateAxisTickLabels()
        {
            if (!gameObject.activeSelf)
                return;

            int currentNumberOfLabels = axisTickLabels.Count;
            int targetNumberOfLabels = CalculateNumAxisTickLabels();

            // If more labels are needed, then we instantiate them now
            for (int i = currentNumberOfLabels; i < targetNumberOfLabels; i++)
            {
                GameObject label = Instantiate(axisTickLabelPrefab, axisTickLabelHolder.transform);
                axisTickLabels.Add(label);
            }

            // Update label positions and text
            for (int i = 0; i < targetNumberOfLabels; i++)
            {
                GameObject label = axisTickLabels[i];
                if (!label.activeSelf) label.SetActive(true);
                float y = GetAxisTickLabelPosition(i, targetNumberOfLabels);
                SetYLocalPosition(label.transform, y * Length);

                TextMeshPro labelText = label.GetComponentInChildren<TextMeshPro>();
                labelText.gameObject.SetActive(y >= 0.0f && y <= 1.0f);
                labelText.text = GetAxisTickLabelText(i, targetNumberOfLabels);
                labelText.color = new Color(1, 1, 1, GetAxisTickLabelFiltered(i, targetNumberOfLabels) ? 0.4f : 1.0f);
            }

            // Disable all leftover labels
            for (int i = targetNumberOfLabels; i < axisTickLabels.Count; i++)
            {
                GameObject label = axisTickLabels[i];
                if (label.activeSelf) label.SetActive(false);
            }
        }

        private void SetMinFilter(float val)
        {
            // UpdateAxisTickLabels();
        }

        private void SetMaxFilter(float val)
        {
            // UpdateAxisTickLabels();
        }

        private void SetMinNormalizer(float val)
        {
            MinNormaliser = Mathf.Clamp(val, 0, 1);

            Vector3 p = minNormaliserObject.transform.localPosition;
            p.y = MinNormaliser * Length;
            minNormaliserObject.transform.localPosition = p;
        }

        private void SetMaxNormalizer(float val)
        {
            MaxNormaliser = Mathf.Clamp(val, 0, 1);

            Vector3 p = maxNormaliserObject.transform.localPosition;
            p.y = MaxNormaliser * Length;
            maxNormaliserObject.transform.localPosition = p;
        }

        public void OnEnable()
        {
            if (isInitialised)
                UpdateAxisTickLabels();
        }

        #region Private helper functions

        private int CalculateNumAxisTickLabels()
        {
            // Special case for categorical x and z axes in bar charts
            if (visualisationReference.visualisationType == AbstractVisualisation.VisualisationTypes.BAR &&
                (AxisDirection == 1 || AxisDirection == 3))
            {
                // For categorical dimensions, use the distinct count of values rather that using the user defined num bins
                if (IsAttributeCategorical())
                {
                    return dataSource[AttributeFilter.Attribute].Data.Distinct().Count();
                }
                else
                {
                    if (AxisDirection == 1)
                        return visualisationReference.numXBins + 1;
                    else
                        return visualisationReference.numZBins + 1;
                }
            }
            else
            {
                if (IsAttributeCategorical())
                {
                    // If this axis dimension has been rescaled at all, don't show any ticks
                    if (AttributeFilter.minScale > 0.001f || AttributeFilter.maxScale < 0.999f)
                        return 0;

                    // If this discrete dimension has less unique values than the maximum number of ticks allowed due to spacing,
                    // give an axis tick label for each unique value
                    int numValues = ((CSVDataSource)dataSource).TextualDimensionsListReverse[AttributeFilter.Attribute].Count;
                    int maxTicks = Mathf.CeilToInt(Mathf.Abs(Length) / AxisTickSpacing);
                    if (numValues < maxTicks)
                    {
                        return numValues;
                    }
                    // Otherwise just use 2 labels
                    else
                    {
                        return 2;
                    }
                }
                else
                {
                    // Always have at least 2 labels for continuous variables
                    return Mathf.Max(Mathf.CeilToInt(Mathf.Abs(Length) / AxisTickSpacing), 2);
                }
            }
        }

        private bool IsAttributeCategorical()
        {
            try
            {
                var type = dataSource[AttributeFilter.Attribute].MetaData.type;
                return (type == DataType.String || type == DataType.Date);
            }
            catch
            {
                return true;
            }
        }

        private float GetAxisTickLabelPosition(int labelIndex, int numLabels)
        {
            // Special case for categorical x and z axes in bar charts
            if (visualisationReference.visualisationType == AbstractVisualisation.VisualisationTypes.BAR &&
                (AxisDirection == 1 || AxisDirection == 3) &&
                IsAttributeCategorical())
            {
                return (labelIndex * 2 + 1) / (float)(numLabels * 2);
            }
            else
            {
                if (numLabels == 1)
                    return 0;

                return (labelIndex / (float) (numLabels - 1));
            }
        }

        private string GetAxisTickLabelText(int labelIndex, int numLabels)
        {
            object v = dataSource.getOriginalValue(Mathf.Lerp(AttributeFilter.minScale, AttributeFilter.maxScale, labelIndex / (numLabels - 1f)), AttributeFilter.Attribute);

            if (v is float && v.ToString().Length > 4)
            {
                return ((float)v).ToString("#,##0.0");
            }
            else
            {
                return v.ToString();
            }
        }

        private bool GetAxisTickLabelFiltered(int labelIndex, int numLabels)
        {
            float n = labelIndex / (float)(numLabels - 1);
            float delta = Mathf.Lerp(AttributeFilter.minScale, AttributeFilter.maxScale, n);
            return delta < AttributeFilter.minFilter || delta > AttributeFilter.maxFilter;
        }


        private void SetXLocalPosition(Transform t, float value)
        {
            var p = t.localPosition;
            p.x = value;
            t.localPosition = p;
        }

        private void SetYLocalPosition(Transform t, float value)
        {
            var p = t.localPosition;
            p.y = value;
            t.localPosition = p;
        }

        #endregion // Private helper functions
    }
}