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
        public List<Vector3> positions;
        public int startIndex;
        
        private void Start()
        {
            for (int i = 0; i < visualisations.Count; i++)
            {
                var vis = visualisations[i];
                positions.Add(vis.holder.transform.localPosition);
                
                if (i != startIndex)
                {
                    vis.holder.SetActive(true);
                    vis.holder.transform.localPosition = new Vector3(1000, 1000, 1000);
                }
                
                vis.button.OnClick.AddListener(() => SwitcherButtonClicked(vis));
            }
            
            SwitcherButtonClicked(visualisations[startIndex]);
        }
        
        public void SwitcherButtonClicked(VisualisationSwitcherButton visButton)
        {
            for (int i = 0; i < visualisations.Count; i++)
            {
                var thisVis = visualisations[i];
                
                if (visButton.holder == thisVis.holder)
                {
                    thisVis.holder.transform.localPosition = positions[i];
                }
                else
                {
                    thisVis.holder.transform.localPosition = new Vector3(1000, 1000, 1000);
                }
            }
        }
    }
}