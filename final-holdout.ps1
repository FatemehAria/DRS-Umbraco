$BaseUrl = "http://localhost:26200/api/chatbot/messages"

function From-Base64Utf8([string]$value)
{
    return [System.Text.Encoding]::UTF8.GetString(
        [System.Convert]::FromBase64String($value)
    )
}

$Cases = @(
    @{
        Id = "P01"
        Type = "Positive"
        ExpectedId = "750cffa0-e281-41b5-b639-9b4ad041562c"
        Message = From-Base64Utf8 "2YXbjOKAjNiu2YjYp9mFINqp2YTZhdmHINi52KjZiNixINin2qnYp9mG2KrZhSDYsdmIINuM2Ycg2obbjNiyINiv24zar9mHINio2LDYp9ix2YXYjCDYp9iyINqp2KzYpyDYqNin24zYryDYp9mG2KzYp9mF2LQg2KjYr9mF2J8="
    },
    @{
        Id = "P02"
        Type = "Positive"
        ExpectedId = "5eeaa1f0-f478-4524-9626-06ca996af901"
        Message = From-Base64Utf8 "2KjYsdin24wg2KfYsdiq2KjYp9i3INio2Kcg2KrbjNmFINm+2LTYqtuM2KjYp9mG24wg2KfYsiDahtmHINio2K7YtNuMINio2KfbjNivINin2YLYr9in2YUg2qnZhtmF2J8="
    },
    @{
        Id = "P03"
        Type = "Positive"
        ExpectedId = "528778ab-e075-4ee0-bf31-fc9bc5326c7a"
        Message = From-Base64Utf8 "2LHZhdiy2YUg24zYp9iv2YUg2LHZgdiq2YfYmyDahti32YjYsSDYr9mI2KjYp9ix2Ycg2KjZhyDYrdiz2KfYqNmFINiv2LPYqtix2LPbjCDYqNqv24zYsdmF2J8="
    },
    @{
        Id = "P04"
        Type = "Positive"
        ExpectedId = "8551757a-28e8-4bf2-bf9b-f3aa473540c4"
        Message = From-Base64Utf8 "2KfYt9mE2KfYudin2Kog2YjYsdmI2K/ZhSDYr9ix2LPYqtmHINmI2YTbjCDZiNin2LHYryDYrdiz2KfYqCDZhtmF24zigIzYtNmF2Jsg2KjYp9uM2K8g2obbjCDaqdin2LEg2qnZhtmF2J8="
    },
    @{
        Id = "P05"
        Type = "Positive"
        ExpectedId = "9e137ce9-b2bf-41e1-945e-b9f5d5e93437"
        Message = From-Base64Utf8 "2K3Ys9in2KjZhSDZgtmB2YQg2LTYr9mHINmIINin2KzYp9iy2Ycg2YjYsdmI2K8g2YbZhduM4oCM2K/Zh9ibINix2KfZhyDYqNin2LIg2qnYsdiv2YbYtCDahtuM2YfYnw=="
    },
    @{
        Id = "P06"
        Type = "Positive"
        ExpectedId = "aaba78db-3981-400c-8e97-12bcf9ed7a8a"
        Message = From-Base64Utf8 "2YXbjOKAjNiu2YjYp9mFINin24zZhduM2YTbjCDaqdmHINio2LHYp9uMINit2LPYp9io2YUg2KvYqNiqINi02K/ZhyDYsdmIINio2Kcg24zZhyDYp9uM2YXbjNmEINiv24zar9mHINis2KfbjNqv2LLbjNmGINqp2YbZhS4="
    },
    @{
        Id = "P07"
        Type = "Positive"
        ExpectedId = "2361d83f-7c37-43fc-af41-7b0bb893db47"
        Message = From-Base64Utf8 "2LTZhdin2LHZhyDYqtmE2YHZhiDYq9io2KrigIzYtNiv2Ycg2LHZiNuMINit2LPYp9io2YUg2LnZiNi2INi02K/Zh9ibINqG2LfZiNixINi02YXYp9ix2Ycg2KzYr9uM2K8g2LHZiCDZiNin2LHYryDaqdmG2YXYnw=="
    },
    @{
        Id = "P08"
        Type = "Positive"
        ExpectedId = "fb4c8c44-faf5-4b67-a5c6-0d1cacb4c531"
        Message = From-Base64Utf8 "2KfYs9mFINuM2Kcg2YXYtNiu2LXYp9iqINm+2LHZiNmB2KfbjNmE2YUg2LHZiCDYp9iyINqp2KzYpyDZhduM4oCM2KrZiNmG2YUg2YjbjNix2KfbjNi0INqp2YbZhdif"
    },
    @{
        Id = "P09"
        Type = "Positive"
        ExpectedId = "244bd9a6-601d-41a4-98d3-276c2ea37f65"
        Message = From-Base64Utf8 "2YfZhtmI2LIg2K3Ys9in2Kgg2YbYr9in2LHZhdibINio2LHYp9uMINiz2KfYrtiqINit2LPYp9ioINqp2KfYsdio2LHbjCDYqNin24zYryDYp9iyINqp2KzYpyDYtNix2YjYuSDaqdmG2YXYnw=="
    },
    @{
        Id = "P10"
        Type = "Positive"
        ExpectedId = "d59c6ca1-2509-405b-844f-bd0dec63416c"
        Message = From-Base64Utf8 "2YXbjOKAjNiu2YjYp9mFINin2qnYp9mG2KrZhSDYsdmIINio2LHYp9uMINmH2YXbjNi02Ycg2KjYqNmG2K/ZhdibINin2LIg2qnYrNinINio2KfbjNivINit2LDZgdi0INqp2YbZhdif"
    },

    @{
        Id = "N01"
        Type = "Negative"
        Message = From-Base64Utf8 "2obYt9mI2LEg2LLYqNin2YYg2LPYp9uM2Kog2LHYpyDYp9mG2q/ZhNuM2LPbjCDaqdmG2YXYnw=="
    },
    @{
        Id = "N02"
        Type = "Negative"
        Message = From-Base64Utf8 "2obYt9mI2LEg2KfYudmE2KfZhuKAjNmH2KfbjCDZvtuM2KfZhdqp24wg2LHYpyDZgti32Lkg2qnZhtmF2J8="
    },
    @{
        Id = "N03"
        Type = "Negative"
        Message = From-Base64Utf8 "2KLbjNinINmF24zigIzYqtmI2KfZhtmFINiq2KfYsduM2K7ahtmHINmI2LHZiNiv2YfYp9uMINit2LPYp9io2YUg2LHYpyDYqNio24zZhtmF2J8="
    },
    @{
        Id = "N04"
        Type = "Negative"
        Message = From-Base64Utf8 "2obYt9mI2LEg2K3Ys9in2KjZhSDYsdinINio2Ycg2K3Ys9in2Kgg2q/ZiNqv2YQg2YjYtdmEINqp2YbZhdif"
    },
    @{
        Id = "N05"
        Type = "Negative"
        Message = From-Base64Utf8 "2obYt9mI2LEg2LHZhdiyINuM2qnYqNin2LHZhdi12LHZgSDYr9ix24zYp9mB2Kog2qnZhtmF2J8="
    },
    @{
        Id = "N06"
        Type = "Negative"
        Message = From-Base64Utf8 "2obYt9mI2LEg2YHYp9qp2KrZiNixINiu2LHbjNiv2YfYp9uM2YUg2LHYpyDYr9ix24zYp9mB2Kog2qnZhtmF2J8="
    },
    @{
        Id = "N07"
        Type = "Negative"
        Message = From-Base64Utf8 "2obYt9mI2LEg2LPZgdin2LHYtCDYq9io2KrigIzYtNiv2Ycg2LHYpyDZhNi62Ygg2qnZhtmF2J8="
    },
    @{
        Id = "N08"
        Type = "Negative"
        Message = From-Base64Utf8 "2obYt9mI2LEg2LHZhtqvINmC2KfZhNioINiz2KfbjNiqINix2Kcg2KrYutuM24zYsSDYqNiv2YfZhdif"
    },
    @{
        Id = "N09"
        Type = "Negative"
        Message = From-Base64Utf8 "2KLbjNinINin2YXaqdin2YYg2YjYsdmI2K8g2KjYpyDYp9ir2LEg2KfZhtqv2LTYqiDZiNis2YjYryDYr9in2LHYr9if"
    },
    @{
        Id = "N10"
        Type = "Negative"
        Message = From-Base64Utf8 "2obYt9mI2LEg2LTZhdin2LHZhyDaqdin2LHYqiDYqNin2YbaqduM4oCM2KfZhSDYsdinINir2KjYqiDaqdmG2YXYnw=="
    }
)

