using Moq;
using Pool.Services;
using Xunit;
using static Pool.Models.Severity;

namespace Pool.Test.Regulator;
public abstract class WhenHandleWaterTransparencyReading : TestRegulator<float>
{
    protected double Transparency;
    protected override void Act() => SUT.HandleWaterTransparencyReading(Transparency);

    public class GivenWaterIsCrystalClearAndWaterPumpIsOn : WhenHandleWaterTransparencyReading
    {
        protected override void Given() => (Transparency, WaterPumpIsOn) = (0.9, true);
        [Fact] public void ThenWaterPumpIsTurnedOff() => Verify<IWaterPump>(pump => pump.TurnOff());
    }

    public class GivenWaterIsCrystalClearAndWaterPumpIsOff : WhenHandleWaterTransparencyReading
    {
        protected override void Given() => (Transparency, WaterPumpIsOn) = (0.9, false);
        [Fact] public void ThenDoNothing() => Verify<IWaterPump>(pump => pump.TurnOff(), Times.Never);
    }

    public class GivenWaterQualityIsLowAndWaterPumpIsOff : WhenHandleWaterTransparencyReading
    {
        protected override void Given() => (Transparency, WaterPumpIsOn) = (0.7, false);
        [Fact] public void ThenWaterPumpIsTurnedOn() => Verify<IWaterPump>(pump => pump.TurnOn());
    }

    public class GivenWaterQualityIsLowAndWaterPumpIsOn : WhenHandleWaterTransparencyReading
    {
        protected override void Given() => (Transparency, WaterPumpIsOn) = (0.7, true);
        [Fact] public void ThenDoNothing() => Verify<IWaterPump>(pump => pump.TurnOn(), Times.Never);
    }

    public class GivenWaterQualityGoFromFairToClearFirstTimeInOneHour : WhenHandleWaterTransparencyReading
    {
        protected override void Given() => Transparency = 0.8;
        [Fact] public void ThenLogWaterClear() => VerifyLog(Normal, "water is clear");
    }

    public class GivenWaterQualityGoFromFairToClearSecondTimeInOneHour : WhenHandleWaterTransparencyReading
    {
        private const double _clear = 0.85;
        private const double _lessThanClear = 0.75;

        protected override void Given() => Transparency = _clear;

        public GivenWaterQualityGoFromFairToClearSecondTimeInOneHour()
        {
            ActAgain(20, false, _lessThanClear);
            ActAgain(20, false, _clear);
        }

        [Fact]
        public void ThenDoNotLogAgain()
            => Verify<ILogger>(logger => logger.Log(Normal, It.IsAny<string>()), Times.Once);
    }

    public class GivenWaterQualityIsPoor : WhenHandleWaterTransparencyReading
    {
        protected override void Given() => (Transparency, WaterPumpIsOn) = (0.55, true);
        [Fact] public void ThenLogWarning() => VerifyLog(Warning, "water is muddy");
    }

    public class GivenWaterQualityIsCritical : WhenHandleWaterTransparencyReading
    {
        protected override void Given() => (Transparency, WaterPumpIsOn) = (0.45, true);
        [Fact] public void ThenLogError() => VerifyLog(Error, "water is very muddy");
    }

    public class GivenWaterPumpWasRecentlyTurnedOff_And_WaterQualityIsLessThanFair : WhenHandleWaterTransparencyReading
    {
        protected override void Given() => (Transparency, WaterPumpIsOn) = (0.95, true);
        public GivenWaterPumpWasRecentlyTurnedOff_And_WaterQualityIsLessThanFair() => ActAgain(59, false, 0.1);
        [Fact] public void ThenDoNotTurnItOn() => Verify<IWaterPump>(pump => pump.TurnOn(), Times.Never);
    }

    public class GivenWaterPumpHasBeenOffLongEnough_And_WaterQualityIsLessThanFair : WhenHandleWaterTransparencyReading
    {
        protected override void Given() => (Transparency, WaterPumpIsOn) = (0.95, true);
        public GivenWaterPumpHasBeenOffLongEnough_And_WaterQualityIsLessThanFair() => ActAgain(61, false, 0.3);
        [Fact] public void ThenTurnItOn() => Verify<IWaterPump>(pump => pump.TurnOn());
    }

    public class GivenWaterPumpHasBeenOpenForMoreThanTwoHours : WhenHandleWaterTransparencyReading
    {
        protected override void Given() => Transparency = 0.55;
        public GivenWaterPumpHasBeenOpenForMoreThanTwoHours() => ActAgain(121, true, 0.85);
        [Fact] public void ThenTurnItOff() => Verify<IWaterPump>(pump => pump.TurnOff());
    }

    private void ActAgain(int inMinutes, bool pumpIsOn, double? transparency = null)
    {
        WaterPumpIsOn = pumpIsOn;
        Transparency = transparency ?? Transparency;
        ActAgain(inMinutes);
    }
}