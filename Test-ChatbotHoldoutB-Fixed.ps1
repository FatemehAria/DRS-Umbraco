param(
    [string]$BaseUrl = "https://localhost:44398",
    [string]$OutputCsv = ".\chatbot-holdout-b-results.csv"
)

$ErrorActionPreference = "Stop"
chcp 65001 > $null
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)

$MessagesUrl = "$BaseUrl/api/chatbot/messages"
$SelectSuggestionUrl = "$BaseUrl/api/chatbot/suggestions/select"

$Utf8NoBom = New-Object System.Text.UTF8Encoding($false)

$IntentIds = @{
    ChangePassword = "750cffa0-e281-41b5-b639-9b4ad041562c"
    Support        = "5eeaa1f0-f478-4524-9626-06ca996af901"
    ForgotPassword = "528778ab-e075-4ee0-bf31-fc9bc5326c7a"
    LoginProblem   = "8551757a-28e8-4bf2-bf9b-f3aa473540c4"
    LockedAccount  = "9e137ce9-b2bf-41e1-945e-b9f5d5e93437"
    ChangeEmail    = "aaba78db-3981-400c-8e97-12bcf9ed7a8a"
    ChangeMobile   = "2361d83f-7c37-43fc-af41-7b0bb893db47"
    EditProfile    = "fb4c8c44-faf5-4b67-a5c6-0d1cacb4c531"
    CreateAccount  = "244bd9a6-601d-41a4-98d3-276c2ea37f65"
    DeleteAccount  = "d59c6ca1-2509-405b-844f-bd0dec63416c"
}

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
        $raw = [System.IO.File]::ReadAllText($responsePath, [System.Text.Encoding]::UTF8)
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

function New-HoldoutCase {
    param(
        [int]$Number,
        [string]$Group,
        [string]$Question,
        [string]$ExpectedIntent = ""
    )

    $expectedId = ""

    if (-not [string]::IsNullOrWhiteSpace($ExpectedIntent)) {
        $expectedId = [string]$IntentIds[$ExpectedIntent]
    }

    [pscustomobject]@{
        Number         = $Number
        Group          = $Group
        Question       = $Question
        ExpectedIntent = $ExpectedIntent
        ExpectedId     = $expectedId
    }
}

