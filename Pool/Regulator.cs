using Pool.Models;
using Pool.Services;
using static Pool.Models.Severity;
using static Pool.Models.WaterQuality;
using static Pool.Models.Command;

namespace Pool
{
    public class Regulator
    {
        private readonly IWaterTap _waterTap;
        private readonly IWaterPump _waterPump;
        private readonly ILogger _logger;
        private readonly ITime _time;
        private readonly Stack<Models.Action> _actions = new();

        public Regulator(IWaterTap waterTap, IWaterPump waterPump, ILogger logger, ITime time)
        {
            _waterTap = waterTap;
            _waterPump = waterPump;
            _logger = logger;
            _time = time;
        }

        public void HandleWaterLevelReading(double level)
        {
            if (level < -1)
                _logger.Log(Error, "The pool is empty!!");
            else if (level >= 0)
                WhenWaterLevelIsHigh(level);
            else if (level <= -0.5)
                WhenWaterLevelIsLow();
        }

        public void HandleWaterTransparencyReading(double transparency)
            => GetWaterQualityHandler(MapToWaterQuality(transparency))();

        private System.Action GetWaterQualityHandler(WaterQuality quality)
            => quality switch
            {
                Crystal => TurnOffPump,
                Clear => HandleClearWaterQuality,
                WaterQuality it when it < Fair => () => HandleLowWaterQuality(quality),
                _ => () => { }
            };

        private void HandleLowWaterQuality(WaterQuality quality)
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

        private void HandleClearWaterQuality()
        {
            if (LoggedWaterClearLastHour())
                return;
            _logger.Log(Normal, "The water is clear again.");
            RegisterAction(LogWaterClear);
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

        private void WhenWaterLevelIsHigh(double level)
        {
            if (level > 0.3)
                _logger.Log(Warning, "The pool is overflowing!");
            CloseTap();
        }

        private void WhenWaterLevelIsLow()
        {
            if (TapWasClosedLessThanOneHourAgo())
                return;
            if (TapHasBeenOpenMoreThanThreeHoursStraight())
            {
                _logger.Log(Warning, "possible leakage, water tap has been open for more than three hours");
                CloseTap();
                return;
            }
            OpenTap();
        }

        private bool TapWasClosedLessThanOneHourAgo()
            => _actions.Any(action => action.Command == TapClose && action.TimeStamp > _time.Current.AddHours(-1));

        private bool TapHasBeenOpenMoreThanThreeHoursStraight()
        {
            var lastOpened = _actions
                .TakeWhile(action => action.Command != TapClose)
                .FirstOrDefault(action => action.Command == TapOpen);
            return lastOpened.Command != default && lastOpened.TimeStamp < _time.Current.AddHours(-3);
        }

        private bool PumpHasBeenOnMoreThanTwoHoursStraight()
        {
            var lastOn = _actions
                .TakeWhile(action => action.Command != PumpOff)
                .FirstOrDefault(action => action.Command == PumpOn);
            return lastOn.Command != default && lastOn.TimeStamp < _time.Current.AddHours(-2);
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
}