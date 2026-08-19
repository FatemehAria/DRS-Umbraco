param(
    [string]$BaseUrl = "https://localhost:44398",
    [string]$OutputCsv = ".\chatbot-holdout-d-failure-diagnostic.csv"
)

$ErrorActionPreference = "Stop"

chcp 65001 > $null
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)

$MessagesUrl = "$BaseUrl/api/chatbot/messages"
$RankingUrl = "$BaseUrl/api/chatbot/debug/answer-ranking-evidence"
$NoMatchEvidenceUrl = "$BaseUrl/api/chatbot/debug/answer-no-match-evidence"

$Utf8NoBom = New-Object System.Text.UTF8Encoding($false)

$IntentIds = @{
    ChangePassword = "750cffa0-e281-41b5-b639-9b4ad041562c"
    ForgotPassword = "528778ab-e075-4ee0-bf31-fc9bc5326c7a"
    CreateAccount  = "244bd9a6-601d-41a4-98d3-276c2ea37f65"
    DeleteAccount  = "d59c6ca1-2509-405b-844f-bd0dec63416c"
}

function Invoke-JsonPost {
    param(
        [Parameter(Mandatory = $true)][string]$Url,
        [Parameter(Mandatory = $true)][object]$Payload
    )

    $requestPath = [System.IO.Path]::GetTempFileName()
    $responsePath = [System.IO.Path]::GetTempFileName()

    try {
        $json = $Payload | ConvertTo-Json -Depth 8 -Compress
        [System.IO.File]::WriteAllText($requestPath, $json, $Utf8NoBom)

        $statusText = & curl.exe `
            -k `
            --ssl-no-revoke `
            --http1.1 `
            -sS `
            -o $responsePath `
            -w "%{http_code}" `
            -X POST `
            -H "Content-Type: application/json; charset=utf-8" `
            --data-binary "@$requestPath" `
            $Url

        if ($LASTEXITCODE -ne 0) {
            throw "curl.exe failed with exit code $LASTEXITCODE."
        }

        $statusCode = [int](($statusText | Out-String).Trim())
        $raw = [System.IO.File]::ReadAllText(
            $responsePath,
            [System.Text.Encoding]::UTF8
        )

        $body = $null

        if (-not [string]::IsNullOrWhiteSpace($raw)) {
            try {
                $body = $raw | ConvertFrom-Json
            }
            catch {
                throw "Response was not valid JSON: $raw"
            }
        }

        [pscustomobject]@{
            StatusCode = $statusCode
            Body       = $body
            Raw        = $raw
        }
    }
    finally {
        Remove-Item $requestPath -Force -ErrorAction SilentlyContinue
        Remove-Item $responsePath -Force -ErrorAction SilentlyContinue
    }
}

function Get-RrfScore {
    param([object]$Candidate)

    $k = 60.0
    $score = 0.0

    foreach ($rankName in @(
        "semanticRank",
        "centroidRank",
        "weightedLexicalRank",
        "bm25Rank"
    )) {
        $rank = $Candidate.$rankName

        if ($null -ne $rank) {
            $score += 1.0 / ($k + [double]$rank)
        }
    }

    return $score
}

function Get-StrategyCount {
    param([object]$Candidate)

    $count = 0

    foreach ($rankName in @(
        "semanticRank",
        "centroidRank",
        "weightedLexicalRank",
        "bm25Rank"
    )) {
        if ($null -ne $Candidate.$rankName) {
            $count++
        }
    }

    return $count
}

