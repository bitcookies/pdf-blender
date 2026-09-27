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

    [Fact]
    public void BuiltInConfigurationsAreReadOnlyAndUseStableIntensityLevels()
    {
        Assert.Equal(3, BuiltInConfigurations.All.Count);
        Assert.All(BuiltInConfigurations.All, configuration => Assert.True(configuration.IsBuiltIn));
        Assert.Equal(0, BuiltInConfigurations.All[0].Intensity);
        Assert.Equal(6, BuiltInConfigurations.All[1].Intensity);
        Assert.Equal(6, BuiltInConfigurations.All[2].Intensity);
    }

    [Fact]
    public void ConfigurationIntensityUsesTheStrongestEnabledFeature()
    {
        var widthOnly = new BlendSettings
        {
            NormalizeContentWidth = true,
            Protection = ProtectionMethod.None,
            Watermark = WatermarkStyle.None,
            Noise = NoiseStrength.Off
        };
        Assert.Equal(0, ConfigurationIntensity.For(widthOnly));

        var fullPageWatermarkAndLowNoise = widthOnly with
        {
            Watermark = WatermarkStyle.FullPage,
            Noise = NoiseStrength.Low
        };
        Assert.Equal(5, ConfigurationIntensity.For(fullPageWatermarkAndLowNoise));
    }
}
