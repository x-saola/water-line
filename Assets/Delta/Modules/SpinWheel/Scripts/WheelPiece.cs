using UnityEngine;

namespace Delta.Modules.SpinWheel
{
   [System.Serializable]
   public class WheelPiece
   {
      public int id;
      
      [Tooltip("Reward data for this wheel piece")]
      public SpinWheelReward reward;
      
      [Tooltip("Probability in %")]
      [Range(0f, 100f)]
      public float Chance = 100f;

      [HideInInspector] public int Index;
      [HideInInspector] public double weight = 0f;

      [Space, Header("Background")]
      [Tooltip("Background color of the piece")]
      public Color CenterColor = Color.white;
      public Color EdgeColor = Color.white;

      public int ID => id;
   }
}