$Cases = @(
    [pscustomobject]@{
        Number = 2
        Group = "Clear"
        ExpectedIntent = "ChangePassword"
        ExpectedId = $IntentIds["ChangePassword"]
        Question = "از کجای حساب می‌تونم یک گذرواژه تازه برای ورود تنظیم کنم؟"
    },
    [pscustomobject]@{
        Number = 4
        Group = "Clear"
        ExpectedIntent = "ForgotPassword"
        ExpectedId = $IntentIds["ForgotPassword"]
        Question = "رمز ورودم از ذهنم رفته و الان نمی‌دونم چطور وارد حساب بشم."
    },
    [pscustomobject]@{
        Number = 13
        Group = "Clear"
        ExpectedIntent = "CreateAccount"
        ExpectedId = $IntentIds["CreateAccount"]
        Question = "هنوز عضو نیستم و می‌خوام یک اکانت برای ورود به سایت بسازم."
    },
    [pscustomobject]@{
        Number = 14
        Group = "Clear"
        ExpectedIntent = "DeleteAccount"
        ExpectedId = $IntentIds["DeleteAccount"]
        Question = "می‌خوام حسابم کاملاً بسته بشه و دیگه روی سایت وجود نداشته باشه."
    },
    [pscustomobject]@{
        Number = 21
        Group = "NoMatch"
        ExpectedIntent = ""
        ExpectedId = ""
        Question = "چطور احراز هویت دو مرحله‌ای رو برای حسابم فعال کنم؟"
    },
    [pscustomobject]@{
        Number = 22
        Group = "NoMatch"
        ExpectedIntent = ""
        ExpectedId = ""
        Question = "چطور اعلان‌های ایمیلی سایت رو غیرفعال کنم؟"
    },
    [pscustomobject]@{
        Number = 23
        Group = "NoMatch"
        ExpectedIntent = ""
        ExpectedId = ""
        Question = "گزارش فعالیت‌های اخیر حسابم رو از کجا ببینم؟"
    },
    [pscustomobject]@{
        Number = 24
        Group = "NoMatch"
        ExpectedIntent = ""
        ExpectedId = ""
        Question = "چه مرورگرهایی برای استفاده از سایت پشتیبانی می‌شن؟"
    }
)

$Rows = @()

Write-Host ""
Write-Host "========================================"
Write-Host "Holdout D Failure Diagnostic"
Write-Host "========================================"
Write-Host ""

