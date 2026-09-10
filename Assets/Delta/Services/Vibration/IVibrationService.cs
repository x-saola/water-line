namespace Delta.Services
{
    public interface IVibrationService
    {
        bool IsOpen { get; set; }
        void LightImpact();
        void MediumImpact();
        void HeavyImpact();
    }
}
