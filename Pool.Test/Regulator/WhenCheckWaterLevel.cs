using Adlibris.B2L.Test;
using Moq;
using Pool.Services;
using Xunit;

namespace Pool.Test.Regulator
{
    public abstract class WhenCheckWaterLevel : TestSubject<Pool.Regulator, float>
    {
        protected double WaterLevel;
        protected DateTime CurrentTime = DateTime.Now;
        protected bool WaterTapIsOpen = false;
        public WhenCheckWaterLevel() => ArrangeAndAct();
        protected override Pool.Regulator CreateSUT()
            => new(MockOf<IWaterTap>(), MockOf<IWaterIndicator>(), MockOf<ILogger>(), MockOf<ITime>());

        protected override void Setup()
        {
            Mocked<IWaterIndicator>().Setup(indicator => indicator.Level).Returns(WaterLevel);
            Mocked<IWaterTap>().Setup(tap => tap.IsOpen).Returns(WaterTapIsOpen);
            Mocked<ITime>().Setup(time => time.Current).Returns(CurrentTime);
        }

        protected override void Act() => SUT.CheckWaterLevel();

        public class GivenLevelIsLow : WhenCheckWaterLevel
        {
            protected override void Given() => WaterLevel = -0.5;
            [Fact] public void ThenWaterTapIsOpen() => Verify<IWaterTap>(tap => tap.Open());
        }

        public class GivenLevelIsHighAndTapIsOpen : WhenCheckWaterLevel
        {
            protected override void Given() => (WaterLevel, WaterTapIsOpen) = (0, true);
            [Fact] public void ThenWaterTapIsClosed() => Verify<IWaterTap>(tap => tap.Close());
        }

        public class GivenLevelIsTooHigh : WhenCheckWaterLevel
        {
            protected override void Given() => WaterLevel = 0.31;
            [Fact] public void ThenLogError() => VerifyLog(Severity.Error, "water level too high");
        }

        public class GivenLevelIsTooLow : WhenCheckWaterLevel
        {
            protected override void Given() => WaterLevel = -1.51;
            [Fact] public void ThenLogError() => VerifyLog(Severity.Error, "water level too low");
        }

        public class GivenWaterTapWasRecentlyClosedAndWaterLevelIslow : WhenCheckWaterLevel
        {
            protected override void Given() => (WaterLevel, WaterTapIsOpen) = (0.1, true);
            [Fact]
            public void ThenDoNothing()
            {
                WaterLevel = -0.6;
                CurrentTime += TimeSpan.FromMinutes(59);
                Setup();
                Act();
                Verify<IWaterTap>(tap => tap.Open(), Times.Never);
            }
        }

        public class GivenWaterTapWasClosedMoreThanOneHourAgoAndWaterLevelIslow : WhenCheckWaterLevel
        {
            protected override void Given() => WaterLevel = 0.1;

            public GivenWaterTapWasClosedMoreThanOneHourAgoAndWaterLevelIslow()
            {
                WaterLevel = -0.6;
                CurrentTime += TimeSpan.FromMinutes(61);
                Setup();
                Act();
            }

            [Fact] public void ThenCloseIt() => Verify<IWaterTap>(tap => tap.Open());
        }

        public class GivenWaterTapHasBeenOpenForMoreThanThreeHours : WhenCheckWaterLevel
        {
            protected override void Given() => WaterLevel = -0.6;

            public GivenWaterTapHasBeenOpenForMoreThanThreeHours()
            {
                CurrentTime += TimeSpan.FromMinutes(181);
                WaterTapIsOpen = true;
                Setup();
                Act();
            }

            [Fact] public void ThenCloseIt() => Verify<IWaterTap>(tap => tap.Close());

            [Fact] public void ThenLogWarning() => VerifyLog(Severity.Warning, "possible leakage");
        }

        public class GivenWaterTapIsOpenAndWaterLevelIsLow : WhenCheckWaterLevel
        {
            protected override void Given() => (WaterLevel, WaterTapIsOpen) = (-0.6, true);
            [Fact] public void ThenDoNotOpenIt() => Verify<IWaterTap>(tap => tap.Open(), Times.Never);
        }

        public class GivenWaterTapIsClosedAndWaterLevelIsHigh : WhenCheckWaterLevel
        {
            protected override void Given() => (WaterLevel, WaterTapIsOpen) = (0.1, false);
            [Fact] public void ThenDoNotCloseIt() => Verify<IWaterTap>(tap => tap.Close(), Times.Never);
        }

        private void VerifyLog(Severity severity, string message)
            => Verify<ILogger>(logger => logger.Log(severity, It.Is<string>(
                    msg => msg.ToLower().Contains(message))));
    }
}