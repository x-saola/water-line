using UnityEngine;

namespace Delta.Modules.Racing
{
    [CreateAssetMenu(fileName = "OpponentNameList", menuName = "Racing/Opponent Name List")]
    public class OpponentNameList : ScriptableObject
    {
        [Header("Opponent Names Pool")]
        [Tooltip("List of names to randomly assign to AI opponents")]
        public string[] opponentNames = new string[]
        {
            "Alex", "Jordan", "Taylor", "Morgan", "Casey",
            "Riley", "Avery", "Quinn", "Drew", "Cameron",
            "Jamie", "Skyler", "Reese", "Dakota", "Parker",
            "Sage", "Phoenix", "River", "Hunter", "Bailey",
            "Rowan", "Charlie", "Blake", "Dylan", "Emerson",
            "Finley", "Hayden", "Justice", "Kai", "Logan"
        };

        [Header("Avatar Settings")]
        [Tooltip("Number of available avatar sprites")]
        public int avatarCount = 10;

        public string GetRandomName()
        {
            if (opponentNames == null || opponentNames.Length == 0)
                return "Opponent";
            
            return opponentNames[Random.Range(0, opponentNames.Length)];
        }

        public int GetRandomAvatarIndex()
        {
            return Random.Range(0, avatarCount);
        }
    }
}

