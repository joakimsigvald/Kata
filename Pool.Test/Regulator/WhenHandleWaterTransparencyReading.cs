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
        protected override void Given() => Transparency = 0.81;
        public GivenWaterQualityGoFromFairToClearSecondTimeInOneHour()
        {
            //Water clear has been logged once
            Transparency = 0.75;
            CurrentTime += TimeSpan.FromMinutes(20);
            Setup();
            //A reading that water is no longer clear is received
            Act();
            Transparency = 0.85;
            CurrentTime += TimeSpan.FromMinutes(20);
            Setup();
            //Water is now read as clear again
            Act();
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
        public GivenWaterPumpWasRecentlyTurnedOff_And_WaterQualityIsLessThanFair()
        {
            Transparency = 0.1;
            CurrentTime += TimeSpan.FromMinutes(59);
            Setup();
            Act();
        }

        [Fact] public void ThenDoNotTurnItOn() => Verify<IWaterPump>(pump => pump.TurnOn(), Times.Never);
    }

    public class GivenWaterPumpHasBeenOffLongEnough_And_WaterQualityIsLessThanFair : WhenHandleWaterTransparencyReading
    {
        protected override void Given() => (Transparency, WaterPumpIsOn) = (0.95, true);

        public GivenWaterPumpHasBeenOffLongEnough_And_WaterQualityIsLessThanFair()
        {
            WaterPumpIsOn = false;
            Transparency = 0.3;
            CurrentTime += TimeSpan.FromMinutes(61);
            Setup();
            Act();
        }

        [Fact] public void ThenTurnItOn() => Verify<IWaterPump>(pump => pump.TurnOn());
    }

    public class GivenWaterPumpHasBeenOpenForMoreThanTwoHours : WhenHandleWaterTransparencyReading
    {
        protected override void Given() => Transparency = 0.55;

        public GivenWaterPumpHasBeenOpenForMoreThanTwoHours()
        {
            WaterPumpIsOn = true;
            CurrentTime += TimeSpan.FromMinutes(121);
            Setup();
            Act();
        }

        [Fact] public void ThenTurnItOff() => Verify<IWaterPump>(pump => pump.TurnOff());
    }
}