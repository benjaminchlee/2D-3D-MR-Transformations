using IATK;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Experimental.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Input;

namespace Experimental.CrossDimensionalTransfer
{
    public class VisualisationSettingGroup : MonoBehaviour
    {
        [SerializeField]
        private VisualisationProperties visualisationSetting;
        public VisualisationProperties Setting
        {
            get { return visualisationSetting; }
        }

        public object Value { get; private set; }
        public bool IsSettingGroupOpened { get; private set; }

        public ButtonConfigHelper MainButton;
        public GameObject ButtonHolder;
        public GameObject ButtonPrefab;

        public UnityEvent OnSettingGroupClicked;
        public UnityEvent OnSettingChanged;

        [HideInInspector]
        public DataSource DataSource;
        private List<ButtonConfigHelper> buttons;
        
        private void Start()
        {
            switch (visualisationSetting)
            {
                case VisualisationProperties.XDimension:
                case VisualisationProperties.YDimension:
                case VisualisationProperties.ZDimension:
                case VisualisationProperties.SizeByDimension:
                case VisualisationProperties.ColorByDimension:
                case VisualisationProperties.GeometryType:
                case VisualisationProperties.LinkingDimension:
                    CreateScrollButtons();
                    break;
            }

            CloseSettingGroup();

            MainButton.OnClick.AddListener(SettingGroupButtonClicked);
        }

        private List<string> GetAttributesList()
        {
            List<string> dimensions = new List<string>();
            for (int i = 0; i < DataSource.DimensionCount; ++i)
            {
                dimensions.Add(DataSource[i].Identifier);
            }
            return dimensions;
        }

        private void CreateScrollButtons()
        {
            buttons = new List<ButtonConfigHelper>();
            List<string> buttonLabels = new List<string>();
            
            switch (Setting)
            {
                case VisualisationProperties.XDimension:
                case VisualisationProperties.YDimension:
                case VisualisationProperties.ZDimension:
                case VisualisationProperties.SizeByDimension:
                case VisualisationProperties.ColorByDimension:
                case VisualisationProperties.LinkingDimension:
                    buttonLabels = GetAttributesList();
                    buttonLabels.Insert(0, "Undefined");
                    break;
                
                case VisualisationProperties.GeometryType:
                    buttonLabels = new List<string>(){ "Points", "Spheres", "Cubes", "Quads", "Bars", "Lines", "LinesAndDots"};
                    break;
            }

            for (int i = 0; i < buttonLabels.Count; i++)
            {
                ButtonConfigHelper button = Instantiate(ButtonPrefab).GetComponent<ButtonConfigHelper>();
                button.MainLabelText = buttonLabels[i];
                button.OnClick.AddListener(() => OptionButtonClicked(button));
                button.SeeItSayItLabelEnabled = false;

                buttons.Add(button);
                
                if (i == 0)
                    button.SetQuadIconByName("IconDone");
                else   
                    button.SetQuadIconByName("IconClose");
                    
                // For some reason the char and sprite icons are visible sometimes. Force set them as inactive here
                button.transform.Find("IconAndText/UIButtonCharIcon").GetComponent<MeshRenderer>().enabled = false;
                button.transform.Find("IconAndText/UIButtonSpriteIcon").GetComponent<SpriteRenderer>().enabled = false;
                
                button.transform.SetParent(ButtonHolder.transform);
                button.transform.localPosition = new Vector3(0, (0.032f + 0.004f) * -i, 0);
                button.transform.localRotation = Quaternion.identity;
            }
        }

        private void SettingGroupButtonClicked()
        {
            OnSettingGroupClicked.Invoke();
        }

        private void OptionButtonClicked(ButtonConfigHelper button)
        {
            Value = button.MainLabelText;

            if (Setting == VisualisationProperties.GeometryType)
                Value = GetGeometryType(button.MainLabelText);

            foreach (var b in buttons)
            {
                if (b != button)
                {
                    b.IconStyle = ButtonIconStyle.None;
                    b.SetQuadIconByName("IconClose");
                }
                else
                {
                    //b.IconStyle = ButtonIconStyle.Quad;
                    b.SetQuadIconByName("IconDone");
                }
            }

            OnSettingChanged.Invoke();
        }

        public void OpenSettingGroup()
        {
            IsSettingGroupOpened = true;

            MainButton.IconStyle = ButtonIconStyle.Quad;
            MainButton.SetQuadIconByName("IconDone");
            
            ButtonHolder.SetActive(true);
        }

        public void CloseSettingGroup()
        {
            IsSettingGroupOpened = false;

            MainButton.IconStyle = ButtonIconStyle.None;

            ButtonHolder.SetActive(false);
        }
        
        private AbstractVisualisation.GeometryType GetGeometryType(string name)
        {
            switch (name)
            {
                case "Points":
                    return AbstractVisualisation.GeometryType.Points;
                
                case "Spheres":
                    return AbstractVisualisation.GeometryType.Spheres;
                
                case "Cubes":
                    return AbstractVisualisation.GeometryType.Cubes;
                
                case "Quads":
                    return AbstractVisualisation.GeometryType.Quads;
                
                case "Bars":
                    return AbstractVisualisation.GeometryType.Bars;
                    
                case "Lines":
                    return AbstractVisualisation.GeometryType.Lines;
                    
                case "LinesAndDots":
                    return AbstractVisualisation.GeometryType.LinesAndDots;
                
                default:
                    return AbstractVisualisation.GeometryType.Undefined;
            }
        }
    }
}