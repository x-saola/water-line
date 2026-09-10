using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Delta.Modules.SpinWheel
{
    public class SpinWheelPiece : MonoBehaviour
    {
        public RectTransform holder;
        public Image bgColor;
        public Image bgImage;
        public Image icon;
        public Text label;
        public TextMeshProUGUI amount;
        
        private Material _bgColorMaterial;

        public void Setup(WheelPiece pieceData, float pieceAngle, float fillAmount)
        {
            if (bgColor != null && bgColor.material != null)
            {
                _bgColorMaterial = new Material(bgColor.material);
                bgColor.material = _bgColorMaterial;
            }
            
            if (pieceData.reward != null)
            {
                icon.sprite = pieceData.reward.icon;
                
                if (!string.IsNullOrEmpty(pieceData.reward.label))
                {
                    label.text = pieceData.reward.label;
                }
                
                if (pieceData.reward.rewardId != 0)
                {
                    amount.text = "x" + pieceData.reward.amount.ToString();
                }
                else
                {
                    amount.text = pieceData.reward.amount.ToString();
                }
            }
            
            SetCenterColor(pieceData.CenterColor);
            SetEdgeColor(pieceData.EdgeColor);
            bgColor.fillAmount = fillAmount;
            bgColor.GetComponent<RectTransform>().localEulerAngles = new Vector3(0f, 0f, pieceAngle);
        }

        private void SetCenterColor(Color color)
        {
            if (_bgColorMaterial != null)
            {
                _bgColorMaterial.SetColor("_CenterColor", color);
            }
        }

        private void SetEdgeColor(Color color)
        {
            if (_bgColorMaterial != null)
            {
                _bgColorMaterial.SetColor("_EdgeColor", color);
            }
        }
    }
}