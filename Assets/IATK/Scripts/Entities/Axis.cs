using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.Events;
using IATK;
using System.Linq;
using System;

public class Axis : MonoBehaviour {

    #region Public variables

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
    private GameObject axisValueLabels;

    [SerializeField]
    [Tooltip("The base axis label to duplicate and use.")]
    private TextMeshPro axisLabelPrefab;

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

    [HideInInspector]
    public Visualisation visualisationReference;
    [HideInInspector]
    public string AttributeName = "";
    [HideInInspector]
    public float Length = 1.0f;
    [HideInInspector]
    public AttributeFilter AttributeFilter;
    [HideInInspector]
    public float MinNormaliser;
    [HideInInspector]
    public float MaxNormaliser;
    [HideInInspector]
    public HashSet<Axis> ConnectedAxis = new HashSet<Axis>();
    [HideInInspector]
    public int SourceIndex = -1;
    [HideInInspector]
    public int MyDirection = 0;

    #endregion


    #region Private variables

    private AxisLabelDelegate labelDelegate;
    private List<TextMeshPro> axisLabels = new List<TextMeshPro>();
    private Vector2 AttributeRange;
    private float ticksScaleFactor = 1.0f;

    #endregion

    public void Init(DataSource srcData, AttributeFilter attributeFilter, Visualisation visualisation)
    {
        AttributeName = attributeFilter.Attribute;
        AttributeFilter = attributeFilter;

        int idx = Array.IndexOf(srcData.Select(m => m.Identifier).ToArray(), attributeFilter.Attribute);
        SourceIndex = idx;
        name = "axis " + srcData[idx].Identifier;
        
        AttributeRange = new Vector2(srcData[idx].MetaData.minValue, srcData[idx].MetaData.maxValue);
        attributeLabel.text = srcData[idx].Identifier;

        visualisationReference = visualisation;
        
        CalculateTicksScale(srcData[idx].MetaData.binCount);
        UpdateTicks();
        GenerateAxisLabels();
    }

    private void GenerateAxisLabels()
    {
        labelDelegate = new BasicAxisLabelDelegate(AttributeFilter, visualisationReference.dataSource, Length);

        List<GameObject> children = new List<GameObject>();
        foreach (Transform child in axisValueLabels.transform)
        {
            if (child.gameObject.activeSelf)
            {
                children.Add(child.gameObject);
            }
        }
        foreach (GameObject go in children)
        {
            #if !UNITY_EDITOR
            Destroy(go);
            #else
            DestroyImmediate(go);
            #endif
        }

        axisLabels.Clear();

        for (int i = 0; i < labelDelegate.NumberOfLabels(); ++i)
        {
            var go = Instantiate(axisLabelPrefab, axisValueLabels.transform);
            go.gameObject.SetActive(true);

            go.text = labelDelegate.LabelText(i);
            SetYPos(go.transform, labelDelegate.LabelPosition(i));

            axisLabels.Add(go);
        }
    }

    private void UpdateAxisLabels()
    {
        labelDelegate = new BasicAxisLabelDelegate(AttributeFilter, visualisationReference.dataSource, Length);

        if (labelDelegate.NumberOfLabels() != axisLabels.Count)
            GenerateAxisLabels();

        for (int i = 0; i < labelDelegate.NumberOfLabels(); ++i)
        {
            var go = axisLabels[i];
            go.text = labelDelegate.LabelText(i);

            float y = labelDelegate.LabelPosition(i);
            SetYPos(go.transform, y * Length);
            go.gameObject.SetActive(y >= 0.0f && y <= 1.0f);

            go.color = new Color(1, 1, 1, labelDelegate.IsFiltered(i) ? 0.4f : 1.0f);
        }
    }
    
    // helper func
    private void SetXPos(Transform t, float value)
    {
        var p = t.localPosition;
        p.x = value;
        t.localPosition = p;
    }

    private void SetYPos(Transform t, float value)
    {
        var p = t.localPosition;
        p.y = value;
        t.localPosition = p;
    }

