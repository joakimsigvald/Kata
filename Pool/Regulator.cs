using Pool.Models;
using Pool.Services;
using static Pool.Services.Severity;

namespace Pool
{
    public class Regulator
    {
        private readonly IWaterTap _waterTap;
        private readonly IWaterIndicator _waterIndicator;
        private readonly ILogger _logger;
        private readonly ITime _time;
        private readonly Stack<Models.Action> _actions = new();

        public Regulator(IWaterTap waterTap, IWaterIndicator waterIndicator, ILogger logger, ITime time)
        {
            _waterTap = waterTap;
            _waterIndicator = waterIndicator;
            _logger = logger;
            _time = time;
        }

        public void CheckWaterLevel()
        {
            var level = _waterIndicator.Level;
            if (level < -1.5)
                _logger.Log(Error, "Water level too low");
            else if (level >= 0)
                WhenWaterLevelIsHigh(level);
            else if (level <= -0.5)
                WhenWaterLevelIsLow();
        }

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
            _actions.Push(new Models.Action { TimeStamp = _time.Current, Command = Command.CloseTap });
        }

        private void OpenTap()
        {
            if (_waterTap.IsOpen)
                return;
            _waterTap.Open();
            _actions.Push(new Models.Action { TimeStamp = _time.Current, Command = Command.OpenTap });
        }
    }
}