using FluentAssertions;
using WcagAnalyzer.Infrastructure.Services;

namespace WcagAnalyzer.Tests.Infrastructure;

public class CrawlerExtractLinksTests
{
    private const string Base = "https://example.com";

    [Fact]
    public void NoHrefs_ShouldReturnEmpty()
    {
        var result = PlaywrightAccessibilityAnalyzer.ExtractSubpageLinks(Base, []);

        result.Should().BeEmpty();
    }

    [Fact]
    public void SameDomainSubpage_ShouldBeIncluded()
    {
        var hrefs = new[] { "https://example.com/about" };

        var result = PlaywrightAccessibilityAnalyzer.ExtractSubpageLinks(Base, hrefs);

        result.Should().ContainSingle().Which.Should().Be("https://example.com/about");
    }

    [Fact]
    public void ExternalDomain_ShouldBeExcluded()
    {
        var hrefs = new[] { "https://other.com/page" };

        var result = PlaywrightAccessibilityAnalyzer.ExtractSubpageLinks(Base, hrefs);

        result.Should().BeEmpty();
    }

    [Fact]
    public void MainPageUrl_ShouldBeExcluded()
    {
        var hrefs = new[] { "https://example.com", "https://example.com/" };

        var result = PlaywrightAccessibilityAnalyzer.ExtractSubpageLinks(Base, hrefs);

        result.Should().BeEmpty();
    }

    [Fact]
    public void WwwVariantOfSameDomain_ShouldBeIncluded()
    {
        var hrefs = new[] { "https://www.example.com/contact" };

        var result = PlaywrightAccessibilityAnalyzer.ExtractSubpageLinks(Base, hrefs);

        result.Should().ContainSingle();
    }

    [Fact]
    public void DifferentSubdomain_ShouldBeExcluded()
    {
        var hrefs = new[] { "https://blog.example.com/post" };

        var result = PlaywrightAccessibilityAnalyzer.ExtractSubpageLinks(Base, hrefs);

        result.Should().BeEmpty();
    }

    [Fact]
    public void DuplicatePaths_ShouldDeduplicateToOne()
    {
        var hrefs = new[]
        {
            "https://example.com/about",
            "https://example.com/about",
            "https://example.com/about?ref=menu",
            "https://example.com/about#section"
        };

        var result = PlaywrightAccessibilityAnalyzer.ExtractSubpageLinks(Base, hrefs);

        result.Should().ContainSingle().Which.Should().Be("https://example.com/about");
    }

    [Fact]
    public void QueryStringStripped_ShouldNormalizePath()
    {
        var hrefs = new[] { "https://example.com/page?ref=header" };

        var result = PlaywrightAccessibilityAnalyzer.ExtractSubpageLinks(Base, hrefs);

        result.Should().ContainSingle().Which.Should().Be("https://example.com/page");
    }

    [Fact]
    public void FragmentStripped_ShouldNormalizePath()
    {
        var hrefs = new[] { "https://example.com/page#top" };

        var result = PlaywrightAccessibilityAnalyzer.ExtractSubpageLinks(Base, hrefs);

        result.Should().ContainSingle().Which.Should().Be("https://example.com/page");
    }

    [Fact]
    public void MoreThanMaxSubpages_ShouldReturnOnlyMax()
    {
        var hrefs = Enumerable.Range(1, 10)
            .Select(i => $"https://example.com/page{i}")
            .ToArray();

        var result = PlaywrightAccessibilityAnalyzer.ExtractSubpageLinks(Base, hrefs, maxSubpages: 4);

        result.Should().HaveCount(4);
    }

    [Fact]
    public void CustomMaxSubpages_ShouldBeRespected()
    {
        var hrefs = Enumerable.Range(1, 10)
            .Select(i => $"https://example.com/page{i}")
            .ToArray();

        var result = PlaywrightAccessibilityAnalyzer.ExtractSubpageLinks(Base, hrefs, maxSubpages: 2);

        result.Should().HaveCount(2);
    }

    [Fact]
    public void NonHttpScheme_ShouldBeExcluded()
    {
        var hrefs = new[]
        {
            "mailto:test@example.com",
            "javascript:void(0)",
            "tel:+48123456789",
            "https://example.com/valid"
        };

        var result = PlaywrightAccessibilityAnalyzer.ExtractSubpageLinks(Base, hrefs);

        result.Should().ContainSingle().Which.Should().Be("https://example.com/valid");
    }

    [Fact]
    public void InvalidBaseUrl_ShouldReturnEmpty()
    {
        var result = PlaywrightAccessibilityAnalyzer.ExtractSubpageLinks(
            "not-a-url",
            ["https://example.com/page"]);

        result.Should().BeEmpty();
    }

    [Fact]
    public void BaseUrlWithWww_ShouldStillMatchSameDomainLinks()
    {
        var hrefs = new[] { "https://example.com/about" };

        var result = PlaywrightAccessibilityAnalyzer.ExtractSubpageLinks(
            "https://www.example.com",
            hrefs);

        result.Should().ContainSingle();
    }

    [Fact]
    public void TrailingSlashOnSubpage_ShouldDeduplicateWithoutSlash()
    {
        var hrefs = new[]
        {
            "https://example.com/about/",
            "https://example.com/about"
        };

        var result = PlaywrightAccessibilityAnalyzer.ExtractSubpageLinks(Base, hrefs);

        result.Should().ContainSingle();
    }
}
