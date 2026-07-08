using System.Data;
using System.Text.Json;
using DrsUmbraco.Cms.Models;
using Microsoft.Data.SqlClient;

namespace DrsUmbraco.Cms.Services;

public sealed class ElementorSubmissionService : IElementorSubmissionService
{
    private readonly IConfiguration _configuration;

    public ElementorSubmissionService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<decimal> CreateConsultRequestAsync(
        ConsultRequestCreateModel model,
        string referer,
        string? refererTitle,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var connectionString = _configuration.GetConnectionString("umbracoDbDSN");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'umbracoDbDSN' is not configured.");
        }

        var utcNow = DateTime.UtcNow;
        var localNow = GetIranLocalTime(utcNow);

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var submissionId = await InsertSubmissionAsync(
                connection,
                (SqlTransaction)transaction,
                referer,
                refererTitle,
                ipAddress,
                userAgent,
                utcNow,
                localNow,
                cancellationToken);

            await InsertSubmissionValuesAsync(
                connection,
                (SqlTransaction)transaction,
                submissionId,
                model,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return submissionId;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static async Task<decimal> InsertSubmissionAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string referer,
        string? refererTitle,
        string? ipAddress,
        string? userAgent,
        DateTime utcNow,
        DateTime localNow,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO dbo.wp_e_submissions
            (
                type,
                hash_id,
                main_meta_id,
                post_id,
                referer,
                referer_title,
                element_id,
                form_name,
                campaign_id,
                user_id,
                user_ip,
                user_agent,
                actions_count,
                actions_succeeded_count,
                status,
                is_read,
                meta,
                created_at_gmt,
                updated_at_gmt,
                created_at,
                updated_at
            )
            OUTPUT INSERTED.id
            VALUES
            (
                @Type,
                @HashId,
                @MainMetaId,
                @PostId,
                @Referer,
                @RefererTitle,
                @ElementId,
                @FormName,
                @CampaignId,
                @UserId,
                @UserIp,
                @UserAgent,
                @ActionsCount,
                @ActionsSucceededCount,
                @Status,
                @IsRead,
                @Meta,
                @CreatedAtGmt,
                @UpdatedAtGmt,
                @CreatedAt,
                @UpdatedAt
            );
            """;

        await using var command = new SqlCommand(sql, connection, transaction);

        var meta = JsonSerializer.Serialize(new
        {
            edit_post_id = "129"
        });

        command.Parameters.Add(new SqlParameter("@Type", SqlDbType.NVarChar, 60)
        {
            Value = "submission"
        });

        command.Parameters.Add(new SqlParameter("@HashId", SqlDbType.NVarChar, 60)
        {
            Value = Guid.NewGuid().ToString()
        });

        command.Parameters.Add(new SqlParameter("@MainMetaId", SqlDbType.Decimal)
        {
            Precision = 20,
            Scale = 0,
            Value = 34
        });

        command.Parameters.Add(new SqlParameter("@PostId", SqlDbType.Decimal)
        {
            Precision = 20,
            Scale = 0,
            Value = 129
        });

        command.Parameters.Add(new SqlParameter("@Referer", SqlDbType.NVarChar, 500)
        {
            Value = TrimOrDefault(referer, 500, "/")
        });

        command.Parameters.Add(new SqlParameter("@RefererTitle", SqlDbType.NVarChar, 300)
        {
            Value = string.IsNullOrWhiteSpace(refererTitle)
                ? DBNull.Value
                : Trim(refererTitle, 300)
        });

        command.Parameters.Add(new SqlParameter("@ElementId", SqlDbType.NVarChar, 20)
        {
            Value = "7f7b520"
        });

        command.Parameters.Add(new SqlParameter("@FormName", SqlDbType.NVarChar, 60)
        {
            Value = "consult_form"
        });

        command.Parameters.Add(new SqlParameter("@CampaignId", SqlDbType.Decimal)
        {
            Precision = 20,
            Scale = 0,
            Value = 0
        });

        command.Parameters.Add(new SqlParameter("@UserId", SqlDbType.Decimal)
        {
            Precision = 20,
            Scale = 0,
            Value = 1
        });

        command.Parameters.Add(new SqlParameter("@UserIp", SqlDbType.NVarChar, 46)
        {
            Value = TrimOrDefault(ipAddress, 46, "unknown")
        });

        command.Parameters.Add(new SqlParameter("@UserAgent", SqlDbType.NVarChar, -1)
        {
            Value = string.IsNullOrWhiteSpace(userAgent)
                ? "unknown"
                : userAgent
        });

        command.Parameters.Add(new SqlParameter("@ActionsCount", SqlDbType.Int)
        {
            Value = 0
        });

        command.Parameters.Add(new SqlParameter("@ActionsSucceededCount", SqlDbType.Int)
        {
            Value = 0
        });

        command.Parameters.Add(new SqlParameter("@Status", SqlDbType.NVarChar, 20)
        {
            Value = "new"
        });

        command.Parameters.Add(new SqlParameter("@IsRead", SqlDbType.SmallInt)
        {
            Value = 0
        });

        command.Parameters.Add(new SqlParameter("@Meta", SqlDbType.NVarChar, -1)
        {
            Value = meta
        });

        command.Parameters.Add(new SqlParameter("@CreatedAtGmt", SqlDbType.DateTime2)
        {
            Value = utcNow
        });

        command.Parameters.Add(new SqlParameter("@UpdatedAtGmt", SqlDbType.DateTime2)
        {
            Value = utcNow
        });

        command.Parameters.Add(new SqlParameter("@CreatedAt", SqlDbType.DateTime2)
        {
            Value = localNow
        });

        command.Parameters.Add(new SqlParameter("@UpdatedAt", SqlDbType.DateTime2)
        {
            Value = localNow
        });

        var result = await command.ExecuteScalarAsync(cancellationToken);

        return Convert.ToDecimal(result);
    }

    private static async Task InsertSubmissionValuesAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        decimal submissionId,
        ConsultRequestCreateModel model,
        CancellationToken cancellationToken)
    {
        var values = new Dictionary<string, string?>
        {
            ["fullname"] = model.FullName,
            ["mobile"] = model.Mobile,
            ["request_type"] = model.RequestType,
            ["message"] = model.Message
        };

        const string sql = """
            INSERT INTO dbo.wp_e_submissions_values
            (
                submission_id,
                [key],
                value
            )
            VALUES
            (
                @SubmissionId,
                @Key,
                @Value
            );
            """;

        foreach (var item in values)
        {
            await using var command = new SqlCommand(sql, connection, transaction);

            command.Parameters.Add(new SqlParameter("@SubmissionId", SqlDbType.Decimal)
            {
                Precision = 20,
                Scale = 0,
                Value = submissionId
            });

            command.Parameters.Add(new SqlParameter("@Key", SqlDbType.NVarChar, 60)
            {
                Value = item.Key
            });

            command.Parameters.Add(new SqlParameter("@Value", SqlDbType.NVarChar, -1)
            {
                Value = string.IsNullOrWhiteSpace(item.Value)
                    ? DBNull.Value
                    : item.Value.Trim()
            });

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static DateTime GetIranLocalTime(DateTime utcNow)
    {
        try
        {
            var iranTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Iran Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(utcNow, iranTimeZone);
        }
        catch
        {
            return utcNow.AddMinutes(210);
        }
    }

    private static string Trim(string value, int maxLength)
    {
        value = value.Trim();

        return value.Length <= maxLength
            ? value
            : value[..maxLength];
    }

    private static string TrimOrDefault(string? value, int maxLength, string defaultValue)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        return Trim(value, maxLength);
    }
}