using PDFBlender.Core.Processing;
using Xunit;

namespace PDFBlender.Core.Tests;

public sealed class BlendOutputPathTests
{
    [Theory]
    [InlineData("阅读优先", "report_阅读优先.pdf")]
    [InlineData("Reading First", "report_Reading First.pdf")]
    [InlineData("  复制层防护 (2)  ", "report_复制层防护 (2).pdf")]
    [InlineData("A/B\\C:D*E?F\"G<H>I|J", "report_A_B_C_D_E_F_G_H_I_J.pdf")]
    [InlineData("配置. ", "report_配置.pdf")]
    [InlineData("...", "report_configuration.pdf")]
    [InlineData("A\nB", "report_A_B.pdf")]
    public void OutputStaysBesideSourceAndUsesSafeConfigurationName(string configurationName, string expectedName)
    {
        var directory = Path.Combine(Path.GetTempPath(), "pdf-output-naming");
        var input = Path.Combine(directory, "report.PDF");
        var output = BlendOutputPath.Create(input, configurationName);

        Assert.Equal(directory, Path.GetDirectoryName(output));
        Assert.Equal(expectedName, Path.GetFileName(output));
        Assert.NotEqual(input, output);
    }

    [Fact]
    public void EmptyConfigurationNameIsRejected()
    {
        Assert.Throws<ArgumentException>(() => BlendOutputPath.Create("report.pdf", " "));
    }
}