$results = @()

foreach ($case in $Cases)
{
    $body = @{
        message = $case.Message
    } | ConvertTo-Json -Compress

    $tempFile =
        Join-Path `
            $env:TEMP `
            "chatbot-final-holdout.json"

    [System.IO.File]::WriteAllText(
        $tempFile,
        $body,
        [System.Text.UTF8Encoding]::new($false)
    )

    $raw = curl.exe `
        -s `
        -H "Content-Type: application/json; charset=utf-8" `
        --data-binary "@$tempFile" `
        $BaseUrl

    $response =
        $raw |
        ConvertFrom-Json

    $passed = $false
    $actual = ""

    if ($case.Type -eq "Positive")
    {
        $ids = @(
            $response.suggestions |
            ForEach-Object {
                $_.knowledgeItemId
            }
        )

        $passed =
            $ids -contains $case.ExpectedId

        $actual =
            if ($ids.Count -gt 0)
            {
                $ids -join ", "
            }
            else
            {
                $response.responseType
            }
    }
    else
    {
        $passed =
            $response.responseType `
            -eq "Fallback"

        $actual =
            $response.responseType
    }

    $results +=
        [PSCustomObject]@{
            Id = $case.Id
            Type = $case.Type
            Passed = $passed
            Actual = $actual
        }

    Write-Host (
        "{0} | {1,-8} | {2}" -f
        $case.Id,
        $case.Type,
        $(if ($passed) { "PASS" } else { "FAIL" })
    )
}

Remove-Item `
    $tempFile `
    -ErrorAction SilentlyContinue

$positive =
    $results |
    Where-Object Type -eq "Positive"

$negative =
    $results |
    Where-Object Type -eq "Negative"

$totalPassed =
    @(
        $results |
        Where-Object Passed
    ).Count

$positivePassed =
    @(
        $positive |
        Where-Object Passed
    ).Count

$negativePassed =
    @(
        $negative |
        Where-Object Passed
    ).Count

$falsePositives =
    10 - $negativePassed

$accuracy =
    $totalPassed / 20.0 * 100

$positiveRecall =
    $positivePassed / 10.0 * 100

Write-Host ""
Write-Host "=============================="
Write-Host "FINAL HOLDOUT RESULT"
Write-Host "=============================="

Write-Host (
    "Overall:          {0}/20 ({1:N1}%)" -f
    $totalPassed,
    $accuracy
)

Write-Host (
    "Positive Recall:  {0}/10 ({1:N1}%)" -f
    $positivePassed,
    $positiveRecall
)

Write-Host (
    "Negative Reject:  {0}/10" -f
    $negativePassed
)

Write-Host (
    "False Positives:  {0}" -f
    $falsePositives
)

$finalPass =
    $accuracy -ge 90 `
    -and $positiveRecall -ge 90 `
    -and $falsePositives -le 1

Write-Host ""
Write-Host (
    "FINAL RESULT: " +
    $(if ($finalPass) { "PASS" } else { "FAIL" })
)

Write-Host ""
Write-Host "Failed cases:"

$results |
    Where-Object { -not $_.Passed } |
    Format-Table -AutoSize