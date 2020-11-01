using IATK;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Experimental.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Microsoft.MixedReality.Toolkit.Utilities;

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
        //public ScrollingObjectCollection ScrollMenu;
        public GridObjectCollection GridCollection;
        public GameObject ButtonPrefab;

        public UnityEvent OnSettingGroupClicked;
        public UnityEvent OnSettingChanged;

        private DataSource dataSource;
        private List<string> dimensions;
        private List<ButtonConfigHelper> buttons;

        private void Start()
        {
            dataSource = DataVisualisationManager.Instance.DataSource;
            dimensions = GetAttributesList();

            switch (visualisationSetting)
            {
                case VisualisationProperties.XDimension:
                case VisualisationProperties.YDimension:
                case VisualisationProperties.ZDimension:
                case VisualisationProperties.SizeByDimension:
                case VisualisationProperties.ColorByDimension:
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
                button.transform.SetParent(GridCollection.transform);
                button.transform.localPosition = Vector3.zero;
                button.transform.localRotation = Quaternion.identity;
                button.MainLabelText = dimension;
                button.OnClick.AddListener(() => OptionButtonClicked(button));
                button.IconStyle = ButtonIconStyle.None;
                button.SeeItSayItLabelEnabled = false;

                buttons.Add(button);
            }

            buttons[0].IconStyle = ButtonIconStyle.Quad;
            buttons[0].SetQuadIconByName("IconDone");

            GridCollection.UpdateCollection();
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

            GridCollection.gameObject.SetActive(true);
        }

        public void CloseSettingGroup()
        {
            IsSettingGroupOpened = false;

            MainButton.IconStyle = ButtonIconStyle.None;

            GridCollection.gameObject.SetActive(false);
        }
    }
}