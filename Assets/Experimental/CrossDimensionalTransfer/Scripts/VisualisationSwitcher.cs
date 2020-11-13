using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;

namespace Experimental.CrossDimensionalTransfer
{
    public class VisualisationSwitcher : MonoBehaviour
    {
        [System.Serializable]
        public struct VisualisationSwitcherButton
        {
            public GameObject holder;
            public ButtonConfigHelper button;
        }
        
        public List<VisualisationSwitcherButton> visualisations;
        
        private void Start()
        {
            foreach (var vis in visualisations)
            {
                vis.button.OnClick.AddListener(() => 
                {
                    foreach (var _ in visualisations)
                    {
                        if (_.holder == vis.holder)
                            _.holder.SetActive(true);
                        else
                            _.holder.SetActive(false);
                    }
                });
            }
        }
    }
}