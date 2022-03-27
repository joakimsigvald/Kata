namespace Pool.Services
{
    public interface IWaterPump
    {
        bool IsOn { get; }
        void TurnOff();
        void TurnOn();
    }
}