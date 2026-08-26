param(
    [string]$BenchmarkPath = "C:\Projects\DrsUmbracoPilot\scale-accuracy-benchmark-200.csv",
    [string]$Endpoint = "https://localhost:44398/api/chatbot/messages",
    [string]$OutputDirectory = "C:\Projects\DrsUmbracoPilot\BenchmarkResults",
    [int]$Limit = 0
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $BenchmarkPath)) {
    throw "Benchmark CSV not found: $BenchmarkPath"
}

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

$rows = Import-Csv -Path $BenchmarkPath -Encoding UTF8

if ($Limit -gt 0) {
    $rows = @($rows | Select-Object -First $Limit)
}

if ($rows.Count -eq 0) {
    throw "Benchmark CSV contains no rows."
}

$requestPath = Join-Path $OutputDirectory "_request.json"
$responsePath = Join-Path $OutputDirectory "_response.json"

$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
$invariant = [System.Globalization.CultureInfo]::InvariantCulture

$results = New-Object System.Collections.Generic.List[object]

function Get-Percentile {
    param(
        [double[]]$Values,
        [double]$Percentile
    )

    if ($Values.Count -eq 0) {
        return 0
    }

    $sorted = @($Values | Sort-Object)

    if ($sorted.Count -eq 1) {
        return [double]$sorted[0]
    }

    $rank = ($Percentile / 100.0) * ($sorted.Count - 1)
    $lower = [math]::Floor($rank)
    $upper = [math]::Ceiling($rank)

    if ($lower -eq $upper) {
        return [double]$sorted[$lower]
    }

    $weight = $rank - $lower
    return ([double]$sorted[$lower] * (1.0 - $weight)) +
           ([double]$sorted[$upper] * $weight)
}

$index = 0

