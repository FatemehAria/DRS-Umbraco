using System.Security;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace DrsUmbraco.Cms.Controllers;

[ApiController]
public sealed class SitemapController : ControllerBase
{
    private readonly IPublishedContentQuery _publishedContentQuery;
    private readonly IPublishedUrlProvider _publishedUrlProvider;
    private readonly IUmbracoContextFactory _umbracoContextFactory;

    public SitemapController(
        IPublishedContentQuery publishedContentQuery,
        IPublishedUrlProvider publishedUrlProvider,
        IUmbracoContextFactory umbracoContextFactory)
    {
        _publishedContentQuery = publishedContentQuery;
        _publishedUrlProvider = publishedUrlProvider;
        _umbracoContextFactory = umbracoContextFactory;
    }

    [HttpGet("sitemap.xml")]
    public IActionResult Index()
    {
        using var umbracoContextReference =
            _umbracoContextFactory.EnsureUmbracoContext();

        var rootNodes = _publishedContentQuery.ContentAtRoot();

        var pages = rootNodes
            .SelectMany(x => x.DescendantsOrSelf())
            .Where(ShouldIncludeInSitemap)
            .OrderBy(x => x.Level)
            .ThenBy(x => x.SortOrder)
            .ToList();

        var xml = BuildSitemapXml(pages);

        return Content(xml, "application/xml", Encoding.UTF8);
    }

    private string BuildSitemapXml(IEnumerable<IPublishedContent> pages)
    {
        var xml = new StringBuilder();

        xml.AppendLine("""<?xml version="1.0" encoding="UTF-8"?>""");
        xml.AppendLine("""<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">""");

        foreach (var page in pages)
        {
            var absoluteUrl = page.Url(_publishedUrlProvider, mode: UrlMode.Absolute);

            if (string.IsNullOrWhiteSpace(absoluteUrl) || absoluteUrl == "#")
            {
                continue;
            }

            xml.AppendLine("  <url>");
            xml.AppendLine($"    <loc>{SecurityElement.Escape(absoluteUrl)}</loc>");
            xml.AppendLine($"    <lastmod>{page.UpdateDate:yyyy-MM-dd}</lastmod>");
            xml.AppendLine("    <changefreq>weekly</changefreq>");
            xml.AppendLine($"    <priority>{GetPriority(page)}</priority>");
            xml.AppendLine("  </url>");
        }

        xml.AppendLine("</urlset>");

        return xml.ToString();
    }

    private static bool ShouldIncludeInSitemap(IPublishedContent page)
    {
        var excludedAliases = new[]
        {
            "page404"
        };

        if (excludedAliases.Contains(page.ContentType.Alias))
        {
            return false;
        }

        return page.TemplateId.HasValue;
    }

    private static string GetPriority(IPublishedContent page)
    {
        return page.Level switch
        {
            1 => "1.0",
            2 => "0.8",
            _ => "0.6"
        };
    }
}