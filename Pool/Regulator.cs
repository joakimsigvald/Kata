namespace Pool;
public class Regulator
{
    private readonly WaterTapRegulator _tap;
    private readonly WaterPumpRegulator _pump;
    private readonly SpotlightsRegulator _spots;

    public Regulator(WaterTapRegulator tap, WaterPumpRegulator pump, SpotlightsRegulator spots)
        => (_tap, _pump, _spots) = (tap, pump, spots);

    public void HandleWaterLevelReading(double level) => _tap.HandleWaterLevelReading(level);

    public void HandleAmbientLightReading(double lightLevel) => _spots.HandleSpotlights(lightLevel);

    public void HandleWaterTransparencyReading(double transparency) => _pump.HandleWaterPump(transparency);
}