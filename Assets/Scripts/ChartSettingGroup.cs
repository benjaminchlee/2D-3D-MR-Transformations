using IATK;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Experimental.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ChartSettingGroup : MonoBehaviour
{

    [SerializeField]
    private ChartSettings chartSetting;
    public ChartSettings Setting
    {
        get { return chartSetting; }
    }

    public object Value { get; private set; }
    public bool IsSettingGroupOpened { get; private set; }

    public ButtonConfigHelper MainButton;
    public ScrollingObjectCollection ScrollMenu;
    public GameObject ButtonPrefab;

    public UnityEvent OnSettingGroupClicked;
    public UnityEvent OnSettingChanged;

    private CSVDataSource dataSource;
    private List<string> dimensions;
    private List<ButtonConfigHelper> buttons;

    private void Start()
    {
        dataSource = ChartManager.Instance.DataSource;
        dimensions = GetAttributesList();

        switch (chartSetting)
        {
            case ChartSettings.XDimension:
            case ChartSettings.YDimension:
            case ChartSettings.ZDimension:
            case ChartSettings.SizeByDimension:
                CreateScrollButtons();
                break;

        }

        CloseSettingGroup();

        MainButton.OnClick.AddListener(SettingGroupButtonClicked);
    }


    private List<string> GetAttributesList()
    {
        List<string> dimensions = new List<string>();
        for (int i = 0; i < dataSource.DimensionCount; ++i)
        {
            dimensions.Add(dataSource[i].Identifier);
        }
        return dimensions;
    }

    private void CreateScrollButtons()
    {
        buttons = new List<ButtonConfigHelper>();
        dimensions.Insert(0, "Undefined");

        foreach (var dimension in dimensions)
        {
            ButtonConfigHelper button = Instantiate(ButtonPrefab).GetComponent<ButtonConfigHelper>();
            button.transform.SetParent(ScrollMenu.transform);
            button.MainLabelText = dimension;
            button.OnClick.AddListener(() => OptionButtonClicked(button));
            button.IconStyle = ButtonIconStyle.None;
            button.SeeItSayItLabelEnabled = false;

            buttons.Add(button);
        }

        buttons[0].IconStyle = ButtonIconStyle.Quad;
        buttons[0].SetQuadIconByName("IconDone");

        ScrollMenu.UpdateCollection();
    }


    private void SettingGroupButtonClicked()
    {
        OnSettingGroupClicked.Invoke();
    }

    private void OptionButtonClicked(ButtonConfigHelper button)
    {
        Value = button.MainLabelText;

        foreach (var b in buttons)
        {
            if (b != button)
            {
                b.IconStyle = ButtonIconStyle.None;
            }
            else
            {
                b.IconStyle = ButtonIconStyle.Quad;
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

        ScrollMenu.gameObject.SetActive(true);
    }

    public void CloseSettingGroup()
    {
        IsSettingGroupOpened = false;

        MainButton.IconStyle = ButtonIconStyle.None;

        ScrollMenu.gameObject.SetActive(false);
    }
}
