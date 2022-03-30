using Pool.Models;
using Pool.Services;
using static Pool.Models.LightCondition;

namespace Pool;
public class SpotlightsRegulator
{
    private readonly ISpotlights _spotlights;

    public SpotlightsRegulator(ISpotlights spotlights) => _spotlights = spotlights;

    public void HandleSpotlights(double lightLevel)
    {
        switch (MapToLightCondition(lightLevel)) {
            case PitchBlack:
            case Bright:
                if (_spotlights.IsOn)
                    _spotlights.TurnOff();
                break;
            case Dim:
                if (!_spotlights.IsOn)
                    _spotlights.TurnOn();
                break;
        }
    }

    private static LightCondition MapToLightCondition(double lightLevel)
        => lightLevel < 0.1 ? PitchBlack
        : lightLevel < 0.2 ? Dark
        : lightLevel < 1 ? Dim
        : lightLevel < 2 ? Light
        : Bright;
}