    // sets the direction of this axis. X=1, Y=2, Z=3
    public void SetDirection(int direction)
    {
        MyDirection = direction;
        switch (direction)
        {
            case 1:
                transform.localEulerAngles = new Vector3(0, 0, -90);

                // Set axis labels rotation
                foreach (TextMeshPro tmp in axisValueLabels.transform.GetComponentsInChildren<TextMeshPro>(true))
                {
                    tmp.GetComponent<RectTransform>().pivot = new Vector2(0, 0.5f);
                    tmp.alignment = TextAlignmentOptions.MidlineLeft;
                }
                SetXPos(axisValueLabels.transform, 0);
                SetXPos(attributeLabel.transform, 0.1f);
                attributeLabel.alignment = TextAlignmentOptions.Top;  
                break;
            case 2:
                transform.localEulerAngles = new Vector3(0, 0, 0);
                SetXPos(minNormaliserObject, -0.03f);
                SetXPos(maxNormaliserObject, -0.03f);
                minNormaliserObject.localEulerAngles = new Vector3(90, 90, 0);
                maxNormaliserObject.localEulerAngles = new Vector3(90, 90, 0);
                break;
            case 3:
                transform.localEulerAngles = new Vector3(90, 0, 0);
                SetXPos(minNormaliserObject, -0.03f);
                SetXPos(maxNormaliserObject, -0.03f);
                minNormaliserObject.localEulerAngles = new Vector3(90, 90, 0);
                maxNormaliserObject.localEulerAngles = new Vector3(90, 90, 0);
                break;
            default:
                SetXPos(minNormaliserObject, -0.03f);
                SetXPos(maxNormaliserObject, -0.03f);
                minNormaliserObject.localEulerAngles = new Vector3(90, 90, 0);
                maxNormaliserObject.localEulerAngles = new Vector3(90, 90, 0);
                break;
        }
    }

    public void UpdateLength()
    {
        axisRod.localScale = new Vector3(axisRod.localScale.x, Length, axisRod.localScale.z);
        axisTip.localPosition = new Vector3(axisTip.localPosition.x, Length, axisTip.localPosition.z);

        SetMinFilter(AttributeFilter.minFilter);
        SetMaxFilter(AttributeFilter.maxFilter);

        SetMinNormalizer(AttributeFilter.minScale);
        SetMaxNormalizer(AttributeFilter.maxScale);

        UpdateAxisLabels();        
    }

    public void UpdateLabelAttribute(string attributeName)
    {
        attributeLabel.text = attributeName;
        var pos = attributeLabel.transform.localPosition;
        pos.y = Length * 0.5f;
        attributeLabel.transform.localPosition = pos;

        UpdateAxisLabels();
    }

    void CalculateTicksScale(int binCount)
    {
        float range = AttributeRange.y - AttributeRange.x;
        if (binCount > range + 2)
        {
            ticksScaleFactor = 1.0f / (binCount / 10);
        }
        else if (range < 20)
        {
            // each tick mark represents one increment
            ticksScaleFactor = 1;
        }
        else if (range < 50)
        {
            ticksScaleFactor = 5;
        }
        else if (range < 200)
        {
            // each tick mark represents ten increment
            ticksScaleFactor = 10;            
        }
        else if (range < 600)
        {
            ticksScaleFactor = 50;           
        }
        else if (range < 3000)
        {
            ticksScaleFactor = 100;            
        }
        else
        {
            ticksScaleFactor = 500;            
        }
    }

    void UpdateTicks()
    {
        float range = Mathf.Lerp(AttributeRange.x, AttributeRange.y, MaxNormaliser + 0.5f) - Mathf.Lerp(AttributeRange.x, AttributeRange.y, MinNormaliser + 0.5f);
        float scale = range / ticksScaleFactor;
        //ticksRenderer.material.mainTextureScale = new Vector3(1, scale);
    }
    
    //
    // filters and scaling
    //

    public void SetMinFilter(float val)
    { }

    public void SetMaxFilter(float val)
    { }

