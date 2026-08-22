$BaseUrl = "https://localhost:44398/api/chatbot/messages"

function From-Base64Utf8([string]$value)
{
    return [System.Text.Encoding]::UTF8.GetString(
        [System.Convert]::FromBase64String($value)
    )
}

$Tests = @(
    @{
        Name = "Exact"
        Message = From-Base64Utf8 "2obYt9mI2LEg2KfbjNmF24zZhCDYrdiz2KfYqCDaqdin2LHYqNix24wg2LHYpyDYqti624zbjNixINio2K/Zh9mF2J8="
    },
    @{
        Name = "Relevant"
        Message = From-Base64Utf8 "2YXbjOKAjNiu2YjYp9mFINii2K/YsdizINin24zZhduM2YQg2YXYqti12YQg2KjZhyDYrdiz2KfYqNmFINix2Ygg2LnZiNi2INqp2YbZhS4="
    },
    @{
        Name = "HardNegative"
        Message = From-Base64Utf8 "2YXbjOKAjNiu2YjYp9mFINin2LIg2K3Ys9in2Kgg2YHYudmE24wg2K7Yp9ix2Kwg2KjYtNmFLg=="
    }
)

$WarmupCount = 3
$RunCount = 20

foreach ($test in $Tests)
{
    Write-Host ""
    Write-Host "=================================="
    Write-Host $test.Name
    Write-Host "=================================="

    $body = @{
        message = $test.Message
    } | ConvertTo-Json

    $requestFile = ".\benchmark-$($test.Name).json"

    [System.IO.File]::WriteAllText(
        $requestFile,
        $body,
        [System.Text.UTF8Encoding]::new($false)
    )

    for ($i = 1; $i -le $WarmupCount; $i++)
    {
        curl.exe `
            -k `
            --ssl-no-revoke `
            --http1.1 `
            -s `
            -o NUL `
            -H "Content-Type: application/json; charset=utf-8" `
            --data-binary "@$requestFile" `
            $BaseUrl | Out-Null
    }

    $times = @()

    for ($i = 1; $i -le $RunCount; $i++)
    {
        $result = curl.exe `
            -k `
            --ssl-no-revoke `
            --http1.1 `
            -s `
            -o NUL `
            -w "%{time_total}" `
            -H "Content-Type: application/json; charset=utf-8" `
            --data-binary "@$requestFile" `
            $BaseUrl

        $seconds = [double]::Parse(
            $result,
            [System.Globalization.CultureInfo]::InvariantCulture
        )

        $milliseconds = $seconds * 1000

        $times += $milliseconds

        Write-Host (
            "Run {0,2}: {1,8:N2} ms" -f
            $i,
            $milliseconds
        )
    }

    $stats =
        $times |
        Measure-Object `
            -Minimum `
            -Maximum `
            -Average

    Write-Host ""

    Write-Host (
        "Average: {0:N2} ms" -f
        $stats.Average
    )

    Write-Host (
        "Min:     {0:N2} ms" -f
        $stats.Minimum
    )

    Write-Host (
        "Max:     {0:N2} ms" -f
        $stats.Maximum
    )

    Remove-Item `
        $requestFile `
        -ErrorAction SilentlyContinue
}