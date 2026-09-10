namespace Delta.Services
{
    public interface IAudioService
    {
        bool IsSfxEnable { get; set; }
        bool IsMusicEnable { get; set; }
        bool IsMusicPaused { get; }
        void PlaySfx(string audioId);
        void PlayBackgroundMusic(string audioId);
        void StopBackgroundMusic();
        void PauseBackgroundMusic();
        void ResumeBackgroundMusic();
    }
}
