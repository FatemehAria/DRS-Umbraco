param(
    [string]$BaseUrl = "https://localhost:44398",
    [double]$CurrentThreshold = 0.85,
    [string]$OutputCsv = ".\chatbot-answer-only-no-match-scores.csv"
)

$ErrorActionPreference = "Stop"
chcp 65001 > $null
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)

$Url = "$BaseUrl/api/chatbot/debug/answer-no-match-evidence"
$Utf8NoBom = New-Object System.Text.UTF8Encoding($false)

function Invoke-PostJson {
    param([string]$Url, [object]$Payload)

    $req = [System.IO.Path]::GetTempFileName()
    $res = [System.IO.Path]::GetTempFileName()

    try {
        $json = $Payload | ConvertTo-Json -Compress -Depth 6
        [System.IO.File]::WriteAllText($req, $json, $Utf8NoBom)

        $status = & curl.exe `
            -k `
            --ssl-no-revoke `
            --http1.1 `
            -sS `
            -o $res `
            -w "%{http_code}" `
            -X POST `
            -H "Content-Type: application/json; charset=utf-8" `
            --data-binary "@$req" `
            $Url

        if ($LASTEXITCODE -ne 0) {
            throw "curl.exe failed with exit code $LASTEXITCODE"
        }

        $code = [int](($status | Out-String).Trim())
        $raw = [System.IO.File]::ReadAllText($res, [System.Text.Encoding]::UTF8)
        $body = $null
        if (-not [string]::IsNullOrWhiteSpace($raw)) {
            $body = $raw | ConvertFrom-Json
        }

        [pscustomobject]@{
            StatusCode = $code
            Body = $body
        }
    }
    finally {
        Remove-Item $req -Force -ErrorAction SilentlyContinue
        Remove-Item $res -Force -ErrorAction SilentlyContinue
    }
}

function C {
    param([int]$N, [string]$G, [string]$Q)
    [pscustomobject]@{ Number=$N; Group=$G; Question=$Q }
}

$Cases = @(
    C 1  "Clear" "رمز فعلی حسابم را می‌خواهم عوض کنم؛ از کدام بخش باید بروم؟"
    C 2  "Clear" "میشه برای اکانتم یک پسورد جدید تعیین کنم؟"
    C 3  "Clear" "برای تغییر دادن گذرواژه حساب چه مراحلی باید انجام بدم؟"
    C 4  "Clear" "اگر برای حسابم مشکلی داشتم از چه راهی می‌تونم با تیم پشتیبانی در ارتباط باشم؟"
    C 5  "Clear" "راهی برای ارتباط مستقیم با پشتیبانی سایت هست؟"
    C 6  "Clear" "برای گرفتن کمک از پشتیبانی باید از کجا اقدام کنم؟"
    C 7  "Clear" "پسوردم یادم نیست؛ چطور دوباره به حسابم دسترسی پیدا کنم؟"
    C 8  "Clear" "رمز ورودم رو فراموش کردم و نمی‌تونم وارد بشم، باید چی کار کنم؟"
    C 9  "Clear" "برای بازیابی گذرواژه‌ای که یادم رفته از کجا شروع کنم؟"
    C 10 "Clear" "نام کاربری و رمز رو وارد می‌کنم اما وارد پنل نمی‌شم؛ مشکل از کجاست؟"
    C 11 "Clear" "صفحه ورود اجازه ورود به حسابم رو نمی‌ده، چه کاری انجام بدم؟"
    C 12 "Clear" "اطلاعات ورودم رو دارم ولی سیستم وارد حسابم نمی‌کنه."
    C 13 "Clear" "پیام می‌ده حسابم قفل شده؛ چطور دوباره فعالش کنم؟"
    C 14 "Clear" "به خاطر چند بار ورود ناموفق اکانتم بسته شده، باید چه کار کنم؟"
    C 15 "Clear" "سیستم می‌گه دسترسی حسابم مسدود شده؛ راه باز کردنش چیه؟"
    C 16 "Clear" "ایمیلی که روی حسابم ثبت شده قدیمیه؛ چطور ایمیل جدید ثبت کنم؟"
    C 17 "Clear" "می‌خوام آدرس ایمیل اکانتم رو با یک ایمیل دیگه جایگزین کنم."
    C 18 "Clear" "از کجا می‌تونم ایمیل متصل به پروفایلم رو عوض کنم؟"
    C 19 "Clear" "سیم‌کارتم عوض شده و می‌خوام شماره جدیدم رو روی حساب ثبت کنم."
    C 20 "Clear" "شماره‌ای که در اکانتم ذخیره شده اشتباهه؛ چطور اصلاحش کنم؟"
    C 21 "Clear" "می‌خوام شماره تماس حسابم رو با شماره تازه جایگزین کنم."
    C 22 "Clear" "نام و مشخصات پروفایلم رو از چه بخشی می‌تونم ویرایش کنم؟"
    C 23 "Clear" "چند تا از اطلاعات شخصی داخل پروفایلم اشتباهه؛ چطور اصلاحشون کنم؟"
    C 24 "Clear" "برای به‌روزرسانی مشخصات حساب کاربری باید کجا برم؟"
    C 25 "Clear" "هنوز حساب ندارم؛ از کجا باید ثبت‌نام کنم؟"
    C 26 "Clear" "برای اولین بار می‌خوام عضو سایت بشم، مراحل ساخت اکانت چیه؟"
    C 27 "Clear" "چطور می‌تونم برای خودم یک پروفایل کاربری جدید بسازم؟"
    C 28 "Clear" "دیگه نمی‌خوام حسابم فعال باشه؛ چطور برای همیشه حذفش کنم؟"
    C 29 "Clear" "می‌خوام اکانتم رو به‌طور کامل پاک کنم، از کجا باید اقدام کنم؟"
    C 30 "Clear" "راه بستن و حذف دائمی حساب کاربری چیه؟"

    C 41 "NoMatch" "امروز هوا چطوره؟"
    C 42 "NoMatch" "سفارشم چه زمانی ارسال می‌شود؟"
    C 43 "NoMatch" "چطور فاکتور خریدم را دانلود کنم؟"
    C 44 "NoMatch" "قیمت اشتراک سایت چقدره؟"
    C 45 "NoMatch" "چطور زبان سایت رو تغییر بدم؟"
    C 46 "NoMatch" "آدرس شعبه حضوری شما کجاست؟"
    C 47 "NoMatch" "ساعت کاری مجموعه چه زمانی است؟"
    C 48 "NoMatch" "چطور محصولی که خریدم رو مرجوع کنم؟"
    C 49 "NoMatch" "کد تخفیف از کجا می‌تونم بگیرم؟"
    C 50 "NoMatch" "برای استخدام در مجموعه باید از کجا اقدام کنم؟"
)

