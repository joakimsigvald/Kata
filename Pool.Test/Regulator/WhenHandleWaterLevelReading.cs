using Moq;
using Pool.Services;
using Xunit;
using static Pool.Models.Severity;

namespace Pool.Test.Regulator;
public abstract class WhenHandleWaterLevelReading : TestRegulator<float>
{
    protected double WaterLevel;

    protected override void Act() => SUT.HandleWaterLevelReading(WaterLevel);

    public class GivenLevelIsLow : WhenHandleWaterLevelReading
    {
        protected override void Given() => WaterLevel = -0.5;
        [Fact] public void ThenOpenWaterTap() => Verify<IWaterTap>(tap => tap.Open());
    }

    public class GivenWaterTapIsOpenAndWaterLevelIsLow : WhenHandleWaterLevelReading
    {
        protected override void Given() => (WaterLevel, WaterTapIsOpen) = (-0.6, true);
        [Fact] public void ThenDoNotOpenIt() => Verify<IWaterTap>(tap => tap.Open(), Times.Never);
    }

    public class GivenLevelIsHighAndTapIsOpen : WhenHandleWaterLevelReading
    {
        protected override void Given() => (WaterLevel, WaterTapIsOpen) = (0, true);
        [Fact] public void ThenCloseWaterTap() => Verify<IWaterTap>(tap => tap.Close());
    }

    public class GivenWaterTapIsClosedAndWaterLevelIsHigh : WhenHandleWaterLevelReading
    {
        protected override void Given() => (WaterLevel, WaterTapIsOpen) = (0.1, false);
        [Fact] public void ThenDoNotCloseIt() => Verify<IWaterTap>(tap => tap.Close(), Times.Never);
    }

    public class GivenLevelIsTooHigh : WhenHandleWaterLevelReading
    {
        protected override void Given() => WaterLevel = 0.31;
        [Fact] public void ThenLogError() => VerifyLog(Warning, "pool is overflowing");
    }

    public class GivenLevelIsTooLow : WhenHandleWaterLevelReading
    {
        protected override void Given() => WaterLevel = -1.01;
        [Fact] public void ThenLogError() => VerifyLog(Error, "pool is empty");
    }

    public class GivenWaterTapWasRecentlyClosedAndWaterLevelIslow : WhenHandleWaterLevelReading
    {
        protected override void Given() => (WaterLevel, WaterTapIsOpen) = (0.1, true);
        [Fact]
        public void ThenDoNotOpenIt()
        {
            WaterLevel = -0.6;
            CurrentTime += TimeSpan.FromMinutes(59);
            Setup();
            Act();
            Verify<IWaterTap>(tap => tap.Open(), Times.Never);
        }
    }

    public class GivenWaterTapWasClosedMoreThanOneHourAgoAndWaterLevelIslow : WhenHandleWaterLevelReading
    {
        protected override void Given() => WaterLevel = 0.1;

        public GivenWaterTapWasClosedMoreThanOneHourAgoAndWaterLevelIslow()
        {
            WaterLevel = -0.6;
            CurrentTime += TimeSpan.FromMinutes(61);
            Setup();
            Act();
        }

        [Fact] public void ThenOpenIt() => Verify<IWaterTap>(tap => tap.Open());
    }

    public class GivenWaterTapHasBeenOpenForMoreThanThreeHours : WhenHandleWaterLevelReading
    {
        protected override void Given() => WaterLevel = -0.6;

        public GivenWaterTapHasBeenOpenForMoreThanThreeHours()
        {
            WaterTapIsOpen = true;
            CurrentTime += TimeSpan.FromMinutes(181);
            Setup();
            Act();
        }

        [Fact] public void ThenCloseIt() => Verify<IWaterTap>(tap => tap.Close());

        [Fact] public void ThenLogWarning() => VerifyLog(Warning, "possible leakage");
    }
}