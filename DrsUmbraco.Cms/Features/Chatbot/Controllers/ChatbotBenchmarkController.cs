using System.Text.Json;
using DrsUmbraco.Cms.Features.Chatbot.Filters;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Serialization;

namespace DrsUmbraco.Cms.Features.Chatbot.Controllers;

[ApiController]
[Route("api/chatbot/benchmark")]
[TypeFilter(typeof(DevelopmentOnlyFilter))]
public sealed class ChatbotBenchmarkController : ControllerBase
{
    private const string KnowledgeBaseAlias = "chatbotKnowledgeBase";
    private const string FaqItemAlias = "chatbotFaqItem";
    private const string QuestionAlias = "question";
    private const string AnswerAlias = "answer";
    private const string AlternativeQuestionsAlias = "alternativeQuestions";
    private const string BenchmarkPrefix = "[BENCH] ";
    // private const string ResponseTypeAlias = "responseType";
    private readonly IContentService _contentService;
    private readonly IContentTypeService _contentTypeService;
    private readonly IWebHostEnvironment _environment;
    private readonly IPublishedContentQuery _publishedContentQuery;
    // private readonly IJsonSerializer _jsonSerializer;

    public ChatbotBenchmarkController(
        IContentService contentService,
        IContentTypeService contentTypeService,
        IWebHostEnvironment environment,
        IPublishedContentQuery publishedContentQuery
        // , IJsonSerializer jsonSerializer
        )
    {
        _contentService =
            contentService
            ?? throw new ArgumentNullException(
                nameof(contentService));

        _contentTypeService =
            contentTypeService
            ?? throw new ArgumentNullException(
                nameof(contentTypeService));

        _environment =
            environment
            ?? throw new ArgumentNullException(
                nameof(environment));

        _publishedContentQuery =
            publishedContentQuery
            ?? throw new ArgumentNullException(nameof(publishedContentQuery));

        // _jsonSerializer =
        //     jsonSerializer
        //     ?? throw new ArgumentNullException(
        //         nameof(jsonSerializer));
    }