$Cases = @(
    New-HoldoutCase -Number 1 -Group "Clear" -Question "رمز فعلی حسابم را می‌خواهم عوض کنم؛ از کدام بخش باید بروم؟" -ExpectedIntent "ChangePassword"
    New-HoldoutCase -Number 2 -Group "Clear" -Question "میشه برای اکانتم یک پسورد جدید تعیین کنم؟" -ExpectedIntent "ChangePassword"
    New-HoldoutCase -Number 3 -Group "Clear" -Question "برای تغییر دادن گذرواژه حساب چه مراحلی باید انجام بدم؟" -ExpectedIntent "ChangePassword"
    New-HoldoutCase -Number 4 -Group "Clear" -Question "اگر برای حسابم مشکلی داشتم از چه راهی می‌تونم با تیم پشتیبانی در ارتباط باشم؟" -ExpectedIntent "Support"
    New-HoldoutCase -Number 5 -Group "Clear" -Question "راهی برای ارتباط مستقیم با پشتیبانی سایت هست؟" -ExpectedIntent "Support"
    New-HoldoutCase -Number 6 -Group "Clear" -Question "برای گرفتن کمک از پشتیبانی باید از کجا اقدام کنم؟" -ExpectedIntent "Support"
    New-HoldoutCase -Number 7 -Group "Clear" -Question "پسوردم یادم نیست؛ چطور دوباره به حسابم دسترسی پیدا کنم؟" -ExpectedIntent "ForgotPassword"
    New-HoldoutCase -Number 8 -Group "Clear" -Question "رمز ورودم رو فراموش کردم و نمی‌تونم وارد بشم، باید چی کار کنم؟" -ExpectedIntent "ForgotPassword"
    New-HoldoutCase -Number 9 -Group "Clear" -Question "برای بازیابی گذرواژه‌ای که یادم رفته از کجا شروع کنم؟" -ExpectedIntent "ForgotPassword"
    New-HoldoutCase -Number 10 -Group "Clear" -Question "نام کاربری و رمز رو وارد می‌کنم اما وارد پنل نمی‌شم؛ مشکل از کجاست؟" -ExpectedIntent "LoginProblem"
    New-HoldoutCase -Number 11 -Group "Clear" -Question "صفحه ورود اجازه ورود به حسابم رو نمی‌ده، چه کاری انجام بدم؟" -ExpectedIntent "LoginProblem"
    New-HoldoutCase -Number 12 -Group "Clear" -Question "اطلاعات ورودم رو دارم ولی سیستم وارد حسابم نمی‌کنه." -ExpectedIntent "LoginProblem"
    New-HoldoutCase -Number 13 -Group "Clear" -Question "پیام می‌ده حسابم قفل شده؛ چطور دوباره فعالش کنم؟" -ExpectedIntent "LockedAccount"
    New-HoldoutCase -Number 14 -Group "Clear" -Question "به خاطر چند بار ورود ناموفق اکانتم بسته شده، باید چه کار کنم؟" -ExpectedIntent "LockedAccount"
    New-HoldoutCase -Number 15 -Group "Clear" -Question "سیستم می‌گه دسترسی حسابم مسدود شده؛ راه باز کردنش چیه؟" -ExpectedIntent "LockedAccount"
    New-HoldoutCase -Number 16 -Group "Clear" -Question "ایمیلی که روی حسابم ثبت شده قدیمیه؛ چطور ایمیل جدید ثبت کنم؟" -ExpectedIntent "ChangeEmail"
    New-HoldoutCase -Number 17 -Group "Clear" -Question "می‌خوام آدرس ایمیل اکانتم رو با یک ایمیل دیگه جایگزین کنم." -ExpectedIntent "ChangeEmail"
    New-HoldoutCase -Number 18 -Group "Clear" -Question "از کجا می‌تونم ایمیل متصل به پروفایلم رو عوض کنم؟" -ExpectedIntent "ChangeEmail"
    New-HoldoutCase -Number 19 -Group "Clear" -Question "سیم‌کارتم عوض شده و می‌خوام شماره جدیدم رو روی حساب ثبت کنم." -ExpectedIntent "ChangeMobile"
    New-HoldoutCase -Number 20 -Group "Clear" -Question "شماره‌ای که در اکانتم ذخیره شده اشتباهه؛ چطور اصلاحش کنم؟" -ExpectedIntent "ChangeMobile"
    New-HoldoutCase -Number 21 -Group "Clear" -Question "می‌خوام شماره تماس حسابم رو با شماره تازه جایگزین کنم." -ExpectedIntent "ChangeMobile"
    New-HoldoutCase -Number 22 -Group "Clear" -Question "نام و مشخصات پروفایلم رو از چه بخشی می‌تونم ویرایش کنم؟" -ExpectedIntent "EditProfile"
    New-HoldoutCase -Number 23 -Group "Clear" -Question "چند تا از اطلاعات شخصی داخل پروفایلم اشتباهه؛ چطور اصلاحشون کنم؟" -ExpectedIntent "EditProfile"
    New-HoldoutCase -Number 24 -Group "Clear" -Question "برای به‌روزرسانی مشخصات حساب کاربری باید کجا برم؟" -ExpectedIntent "EditProfile"
    New-HoldoutCase -Number 25 -Group "Clear" -Question "هنوز حساب ندارم؛ از کجا باید ثبت‌نام کنم؟" -ExpectedIntent "CreateAccount"
    New-HoldoutCase -Number 26 -Group "Clear" -Question "برای اولین بار می‌خوام عضو سایت بشم، مراحل ساخت اکانت چیه؟" -ExpectedIntent "CreateAccount"
    New-HoldoutCase -Number 27 -Group "Clear" -Question "چطور می‌تونم برای خودم یک پروفایل کاربری جدید بسازم؟" -ExpectedIntent "CreateAccount"
    New-HoldoutCase -Number 28 -Group "Clear" -Question "دیگه نمی‌خوام حسابم فعال باشه؛ چطور برای همیشه حذفش کنم؟" -ExpectedIntent "DeleteAccount"
    New-HoldoutCase -Number 29 -Group "Clear" -Question "می‌خوام اکانتم رو به‌طور کامل پاک کنم، از کجا باید اقدام کنم؟" -ExpectedIntent "DeleteAccount"
    New-HoldoutCase -Number 30 -Group "Clear" -Question "راه بستن و حذف دائمی حساب کاربری چیه؟" -ExpectedIntent "DeleteAccount"
    New-HoldoutCase -Number 31 -Group "Ambiguous" -Question "می‌خوام اطلاعات حسابم رو تغییر بدم."
    New-HoldoutCase -Number 32 -Group "Ambiguous" -Question "یکی از اطلاعاتی که موقع ثبت‌نام وارد کردم اشتباهه."
    New-HoldoutCase -Number 33 -Group "Ambiguous" -Question "اطلاعات تماس حسابم رو می‌خوام عوض کنم."
    New-HoldoutCase -Number 34 -Group "Ambiguous" -Question "راه ارتباطی‌ای که روی حسابم ثبت شده دیگه درست نیست."
    New-HoldoutCase -Number 35 -Group "Ambiguous" -Question "برای ورود به حسابم یه مشکلی پیش اومده، باید چی کار کنم؟"
    New-HoldoutCase -Number 36 -Group "Ambiguous" -Question "نمی‌تونم مثل قبل از حسابم استفاده کنم."
    New-HoldoutCase -Number 37 -Group "Ambiguous" -Question "برای حساب کاربری‌ام یه تغییر لازم دارم، از کجا باید انجامش بدم؟"
    New-HoldoutCase -Number 38 -Group "Ambiguous" -Question "مشخصات حسابم نیاز به اصلاح داره."
    New-HoldoutCase -Number 39 -Group "Ambiguous" -Question "در مورد حسابم به مشکل خوردم و نمی‌دونم از کجا باید شروع کنم."
    New-HoldoutCase -Number 40 -Group "Ambiguous" -Question "یکی از چیزهایی که برای اکانتم ثبت شده رو می‌خوام عوض کنم."
    New-HoldoutCase -Number 41 -Group "NoMatch" -Question "امروز هوا چطوره؟"
    New-HoldoutCase -Number 42 -Group "NoMatch" -Question "سفارشم چه زمانی ارسال می‌شود؟"
    New-HoldoutCase -Number 43 -Group "NoMatch" -Question "چطور فاکتور خریدم را دانلود کنم؟"
    New-HoldoutCase -Number 44 -Group "NoMatch" -Question "قیمت اشتراک سایت چقدره؟"
    New-HoldoutCase -Number 45 -Group "NoMatch" -Question "چطور زبان سایت رو تغییر بدم؟"
    New-HoldoutCase -Number 46 -Group "NoMatch" -Question "آدرس شعبه حضوری شما کجاست؟"
    New-HoldoutCase -Number 47 -Group "NoMatch" -Question "ساعت کاری مجموعه چه زمانی است؟"
    New-HoldoutCase -Number 48 -Group "NoMatch" -Question "چطور محصولی که خریدم رو مرجوع کنم؟"
    New-HoldoutCase -Number 49 -Group "NoMatch" -Question "کد تخفیف از کجا می‌تونم بگیرم؟"
    New-HoldoutCase -Number 50 -Group "NoMatch" -Question "برای استخدام در مجموعه باید از کجا اقدام کنم؟"
    New-HoldoutCase -Number 51 -Group "Exact" -Question "چطور رمز عبورم را تغییر بدهم؟" -ExpectedIntent "ChangePassword"
    New-HoldoutCase -Number 52 -Group "Exact" -Question "چطور با پشتیبانی تماس بگیرم؟" -ExpectedIntent "Support"
    New-HoldoutCase -Number 53 -Group "Exact" -Question "رمز عبورم را فراموش کرده‌ام، چه کار کنم؟" -ExpectedIntent "ForgotPassword"
    New-HoldoutCase -Number 54 -Group "Exact" -Question "چرا نمی‌توانم وارد حساب کاربری شوم؟" -ExpectedIntent "LoginProblem"
    New-HoldoutCase -Number 55 -Group "Exact" -Question "حساب کاربری من قفل شده است، چه کار کنم؟" -ExpectedIntent "LockedAccount"
    New-HoldoutCase -Number 56 -Group "Exact" -Question "چطور ایمیل حساب کاربری را تغییر بدهم؟" -ExpectedIntent "ChangeEmail"
    New-HoldoutCase -Number 57 -Group "Exact" -Question "چطور شماره موبایل حسابم را تغییر بدهم؟" -ExpectedIntent "ChangeMobile"
    New-HoldoutCase -Number 58 -Group "Exact" -Question "چطور اطلاعات پروفایلم را ویرایش کنم؟" -ExpectedIntent "EditProfile"
    New-HoldoutCase -Number 59 -Group "Exact" -Question "چطور یک حساب کاربری ایجاد کنم؟" -ExpectedIntent "CreateAccount"
    New-HoldoutCase -Number 60 -Group "Exact" -Question "چطور حساب کاربری‌ام را حذف کنم؟" -ExpectedIntent "DeleteAccount"
)

