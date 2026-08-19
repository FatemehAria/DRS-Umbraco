param(
    [string]$BaseUrl = "https://localhost:44398",
    [double]$Threshold = 0.85,
    [string]$OutputCsv = ".\chatbot-semantic-score-diagnostic.csv"
)

$ErrorActionPreference = "Stop"

chcp 65001 > $null
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)

$SemanticTopUrl = "$BaseUrl/api/chatbot/debug/semantic-top?limit=3"
$Utf8NoBom = New-Object System.Text.UTF8Encoding($false)

function Invoke-ChatbotPost {
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
            try { $body = $raw | ConvertFrom-Json } catch { }
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

function New-ScoreCase {
    param(
        [int]$Number,
        [string]$Group,
        [string]$Question,
        [string]$ExpectedIntent = ""
    )

    [pscustomobject]@{
        Number         = $Number
        Group          = $Group
        Question       = $Question
        ExpectedIntent = $ExpectedIntent
    }
}

$Cases = @(
    New-ScoreCase -Number 1  -Group "Clear" -Question "رمز فعلی حسابم را می‌خواهم عوض کنم؛ از کدام بخش باید بروم؟" -ExpectedIntent "ChangePassword"
    New-ScoreCase -Number 2  -Group "Clear" -Question "میشه برای اکانتم یک پسورد جدید تعیین کنم؟" -ExpectedIntent "ChangePassword"
    New-ScoreCase -Number 3  -Group "Clear" -Question "برای تغییر دادن گذرواژه حساب چه مراحلی باید انجام بدم؟" -ExpectedIntent "ChangePassword"
    New-ScoreCase -Number 4  -Group "Clear" -Question "اگر برای حسابم مشکلی داشتم از چه راهی می‌تونم با تیم پشتیبانی در ارتباط باشم؟" -ExpectedIntent "Support"
    New-ScoreCase -Number 5  -Group "Clear" -Question "راهی برای ارتباط مستقیم با پشتیبانی سایت هست؟" -ExpectedIntent "Support"
    New-ScoreCase -Number 6  -Group "Clear" -Question "برای گرفتن کمک از پشتیبانی باید از کجا اقدام کنم؟" -ExpectedIntent "Support"
    New-ScoreCase -Number 7  -Group "Clear" -Question "پسوردم یادم نیست؛ چطور دوباره به حسابم دسترسی پیدا کنم؟" -ExpectedIntent "ForgotPassword"
    New-ScoreCase -Number 8  -Group "Clear" -Question "رمز ورودم رو فراموش کردم و نمی‌تونم وارد بشم، باید چی کار کنم؟" -ExpectedIntent "ForgotPassword"
    New-ScoreCase -Number 9  -Group "Clear" -Question "برای بازیابی گذرواژه‌ای که یادم رفته از کجا شروع کنم؟" -ExpectedIntent "ForgotPassword"
    New-ScoreCase -Number 10 -Group "Clear" -Question "نام کاربری و رمز رو وارد می‌کنم اما وارد پنل نمی‌شم؛ مشکل از کجاست؟" -ExpectedIntent "LoginProblem"
    New-ScoreCase -Number 11 -Group "Clear" -Question "صفحه ورود اجازه ورود به حسابم رو نمی‌ده، چه کاری انجام بدم؟" -ExpectedIntent "LoginProblem"
    New-ScoreCase -Number 12 -Group "Clear" -Question "اطلاعات ورودم رو دارم ولی سیستم وارد حسابم نمی‌کنه." -ExpectedIntent "LoginProblem"
    New-ScoreCase -Number 13 -Group "Clear" -Question "پیام می‌ده حسابم قفل شده؛ چطور دوباره فعالش کنم؟" -ExpectedIntent "LockedAccount"
    New-ScoreCase -Number 14 -Group "Clear" -Question "به خاطر چند بار ورود ناموفق اکانتم بسته شده، باید چه کار کنم؟" -ExpectedIntent "LockedAccount"
    New-ScoreCase -Number 15 -Group "Clear" -Question "سیستم می‌گه دسترسی حسابم مسدود شده؛ راه باز کردنش چیه؟" -ExpectedIntent "LockedAccount"
    New-ScoreCase -Number 16 -Group "Clear" -Question "ایمیلی که روی حسابم ثبت شده قدیمیه؛ چطور ایمیل جدید ثبت کنم؟" -ExpectedIntent "ChangeEmail"
    New-ScoreCase -Number 17 -Group "Clear" -Question "می‌خوام آدرس ایمیل اکانتم رو با یک ایمیل دیگه جایگزین کنم." -ExpectedIntent "ChangeEmail"
    New-ScoreCase -Number 18 -Group "Clear" -Question "از کجا می‌تونم ایمیل متصل به پروفایلم رو عوض کنم؟" -ExpectedIntent "ChangeEmail"
    New-ScoreCase -Number 19 -Group "Clear" -Question "سیم‌کارتم عوض شده و می‌خوام شماره جدیدم رو روی حساب ثبت کنم." -ExpectedIntent "ChangeMobile"
    New-ScoreCase -Number 20 -Group "Clear" -Question "شماره‌ای که در اکانتم ذخیره شده اشتباهه؛ چطور اصلاحش کنم؟" -ExpectedIntent "ChangeMobile"
    New-ScoreCase -Number 21 -Group "Clear" -Question "می‌خوام شماره تماس حسابم رو با شماره تازه جایگزین کنم." -ExpectedIntent "ChangeMobile"
    New-ScoreCase -Number 22 -Group "Clear" -Question "نام و مشخصات پروفایلم رو از چه بخشی می‌تونم ویرایش کنم؟" -ExpectedIntent "EditProfile"
    New-ScoreCase -Number 23 -Group "Clear" -Question "چند تا از اطلاعات شخصی داخل پروفایلم اشتباهه؛ چطور اصلاحشون کنم؟" -ExpectedIntent "EditProfile"
    New-ScoreCase -Number 24 -Group "Clear" -Question "برای به‌روزرسانی مشخصات حساب کاربری باید کجا برم؟" -ExpectedIntent "EditProfile"
    New-ScoreCase -Number 25 -Group "Clear" -Question "هنوز حساب ندارم؛ از کجا باید ثبت‌نام کنم؟" -ExpectedIntent "CreateAccount"
    New-ScoreCase -Number 26 -Group "Clear" -Question "برای اولین بار می‌خوام عضو سایت بشم، مراحل ساخت اکانت چیه؟" -ExpectedIntent "CreateAccount"
    New-ScoreCase -Number 27 -Group "Clear" -Question "چطور می‌تونم برای خودم یک پروفایل کاربری جدید بسازم؟" -ExpectedIntent "CreateAccount"
    New-ScoreCase -Number 28 -Group "Clear" -Question "دیگه نمی‌خوام حسابم فعال باشه؛ چطور برای همیشه حذفش کنم؟" -ExpectedIntent "DeleteAccount"
    New-ScoreCase -Number 29 -Group "Clear" -Question "می‌خوام اکانتم رو به‌طور کامل پاک کنم، از کجا باید اقدام کنم؟" -ExpectedIntent "DeleteAccount"
    New-ScoreCase -Number 30 -Group "Clear" -Question "راه بستن و حذف دائمی حساب کاربری چیه؟" -ExpectedIntent "DeleteAccount"

    New-ScoreCase -Number 41 -Group "NoMatch" -Question "امروز هوا چطوره؟"
    New-ScoreCase -Number 42 -Group "NoMatch" -Question "سفارشم چه زمانی ارسال می‌شود؟"
    New-ScoreCase -Number 43 -Group "NoMatch" -Question "چطور فاکتور خریدم را دانلود کنم؟"
    New-ScoreCase -Number 44 -Group "NoMatch" -Question "قیمت اشتراک سایت چقدره؟"
    New-ScoreCase -Number 45 -Group "NoMatch" -Question "چطور زبان سایت رو تغییر بدم؟"
    New-ScoreCase -Number 46 -Group "NoMatch" -Question "آدرس شعبه حضوری شما کجاست؟"
    New-ScoreCase -Number 47 -Group "NoMatch" -Question "ساعت کاری مجموعه چه زمانی است؟"
    New-ScoreCase -Number 48 -Group "NoMatch" -Question "چطور محصولی که خریدم رو مرجوع کنم؟"
    New-ScoreCase -Number 49 -Group "NoMatch" -Question "کد تخفیف از کجا می‌تونم بگیرم؟"
    New-ScoreCase -Number 50 -Group "NoMatch" -Question "برای استخدام در مجموعه باید از کجا اقدام کنم؟"
)

$Results = @()

Write-Host ""
Write-Host "========================================"
Write-Host "Semantic Score Diagnostic"
Write-Host "Threshold: $Threshold"
Write-Host "========================================"
Write-Host ""

foreach ($case in $Cases) {
    try {
        $response = Invoke-ChatbotPost `
            -Url $SemanticTopUrl `
            -Payload @{ Message = $case.Question }

        $top1Score = $null
        $top1Id = ""
        $top1Text = ""
        $top2Score = $null
        $top2Text = ""
        $top3Score = $null
        $top3Text = ""

        if ($response.StatusCode -eq 200 -and $null -ne $response.Body) {
            $items = @($response.Body.results)

            if ($items.Count -gt 0) {
                $top1Score = [double]$items[0].score
                $top1Id = [string]$items[0].knowledgeItemId
                $top1Text = [string]$items[0].matchedText
            }

            if ($items.Count -gt 1) {
                $top2Score = [double]$items[1].score
                $top2Text = [string]$items[1].matchedText
            }

            if ($items.Count -gt 2) {
                $top3Score = [double]$items[2].score
                $top3Text = [string]$items[2].matchedText
            }
        }

        $decisionAtCurrentThreshold = ""

        if ($null -ne $top1Score) {
            if ($top1Score -lt $Threshold) {
                $decisionAtCurrentThreshold = "NoMatch"
            }
            else {
                $decisionAtCurrentThreshold = "InDomain"
            }
        }

        $Results += [pscustomobject]@{
            Number = $case.Number
            Group = $case.Group
            ExpectedIntent = $case.ExpectedIntent
            Question = $case.Question
            Top1Score = $top1Score
            CurrentDecision = $decisionAtCurrentThreshold
            Top1KnowledgeItemId = $top1Id
            Top1MatchedText = $top1Text
            Top2Score = $top2Score
            Top2MatchedText = $top2Text
            Top3Score = $top3Score
            Top3MatchedText = $top3Text
        }

        $scoreText = if ($null -eq $top1Score) { "n/a" } else { "{0:N4}" -f $top1Score }

        Write-Host ("#{0,-2} {1,-7} Top1={2}  Decision={3}" -f `
            $case.Number,
            $case.Group,
            $scoreText,
            $decisionAtCurrentThreshold)
    }
    catch {
        Write-Host ("#{0,-2} ERROR: {1}" -f $case.Number, $_.Exception.Message) -ForegroundColor Red

        $Results += [pscustomobject]@{
            Number = $case.Number
            Group = $case.Group
            ExpectedIntent = $case.ExpectedIntent
            Question = $case.Question
            Top1Score = $null
            CurrentDecision = "ERROR"
            Top1KnowledgeItemId = ""
            Top1MatchedText = ""
            Top2Score = $null
            Top2MatchedText = ""
            Top3Score = $null
            Top3MatchedText = ""
        }
    }
}

$Results | Export-Csv -Path $OutputCsv -NoTypeInformation -Encoding UTF8

$clearScores = @(
    $Results |
        Where-Object { $_.Group -eq "Clear" -and $null -ne $_.Top1Score } |
        ForEach-Object { [double]$_.Top1Score }
)

$noMatchScores = @(
    $Results |
        Where-Object { $_.Group -eq "NoMatch" -and $null -ne $_.Top1Score } |
        ForEach-Object { [double]$_.Top1Score }
)

Write-Host ""
Write-Host "========================================"
Write-Host "SUMMARY"
Write-Host "========================================"

if ($clearScores.Count -gt 0) {
    $clearMin = ($clearScores | Measure-Object -Minimum).Minimum
    $clearMax = ($clearScores | Measure-Object -Maximum).Maximum
    Write-Host ("Clear   min={0:N4} max={1:N4}" -f $clearMin, $clearMax)
}

if ($noMatchScores.Count -gt 0) {
    $noMatchMin = ($noMatchScores | Measure-Object -Minimum).Minimum
    $noMatchMax = ($noMatchScores | Measure-Object -Maximum).Maximum
    Write-Host ("NoMatch min={0:N4} max={1:N4}" -f $noMatchMin, $noMatchMax)
}

Write-Host ""
Write-Host "NoMatch cases currently classified InDomain:"
$Results |
    Where-Object { $_.Group -eq "NoMatch" -and $_.CurrentDecision -eq "InDomain" } |
    Select-Object Number, Top1Score, Top1MatchedText |
    Format-Table -AutoSize

Write-Host ""
Write-Host "Clear cases currently classified NoMatch:"
$Results |
    Where-Object { $_.Group -eq "Clear" -and $_.CurrentDecision -eq "NoMatch" } |
    Select-Object Number, ExpectedIntent, Top1Score, Top1MatchedText |
    Format-Table -AutoSize

Write-Host ""
Write-Host "CSV: $OutputCsv"
