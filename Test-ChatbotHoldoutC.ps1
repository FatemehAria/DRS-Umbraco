param(
    [string]$BaseUrl = "https://localhost:44398",
    [string]$OutputCsv = ".\chatbot-holdout-c-results.csv"
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
                # Leave Body null. Caller will record invalid JSON.
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
    # 1-20: Clear Non-Exact — 2 new questions per intent
    New-HoldoutCase -Number 1  -Group "Clear" -Question "می‌خوام رمز ورود حسابم رو به یه رمز جدید تبدیل کنم، باید کجا انجامش بدم؟" -ExpectedIntent "ChangePassword"
    New-HoldoutCase -Number 2  -Group "Clear" -Question "برای امنیت بیشتر می‌خوام پسورد اکانتم رو عوض کنم، از چه بخشی میشه؟" -ExpectedIntent "ChangePassword"

    New-HoldoutCase -Number 3  -Group "Clear" -Question "برای مشکلی که حل نمی‌شه چطور می‌تونم از پشتیبانی سایت کمک بگیرم؟" -ExpectedIntent "Support"
    New-HoldoutCase -Number 4  -Group "Clear" -Question "اگه نیاز به راهنمایی داشته باشم، مسیر تماس با تیم پاسخ‌گویی کجاست؟" -ExpectedIntent "Support"

    New-HoldoutCase -Number 5  -Group "Clear" -Question "رمزی که باهاش وارد می‌شدم یادم رفته؛ راه برگشت به حساب چیه؟" -ExpectedIntent "ForgotPassword"
    New-HoldoutCase -Number 6  -Group "Clear" -Question "پسورد ورود رو به خاطر نمیارم، چطور دوباره رمز تعیین کنم؟" -ExpectedIntent "ForgotPassword"

    New-HoldoutCase -Number 7  -Group "Clear" -Question "هرچی تلاش می‌کنم صفحه حسابم باز نمی‌شه، با اینکه اطلاعات ورود رو وارد می‌کنم." -ExpectedIntent "LoginProblem"
    New-HoldoutCase -Number 8  -Group "Clear" -Question "ورودم تکمیل نمی‌شه و بعد از زدن دکمه ورود داخل حساب نمی‌رم." -ExpectedIntent "LoginProblem"

    New-HoldoutCase -Number 9  -Group "Clear" -Question "اکانتم بعد از چند تلاش بسته شده و دیگه اجازه ورود نمی‌ده." -ExpectedIntent "LockedAccount"
    New-HoldoutCase -Number 10 -Group "Clear" -Question "پیغام محدود شدن حساب می‌بینم؛ چطور دسترسی‌ام رو برگردونم؟" -ExpectedIntent "LockedAccount"

    New-HoldoutCase -Number 11 -Group "Clear" -Question "ایمیل قبلی دیگه در دسترسم نیست و می‌خوام ایمیل حساب رو عوض کنم." -ExpectedIntent "ChangeEmail"
    New-HoldoutCase -Number 12 -Group "Clear" -Question "برای حسابم باید یک آدرس ایمیل جدید جایگزین قبلی کنم، راهش چیه؟" -ExpectedIntent "ChangeEmail"

    New-HoldoutCase -Number 13 -Group "Clear" -Question "شماره قبلی رو ندارم و می‌خوام شماره تازه‌ام روی اکانت باشه." -ExpectedIntent "ChangeMobile"
    New-HoldoutCase -Number 14 -Group "Clear" -Question "تلفن ثبت‌شده در حساب عوض شده؛ از کجا شماره جدید رو وارد کنم؟" -ExpectedIntent "ChangeMobile"

    New-HoldoutCase -Number 15 -Group "Clear" -Question "اسم و اطلاعات شخصی‌ای که در حساب می‌بینم نیاز به ویرایش داره." -ExpectedIntent "EditProfile"
    New-HoldoutCase -Number 16 -Group "Clear" -Question "می‌خوام جزئیات پروفایلم رو به‌روز کنم؛ بخش ویرایش کجاست؟" -ExpectedIntent "EditProfile"

    New-HoldoutCase -Number 17 -Group "Clear" -Question "تازه وارد سایت شدم و می‌خوام برای خودم حساب بسازم." -ExpectedIntent "CreateAccount"
    New-HoldoutCase -Number 18 -Group "Clear" -Question "برای استفاده از سایت باید عضو بشم؛ از کجا حساب جدید باز کنم؟" -ExpectedIntent "CreateAccount"

    New-HoldoutCase -Number 19 -Group "Clear" -Question "می‌خوام عضویتم رو خاتمه بدم و حساب کاربری‌ام باقی نمونه." -ExpectedIntent "DeleteAccount"
    New-HoldoutCase -Number 20 -Group "Clear" -Question "دیگه از این اکانت استفاده نمی‌کنم؛ راه پاک‌کردن کاملش چیه؟" -ExpectedIntent "DeleteAccount"

    # 21-30: Ambiguous — intentionally insufficient to choose one exact answer
    New-HoldoutCase -Number 21 -Group "Ambiguous" -Question "می‌خوام یه مورد از حسابم رو اصلاح کنم."
    New-HoldoutCase -Number 22 -Group "Ambiguous" -Question "یه چیزی توی مشخصات اکانتم درست نیست."
    New-HoldoutCase -Number 23 -Group "Ambiguous" -Question "یکی از راه‌های تماس ثبت‌شده برای من باید عوض بشه."
    New-HoldoutCase -Number 24 -Group "Ambiguous" -Question "اطلاعات ارتباطی پروفایلم قدیمی شده."
    New-HoldoutCase -Number 25 -Group "Ambiguous" -Question "در بخش ورود یه مسئله دارم و نمی‌دونم مربوط به رمز هست یا خود حساب."
    New-HoldoutCase -Number 26 -Group "Ambiguous" -Question "حسابم مثل قبل در دسترس نیست و علتش رو نمی‌دونم."
    New-HoldoutCase -Number 27 -Group "Ambiguous" -Question "باید یکی از موارد مربوط به اکانتم رو به‌روزرسانی کنم."
    New-HoldoutCase -Number 28 -Group "Ambiguous" -Question "چند تا چیز در پروفایلم نیاز به تغییر داره."
    New-HoldoutCase -Number 29 -Group "Ambiguous" -Question "برای اکانتم یه مشکل پیش اومده و نمی‌دونم کدوم راهنما به دردم می‌خوره."
    New-HoldoutCase -Number 30 -Group "Ambiguous" -Question "می‌خوام یکی از اطلاعات ثبت‌شده روی حساب رو عوض کنم ولی نمی‌دونم از کجا."

    # 31-40: NoMatch — topics not represented by the current account FAQ knowledge base
    New-HoldoutCase -Number 31 -Group "NoMatch" -Question "چطور موجودی کیف پولم رو ببینم؟"
    New-HoldoutCase -Number 32 -Group "NoMatch" -Question "آیا امکان پرداخت قسطی وجود داره؟"
    New-HoldoutCase -Number 33 -Group "NoMatch" -Question "چطور تاریخچه تراکنش‌هام رو مشاهده کنم؟"
    New-HoldoutCase -Number 34 -Group "NoMatch" -Question "شرایط گارانتی محصولات چیه؟"
    New-HoldoutCase -Number 35 -Group "NoMatch" -Question "نسخه موبایل برنامه رو از کجا دانلود کنم؟"
    New-HoldoutCase -Number 36 -Group "NoMatch" -Question "چطور اعلان‌های پیامکی سایت رو غیرفعال کنم؟"
    New-HoldoutCase -Number 37 -Group "NoMatch" -Question "چطور محصولی رو به لیست علاقه‌مندی‌هام اضافه کنم؟"
    New-HoldoutCase -Number 38 -Group "NoMatch" -Question "حداقل مبلغ خرید چقدره؟"
    New-HoldoutCase -Number 39 -Group "NoMatch" -Question "چطور امتیازهای باشگاه مشتریانم رو ببینم؟"
    New-HoldoutCase -Number 40 -Group "NoMatch" -Question "امکان چت با فروشنده وجود داره؟"

    # 41-50: Exact regression — known main FAQ questions
    New-HoldoutCase -Number 41 -Group "Exact" -Question "چطور رمز عبورم را تغییر بدهم؟" -ExpectedIntent "ChangePassword"
    New-HoldoutCase -Number 42 -Group "Exact" -Question "چطور با پشتیبانی تماس بگیرم؟" -ExpectedIntent "Support"
    New-HoldoutCase -Number 43 -Group "Exact" -Question "رمز عبورم را فراموش کرده‌ام، چه کار کنم؟" -ExpectedIntent "ForgotPassword"
    New-HoldoutCase -Number 44 -Group "Exact" -Question "چرا نمی‌توانم وارد حساب کاربری شوم؟" -ExpectedIntent "LoginProblem"
    New-HoldoutCase -Number 45 -Group "Exact" -Question "حساب کاربری من قفل شده است، چه کار کنم؟" -ExpectedIntent "LockedAccount"
    New-HoldoutCase -Number 46 -Group "Exact" -Question "چطور ایمیل حساب کاربری را تغییر بدهم؟" -ExpectedIntent "ChangeEmail"
    New-HoldoutCase -Number 47 -Group "Exact" -Question "چطور شماره موبایل حسابم را تغییر بدهم؟" -ExpectedIntent "ChangeMobile"
    New-HoldoutCase -Number 48 -Group "Exact" -Question "چطور اطلاعات پروفایلم را ویرایش کنم؟" -ExpectedIntent "EditProfile"
    New-HoldoutCase -Number 49 -Group "Exact" -Question "چطور یک حساب کاربری ایجاد کنم؟" -ExpectedIntent "CreateAccount"
    New-HoldoutCase -Number 50 -Group "Exact" -Question "چطور حساب کاربری‌ام را حذف کنم؟" -ExpectedIntent "DeleteAccount"
)

