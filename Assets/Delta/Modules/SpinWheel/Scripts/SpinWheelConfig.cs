using System.Collections;
using System.Collections.Generic;
using Delta.Modules.SpinWheel;
using UnityEngine;

[CreateAssetMenu(fileName = "SpinWheelConfig", menuName = "SpinWheel/Config", order = 1)]
public class SpinWheelConfig : ScriptableObject
{
    [Tooltip("Level required to unlock the spin wheel feature")]
    public int unlockLevel = 10;
    
    [Tooltip("Array of wheel pieces/rewards")]
    public WheelPiece[] wheelPieces;
    
    public WheelPiece[] GetWheelPieces()
    {
        return wheelPieces;
    }
}
