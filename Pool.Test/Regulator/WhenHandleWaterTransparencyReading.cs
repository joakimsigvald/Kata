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

        public class GivenTransparencyIsHighAndWaterPumpIsOn : WhenHandleWaterTransparencyReading
        {
            protected override void Given() => (Transparency, WaterPumpIsOn) = (0.9, true);

            [Fact] public void ThenWaterPumpIsTurnedOff() => Verify<IWaterPump>(pump => pump.TurnOff());
        }

        public class GivenTransparencyIsHighAndWaterPumpIsOff : WhenHandleWaterTransparencyReading
        {
            protected override void Given() => (Transparency, WaterPumpIsOn) = (0.9, false);

            [Fact] public void ThenDoNothing() => Verify<IWaterPump>(pump => pump.TurnOff(), Times.Never);
        }

        public class GivenTransparencyIsLowAndWaterPumpIsOff : WhenHandleWaterTransparencyReading
        {
            protected override void Given() => (Transparency, WaterPumpIsOn) = (0.7, false);

            [Fact] public void ThenWaterPumpIsTurnedOn() => Verify<IWaterPump>(pump => pump.TurnOn());
        }

        public class GivenTransparencyIsLowAndWaterPumpIsOn : WhenHandleWaterTransparencyReading
        {
            protected override void Given() => (Transparency, WaterPumpIsOn) = (0.7, true);

            [Fact] public void ThenDoNothing() => Verify<IWaterPump>(pump => pump.TurnOn(), Times.Never);
        }

        public class GivenTransparencyGoFromFairToClearFirstTimeInOneHour : WhenHandleWaterTransparencyReading
        {
            protected override void Given() => Transparency = 0.8;

            [Fact] public void ThenLogNormal() => VerifyLog(Normal, "water quality ok");
        }

        public class GivenTransparencyGoFromFairToClearSecondTimeInOneHour : WhenHandleWaterTransparencyReading
        {
            protected override void Given() => Transparency = 0.81;
            public GivenTransparencyGoFromFairToClearSecondTimeInOneHour()
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