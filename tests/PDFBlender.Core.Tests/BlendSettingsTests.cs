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

        Assert.Contains(errors, error => error == "validationFlattenRequired");
    }

    [Fact]
    public void ValidatorRejectsUnsafeDpiAndWidth()
    {
        var errors = SettingsValidator.Validate(new BlendSettings { Protection = ProtectionMethod.Flatten, NormalizeContentWidth = true, Dpi = 20, TargetWidthMillimeters = 1 });
        Assert.Equal(2, errors.Count);
    }

    [Fact]
    public void BuiltInConfigurationsContainStableLocalizedPresetIds()
    {
        Assert.Equal(13, BuiltInConfigurations.All.Count);
        Assert.Equal(9, BuiltInConfigurations.Definitions.Count(definition => definition.Category == PresetCategory.Functional));
        Assert.Equal(4, BuiltInConfigurations.Definitions.Count(definition => definition.Category == PresetCategory.Common));
        Assert.All(BuiltInConfigurations.Definitions, definition =>
        {
            Assert.True(definition.Configuration.IsBuiltIn);
            Assert.False(string.IsNullOrWhiteSpace(definition.NameKey));
            Assert.False(string.IsNullOrWhiteSpace(definition.DescriptionKey));
            Assert.False(string.IsNullOrWhiteSpace(definition.IconKey));
            var errors = SettingsValidator.Validate(definition.Configuration.Settings);
            if (definition.IsExperimental)
                Assert.Contains("validationGanUnsupported", errors);
            else
                Assert.Empty(errors);
        });
        Assert.Equal("normalize-width-a4", BuiltInConfigurations.All[0].Id);
        Assert.Equal("maximum-protection", BuiltInConfigurations.All[^1].Id);
        Assert.Equal(0, BuiltInConfigurations.All[0].Intensity);
        Assert.True(BuiltInConfigurations.All[^1].Intensity > BuiltInConfigurations.All[0].Intensity);
    }

    [Fact]
    public void ConfigurationIntensityUsesTheSumOfEnabledFeatureWeights()
    {
        var widthOnly = new BlendSettings { NormalizeContentWidth = true };
        Assert.Equal(0, ConfigurationIntensity.For(widthOnly));

        var fullPageWatermarkAndLowNoise = widthOnly with
        {
            Watermark = WatermarkStyle.Repeated,
            Noise = NoiseStrength.Low
        };
        Assert.Equal(6, ConfigurationIntensity.For(fullPageWatermarkAndLowNoise));
    }

    [Fact]
    public void DisabledParametersDoNotBlockProcessingAndPermissionsDoNotRequireFlattening()
    {
        Assert.Empty(SettingsValidator.Validate(new BlendSettings { Dpi = 0, TargetWidthMillimeters = double.NaN }));
        Assert.Empty(SettingsValidator.Validate(new BlendSettings { PermissionRestriction = true }));
        Assert.Contains("validationGanUnsupported", SettingsValidator.Validate(new BlendSettings { GanAdversarialPerturbation = true }));
        Assert.Contains("validationWidth", SettingsValidator.Validate(new BlendSettings { NormalizeContentWidth = true, TargetWidthMillimeters = double.NaN }));
        Assert.Contains("validationPrompt", SettingsValidator.Validate(new BlendSettings { PromptInjection = true }));
        Assert.Contains("validationAngle", SettingsValidator.Validate(new BlendSettings { Watermark = WatermarkStyle.Single, WatermarkAngle = double.PositiveInfinity }));
    }

    [Fact]
    public void ActiveParameterSnapshotRoundTripsWithoutLosingSelections()
    {
        var settings = new BlendSettings
        {
            NormalizeContentWidth = true, ContentWidthPreset = PageSizePreset.Custom, TargetWidthMillimeters = 190.5,
            Protection = ProtectionMethod.Flatten, Dpi = 450, ImageQuality = ImageQuality.Lossless,
            Watermark = WatermarkStyle.Repeated, WatermarkAngle = 45, WatermarkText = "中文 Watermark",
            Noise = NoiseStrength.Medium, Texture = TextureStrength.Low, GeometryPerturbation = true,
            MicroTextPattern = true, CopyLayer = CopyLayerMode.UnicodeScramble, HiddenAiText = true,
            PromptInjection = true, PromptInjectionText = "Synthetic prompt", MetadataNormalization = true,
            ProtectionDeclaration = true, PermissionRestriction = true
        };
        Assert.Empty(SettingsValidator.Validate(settings));
        var json = System.Text.Json.JsonSerializer.Serialize(settings);
        Assert.Equal(settings, System.Text.Json.JsonSerializer.Deserialize<BlendSettings>(json));
        Assert.False(BlendSettings.Default.PromptInjection);
        Assert.Equal(300, BlendSettings.Default.Dpi);
        Assert.Equal(ImageQuality.Standard, BlendSettings.Default.ImageQuality);
    }
}
