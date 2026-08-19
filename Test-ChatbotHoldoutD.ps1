param(
    [string]$BaseUrl = "https://localhost:44398",
    [string]$OutputCsv = ".\\chatbot-holdout-d-results.csv"
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
    param([string]$Url, [object]$Payload)

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
            Body = $body
            Raw = $raw
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
        Number = $Number
        Group = $Group
        Question = $Question
        ExpectedIntent = $ExpectedIntent
        ExpectedId = $expectedId
    }
}

$Cases = @(
    New-HoldoutCase -Number 1  -Group "Clear" -Question "می‌خوام رمز حسابم رو عوض کنم چون رمز قبلی دیگه مناسب نیست." -ExpectedIntent "ChangePassword"
    New-HoldoutCase -Number 2  -Group "Clear" -Question "از کجای حساب می‌تونم یک گذرواژه تازه برای ورود تنظیم کنم؟" -ExpectedIntent "ChangePassword"
    New-HoldoutCase -Number 3  -Group "Clear" -Question "برای حل مشکلم نیاز دارم با پشتیبانی صحبت کنم؛ راه تماس چیه؟" -ExpectedIntent "Support"
    New-HoldoutCase -Number 4  -Group "Clear" -Question "رمز ورودم از ذهنم رفته و الان نمی‌دونم چطور وارد حساب بشم." -ExpectedIntent "ForgotPassword"
    New-HoldoutCase -Number 5  -Group "Clear" -Question "به پسوردم دسترسی ندارم؛ راه بازیابی ورود به اکانت چیه؟" -ExpectedIntent "ForgotPassword"
    New-HoldoutCase -Number 6  -Group "Clear" -Question "اطلاعات ورود رو می‌زنم ولی حساب کاربری برای من باز نمی‌شه." -ExpectedIntent "LoginProblem"
    New-HoldoutCase -Number 7  -Group "Clear" -Question "حسابم محدود شده و سیستم اجازه نمی‌ده مثل قبل واردش بشم." -ExpectedIntent "LockedAccount"
    New-HoldoutCase -Number 8  -Group "Clear" -Question "آدرس ایمیل فعلی حساب رو می‌خوام با آدرس تازه عوض کنم." -ExpectedIntent "ChangeEmail"
    New-HoldoutCase -Number 9  -Group "Clear" -Question "ایمیل متصل به اکانتم باید جایگزین بشه؛ این کار از کجا انجام می‌شه؟" -ExpectedIntent "ChangeEmail"
    New-HoldoutCase -Number 10 -Group "Clear" -Question "می‌خوام تلفن جدیدم جای شماره فعلی حساب ثبت بشه." -ExpectedIntent "ChangeMobile"
    New-HoldoutCase -Number 11 -Group "Clear" -Question "شماره تماس اکانتم دیگه معتبر نیست؛ چطور شماره تازه وارد کنم؟" -ExpectedIntent "ChangeMobile"
    New-HoldoutCase -Number 12 -Group "Clear" -Question "می‌خوام مشخصاتی که در صفحه پروفایلم نمایش داده می‌شه رو اصلاح کنم." -ExpectedIntent "EditProfile"
    New-HoldoutCase -Number 13 -Group "Clear" -Question "هنوز عضو نیستم و می‌خوام یک اکانت برای ورود به سایت بسازم." -ExpectedIntent "CreateAccount"
    New-HoldoutCase -Number 14 -Group "Clear" -Question "می‌خوام حسابم کاملاً بسته بشه و دیگه روی سایت وجود نداشته باشه." -ExpectedIntent "DeleteAccount"
    New-HoldoutCase -Number 15 -Group "Clear" -Question "قصد دارم اکانتم رو برای همیشه پاک کنم؛ باید از کجا شروع کنم؟" -ExpectedIntent "DeleteAccount"

    New-HoldoutCase -Number 16 -Group "Ambiguous" -Question "یه بخش از اطلاعات حسابم باید عوض بشه ولی دقیق نمی‌دونم کدوم قسمت."
    New-HoldoutCase -Number 17 -Group "Ambiguous" -Question "یکی از اطلاعات ارتباطی من روی حساب درست نیست."
    New-HoldoutCase -Number 18 -Group "Ambiguous" -Question "برای ورود به اکانتم مشکل دارم اما مطمئن نیستم مشکل از رمز باشه یا حساب."
    New-HoldoutCase -Number 19 -Group "Ambiguous" -Question "می‌خوام چند مورد از مشخصات حسابم رو مرتب کنم."
    New-HoldoutCase -Number 20 -Group "Ambiguous" -Question "برای حساب کاربری‌ام راهنمایی لازم دارم ولی نمی‌دونم کدوم موضوع رو انتخاب کنم."

    New-HoldoutCase -Number 21 -Group "NoMatch" -Question "چطور احراز هویت دو مرحله‌ای رو برای حسابم فعال کنم؟"
    New-HoldoutCase -Number 22 -Group "NoMatch" -Question "چطور اعلان‌های ایمیلی سایت رو غیرفعال کنم؟"
    New-HoldoutCase -Number 23 -Group "NoMatch" -Question "گزارش فعالیت‌های اخیر حسابم رو از کجا ببینم؟"
    New-HoldoutCase -Number 24 -Group "NoMatch" -Question "چه مرورگرهایی برای استفاده از سایت پشتیبانی می‌شن؟"
    New-HoldoutCase -Number 25 -Group "NoMatch" -Question "آیا محصولات شما روی لینوکس هم اجرا می‌شن؟"
    New-HoldoutCase -Number 26 -Group "NoMatch" -Question "برای دریافت نسخه آزمایشی محصول باید چه کار کنم؟"
    New-HoldoutCase -Number 27 -Group "NoMatch" -Question "آیا برای توسعه‌دهنده‌ها API در اختیار می‌گذارید؟"
    New-HoldoutCase -Number 28 -Group "NoMatch" -Question "چطور عضویت در خبرنامه سایت رو لغو کنم؟"
    New-HoldoutCase -Number 29 -Group "NoMatch" -Question "امکان خروجی گرفتن از اطلاعات حساب به صورت فایل وجود داره؟"
    New-HoldoutCase -Number 30 -Group "NoMatch" -Question "سایت حالت تاریک یا تم شب داره؟"

    New-HoldoutCase -Number 31 -Group "Exact" -Question "چطور رمز عبورم را تغییر بدهم؟" -ExpectedIntent "ChangePassword"
    New-HoldoutCase -Number 32 -Group "Exact" -Question "چطور با پشتیبانی تماس بگیرم؟" -ExpectedIntent "Support"
    New-HoldoutCase -Number 33 -Group "Exact" -Question "رمز عبورم را فراموش کرده‌ام، چه کار کنم؟" -ExpectedIntent "ForgotPassword"
    New-HoldoutCase -Number 34 -Group "Exact" -Question "چرا نمی‌توانم وارد حساب کاربری شوم؟" -ExpectedIntent "LoginProblem"
    New-HoldoutCase -Number 35 -Group "Exact" -Question "حساب کاربری من قفل شده است، چه کار کنم؟" -ExpectedIntent "LockedAccount"
    New-HoldoutCase -Number 36 -Group "Exact" -Question "چطور ایمیل حساب کاربری را تغییر بدهم؟" -ExpectedIntent "ChangeEmail"
    New-HoldoutCase -Number 37 -Group "Exact" -Question "چطور شماره موبایل حسابم را تغییر بدهم؟" -ExpectedIntent "ChangeMobile"
    New-HoldoutCase -Number 38 -Group "Exact" -Question "چطور اطلاعات پروفایلم را ویرایش کنم؟" -ExpectedIntent "EditProfile"
    New-HoldoutCase -Number 39 -Group "Exact" -Question "چطور یک حساب کاربری ایجاد کنم؟" -ExpectedIntent "CreateAccount"
    New-HoldoutCase -Number 40 -Group "Exact" -Question "چطور حساب کاربری‌ام را حذف کنم؟" -ExpectedIntent "DeleteAccount"
)