Write-Host ""
Write-Host "========================================"
Write-Host "Chatbot Holdout C"
Write-Host "Base URL: $BaseUrl"
Write-Host "========================================"
Write-Host ""

# Preflight and deterministic expected answers.
$ExpectedAnswers = @{}
$SelectionResults = @()

Write-Host "Preflight: validating 10 known Answer IDs..." -ForegroundColor Cyan

foreach ($intentName in $IntentIds.Keys) {
    $id = $IntentIds[$intentName]

    try {
        $selection = Invoke-ChatbotPost `
            -Url $SelectSuggestionUrl `
            -Payload @{ knowledgeItemId = $id }

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

        if ($passed) {
            $ExpectedAnswers[$intentName] = $reply
        }

        $SelectionResults += [pscustomobject]@{
            Intent     = $intentName
            StatusCode = $selection.StatusCode
            Type       = $responseType
            Pass       = $passed
        }

        if ($passed) {
            Write-Host "  PASS  $intentName" -ForegroundColor Green
        }
        else {
            Write-Host "  FAIL  $intentName  HTTP=$($selection.StatusCode) Type=$responseType" -ForegroundColor Red
        }
    }
    catch {
        $SelectionResults += [pscustomobject]@{
            Intent     = $intentName
            StatusCode = 0
            Type       = ""
            Pass       = $false
        }

        Write-Host "  ERROR $intentName  $($_.Exception.Message)" -ForegroundColor Red
    }
}