Write-Host ""
Write-Host "========================================"
Write-Host "Chatbot Holdout B"
Write-Host "Base URL: $BaseUrl"
Write-Host "========================================"
Write-Host ""

$ExpectedAnswers = @{}
$SelectionResults = @()

Write-Host "Preflight: validating 10 known Answer IDs..." -ForegroundColor Cyan

foreach ($intentName in $IntentIds.Keys) {
    $id = $IntentIds[$intentName]

    try {
        $selection = Invoke-ChatbotPost -Url $SelectSuggestionUrl -Payload @{ knowledgeItemId = $id }

        $responseType = ""
        $reply = ""

        if ($null -ne $selection.Body) {
            $responseType = [string]$selection.Body.responseType
            $reply = [string]$selection.Body.reply
        }

        $passed = (
            $selection.StatusCode -eq 200 -and
            $responseType -eq "Answer" -and
            -not [string]::IsNullOrWhiteSpace($reply)
        )

        if ($passed) { $ExpectedAnswers[$intentName] = $reply }

        $SelectionResults += [pscustomobject]@{
            Intent = $intentName
            StatusCode = $selection.StatusCode
            Type = $responseType
            Pass = $passed
        }

        if ($passed) {
            Write-Host "  PASS  $intentName" -ForegroundColor Green
        } else {
            Write-Host "  FAIL  $intentName  HTTP=$($selection.StatusCode) Type=$responseType" -ForegroundColor Red
        }
    }
    catch {
        $SelectionResults += [pscustomobject]@{
            Intent = $intentName
            StatusCode = 0
            Type = ""
            Pass = $false
        }
        Write-Host "  ERROR $intentName  $($_.Exception.Message)" -ForegroundColor Red
    }
}

