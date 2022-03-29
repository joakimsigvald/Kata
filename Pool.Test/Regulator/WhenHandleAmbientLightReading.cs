using Moq;
using Pool.Services;
using Xunit;

namespace Pool.Test.Regulator;
public abstract class WhenHandleAmbientLightReading : TestRegulator<float>
{
    protected double LightLevel;

    protected override void Act() => SUT.HandleAmbientLightReading(LightLevel);

    public class GivenDusk : WhenHandleAmbientLightReading
    {
        protected override void Given() => LightLevel = 0.2;
        [Fact] public void ThenTurnSpotlightsOn() => Verify<ISpotlights>(tap => tap.TurnOn());
    }

    public class GivenBeforeSunrise : WhenHandleAmbientLightReading
    {
        protected override void Given() => (LightLevel, SpotlightsIsOn) = (1.99, true);
        [Fact] public void ThenKeepSpotlightsOn() => VerifySpotsDoNotChangeState();
    }

    public class GivenSunrise : WhenHandleAmbientLightReading
    {
        protected override void Given() => (LightLevel, SpotlightsIsOn) = (2, true);
        [Fact] public void ThenTurnSpotlightsOff() => Verify<ISpotlights>(tap => tap.TurnOff());
    }

    public class GivenLateAfternoon : WhenHandleAmbientLightReading
    {
        protected override void Given() => LightLevel = 1;
        [Fact] public void ThenKeepSpotlightsOff() => VerifySpotsDoNotChangeState();
    }

    public class GivenDawn : WhenHandleAmbientLightReading
    {
        protected override void Given() => LightLevel = 0.99;
        [Fact] public void ThenTurnSpotlightsOn() => Verify<ISpotlights>(tap => tap.TurnOn());
    }

    public class GivenLateEvening : WhenHandleAmbientLightReading
    {
        protected override void Given() => (LightLevel, SpotlightsIsOn) = (0.1, true);
        [Fact] public void ThenKeepSpotlightsOn() => VerifySpotsDoNotChangeState();
    }

    public class GivenNightfall : WhenHandleAmbientLightReading
    {
        protected override void Given() => (LightLevel, SpotlightsIsOn) = (0.09, true);
        [Fact] public void ThenTurnSpotlightsOff() => Verify<ISpotlights>(tap => tap.TurnOff());
    }

    public class GivenLateNight : WhenHandleAmbientLightReading
    {
        protected override void Given() => LightLevel = 0.19;
        [Fact] public void ThenKeepSpotlightsOff() => VerifySpotsDoNotChangeState();
    }

    public void VerifySpotsDoNotChangeState()
    {
        Verify<ISpotlights>(tap => tap.TurnOn(), Times.Never);
        Verify<ISpotlights>(tap => tap.TurnOff(), Times.Never);
    }
}