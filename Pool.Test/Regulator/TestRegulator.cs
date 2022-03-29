using Applique.WhenGivenThen.Core;
using Moq;
using Pool.Models;
using Pool.Services;

namespace Pool.Test.Regulator;
public abstract class TestRegulator<TResult> : TestSubject<Pool.Regulator, TResult>
{
    protected DateTime CurrentTime = DateTime.Now;
    protected bool WaterTapIsOpen = false;
    protected bool WaterPumpIsOn = false;
    protected bool SpotlightsIsOn = false;

    public TestRegulator() => ArrangeAndAct();

    protected override Pool.Regulator CreateSUT()
    {
        var actions = new ActionRecord(MockOf<ITime>());
        var tap = new WaterTapRegulator(MockOf<IWaterTap>(), MockOf<ILogger>(), actions);
        var pump = new WaterPumpRegulator(MockOf<IWaterPump>(), MockOf<ILogger>(), actions);
        var spots = new SpotlightsRegulator(MockOf<ISpotlights>());
        return new Pool.Regulator(tap, pump, spots);
    }

    protected override void Setup()
    {
        Mocked<IWaterTap>().Setup(tap => tap.IsOpen).Returns(WaterTapIsOpen);
        Mocked<IWaterPump>().Setup(pump => pump.IsOn).Returns(WaterPumpIsOn);
        Mocked<ISpotlights>().Setup(spots => spots.IsOn).Returns(SpotlightsIsOn);
        Mocked<ITime>().Setup(time => time.Current).Returns(CurrentTime);
    }

    protected void VerifyLog(Severity severity, string message, Times times = default)
        => Verify<ILogger>(logger => logger.Log(severity, It.Is<string>(
                msg => msg.ToLower().Contains(message))), times);
}