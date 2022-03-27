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
            if (level < -1.5)
                _logger.Log(Error, "Water level too low");
            else if (level >= 0)
                WhenWaterLevelIsHigh(level);
            else if (level <= -0.5)
                WhenWaterLevelIsLow();
        }

        public void HandleWaterTransparencyReading(double transparency)
        {
            switch (MapToWaterQuality(transparency))
            {
                case Crystal:
                    if (_waterPump.IsOn)
                        _waterPump.TurnOff();
                    break;
                case Clear:
                    if (!LoggedWaterClearLastHour())
                    {
                        _logger.Log(Normal, "Water quality ok again");
                        RegisterAction(LogWaterClear);
                    }
                    break;
                case Fair:
                    return;
                case Low:
                    if (!_waterPump.IsOn)
                        _waterPump.TurnOn();
                    break;
                default: throw new NotImplementedException();
            }
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
                _logger.Log(Error, "Water level too high");
            CloseTap();
        }

        private void WhenWaterLevelIsLow()
        {
            if (TapWasClosedLessThanOneHourAgo())
                return;
            if (TapWasOpenedMoreThanThreeHoursAgo())
            {
                _logger.Log(Warning, "possible leakage, water tap has been open for more than three hours");
                CloseTap();
                return;
            }
            OpenTap();
        }

        private bool TapWasClosedLessThanOneHourAgo()
            => _actions.Any(action => action.Command == Command.CloseTap && action.TimeStamp > _time.Current.AddHours(-1));

        private bool TapWasOpenedMoreThanThreeHoursAgo()
        {
            var lastOpened = _actions
                    .TakeWhile(action => action.Command != Command.CloseTap)
                    .FirstOrDefault(action => action.Command == Command.OpenTap);
            return lastOpened.Command != default && lastOpened.TimeStamp < _time.Current.AddHours(-3);
        }

        private void CloseTap()
        {
            if (!_waterTap.IsOpen)
                return;
            _waterTap.Close();
            RegisterAction(Command.CloseTap);
        }

        private void OpenTap()
        {
            if (_waterTap.IsOpen)
                return;
            _waterTap.Open();
            RegisterAction(Command.OpenTap);
        }

        private void RegisterAction(Command command)
            => _actions.Push(new Models.Action { TimeStamp = _time.Current, Command = command });
    }
}