$Results = @()

Write-Host ""
Write-Host "========================================"
Write-Host "Answer-only NoMatch Diagnostic"
Write-Host "Threshold: $CurrentThreshold"
Write-Host "========================================"
Write-Host ""

foreach ($case in $Cases) {
    try {
        $response = Invoke-PostJson -Url $Url -Payload @{ message = $case.Question }

        if ($response.StatusCode -ne 200 -or $null -eq $response.Body) {
            throw "HTTP $($response.StatusCode)"
        }

        $score = [double]$response.Body.semanticTopScore
        $decision = if ($score -lt $CurrentThreshold) { "NoMatch" } else { "InDomain" }

        $semanticCandidates = @(
            $response.Body.candidates |
                Where-Object { $null -ne $_.semanticScore } |
                Sort-Object semanticScore -Descending
        )

        $top1Text = ""
        if ($semanticCandidates.Count -gt 0) {
            $top1Text = [string]$semanticCandidates[0].semanticMatchedText
        }

        $Results += [pscustomobject]@{
            Number = $case.Number
            Group = $case.Group
            Question = $case.Question
            SemanticTopScore = $score
            CurrentDecision = $decision
            Top1MatchedText = $top1Text
        }

        Write-Host ("#{0,-2} {1,-7} Score={2:N4}  Decision={3}" -f `
            $case.Number, $case.Group, $score, $decision)
    }
    catch {
        Write-Host ("#{0,-2} ERROR: {1}" -f $case.Number, $_.Exception.Message) -ForegroundColor Red
    }
}

$Results | Export-Csv -Path $OutputCsv -NoTypeInformation -Encoding UTF8

$clearScores = @(
    $Results |
        Where-Object { $_.Group -eq "Clear" } |
        ForEach-Object { [double]$_.SemanticTopScore }
)

$noMatchScores = @(
    $Results |
        Where-Object { $_.Group -eq "NoMatch" } |
        ForEach-Object { [double]$_.SemanticTopScore }
)

$clearMin = ($clearScores | Measure-Object -Minimum).Minimum
$clearMax = ($clearScores | Measure-Object -Maximum).Maximum
$noMatchMin = ($noMatchScores | Measure-Object -Minimum).Minimum
$noMatchMax = ($noMatchScores | Measure-Object -Maximum).Maximum

Write-Host ""
Write-Host "========================================"
Write-Host "SUMMARY"
Write-Host "========================================"
Write-Host ("Clear   min={0:N4} max={1:N4}" -f $clearMin, $clearMax)
Write-Host ("NoMatch min={0:N4} max={1:N4}" -f $noMatchMin, $noMatchMax)
Write-Host ("Gap     {0:N4}" -f ($clearMin - $noMatchMax))

Write-Host ""
Write-Host "NoMatch currently classified InDomain:"
$Results |
    Where-Object { $_.Group -eq "NoMatch" -and $_.CurrentDecision -eq "InDomain" } |
    Select-Object Number, SemanticTopScore, Top1MatchedText |
    Format-Table -AutoSize

Write-Host ""
Write-Host "Clear currently classified NoMatch:"
$Results |
    Where-Object { $_.Group -eq "Clear" -and $_.CurrentDecision -eq "NoMatch" } |
    Select-Object Number, SemanticTopScore, Top1MatchedText |
    Format-Table -AutoSize

Write-Host ""
Write-Host "CSV: $OutputCsv"
