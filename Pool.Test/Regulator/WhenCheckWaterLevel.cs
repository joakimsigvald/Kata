using Adlibris.B2L.Test;
using Moq;
using Xunit;

namespace Pool.Test.Regulator
{
    public abstract class WhenCheckWaterLevel : TestSubject<Pool.Regulator, float>
    {
        protected double WaterLevel;
        protected DateTime CurrentTime = DateTime.Now;
        public WhenCheckWaterLevel() => ArrangeAndAct();
        protected override Pool.Regulator CreateSUT()
            => new(MockOf<IWaterTap>(), MockOf<IWaterIndicator>(), MockOf<ILogger>(), MockOf<ITime>());

        protected override void Setup()
        {
            Mocked<IWaterIndicator>().Setup(indicator => indicator.Level).Returns(WaterLevel);
            Mocked<ITime>().Setup(time => time.Current).Returns(CurrentTime);
        }

        protected override void Act() => SUT.CheckWaterLevel();

        public class GivenLevelIsLow : WhenCheckWaterLevel
        {
            protected override void Given() => WaterLevel = -0.5;
            [Fact] public void ThenWaterTapIsOpen() => Verify<IWaterTap>(tap => tap.Open());
        }

        public class GivenLevelIsHigh : WhenCheckWaterLevel
        {
            protected override void Given() => WaterLevel = 0;
            [Fact] public void ThenWaterTapIsClosed() => Verify<IWaterTap>(tap => tap.Close());
        }

        public class GivenLevelIsTooHigh : WhenCheckWaterLevel
        {
            protected override void Given() => WaterLevel = 0.31;
            [Fact]
            public void ThenLogError()
                => Verify<ILogger>(logger => logger.Error(It.Is<string>(
                    msg => msg.ToLower().Contains("water level too high"))));
        }

        public class GivenLevelIsTooLow : WhenCheckWaterLevel
        {
            protected override void Given() => WaterLevel = -1.51;
            [Fact]
            public void ThenLogError()
                => Verify<ILogger>(logger => logger.Error(It.Is<string>(
                    msg => msg.ToLower().Contains("water level too low"))));
        }

        public class GivenWaterTapWasRecentlyClosedAndWaterLevelIslow : WhenCheckWaterLevel
        {
            protected override void Given() => WaterLevel = 0.1;
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
            [Fact]
            public void ThenCloseIt()
            {
                WaterLevel = -0.6;
                CurrentTime += TimeSpan.FromMinutes(61);
                Setup();
                Act();
                Verify<IWaterTap>(tap => tap.Open());
            }
        }
    }
}