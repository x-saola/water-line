using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ClueMaster
{
    public class MiniGamesConfig
    {
        public List<MiniGameConfig> AvailableMiniGames;
    }

    public class MiniGameConfig
    {
        public string MiniGameMode;
        public string IntroduceIconPath;
        public string IntroduceText;
        public string TutorialText;
    }
}