    public void SetMinNormalizer(float val)
    {
        MinNormaliser = Mathf.Clamp(val, 0, 1);

        Vector3 p = minNormaliserObject.transform.localPosition;
        p.y = MinNormaliser * Length;
        minNormaliserObject.transform.localPosition = p;

        UpdateTicks();
    }

    public void SetMaxNormalizer(float val)
    {
        MaxNormaliser = Mathf.Clamp(val, 0, 1);

        Vector3 p = maxNormaliserObject.transform.localPosition;
        p.y = MaxNormaliser * Length;
        maxNormaliserObject.transform.localPosition = p;

        UpdateTicks();
    }

    #region euclidan functions

    // calculates the project of the transform tr (assumed to be the user's hand) onto the axis
    // as a float between 0...1
    public float CalculateLinearMapping(Transform tr)
    {
        Vector3 direction = MaxPosition - MinPosition;
        float length = direction.magnitude;
        direction.Normalize();

        Vector3 displacement = transform.InverseTransformPoint(tr.position) - MinPosition;

        return Vector3.Dot(displacement, direction) / length;
    }
    
    Vector3 _maxPos;
    public Vector3 MaxPosition
    {
        //get { return _maxPos; }
        get { return Vector3.up * Length; }
    }

    Vector3 _minPos;
    public Vector3 MinPosition
    {
        //get { return _minPos; }
        get { return Vector3.zero; }
    }

    #endregion
    

    abstract class AxisLabelDelegate
    {
        public virtual int NumberOfLabels()
        {
            return 0;
        }

        public virtual float LabelPosition(int labelIndex)
        {
            return 0;
        }

        public virtual string LabelText(int labelIndex)
        {
            return "";
        }

        public virtual bool IsFiltered(int labelIndex)
        {
            return false;
        }
    }

    class BasicAxisLabelDelegate : AxisLabelDelegate
    {
        public AttributeFilter attributeFilter;
        public DataSource dataSource;
        public float axisLength;

        public BasicAxisLabelDelegate(AttributeFilter attributeFilter, DataSource dataSource, float axisLength)
        {
            this.attributeFilter = attributeFilter;
            this.dataSource = dataSource;
            this.axisLength = axisLength;
        }
        
        bool IsDiscreet()
        {
            var type = dataSource[attributeFilter.Attribute].MetaData.type;
            if (type == DataType.String || type == DataType.Date)// || type == DataType.Time)
            {
                return true;
            }
            return false;
        }

        public override int NumberOfLabels()
        {
            if (IsDiscreet())
            {
                if (attributeFilter.minScale > 0.001f || attributeFilter.maxScale < 0.999f)
                {
                    return 0;
                }
                
                CSVDataSource csvDataSource = (CSVDataSource) dataSource;
                int numValues = csvDataSource.TextualDimensionsListReverse[attributeFilter.Attribute].Count;

                if (numValues < (Mathf.CeilToInt(axisLength / 0.075f)))
                {
                    return numValues;
                }
                else
                {
                    return 2;
                }
            }
            else
            {
                return Mathf.CeilToInt(axisLength / 0.125f);
            }
        }

        public override float LabelPosition(int labelIndex)
        {
            return (labelIndex / (float) (NumberOfLabels() - 1));
        }

        public override string LabelText(int labelIndex)
        {
            object v = dataSource.getOriginalValue(Mathf.Lerp(attributeFilter.minScale, attributeFilter.maxScale, labelIndex / (NumberOfLabels() - 1f)), attributeFilter.Attribute);

            if (v is float && v.ToString().Length > 4)
            {
                return ((float)v).ToString("#,##0.0");
            }
            else
            {
                return v.ToString();
            }
        }

        public override bool IsFiltered(int labelIndex)
        {
            float n = labelIndex / (float)(NumberOfLabels() - 1);
            float delta = Mathf.Lerp(attributeFilter.minScale, attributeFilter.maxScale, n);
            return delta < attributeFilter.minFilter || delta > attributeFilter.maxFilter;
        }

        private float NormaliseValue(float value, float min1, float max1, float min2, float max2)
        {
            float i = (min2 - max2) / (min1 - max1);
            return (min2 - (i * min1) + (i * value));
        }
    }

}