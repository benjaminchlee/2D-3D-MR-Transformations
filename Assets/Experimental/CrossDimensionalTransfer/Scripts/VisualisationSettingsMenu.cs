using IATK;
using Microsoft.MixedReality.Toolkit.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Experimental.CrossDimensionalTransfer
{
    public enum VisualisationProperties
    {
        XDimension,
        YDimension,
        ZDimension,
        SizeByDimension,
        ColorByDimension,
        Color,
        GeometryType
    }

    public class VisualisationSettingsMenu : MonoBehaviour
    {
        public DataVisualisation ParentDataVisualisation;
        public List<VisualisationSettingGroup> VisualisationSettingGroups;
        public ButtonConfigHelper ColorButton;
        public GameObject ColorPicker;

        private VisualisationSettingGroup activeSettingGroup;

        private void Start()
        {
            foreach (var group in VisualisationSettingGroups)
            {
                group.OnSettingGroupClicked.AddListener(() => SettingGroupClicked(group));
                group.OnSettingChanged.AddListener(() => SettingChanged(group));
            }
            
            ColorButton.OnClick.AddListener(ToggleColorButton);
        }

        private void SettingGroupClicked(VisualisationSettingGroup group)
        {
            if (group.IsSettingGroupOpened)
            {
                group.CloseSettingGroup();
            }
            else
            {
                foreach (var g in VisualisationSettingGroups)
                {
                    if (g != group)
                        g.CloseSettingGroup();
                    else   
                        group.OpenSettingGroup();
                }
                
                ColorPicker.SetActive(false);
            }
        }

        private void SettingChanged(VisualisationSettingGroup group)
        {
            SetVisualisationSetting(group.Setting, group.Value);
        }

        private void ToggleColorButton()
        {
            foreach (var g in VisualisationSettingGroups)
                g.CloseSettingGroup();
            
            ColorPicker.SetActive(!ColorPicker.activeSelf);
        }

        private void SetVisualisationSetting(VisualisationProperties setting, object value)
        {
            switch (setting)
            {
                case VisualisationProperties.XDimension:
                    ParentDataVisualisation.XDimension = (string) value;
                    break;
                
                case VisualisationProperties.YDimension:
                    ParentDataVisualisation.YDimension = (string) value;
                    break;

                case VisualisationProperties.ZDimension:
                    ParentDataVisualisation.ZDimension = (string) value;
                    break;

                case VisualisationProperties.SizeByDimension:
                    ParentDataVisualisation.SizeByDimension = (string) value;
                    break;
                
                case VisualisationProperties.ColorByDimension:
                    ParentDataVisualisation.ColourByDimension = (string) value;
                    break;
                    
                case VisualisationProperties.GeometryType:
                    ParentDataVisualisation.GeometryType = (AbstractVisualisation.GeometryType) value;
                    break;
            }
        }

    }    
}