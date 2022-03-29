namespace Pool.Services;
public interface ISpotlights
{
    bool IsOn { get; }
    void TurnOn();
    void TurnOff();
}