    [HttpPost("seed")]
    public IActionResult Seed()
    {
        string filePath =
            Path.Combine(
                _environment.ContentRootPath,
                "BenchmarkData",
                "synthetic-faq-distractors-490.json");

        if (!System.IO.File.Exists(filePath))
        {
            return NotFound(
                $"Benchmark file was not found: {filePath}");
        }

        IContentType? faqContentType =
            _contentTypeService.Get(
                FaqItemAlias);

        if (faqContentType is null)
        {
            return BadRequest(
                $"Document type '{FaqItemAlias}' was not found.");
        }

        HashSet<string> propertyAliases =
            faqContentType
                .PropertyTypes
                .Select(property => property.Alias)
                .ToHashSet(
                    StringComparer.OrdinalIgnoreCase);

        string[] requiredProperties =
        [
            QuestionAlias,
            AnswerAlias,
            AlternativeQuestionsAlias
        ];

        string[] missingProperties =
            requiredProperties
                .Where(
                    alias =>
                        !propertyAliases.Contains(alias))
                .ToArray();

        if (missingProperties.Length > 0)
        {
            return BadRequest(
                new
                {
                    message =
                        "Required FAQ properties are missing.",
                    missingProperties
                });
        }

        IContent? knowledgeBase = GetChatbotKnowledgeBase();

        if (knowledgeBase is null)
        {
            return BadRequest(
                $"Root content '{KnowledgeBaseAlias}' was not found.");
        }

        string json =
            System.IO.File.ReadAllText(
                filePath);

        List<SyntheticFaq>? items =
            JsonSerializer.Deserialize<
                List<SyntheticFaq>>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive =
                        true
                });

        if (items is null ||
            items.Count != 490)
        {
            return BadRequest(
                $"Expected 490 FAQ items, but found {items?.Count ?? 0}.");
        }

        IEnumerable<IContent> existingChildren =
            _contentService.GetPagedChildren(
                knowledgeBase.Id,
                0,
                1000,
                out long _);

        HashSet<string> existingNames =
            existingChildren
                .Select(content => content.Name)
                .Where(name => name is not null)
                .Select(name => name!)
                .ToHashSet(
                    StringComparer.OrdinalIgnoreCase);

        int created = 0;
        int skipped = 0;

        foreach (SyntheticFaq item in items)
        {
            string nodeName =
                BenchmarkPrefix
                + item.Intent;

            if (existingNames.Contains(nodeName))
            {
                skipped++;
                continue;
            }

            IContent content =
                _contentService.Create(
                    nodeName,
                    knowledgeBase.Id,
                    FaqItemAlias);

            content.Key = item.Id;

            content.SetValue(
                QuestionAlias,
                item.Question);

            content.SetValue(
                AnswerAlias,
                item.Answer);

            content.SetValue(
                AlternativeQuestionsAlias,
                string.Join(
                    Environment.NewLine,
                    item.AlternativeQuestions));

            _contentService.Save(content);

            _contentService.Publish(
                content,
                []);

            created++;
        }

        return Ok(
            new
            {
                expected = 490,
                created,
                skipped,
                totalBenchmarkItems =
                    created + skipped
            });
    }

    [HttpGet("status")]
    public IActionResult Status()
    {
        IContentType? faqContentType =
            _contentTypeService.Get(FaqItemAlias);

        if (faqContentType is null)
        {
            return BadRequest(
                $"Document type '{FaqItemAlias}' was not found.");
        }

        IContent? knowledgeBase = GetChatbotKnowledgeBase();

        if (knowledgeBase is null)
        {
            return BadRequest(
                $"Root content '{KnowledgeBaseAlias}' was not found.");
        }

        IContent[] benchmarkItems =
            _contentService
                .GetPagedChildren(
                    knowledgeBase.Id,
                    0,
                    1000,
                    out long totalChildren)
                .Where(
                    content =>
                        content.Name?.StartsWith(
                            BenchmarkPrefix,
                            StringComparison.OrdinalIgnoreCase)
                        == true)
                .ToArray();

        int publishedCount =
            benchmarkItems.Count(
                item =>
                    _contentService.IsPathPublished(item));

        int publishableCount =
            benchmarkItems.Count(
                item =>
                    _contentService.IsPathPublishable(item));

        return Ok(
            new
            {
                documentTypeVariations =
                    faqContentType.Variations.ToString(),

                totalChildren,

                benchmarkSaved =
                    benchmarkItems.Length,

                benchmarkPathPublished =
                    publishedCount,

                benchmarkPathPublishable =
                    publishableCount,

                samples =
                    benchmarkItems
                        .Take(3)
                        .Select(
                            item =>
                                new
                                {
                                    item.Name,
                                    item.Key,
                                    pathPublished =
                                        _contentService
                                            .IsPathPublished(item),

                                    pathPublishable =
                                        _contentService
                                            .IsPathPublishable(item),

                                    cultureNames =
                                        item.CultureInfos?
                                            .Keys
                                            .ToArray()
                                })
            });
    }

    private IContent? GetWrongRootKnowledgeBase()
    {
        IContent? correctKnowledgeBase =
            GetChatbotKnowledgeBase();

        return _contentService
            .GetRootContent()
            .SingleOrDefault(content =>
                content.ContentType.Alias == KnowledgeBaseAlias
                &&
                (correctKnowledgeBase is null ||
                 content.Id != correctKnowledgeBase.Id));
    }

    [HttpGet("cleanup-preview")]
    public IActionResult CleanupPreview()
    {
        IContent? correctKnowledgeBase =
            GetChatbotKnowledgeBase();

        if (correctKnowledgeBase is null)
        {
            return BadRequest(
                "Correct chatbot knowledge base was not found.");
        }

        IContent? wrongKnowledgeBase =
            GetWrongRootKnowledgeBase();

        if (wrongKnowledgeBase is null)
        {
            return Ok(new
            {
                message =
                    "No wrong root-level knowledge base was found."
            });
        }

        IContent[] wrongBenchmarkItems =
            GetBenchmarkChildren(
                wrongKnowledgeBase);

        IContent[] correctBenchmarkItems =
            GetBenchmarkChildren(
                correctKnowledgeBase);

        return Ok(new
        {
            correctKnowledgeBase =
                new
                {
                    correctKnowledgeBase.Id,
                    correctKnowledgeBase.Key,
                    correctKnowledgeBase.Name,
                    benchmarkItems =
                        correctBenchmarkItems.Length
                },

            wrongKnowledgeBase =
                new
                {
                    wrongKnowledgeBase.Id,
                    wrongKnowledgeBase.Key,
                    wrongKnowledgeBase.Name,
                    benchmarkItems =
                        wrongBenchmarkItems.Length
                },

            safeToCleanup =
                wrongKnowledgeBase.Id
                    != correctKnowledgeBase.Id
                &&
                wrongBenchmarkItems.Length == 490
                &&
                correctBenchmarkItems.Length == 490,

            samples =
                wrongBenchmarkItems
                    .Take(5)
                    .Select(item =>
                        new
                        {
                            item.Id,
                            item.Key,
                            item.Name
                        })
        });
    }

    [HttpPost("cleanup-wrong-seed")]
    public IActionResult CleanupWrongSeed()
    {
        IContent? correctKnowledgeBase =
            GetChatbotKnowledgeBase();

        if (correctKnowledgeBase is null)
        {
            return BadRequest(
                "Correct chatbot knowledge base was not found.");
        }

        IContent? wrongKnowledgeBase =
            GetWrongRootKnowledgeBase();

        if (wrongKnowledgeBase is null)
        {
            return Ok(new
            {
                message =
                    "Nothing to clean up.",
                movedToRecycleBin = 0
            });
        }

        if (wrongKnowledgeBase.Id ==
            correctKnowledgeBase.Id)
        {
            return BadRequest(
                "Cleanup aborted because knowledge bases are identical.");
        }

        IContent[] wrongBenchmarkItems =
            GetBenchmarkChildren(
                wrongKnowledgeBase);

        IContent[] correctBenchmarkItems =
            GetBenchmarkChildren(
                correctKnowledgeBase);

        // Safety guards:
        if (wrongBenchmarkItems.Length != 490)
        {
            return BadRequest(new
            {
                message =
                    "Cleanup aborted. Wrong knowledge base does not contain exactly 490 benchmark items.",
                found =
                    wrongBenchmarkItems.Length
            });
        }

        if (correctBenchmarkItems.Length != 490)
        {
            return BadRequest(new
            {
                message =
                    "Cleanup aborted. Correct knowledge base does not contain exactly 490 benchmark items.",
                found =
                    correctBenchmarkItems.Length
            });
        }

        int moved = 0;

        foreach (IContent item in
                 wrongBenchmarkItems)
        {
            _contentService
                .MoveToRecycleBin(item);

            moved++;
        }

        IContent[] remaining =
            GetBenchmarkChildren(
                wrongKnowledgeBase);

        return Ok(new
        {
            expected = 490,
            movedToRecycleBin = moved,
            remainingWrongBenchmarkItems =
                remaining.Length
        });
    }

    // [HttpPost("reset-malformed-response-types")]
    // public IActionResult ResetMalformedResponseTypes()
    // {
    //     IContent? knowledgeBase =
    //         GetChatbotKnowledgeBase();

    //     if (knowledgeBase is null)
    //     {
    //         return BadRequest(
    //             "Correct chatbot knowledge base was not found.");
    //     }

    //     IContent[] benchmarkItems =
    //         GetBenchmarkChildren(knowledgeBase);

    //     int cleared = 0;

    //     foreach (IContent item in benchmarkItems)
    //     {
    //         string? raw =
    //             item.GetValue<string>(
    //                 ResponseTypeAlias)?
    //                 .Trim();

    //         if (!string.Equals(
    //             raw,
    //             "Answer",
    //             StringComparison.OrdinalIgnoreCase))
    //         {
    //             continue;
    //         }

    //         item.SetValue(
    //             ResponseTypeAlias,
    //             null);

    //         _contentService.Save(item);

    //         _contentService.Publish(
    //             item,
    //             []);

    //         cleared++;
    //     }

    //     return Ok(new
    //     {
    //         cleared
    //     });
    // }

    // [HttpGet("response-type-status")]
    // public IActionResult ResponseTypeStatus()
    // {
    //     IContent? correctKnowledgeBase =
    //         GetChatbotKnowledgeBase();

    //     if (correctKnowledgeBase is null)
    //     {
    //         return BadRequest(
    //             "Correct chatbot knowledge base was not found.");
    //     }

    //     IContent[] benchmarkItems =
    //         GetBenchmarkChildren(correctKnowledgeBase);

    //     int answerCount = 0;
    //     int clarificationCount = 0;
    //     int emptyCount = 0;

    //     // مقدار plain "Answer" که از اجرای ناموفق قبلی
    //     // ممکن است Save شده باشد.
    //     int malformedPlainAnswerCount = 0;

    //     int invalidCount = 0;

    //     foreach (IContent item in benchmarkItems)
    //     {
    //         string? raw =
    //             item.GetValue<string>(
    //                 ResponseTypeAlias)?
    //                 .Trim();

    //         if (string.IsNullOrWhiteSpace(raw))
    //         {
    //             emptyCount++;
    //             continue;
    //         }

    //         // Repairable state created by our previous attempt.
    //         if (string.Equals(
    //             raw,
    //             "Answer",
    //             StringComparison.OrdinalIgnoreCase))
    //         {
    //             malformedPlainAnswerCount++;
    //             continue;
    //         }

    //         try
    //         {
    //             string[]? values =
    //                 JsonSerializer.Deserialize<string[]>(
    //                     raw);

    //             if (values is null ||
    //                 values.Length != 1)
    //             {
    //                 invalidCount++;
    //                 continue;
    //             }

    //             string value =
    //                 values[0].Trim();

    //             if (string.Equals(
    //                 value,
    //                 "Answer",
    //                 StringComparison.OrdinalIgnoreCase))
    //             {
    //                 answerCount++;
    //             }
    //             else if (string.Equals(
    //                 value,
    //                 "Clarification",
    //                 StringComparison.OrdinalIgnoreCase))
    //             {
    //                 clarificationCount++;
    //             }
    //             else
    //             {
    //                 invalidCount++;
    //             }
    //         }
    //         catch (JsonException)
    //         {
    //             invalidCount++;
    //         }
    //     }

    //     return Ok(new
    //     {
    //         totalBenchmarkItems =
    //             benchmarkItems.Length,

    //         answerCount,
    //         clarificationCount,
    //         emptyCount,
    //         malformedPlainAnswerCount,
    //         invalidCount,

    //         safeToUpdate =
    //             benchmarkItems.Length == 490
    //             &&
    //             clarificationCount == 0
    //             &&
    //             invalidCount == 0
    //     });
    // }

    // [HttpPost("reset-benchmark-response-types")]
    // public IActionResult ResetBenchmarkResponseTypes()
    // {
    //     IContent? knowledgeBase =
    //         GetChatbotKnowledgeBase();

    //     if (knowledgeBase is null)
    //     {
    //         return BadRequest(
    //             "Correct chatbot knowledge base was not found.");
    //     }

    //     IContent[] benchmarkItems =
    //         GetBenchmarkChildren(knowledgeBase);

    //     if (benchmarkItems.Length != 490)
    //     {
    //         return BadRequest(new
    //         {
    //             message =
    //                 "Reset aborted. Expected exactly 490 benchmark items.",
    //             found = benchmarkItems.Length
    //         });
    //     }

    //     int cleared = 0;

    //     foreach (IContent item in benchmarkItems)
    //     {
    //         string? raw =
    //             item.GetValue<string>("responseType");

    //         if (string.IsNullOrWhiteSpace(raw))
    //         {
    //             continue;
    //         }

    //         item.SetValue(
    //             "responseType",
    //             null);

    //         _contentService.Save(item);

    //         _contentService.Publish(
    //             item,
    //             []);

    //         cleared++;
    //     }

    //     return Ok(new
    //     {
    //         cleared
    //     });
    // }

    private IContent[] GetBenchmarkChildren(
        IContent parent)
    {
        return _contentService
            .GetPagedChildren(
                parent.Id,
                0,
                1000,
                out long _)
            .Where(content =>
                content.ContentType.Alias == FaqItemAlias &&
                content.Name?.StartsWith(
                    BenchmarkPrefix,
                    StringComparison.OrdinalIgnoreCase)
                == true)
            .ToArray();
    }

    private IContent? GetChatbotKnowledgeBase()
    {
        IPublishedContent? publishedKnowledgeBase =
            _publishedContentQuery
                .ContentAtRoot()
                .SelectMany(root => root.Children())
                .FirstOrDefault(item =>
                    item.ContentType.Alias == KnowledgeBaseAlias);

        if (publishedKnowledgeBase is null)
        {
            return null;
        }

        return _contentService.GetById(
            publishedKnowledgeBase.Key);
    }

    private sealed class SyntheticFaq
    {
        public Guid Id { get; init; }

        public required string Question
        {
            get;
            init;
        }

        public required string[] AlternativeQuestions
        {
            get;
            init;
        }

        public required string Answer
        {
            get;
            init;
        }

        public required string Intent
        {
            get;
            init;
        }
    }
}