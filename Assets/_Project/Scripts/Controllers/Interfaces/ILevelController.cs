using System.Threading.Tasks;

namespace Delta.ProjectName
{
    public interface ILevelController
    {
        string CurrentLevelId { get; }
        bool IsStarted { get; }
        bool IsPaused { get; }
        bool IsOver { get; }

        event System.Action OnLevelStarted;
        event System.Action OnLevelPaused;
        event System.Action OnLevelResumed;
        event System.Action OnLevelWin;
        event System.Action OnLevelLose;

        Task<bool> LoadLevelAsync(string levelId);
        void SetupLevel(LevelConfig config);
        void StartLevel();
        void PauseLevel();
        void ResumeLevel();
        void WinLevel();
        void LoseLevel();
        void RestartLevel();
    }
}