$selectionPassCount = @($SelectionResults | Where-Object { $_.Pass }).Count

Write-Host ""
Write-Host "Selection regression: $selectionPassCount/10"
Write-Host ""

if ($selectionPassCount -lt 10) {
    Write-Host "WARNING: Selection preflight is not 10/10." -ForegroundColor Yellow
    Write-Host "If your route differs, update `$SelectSuggestionUrl at the top of the script." -ForegroundColor Yellow
    Write-Host ""
}

$Results = @()

foreach ($case in $Cases) {
    Write-Host ("[{0}/60] {1}: {2}" -f $case.Number, $case.Group, $case.Question)

    $statusCode = 0
    $responseType = ""
    $reply = ""
    $suggestion1 = ""
    $suggestion2 = ""
    $suggestion3 = ""
    $expectedRank = $null
    $pass = $false
    $note = ""

    try {
        $response = Invoke-ChatbotPost -Url $MessagesUrl -Payload @{ message = $case.Question }
        $statusCode = $response.StatusCode

        if ($null -eq $response.Body) {
            $note = "Response was not valid JSON."
        }
        else {
            $responseType = [string]$response.Body.responseType
            $reply = [string]$response.Body.reply
            $suggestions = @($response.Body.suggestions)

            if ($suggestions.Count -gt 0) { $suggestion1 = [string]$suggestions[0].label }
            if ($suggestions.Count -gt 1) { $suggestion2 = [string]$suggestions[1].label }
            if ($suggestions.Count -gt 2) { $suggestion3 = [string]$suggestions[2].label }

            switch ($case.Group) {
                "Clear" {
                    if ($responseType -eq "Suggestions") {
                        for ($i = 0; $i -lt $suggestions.Count; $i++) {
                            if (([string]$suggestions[$i].knowledgeItemId) -ieq $case.ExpectedId) {
                                $expectedRank = $i + 1
                                break
                            }
                        }

                        $pass = ($null -ne $expectedRank -and $expectedRank -le 3)
                        if (-not $pass) { $note = "Expected intent was not present in Top-3 suggestions." }
                    }
                    elseif ($responseType -eq "Answer") {
                        $expectedReply = $ExpectedAnswers[$case.ExpectedIntent]

                        if (-not [string]::IsNullOrWhiteSpace($expectedReply) -and $reply -eq $expectedReply) {
                            $note = "Correct direct Answer returned, but this case did not exercise Top-3 Suggestions."
                        } else {
                            $note = "Unexpected direct Answer."
                        }

                        $pass = $false
                    }
                    else {
                        $note = "Expected Suggestions."
                    }
                }

                "Ambiguous" {
                    $pass = ($responseType -eq "Clarification" -or $responseType -eq "Suggestions")
                    if (-not $pass) {
                        if ($responseType -eq "Answer") {
                            $note = "Unsafe direct Answer for an ambiguous question."
                        }
                        elseif ($responseType -eq "Fallback") {
                            $note = "Safe but over-rejected; expected Clarification or Suggestions."
                        }
                        else {
                            $note = "Unexpected response type."
                        }
                    }
                }

                "NoMatch" {
                    $pass = ($responseType -eq "Fallback")
                    if (-not $pass) { $note = "Expected Fallback." }
                }

                "Exact" {
                    $expectedReply = $ExpectedAnswers[$case.ExpectedIntent]

                    if ([string]::IsNullOrWhiteSpace($expectedReply)) {
                        $pass = ($responseType -eq "Answer")
                        $note = "Expected reply unavailable; validated ResponseType only."
                    }
                    else {
                        $pass = ($responseType -eq "Answer" -and $reply -eq $expectedReply)
                        if (-not $pass) { $note = "Expected exact Answer reply for the known intent." }
                    }
                }
            }
        }
    }
    catch {
        $note = $_.Exception.Message
    }

    $Results += [pscustomobject]@{
        Number = $case.Number
        Group = $case.Group
        ExpectedIntent = $case.ExpectedIntent
        Question = $case.Question
        HttpStatus = $statusCode
        ResponseType = $responseType
        ExpectedRank = $expectedRank
        Suggestion1 = $suggestion1
        Suggestion2 = $suggestion2
        Suggestion3 = $suggestion3
        Pass = $pass
        Note = $note
    }

    if ($pass) {
        if ($null -ne $expectedRank) {
            Write-Host "  PASS  Type=$responseType Rank=$expectedRank" -ForegroundColor Green
        } else {
            Write-Host "  PASS  Type=$responseType" -ForegroundColor Green
        }
    } else {
        Write-Host "  FAIL  Type=$responseType  $note" -ForegroundColor Red
    }

    Write-Host ""
}