$selectionPassCount = @($SelectionResults | Where-Object { $_.Pass }).Count

Write-Host ""
Write-Host "Selection regression: $selectionPassCount/10"
Write-Host ""

if ($selectionPassCount -lt 10) {
    Write-Host "STOP: preflight is not 10/10. Do not interpret Holdout C results." -ForegroundColor Red
    exit 1
}

$Results = @()

foreach ($case in $Cases) {
    Write-Host ("[{0}/50] {1}: {2}" -f $case.Number, $case.Group, $case.Question)

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
        $response = Invoke-ChatbotPost `
            -Url $MessagesUrl `
            -Payload @{ message = $case.Question }

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
                            $actualId = [string]$suggestions[$i].knowledgeItemId

                            if ($actualId -ieq $case.ExpectedId) {
                                $expectedRank = $i + 1
                                break
                            }
                        }

                        $pass = ($null -ne $expectedRank -and $expectedRank -le 3)

                        if (-not $pass) {
                            $note = "Expected intent was not present in Top-3 suggestions."
                        }
                    }
                    else {
                        $note = "Expected Suggestions."
                    }
                }

                "Ambiguous" {
                    $pass = (
                        $responseType -eq "Clarification" -or
                        $responseType -eq "Suggestions"
                    )

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

                    if (-not $pass) {
                        $note = "Expected Fallback."
                    }
                }

                "Exact" {
                    $expectedReply = $ExpectedAnswers[$case.ExpectedIntent]

                    $pass = (
                        $responseType -eq "Answer" -and
                        $reply -eq $expectedReply
                    )

                    if (-not $pass) {
                        $note = "Expected exact Answer reply for the known intent."
                    }
                }
            }
        }
    }
    catch {
        $note = $_.Exception.Message
    }

    $Results += [pscustomobject]@{
        Number         = $case.Number
        Group          = $case.Group
        ExpectedIntent = $case.ExpectedIntent
        Question       = $case.Question
        HttpStatus     = $statusCode
        ResponseType   = $responseType
        ExpectedRank   = $expectedRank
        Suggestion1    = $suggestion1
        Suggestion2    = $suggestion2
        Suggestion3    = $suggestion3
        Pass           = $pass
        Note           = $note
    }

    if ($pass) {
        if ($null -ne $expectedRank) {
            Write-Host "  PASS  Type=$responseType Rank=$expectedRank" -ForegroundColor Green
        }
        else {
            Write-Host "  PASS  Type=$responseType" -ForegroundColor Green
        }
    }
    else {
        Write-Host "  FAIL  Type=$responseType  $note" -ForegroundColor Red
    }

    Write-Host ""
}

