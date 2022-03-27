namespace Pool
{
    public class Regulator
    {
        private readonly IWaterTap _waterTap;
        private readonly IWaterIndicator _waterIndicator;
        private readonly ILogger _logger;
        private readonly ITime _time;
        private readonly Stack<Action> _actions = new();

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
            if (level > 0.3)
                _logger.Error("Water level too high");
            if (level >= 0)
                CloseTap();
            else if (level <= -0.5)
                OpenTap();
            if (level < -1.5)
                _logger.Error("Water level too low");
        }

        private void OpenTap()
        {
            if (_actions.Any(action => action.Command == Command.CloseTap && action.TimeStamp > _time.Current.AddHours(-1)))
                return;
            _waterTap.Open();
            _actions.Push(new Action { TimeStamp = _time.Current, Command = Command.OpenTap });
        }

        private void CloseTap()
        {
            _waterTap.Close();
            _actions.Push(new Action { TimeStamp = _time.Current, Command = Command.CloseTap });
        }
    }
}