$Results | Export-Csv -Path $OutputCsv -NoTypeInformation -Encoding UTF8

function Get-GroupSummary {
    param([string]$Group, [int]$Required)

    $groupResults = @($Results | Where-Object { $_.Group -eq $Group })
    $passed = @($groupResults | Where-Object { $_.Pass }).Count

    [pscustomobject]@{
        Group = $Group
        Passed = $passed
        Total = $groupResults.Count
        Required = $Required
        Accepted = ($passed -ge $Required)
    }
}

$Summary = @(
    Get-GroupSummary "Clear" 27
    Get-GroupSummary "Ambiguous" 9
    Get-GroupSummary "NoMatch" 9
    Get-GroupSummary "Exact" 10
)

Write-Host ""
Write-Host "========================================"
Write-Host "FINAL SUMMARY"
Write-Host "========================================"

foreach ($row in $Summary) {
    $status = if ($row.Accepted) { "PASS" } else { "FAIL" }

    if ($row.Accepted) {
        Write-Host ("{0,-10} {1}/{2}  Required >= {3}  {4}" -f $row.Group, $row.Passed, $row.Total, $row.Required, $status) -ForegroundColor Green
    } else {
        Write-Host ("{0,-10} {1}/{2}  Required >= {3}  {4}" -f $row.Group, $row.Passed, $row.Total, $row.Required, $status) -ForegroundColor Red
    }
}

$selectionStatus = if ($selectionPassCount -eq 10) { "PASS" } else { "FAIL" }

if ($selectionPassCount -eq 10) {
    Write-Host ("Select IDs  {0}/10  Required = 10  {1}" -f $selectionPassCount, $selectionStatus) -ForegroundColor Green
} else {
    Write-Host ("Select IDs  {0}/10  Required = 10  {1}" -f $selectionPassCount, $selectionStatus) -ForegroundColor Red
}

$allAccepted = (
    @($Summary | Where-Object { -not $_.Accepted }).Count -eq 0 -and
    $selectionPassCount -eq 10
)

Write-Host ""
Write-Host "CSV: $OutputCsv"

if ($allAccepted) {
    Write-Host ""
    Write-Host "HOLDOUT B: ACCEPTED" -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "HOLDOUT B: NOT ACCEPTED" -ForegroundColor Red
    Write-Host "Do not tune the system yet. First review the full failed-case list." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Failed cases:"
$failedCases = @($Results | Where-Object { -not $_.Pass })

if ($failedCases.Count -eq 0) {
    Write-Host "  None" -ForegroundColor Green
} else {
    $failedCases |
        Select-Object Number, Group, ExpectedIntent, ResponseType, ExpectedRank, Note |
        Format-Table -AutoSize
}
