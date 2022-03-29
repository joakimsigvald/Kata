using Pool.Models;
using Pool.Services;
using static Pool.Models.Severity;
using static Pool.Models.WaterQuality;
using static Pool.Models.Command;

namespace Pool;
public class WaterPumpRegulator
{
    private readonly IWaterPump _waterPump;
    private readonly ILogger _logger;
    private readonly ActionRecord _actions;

    public WaterPumpRegulator(IWaterPump waterPump, ILogger logger, ActionRecord actions)
        => (_waterPump, _logger, _actions) = (waterPump, logger, actions);

    public void HandleWaterPump(double transparency)
    {
        var quality = MapToWaterQuality(transparency);
        if (quality == Crystal)
            TurnOffPump();
        else if (quality == Clear)
            WhenWaterIsClear();
        else if (quality < Fair)
            WhenWaterIsLessThanFair(quality);
    }

    private void WhenWaterIsClear()
    {
        if (_actions.LoggedWaterClearLastHour())
            return;
        _logger.Log(Normal, "The water is clear again.");
        _actions.RegisterAction(LogWaterClear);
    }

    private void WhenWaterIsLessThanFair(WaterQuality quality)
    {
        if (quality == Poor)
            _logger.Log(Warning, "The water is muddy!");
        else if (quality == Critical)
            _logger.Log(Error, "The water is very muddy!!");
        if (!_waterPump.IsOn)
            TurnOnPump();
        else if (_actions.PumpHasBeenOnMoreThanTwoHoursStraight())
            TurnOffPump();
    }

    private static WaterQuality MapToWaterQuality(double transparency)
        => transparency <= 0.5 ? Critical
        : transparency <= 0.6 ? Poor
        : transparency <= 0.7 ? Low
        : transparency < 0.8 ? Fair
        : transparency < 0.9 ? Clear
        : Crystal;

    private void TurnOffPump()
    {
        if (!_waterPump.IsOn)
            return;
        _waterPump.TurnOff();
        _actions.RegisterAction(PumpOff);
    }

    private void TurnOnPump()
    {
        if (_waterPump.IsOn)
            return;
        _waterPump.TurnOn();
        _actions.RegisterAction(PumpOn);
    }
}