Write-Host ""
Write-Host "========================================"
Write-Host "Chatbot Holdout D - FINAL"
Write-Host "Base URL: $BaseUrl"
Write-Host "========================================"
Write-Host ""

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
        Write-Host "  ERROR $intentName  $($_.Exception.Message)" -ForegroundColor Red
    }
}

$selectionPassCount = @($SelectionResults | Where-Object { $_.Pass }).Count

Write-Host ""
Write-Host "Selection regression: $selectionPassCount/10"
Write-Host ""

if ($selectionPassCount -lt 10) {
    Write-Host "STOP: preflight is not 10/10. Do not interpret Holdout D results." -ForegroundColor Red
    exit 1
}

$Results = @()

foreach ($case in $Cases) {
    Write-Host ("[{0}/40] {1}: {2}" -f $case.Number, $case.Group, $case.Question)

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
    Get-GroupSummary "Clear" 14
    Get-GroupSummary "Ambiguous" 5
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
    }
    else {
        Write-Host ("{0,-10} {1}/{2}  Required >= {3}  {4}" -f $row.Group, $row.Passed, $row.Total, $row.Required, $status) -ForegroundColor Red
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
    Write-Host "HOLDOUT D: ACCEPTED" -ForegroundColor Green
}
else {
    Write-Host ""
    Write-Host "HOLDOUT D: NOT ACCEPTED" -ForegroundColor Red
    Write-Host "This is the final untouched validation set. Do not tune against it before reviewing the result." -ForegroundColor Yellow
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
