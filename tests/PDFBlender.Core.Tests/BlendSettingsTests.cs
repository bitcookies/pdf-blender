using PDFBlender.Core.Models;
using Xunit;

namespace PDFBlender.Core.Tests;

public sealed class BlendSettingsTests
{
    [Fact]
    public void CustomDefaultsHaveAllFeaturesDisabled()
    {
        var settings = BlendSettings.Default;
        Assert.False(settings.NormalizeContentWidth);
        Assert.False(settings.NormalizePageSize);
        Assert.Equal(ProtectionMethod.None, settings.Protection);
        Assert.Equal(WatermarkStyle.None, settings.Watermark);
        Assert.Equal(NoiseStrength.Off, settings.Noise);
        Assert.Equal(TextureStrength.Off, settings.Texture);
        Assert.Equal(CopyLayerMode.Preserve, settings.CopyLayer);
        Assert.False(settings.HiddenAiText);
    }

    [Fact]
    public void ValidatorRequiresFlatteningForGibberishAndVisualPerturbations()
    {
        var errors = SettingsValidator.Validate(new BlendSettings
        {
            CopyLayer = CopyLayerMode.ReplaceWithGibberish,
            Noise = NoiseStrength.Low
        });

        Assert.Contains(errors, error => error.Contains("flattening", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ValidatorRejectsUnsafeDpiAndWidth()
    {
        var errors = SettingsValidator.Validate(new BlendSettings { Dpi = 20, TargetWidthMillimeters = 1 });
        Assert.Equal(2, errors.Count);
    }

    [Fact]
    public void BuiltInConfigurationsContainStableLocalizedPresetIds()
    {
        Assert.Equal(5, BuiltInConfigurations.All.Count);
        Assert.All(BuiltInConfigurations.All, configuration => Assert.True(configuration.IsBuiltIn));
        Assert.Equal("format-only", BuiltInConfigurations.All[0].Id);
        Assert.Equal("copy-layer-shield", BuiltInConfigurations.All[4].Id);
        Assert.Equal(0, BuiltInConfigurations.All[0].Intensity);
        Assert.True(BuiltInConfigurations.All[4].Intensity >= 7);
    }

    [Fact]
    public void ConfigurationIntensityUsesTheStrongestEnabledFeature()
    {
        var widthOnly = new BlendSettings { NormalizeContentWidth = true };
        Assert.Equal(0, ConfigurationIntensity.For(widthOnly));

        var fullPageWatermarkAndLowNoise = widthOnly with
        {
            Watermark = WatermarkStyle.FullPage,
            Noise = NoiseStrength.Low
        };
        Assert.Equal(4, ConfigurationIntensity.For(fullPageWatermarkAndLowNoise));
    }
}
