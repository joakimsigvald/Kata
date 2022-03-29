using Pool.Services;
using static Pool.Models.Severity;
using static Pool.Models.Command;

namespace Pool;
public class WaterTapRegulator
{
    private const double _bottomLevel = -1;
    private const double _lowLevel = -0.5;
    private const double _highLevel = 0;
    private const double _brinkLevel = 0.3;

    private readonly IWaterTap _waterTap;
    private readonly ILogger _logger;
    private readonly ActionRecord _actions;

    public WaterTapRegulator(IWaterTap waterTap, ILogger logger, ActionRecord actions)
        => (_waterTap, _logger, _actions) = (waterTap, logger, actions);

    public void HandleWaterLevelReading(double level)
    {
        if (level < _bottomLevel)
            _logger.Log(Error, "The pool is empty!!");
        else if (level >= _highLevel)
            WhenWaterLevelIsHigh(level);
        else if (_actions.TapHasBeenOpenMoreThanThreeHoursStraight())
            WhenPossibleLeakage();
        else if (TimeToFill(level))
            OpenTap();
    }

    private bool TimeToFill(double level) => level < _lowLevel && !_actions.TapWasClosedLessThanOneHourAgo();

    private void WhenPossibleLeakage()
    {
        _logger.Log(Warning, "possible leakage, water tap has been open for more than three hours");
        CloseTap();
    }

    private void WhenWaterLevelIsHigh(double level)
    {
        if (level > _brinkLevel)
            _logger.Log(Warning, "The pool is overflowing!");
        CloseTap();
    }

    private void CloseTap()
    {
        if (!_waterTap.IsOpen)
            return;
        _waterTap.Close();
        _actions.RegisterAction(TapClose);
    }

    private void OpenTap()
    {
        if (_waterTap.IsOpen)
            return;
        _waterTap.Open();
        _actions.RegisterAction(TapOpen);
    }
}