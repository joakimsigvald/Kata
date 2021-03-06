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
        protected override void Given() => WaterLevel = -0.51;
        [Fact] public void ThenOpenWaterTap() => Verify<IWaterTap>(tap => tap.Open());
    }

    public class GivenLevelIsLowAndTapIsOpen : WhenHandleWaterLevelReading
    {
        protected override void Given() => (WaterLevel, WaterTapIsOpen) = (-0.6, true);
        [Fact] public void ThenDoNotOpenIt() => Verify<IWaterTap>(tap => tap.Open(), Times.Never);
    }

    public class GivenLevelIsHighAndTapIsOpen : WhenHandleWaterLevelReading
    {
        protected override void Given() => (WaterLevel, WaterTapIsOpen) = (0, true);
        [Fact] public void ThenCloseWaterTap() => Verify<IWaterTap>(tap => tap.Close());
    }

    public class GivenLevelIsHighAndTapIsClosed : WhenHandleWaterLevelReading
    {
        protected override void Given() => WaterLevel = 0.1;
        [Fact] public void ThenDoNotCloseIt() => Verify<IWaterTap>(tap => tap.Close(), Times.Never);
    }

    public class GivenLevelIsTooHigh : WhenHandleWaterLevelReading
    {
        protected override void Given() => WaterLevel = 0.31;
        [Fact] public void ThenLogWarning() => VerifyLog(Warning, "pool is overflowing");
    }

    public class GivenLevelIsTooLow : WhenHandleWaterLevelReading
    {
        protected override void Given() => WaterLevel = -1.01;
        [Fact] public void ThenLogError() => VerifyLog(Error, "pool is empty");
    }

    public class GivenWaterTapWasRecentlyClosed_And_WaterLevelIslow : WhenHandleWaterLevelReading
    {
        protected override void Given() => (WaterLevel, WaterTapIsOpen) = (0.1, true); // Trigger turn off
        public GivenWaterTapWasRecentlyClosed_And_WaterLevelIslow() => ActAgain(59, false, -0.6);
        [Fact] public void ThenDoNotOpenIt() => Verify<IWaterTap>(tap => tap.Open(), Times.Never);
    }

    public class GivenWaterTapHasBeenOffOneHour_And_WaterLevelIslow : WhenHandleWaterLevelReading
    {
        protected override void Given() => (WaterLevel, WaterTapIsOpen) = (0.1, true); // Trigger turn off
        public GivenWaterTapHasBeenOffOneHour_And_WaterLevelIslow() => ActAgain(61, false, -0.6);
        [Fact] public void ThenOpenIt() => Verify<IWaterTap>(tap => tap.Open());
    }

    public class GivenWaterTapHasBeenOpenThreeHours : WhenHandleWaterLevelReading
    {
        protected override void Given() => WaterLevel = -0.6; // Trigger turn on
        public GivenWaterTapHasBeenOpenThreeHours() => ActAgain(181, true);
        [Fact] public void ThenCloseIt() => Verify<IWaterTap>(tap => tap.Close());
        [Fact] public void ThenLogWarning() => VerifyLog(Warning, "possible leakage");
    }

    private void ActAgain(int inMinutes, bool tapIsOpen, double? level = null)
    {
        WaterTapIsOpen = tapIsOpen;
        WaterLevel = level ?? WaterLevel;
        ActAgain(inMinutes);
    }
}