foreach ($case in $Cases) {
    Write-Host ("#{0} [{1}] {2}" -f $case.Number, $case.Group, $case.Question) -ForegroundColor Cyan

    try {
        $messageResponse = Invoke-JsonPost `
            -Url $MessagesUrl `
            -Payload @{ message = $case.Question }

        $rankingResponse = Invoke-JsonPost `
            -Url $RankingUrl `
            -Payload @{ message = $case.Question }

        $noMatchResponse = Invoke-JsonPost `
            -Url $NoMatchEvidenceUrl `
            -Payload @{ message = $case.Question }

        if ($messageResponse.StatusCode -ne 200) {
            throw "Messages endpoint returned HTTP $($messageResponse.StatusCode)"
        }

        if ($rankingResponse.StatusCode -ne 200) {
            throw "Ranking endpoint returned HTTP $($rankingResponse.StatusCode)"
        }

        if ($noMatchResponse.StatusCode -ne 200) {
            throw "NoMatch evidence endpoint returned HTTP $($noMatchResponse.StatusCode)"
        }

        $responseType = [string]$messageResponse.Body.responseType
        $semanticTopScore = [double]$noMatchResponse.Body.semanticTopScore

        $production = @($rankingResponse.Body.production)
        $expanded = @($rankingResponse.Body.expanded)

        $productionTop1 = if ($production.Count -ge 1) { $production[0] } else { $null }
        $productionTop2 = if ($production.Count -ge 2) { $production[1] } else { $null }
        $productionTop3 = if ($production.Count -ge 3) { $production[2] } else { $null }

        $expected = $null

        if ($case.Group -eq "Clear") {
            $expected = $expanded |
                Where-Object {
                    ([string]$_.knowledgeItemId) -ieq $case.ExpectedId
                } |
                Select-Object -First 1
        }

        Write-Host ("  ResponseType: {0}" -f $responseType)
        Write-Host ("  SemanticTopScore: {0:N6}" -f $semanticTopScore)

        if ($production.Count -gt 0) {
            Write-Host "  Production order:"
            foreach ($candidate in $production) {
                $rrf = Get-RrfScore $candidate
                $strategyCount = Get-StrategyCount $candidate

                Write-Host (
                    "    #{0} {1} | RRF={2:N6} | S={3} C={4} L={5} B={6} | strategies={7}" -f `
                    $candidate.position,
                    $candidate.knowledgeItemId,
                    $rrf,
                    $candidate.semanticRank,
                    $candidate.centroidRank,
                    $candidate.weightedLexicalRank,
                    $candidate.bm25Rank,
                    $strategyCount
                )
            }
        }

        if ($case.Group -eq "Clear") {
            if ($null -eq $expected) {
                Write-Host "  Expected candidate: NOT FOUND even in expanded results" -ForegroundColor Red
            }
            else {
                $expectedRrf = Get-RrfScore $expected
                $expectedStrategyCount = Get-StrategyCount $expected

                Write-Host (
                    "  Expected {0}: expanded position={1}, RRF={2:N6}, S={3}, C={4}, L={5}, B={6}, strategies={7}" -f `
                    $case.ExpectedIntent,
                    $expected.position,
                    $expectedRrf,
                    $expected.semanticRank,
                    $expected.centroidRank,
                    $expected.weightedLexicalRank,
                    $expected.bm25Rank,
                    $expectedStrategyCount
                ) -ForegroundColor Yellow
            }
        }

        $Rows += [pscustomobject]@{
            Number = $case.Number
            Group = $case.Group
            ExpectedIntent = $case.ExpectedIntent
            Question = $case.Question
            ResponseType = $responseType
            SemanticTopScore = $semanticTopScore

            Top1Id = if ($null -ne $productionTop1) { [string]$productionTop1.knowledgeItemId } else { "" }
            Top1Rrf = if ($null -ne $productionTop1) { Get-RrfScore $productionTop1 } else { $null }

            Top2Id = if ($null -ne $productionTop2) { [string]$productionTop2.knowledgeItemId } else { "" }
            Top2Rrf = if ($null -ne $productionTop2) { Get-RrfScore $productionTop2 } else { $null }

            Top3Id = if ($null -ne $productionTop3) { [string]$productionTop3.knowledgeItemId } else { "" }
            Top3Rrf = if ($null -ne $productionTop3) { Get-RrfScore $productionTop3 } else { $null }

            ExpectedFoundExpanded = ($null -ne $expected)
            ExpectedExpandedPosition = if ($null -ne $expected) { $expected.position } else { $null }
            ExpectedSemanticRank = if ($null -ne $expected) { $expected.semanticRank } else { $null }
            ExpectedCentroidRank = if ($null -ne $expected) { $expected.centroidRank } else { $null }
            ExpectedWeightedLexicalRank = if ($null -ne $expected) { $expected.weightedLexicalRank } else { $null }
            ExpectedBm25Rank = if ($null -ne $expected) { $expected.bm25Rank } else { $null }
            ExpectedRrf = if ($null -ne $expected) { Get-RrfScore $expected } else { $null }
            ExpectedStrategyCount = if ($null -ne $expected) { Get-StrategyCount $expected } else { 0 }
        }

        Write-Host ""
    }
    catch {
        Write-Host ("  ERROR: {0}" -f $_.Exception.Message) -ForegroundColor Red
        Write-Host ""
    }
}

$Rows | Export-Csv `
    -Path $OutputCsv `
    -NoTypeInformation `
    -Encoding UTF8

Write-Host "========================================"
Write-Host "COMPACT SUMMARY"
Write-Host "========================================"

$Rows |
    Select-Object `
        Number,
        Group,
        ExpectedIntent,
        ResponseType,
        SemanticTopScore,
        ExpectedFoundExpanded,
        ExpectedExpandedPosition,
        ExpectedRrf,
        ExpectedStrategyCount |
    Format-Table -AutoSize

Write-Host ""
Write-Host "CSV: $OutputCsv"
