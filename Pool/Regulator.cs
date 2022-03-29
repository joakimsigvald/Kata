using Pool.Models;
using Pool.Services;
using static Pool.Models.Severity;
using static Pool.Models.WaterQuality;
using static Pool.Models.LightCondition;
using static Pool.Models.Command;

namespace Pool;
public class Regulator
{
    private const double _bottomLevel = -1;
    private const double _lowLevel = -0.5;

    private const double _highLevel = 0;
    private const double _brinkLevel = 0.3;

    private readonly IWaterTap _waterTap;
    private readonly IWaterPump _waterPump;
    private readonly ISpotlights _spotlights;
    private readonly ILogger _logger;
    private readonly ITime _time;
    private readonly Stack<Models.Action> _actions = new();

    public Regulator(IWaterTap waterTap, IWaterPump waterPump, ISpotlights spotlights, ILogger logger, ITime time)
        => (_waterTap, _waterPump, _spotlights, _logger, _time) = (waterTap, waterPump, spotlights, logger, time);

    public void HandleWaterLevelReading(double level)
    {
        if (level < _bottomLevel)
            _logger.Log(Error, "The pool is empty!!");
        else if (level >= _highLevel)
            WhenWaterLevelIsHigh(level);
        else if (TapHasBeenOpenMoreThanThreeHoursStraight())
            WhenPossibleLeakage();
        else if (TimeToFill(level))
            OpenTap();
    }

    public void HandleAmbientLightReading(double lightLevel)
    {
        //var lightCondition = MapToLightCondition(lightLevel);
        switch (MapToLightCondition(lightLevel)) {
            case PitchBlack:
                if (_spotlights.IsOn)
                    _spotlights.TurnOff();
                break;
            case Dim:
                if (!_spotlights.IsOn)
                    _spotlights.TurnOn();
                break;
            case Bright:
                if (_spotlights.IsOn)
                    _spotlights.TurnOff();
                break;
        }
    }

    public void HandleWaterTransparencyReading(double transparency)
    {
        var quality = MapToWaterQuality(transparency);
        if (quality == Crystal)
            TurnOffPump();
        else if (quality == Clear)
            WhenWaterIsClear();
        else if (quality < Fair)
            WhenWaterIsLessThanFair(quality);
    }

    private bool TimeToFill(double level) => level < _lowLevel && !TapWasClosedLessThanOneHourAgo();

    private void WhenPossibleLeakage()
    {
        _logger.Log(Warning, "possible leakage, water tap has been open for more than three hours");
        CloseTap();
    }

    private void WhenWaterIsClear()
    {
        if (LoggedWaterClearLastHour())
            return;
        _logger.Log(Normal, "The water is clear again.");
        RegisterAction(LogWaterClear);
    }

    private void WhenWaterIsLessThanFair(WaterQuality quality)
    {
        if (quality == Poor)
            _logger.Log(Warning, "The water is muddy!");
        else if (quality == Critical)
            _logger.Log(Error, "The water is very muddy!!");
        if (!_waterPump.IsOn)
            TurnOnPump();
        else if (PumpHasBeenOnMoreThanTwoHoursStraight())
            TurnOffPump();
    }

    private bool LoggedWaterClearLastHour()
        => _actions.Any(action => action.TimeStamp > _time.Current.AddHours(-1) && action.Command == LogWaterClear);

    private WaterQuality MapToWaterQuality(double transparency)
        => transparency <= 0.5 ? Critical
        : transparency <= 0.6 ? Poor
        : transparency <= 0.7 ? Low
        : transparency < 0.8 ? Fair
        : transparency < 0.9 ? Clear
        : Crystal;

    private LightCondition MapToLightCondition(double lightLevel)
        => lightLevel < 0.1 ? PitchBlack
        : lightLevel < 0.2 ? Dark
        : lightLevel < 1 ? Dim
        : lightLevel < 2 ? Light
        : Bright;

    private void WhenWaterLevelIsHigh(double level)
    {
        if (level > _brinkLevel)
            _logger.Log(Warning, "The pool is overflowing!");
        CloseTap();
    }

    private bool TapWasClosedLessThanOneHourAgo()
        => _actions.Any(action => action.Command == TapClose && _time.Current < action.TimeStamp.AddHours(1));

    private bool TapHasBeenOpenMoreThanThreeHoursStraight()
        => HasBeenActiveAtLeast(TapOpen, TapClose, TimeSpan.FromHours(3));

    private bool PumpHasBeenOnMoreThanTwoHoursStraight()
        => HasBeenActiveAtLeast(PumpOn, PumpOff, TimeSpan.FromHours(2));

    private bool HasBeenActiveAtLeast(Command activate, Command deactivate, TimeSpan period)
    {
        var lastActivation = _actions
            .TakeWhile(action => action.Command != deactivate)
            .FirstOrDefault(action => action.Command == activate);
        return lastActivation.Command != default && _time.Current > lastActivation.TimeStamp + period;
    }

    private void CloseTap()
    {
        if (!_waterTap.IsOpen)
            return;
        _waterTap.Close();
        RegisterAction(TapClose);
    }

    private void OpenTap()
    {
        if (_waterTap.IsOpen)
            return;
        _waterTap.Open();
        RegisterAction(TapOpen);
    }

    private void TurnOffPump()
    {
        if (!_waterPump.IsOn)
            return;
        _waterPump.TurnOff();
        RegisterAction(PumpOff);
    }

    private void TurnOnPump()
    {
        if (_waterPump.IsOn)
            return;
        _waterPump.TurnOn();
        RegisterAction(PumpOn);
    }

    private void RegisterAction(Command command)
        => _actions.Push(new Models.Action { TimeStamp = _time.Current, Command = command });
}