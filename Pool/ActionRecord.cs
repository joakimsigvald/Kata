using Pool.Models;
using Pool.Services;
using static Pool.Models.Command;

namespace Pool;
public class ActionRecord
{
    private readonly ITime _time;
    private readonly Stack<Models.Action> _actions = new();

    public ActionRecord(ITime time) => _time = time;

    public bool LoggedWaterClearLastHour()
        => _actions.Any(action => action.TimeStamp > _time.Current.AddHours(-1) && action.Command == LogWaterClear);

    public bool TapWasClosedLessThanOneHourAgo()
        => _actions.Any(action => action.Command == TapClose && _time.Current < action.TimeStamp.AddHours(1));

    public bool TapHasBeenOpenMoreThanThreeHoursStraight()
        => HasBeenActiveAtLeast(TapOpen, TapClose, TimeSpan.FromHours(3));

    public bool PumpHasBeenOnMoreThanTwoHoursStraight()
        => HasBeenActiveAtLeast(PumpOn, PumpOff, TimeSpan.FromHours(2));

    public void RegisterAction(Command command)
        => _actions.Push(new Models.Action { TimeStamp = _time.Current, Command = command });

    private bool HasBeenActiveAtLeast(Command activate, Command deactivate, TimeSpan period)
    {
        var lastActivation = _actions
            .TakeWhile(action => action.Command != deactivate)
            .FirstOrDefault(action => action.Command == activate);
        return lastActivation.Command != default && _time.Current > lastActivation.TimeStamp + period;
    }
}