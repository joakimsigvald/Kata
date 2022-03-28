using Applique.WhenGivenThen.Core;
using Moq;
using Pool.Models;
using Pool.Services;

namespace Pool.Test.Regulator
{
    public abstract class TestRegulator<TResult> : TestSubject<Pool.Regulator, TResult>
    {
        protected DateTime CurrentTime = DateTime.Now;
        protected bool WaterTapIsOpen = false;
        protected bool WaterPumpIsOn = false;

        public TestRegulator() => ArrangeAndAct();

        protected override Pool.Regulator CreateSUT()
            => new(MockOf<IWaterTap>(), MockOf<IWaterPump>(), MockOf<ILogger>(), MockOf<ITime>());

        protected override void Setup()
        {
            Mocked<IWaterTap>().Setup(tap => tap.IsOpen).Returns(WaterTapIsOpen);
            Mocked<IWaterPump>().Setup(pump => pump.IsOn).Returns(WaterPumpIsOn);
            Mocked<ITime>().Setup(time => time.Current).Returns(CurrentTime);
        }

        protected void VerifyLog(Severity severity, string message, Times times = default)
            => Verify<ILogger>(logger => logger.Log(severity, It.Is<string>(
                    msg => msg.ToLower().Contains(message))), times);
    }
}