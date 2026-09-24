using PDFBlender.Core.Models;
using Xunit;

namespace PDFBlender.Core.Tests;

public sealed class BlendSettingsTests
{
    [Fact]
    public void BlendPresetHasExpectedFlattenAndWatermarkDefaults()
    {
        var settings = Presets.For(BlendMode.Blend);

        Assert.True(settings.NormalizeContentWidth);
        Assert.Equal(ProtectionMethod.Flatten, settings.Protection);
        Assert.Equal(WatermarkStyle.Text, settings.Watermark);
        Assert.Equal(NoiseStrength.Low, settings.Noise);
    }

    [Fact]
    public void ValidatorRejectsDeferredProtectionMethods()
    {
        var errors = SettingsValidator.Validate(new BlendSettings { Protection = ProtectionMethod.TextScrambling });

        Assert.Contains(errors, error => error.Contains("future release", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ValidatorRejectsUnsafeDpiAndWidth()
    {
        var errors = SettingsValidator.Validate(new BlendSettings { Dpi = 20, TargetWidthMillimeters = 1 });

        Assert.Equal(2, errors.Count);
    }
}
