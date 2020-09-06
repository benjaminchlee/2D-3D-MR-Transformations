using Microsoft.MixedReality.Toolkit.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ChartSettings
{
    XDimension,
    YDimension,
    ZDimension,
    SizeByDimension
}

public class ChartSettingsMenu : MonoBehaviour
{
    public Chart ParentChart;
    public List<ChartSettingGroup> ChartSettingGroups;

    private ChartSettingGroup activeSettingGroup;

    private void Start()
    {
        foreach (var group in ChartSettingGroups)
        {
            group.OnSettingGroupClicked.AddListener(() => SettingGroupClicked(group));
            group.OnSettingChanged.AddListener(() => SettingChanged(group));
        }
    }

    private void SettingGroupClicked(ChartSettingGroup group)
    {
        if (group.IsSettingGroupOpened)
        {
            group.CloseSettingGroup();
        }
        else
        {
            foreach (var g in ChartSettingGroups)
            {
                if (g != group)
                    g.CloseSettingGroup();
                else   
                    group.OpenSettingGroup();
            }
        }
    }

    private void SettingChanged(ChartSettingGroup group)
    {
        SetChartSetting(group.Setting, group.Value);

    }

    private void SetChartSetting(ChartSettings setting, object value)
    {
        switch (setting)
        {
            case ChartSettings.XDimension:
                ParentChart.XDimension = (string) value;
                break;
            
            case ChartSettings.YDimension:
                ParentChart.YDimension = (string) value;
                break;

            case ChartSettings.ZDimension:
                ParentChart.ZDimension = (string) value;
                break;

            case ChartSettings.SizeByDimension:
                ParentChart.SizeByDimension = (string) value;
                break;
        }
    }

}