foreach ($row in $rows) {
    $index++

    $body = @{
        message = $row.UserQuestion
    } | ConvertTo-Json -Compress

    [System.IO.File]::WriteAllText(
        $requestPath,
        $body,
        $utf8NoBom
    )

    $curlMeta = & curl.exe `
        -k `
        --ssl-no-revoke `
        --http1.1 `
        -s `
        -o $responsePath `
        -w "%{http_code}|%{time_total}" `
        -H "Content-Type: application/json; charset=utf-8" `
        --data-binary "@$requestPath" `
        $Endpoint

    if ($LASTEXITCODE -ne 0) {
        throw "curl failed for benchmark row $($row.Id), exit code $LASTEXITCODE"
    }

    $metaParts = [string]$curlMeta -split "\|"

    $httpCode = [int]$metaParts[0]
    $latencySeconds = [double]::Parse(
        $metaParts[1],
        $invariant
    )
    $latencyMs = $latencySeconds * 1000.0

    $responseText = [System.IO.File]::ReadAllText(
        $responsePath,
        [System.Text.Encoding]::UTF8
    )

    $response = $null
    $responseType = ""
    $reply = ""
    $actualIds = @()
    $parseError = ""

    try {
        if (-not [string]::IsNullOrWhiteSpace($responseText)) {
            $response = $responseText | ConvertFrom-Json

            if ($response.PSObject.Properties.Name -contains "responseType") {
                $responseType = [string]$response.responseType
            }

            if ($response.PSObject.Properties.Name -contains "reply") {
                $reply = [string]$response.reply
            }

            if (
                $response.PSObject.Properties.Name -contains "knowledgeItemId" -and
                $null -ne $response.knowledgeItemId
            ) {
                $actualIds += [string]$response.knowledgeItemId
            }

            if (
                $response.PSObject.Properties.Name -contains "suggestions" -and
                $null -ne $response.suggestions
            ) {
                foreach ($suggestion in @($response.suggestions)) {
                    if (
                        $null -ne $suggestion -and
                        $suggestion.PSObject.Properties.Name -contains "knowledgeItemId" -and
                        $null -ne $suggestion.knowledgeItemId
                    ) {
                        $actualIds += [string]$suggestion.knowledgeItemId
                    }
                }
            }
        }
    }
    catch {
        $parseError = $_.Exception.Message
    }

    $actualIds = @(
        $actualIds |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        Select-Object -Unique
    )

    $isPositive = $row.Type -eq "Positive"
    $expectedId = [string]$row.ExpectedKnowledgeItemId

    if ($isPositive) {
        $passed =
            $httpCode -eq 200 -and
            -not [string]::IsNullOrWhiteSpace($expectedId) -and
            ($actualIds -contains $expectedId)
    }
    else {
        $passed =
            $httpCode -eq 200 -and
            ($responseType -eq "Fallback" -or $responseType -eq "NoMatch")
    }

    $actualIdText = $actualIds -join ";"

    $results.Add(
        [pscustomobject]@{
            Id                      = $row.Id
            Type                    = $row.Type
            Difficulty              = $row.Difficulty
            Category                = $row.Category
            Intent                  = $row.Intent
            UserQuestion            = $row.UserQuestion
            ExpectedKnowledgeItemId = $expectedId
            ActualKnowledgeItemIds  = $actualIdText
            ResponseType            = $responseType
            Passed                  = [bool]$passed
            HttpCode                = $httpCode
            LatencyMs               = [math]::Round($latencyMs, 2)
            Reply                   = $reply
            ParseError              = $parseError
        }
    )

    $state = if ($passed) { "PASS" } else { "FAIL" }

    Write-Progress `
        -Activity "Chatbot 500-FAQ benchmark" `
        -Status "$index / $($rows.Count) - $state - $($row.Id)" `
        -PercentComplete (($index / $rows.Count) * 100)

    Write-Host ("[{0}/{1}] {2} {3} {4:N0} ms" -f `
        $index, $rows.Count, $row.Id, $state, $latencyMs)
}

Write-Progress -Activity "Chatbot 500-FAQ benchmark" -Completed

$total = $results.Count
$passedCount = @($results | Where-Object Passed).Count

$positives = @($results | Where-Object Type -eq "Positive")
$positivePassed = @($positives | Where-Object Passed).Count

$negatives = @($results | Where-Object Type -eq "Negative")
$negativePassed = @($negatives | Where-Object Passed).Count
$falsePositives = $negatives.Count - $negativePassed

$latencies = [double[]]@($results | ForEach-Object { [double]$_.LatencyMs })

$accuracy = if ($total -gt 0) {
    ($passedCount / $total) * 100.0
} else { 0 }

$positiveRecall = if ($positives.Count -gt 0) {
    ($positivePassed / $positives.Count) * 100.0
} else { 0 }

$negativeRejection = if ($negatives.Count -gt 0) {
    ($negativePassed / $negatives.Count) * 100.0
} else { 0 }

$falsePositiveRate = if ($negatives.Count -gt 0) {
    ($falsePositives / $negatives.Count) * 100.0
} else { 0 }

$averageLatency = if ($latencies.Count -gt 0) {
    ($latencies | Measure-Object -Average).Average
} else { 0 }

$p50 = Get-Percentile -Values $latencies -Percentile 50
$p95 = Get-Percentile -Values $latencies -Percentile 95

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"

$detailsPath = Join-Path $OutputDirectory "benchmark-details-$timestamp.csv"
$summaryPath = Join-Path $OutputDirectory "benchmark-summary-$timestamp.json"

$results |
    Export-Csv `
        -Path $detailsPath `
        -NoTypeInformation `
        -Encoding UTF8

$summary = [ordered]@{
    CorpusAnswerFaqCount    = 500
    BenchmarkTotal          = $total
    Passed                  = $passedCount
    Failed                  = $total - $passedCount
    AccuracyPercent         = [math]::Round($accuracy, 2)
    PositiveTotal           = $positives.Count
    PositivePassed          = $positivePassed
    PositiveRecallPercent   = [math]::Round($positiveRecall, 2)
    NegativeTotal           = $negatives.Count
    NegativeRejected        = $negativePassed
    FalsePositives          = $falsePositives
    NegativeRejectionPercent= [math]::Round($negativeRejection, 2)
    FalsePositiveRatePercent= [math]::Round($falsePositiveRate, 2)
    AverageLatencyMs        = [math]::Round($averageLatency, 2)
    P50LatencyMs            = [math]::Round($p50, 2)
    P95LatencyMs            = [math]::Round($p95, 2)
    Endpoint                = $Endpoint
    GeneratedAt             = (Get-Date).ToString("o")
}

$summary |
    ConvertTo-Json -Depth 5 |
    Set-Content `
        -Path $summaryPath `
        -Encoding UTF8

Remove-Item $requestPath -ErrorAction SilentlyContinue
Remove-Item $responsePath -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "================ BENCHMARK SUMMARY ================"
Write-Host ("Total:              {0}" -f $total)
Write-Host ("Passed:             {0}" -f $passedCount)
Write-Host ("Failed:             {0}" -f ($total - $passedCount))
Write-Host ("Accuracy:           {0:N2}%" -f $accuracy)
Write-Host ("Positive recall:    {0:N2}% ({1}/{2})" -f `
    $positiveRecall, $positivePassed, $positives.Count)
Write-Host ("Negative rejection: {0:N2}% ({1}/{2})" -f `
    $negativeRejection, $negativePassed, $negatives.Count)
Write-Host ("False positives:    {0} ({1:N2}%)" -f `
    $falsePositives, $falsePositiveRate)
Write-Host ("Average latency:    {0:N2} ms" -f $averageLatency)
Write-Host ("P50 latency:        {0:N2} ms" -f $p50)
Write-Host ("P95 latency:        {0:N2} ms" -f $p95)
Write-Host ""
Write-Host "Details: $detailsPath"
Write-Host "Summary: $summaryPath"