$Results | Export-Csv `
    -Path $OutputCsv `
    -NoTypeInformation `
    -Encoding UTF8

function Get-GroupSummary {
    param(
        [string]$Group,
        [int]$Required
    )

    $groupResults = @($Results | Where-Object { $_.Group -eq $Group })
    $passed = @($groupResults | Where-Object { $_.Pass }).Count

    [pscustomobject]@{
        Group    = $Group
        Passed   = $passed
        Total    = $groupResults.Count
        Required = $Required
        Accepted = ($passed -ge $Required)
    }
}

# Acceptance criteria fixed BEFORE running Holdout C:
# Clear >= 18/20 (90%)
# Ambiguous >= 9/10
# NoMatch >= 9/10
# Exact = 10/10
$Summary = @(
    Get-GroupSummary "Clear" 18
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
        Write-Host ("{0,-10} {1}/{2}  Required >= {3}  {4}" -f `
            $row.Group, $row.Passed, $row.Total, $row.Required, $status) `
            -ForegroundColor Green
    }
    else {
        Write-Host ("{0,-10} {1}/{2}  Required >= {3}  {4}" -f `
            $row.Group, $row.Passed, $row.Total, $row.Required, $status) `
            -ForegroundColor Red
    }
}

Write-Host ("Select IDs  {0}/10  Required = 10  PASS" -f $selectionPassCount) -ForegroundColor Green

$allAccepted = (
    @($Summary | Where-Object { -not $_.Accepted }).Count -eq 0 -and
    $selectionPassCount -eq 10
)

Write-Host ""
Write-Host "CSV: $OutputCsv"

if ($allAccepted) {
    Write-Host ""
    Write-Host "HOLDOUT C: ACCEPTED" -ForegroundColor Green
}
else {
    Write-Host ""
    Write-Host "HOLDOUT C: NOT ACCEPTED" -ForegroundColor Red
    Write-Host "Do not tune the system before reviewing the complete result." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Failed cases:"
$failedCases = @($Results | Where-Object { -not $_.Pass })

if ($failedCases.Count -eq 0) {
    Write-Host "  None" -ForegroundColor Green
}
else {
    $failedCases |
        Select-Object Number, Group, ExpectedIntent, ResponseType, ExpectedRank, Note |
        Format-Table -AutoSize
}
