using Moq;
using Pool.Services;
using Xunit;
using static Pool.Models.Severity;

namespace Pool.Test.Regulator
{
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

            [Fact] public void ThenLogNormal() => VerifyLog(Normal, "water quality ok");
        }

        public class GivenWaterQualityGoFromFairToClearSecondTimeInOneHour : WhenHandleWaterTransparencyReading
        {
            protected override void Given() => Transparency = 0.81;
            public GivenWaterQualityGoFromFairToClearSecondTimeInOneHour()
            {
                Transparency = 0.75;
                CurrentTime += TimeSpan.FromMinutes(20);
                Setup();
                Act();
                Transparency = 0.85;
                CurrentTime += TimeSpan.FromMinutes(20);
                Setup();
                Act();
            }

            [Fact]
            public void ThenDoNotLogAgain()
                => Verify<ILogger>(logger => logger.Log(Normal, It.IsAny<string>()), Times.Once);
        }
    }
}