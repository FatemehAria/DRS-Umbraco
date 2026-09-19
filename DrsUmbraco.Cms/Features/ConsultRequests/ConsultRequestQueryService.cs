using System.Data;
using System.Globalization;
using DrsUmbraco.Cms.Features.ConsultRequests.Configuration;
using DrsUmbraco.Cms.Features.ConsultRequests.Contracts;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace DrsUmbraco.Cms.Features.ConsultRequests;

public sealed class ConsultRequestQueryService
    : IConsultRequestQueryService
{
    private readonly IConfiguration _configuration;
    private readonly string _resumeStoragePath;
    private readonly ILogger<ConsultRequestQueryService> _logger;

    public ConsultRequestQueryService(
        IConfiguration configuration,
        IOptions<ConsultRequestOptions> consultRequestOptions,
        ILogger<ConsultRequestQueryService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        _resumeStoragePath = Path.GetFullPath(consultRequestOptions.Value.ResumeStoragePath);
    }

    public async Task<IReadOnlyList<JobApplicationSummary>>
        GetLatestJobApplicationsAsync(
            int take,
            CancellationToken cancellationToken = default)
    {
        var safeTake = Math.Clamp(take, 1, 100);

        const string sql = """
            SELECT TOP (@Take)
                submission.id,
                submission.created_at,

                MAX(
                    CASE
                        WHEN submissionValue.[key] = 'fullname'
                        THEN submissionValue.value
                    END
                ) AS full_name,

                MAX(
                    CASE
                        WHEN submissionValue.[key] = 'mobile'
                        THEN submissionValue.value
                    END
                ) AS mobile,

                MAX(
                    CASE
                        WHEN submissionValue.[key] = 'request_type'
                        THEN submissionValue.value
                    END
                ) AS request_type,

                MAX(
                    CASE
                        WHEN submissionValue.[key] = 'message'
                        THEN submissionValue.value
                    END
                ) AS message,

                MAX(
                    CASE
                        WHEN submissionValue.[key] = 'resume_file'
                             AND submissionValue.value IS NOT NULL
                        THEN 1
                        ELSE 0
                    END
                ) AS has_resume

            FROM dbo.wp_e_submissions AS submission

            LEFT JOIN dbo.wp_e_submissions_values AS submissionValue
                ON submissionValue.submission_id = submission.id

            WHERE submission.form_name = 'job_interest_form'

            GROUP BY
                submission.id,
                submission.created_at

            ORDER BY
                submission.id DESC;
            """;

        var applications = new List<JobApplicationSummary>();

        await using var connection = new SqlConnection(GetConnectionString());

        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);

        command.Parameters.Add(
            new SqlParameter(
                "@Take",
                SqlDbType.Int)
            {
                Value = safeTake
            });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            applications.Add(
                new JobApplicationSummary(
                    Id: reader.GetDecimal(0),

                    CreatedAt:
                        reader.GetDateTime(1),

                    FullName:
                        GetNullableString(reader, 2) ??
                        string.Empty,

                    Mobile:
                        GetNullableString(reader, 3) ??
                        string.Empty,

                    RequestType:
                        GetNullableString(reader, 4) ??
                        string.Empty,

                    Message:
                        GetNullableString(reader, 5),

                    HasResume:
                        reader.GetInt32(6) == 1));
        }

        return applications;
    }

    public async Task<ResumeDownload?>
        GetResumeDownloadAsync(
            decimal submissionId,
            CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT TOP (1)
                submissionValue.value

            FROM dbo.wp_e_submissions AS submission

            INNER JOIN dbo.wp_e_submissions_values AS submissionValue
                ON submissionValue.submission_id = submission.id

            WHERE
                submission.id = @SubmissionId
                AND submission.form_name = 'job_interest_form'
                AND submissionValue.[key] = 'resume_file';
            """;

        await using var connection = new SqlConnection(GetConnectionString());

        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);

        command.Parameters.Add(
            new SqlParameter(
                "@SubmissionId",
                SqlDbType.Decimal)
            {
                Precision = 20,
                Scale = 0,
                Value = submissionId
            });

        var result = await command.ExecuteScalarAsync(cancellationToken);

        if (result is null || result == DBNull.Value)
        {
            return null;
        }

        var storedFileName = Convert.ToString(result, CultureInfo.InvariantCulture);

        if (!IsValidStoredFileName(storedFileName))
        {
            _logger.LogWarning(
                "Invalid stored resume file name was found for submission {SubmissionId}.",
                submissionId);

            return null;
        }

        var physicalPath =
            Path.GetFullPath(
                Path.Combine(
                    _resumeStoragePath,
                    storedFileName!));

        if (!File.Exists(physicalPath))
        {
            _logger.LogWarning(
                "Resume file {StoredFileName} for submission {SubmissionId} was not found.",
                storedFileName,
                submissionId);

            return null;
        }

        var idText = submissionId.ToString("0", CultureInfo.InvariantCulture);

        return new ResumeDownload(PhysicalPath: physicalPath, DownloadFileName: $"resume-{idText}.pdf");
    }

    private string GetConnectionString()
    {
        var connectionString = _configuration.GetConnectionString("umbracoDbDSN");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'umbracoDbDSN' is not configured.");
        }

        return connectionString;
    }

    private static string? GetNullableString(
        SqlDataReader reader,
        int ordinal)
    {
        return reader.IsDBNull(ordinal)
            ? null
            : reader.GetString(ordinal);
    }

    private static bool IsValidStoredFileName(
        string? storedFileName)
    {
        if (string.IsNullOrWhiteSpace(storedFileName))
        {
            return false;
        }

        if (!string.Equals(
                Path.GetFileName(storedFileName),
                storedFileName,
                StringComparison.Ordinal))
        {
            return false;
        }

        if (!string.Equals(
                Path.GetExtension(storedFileName),
                ".pdf",
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var nameWithoutExtension =
            Path.GetFileNameWithoutExtension(
                storedFileName);

        return Guid.TryParseExact(
            nameWithoutExtension,
            "N",
            out _);
    }
}