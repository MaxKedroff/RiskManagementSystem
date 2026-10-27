#requires -Version 5.1

param(
    [string]$BaseUrl = "http://185.32.85.146:8080/v1",
    [string]$AdminEmail = "admin@riskplatform.local",
    [string]$AdminPassword = "rms123"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Invoke-PostAsync {
    param([string]$Url, [object]$Body)
    $headers = @{}
    if ($using:Token) { $headers["Authorization"] = "Bearer $using:Token" }
    $resp = Invoke-RestMethod -Uri $Url -Method Post -Headers $headers -ContentType "application/json" -Body ($Body | ConvertTo-Json -Depth 10)
    return $resp
}

Write-Host "==> Logging in as admin..." -ForegroundColor Cyan
$loginResp = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -ContentType "application/json" `
    -Body (ConvertTo-Json -Depth 3 -InputObject ([PSCustomObject]@{ Email = $AdminEmail; Password = $AdminPassword }))

$Token = $loginResp.accessToken
Write-Host "  Token received (expires $($loginResp.expiresInMinutes) min)." -ForegroundColor Green

Write-Host "==> Fetching roles..." -ForegroundColor Cyan
$rolesUrl = "$BaseUrl/roles"
try {
    $roles = Invoke-RestMethod -Uri $rolesUrl -Method Get -Headers @{ Authorization = "Bearer $Token" }
} catch {
    Write-Host "`nERROR: Cannot fetch roles from $rolesUrl" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Yellow
    exit 1
}

if (-not $roles -or $roles.Count -eq 0) {
    Write-Host "`nERROR: No roles found in the database." -ForegroundColor Red
    exit 1
}

Write-Host "  Found $($roles.Count) role(s):" -ForegroundColor Green
$roles | ForEach-Object { Write-Host "    [$($_.roleId)] $($_.roleCode)" -ForegroundColor White }

Write-Host "`n==> Creating users..." -ForegroundColor Cyan

$roleNames = @{
    "ADMIN"           = "Administrator"
    "CEO"             = "CEO"
    "RISK_SPECIALIST" = "Risk Specialist"
    "DIRECTOR"        = "Director"
    "OWNER"           = "Risk Owner"
    "EMPLOYEE"        = "Employee"
}

$roleEmails = @{
    "ADMIN"           = "admin@riskplatform.local"
    "CEO"             = "ceo@riskplatform.local"
    "RISK_SPECIALIST" = "risk@riskplatform.local"
    "DIRECTOR"        = "director@riskplatform.local"
    "OWNER"           = "owner@riskplatform.local"
    "EMPLOYEE"        = "employee@riskplatform.local"
}

$results = @()

foreach ($role in $roles) {
    $code = $role.roleCode
    $name = if ($roleNames.ContainsKey($code)) { $roleNames[$code] } else { $code }
    $email = if ($roleEmails.ContainsKey($code)) { $roleEmails[$code] } else { "$code@riskplatform.local" }
    $roleId = $role.roleId

    Write-Host "  Creating user for role [$roleId] $code..." -ForegroundColor Yellow

    $body = [PSCustomObject]@{
        userFio         = $name
        userEmail       = $email
        roleIds         = @($roleId)
        personId        = $null
        directorateId   = $null
        responsibilityType = $null
    }

    try {
        $resp = Invoke-PostAsync -Url "$BaseUrl/users" -Body $body
        $results += [PSCustomObject]@{
            Role      = $code
            UserId    = $resp.userId
            Email     = $email
            Status    = "OK"
        }
        Write-Host "    OK (userId=$($resp.userId))" -ForegroundColor Green
    } catch {
        $msg = $_.Exception.Message
        $results += [PSCustomObject]@{
            Role      = $code
            UserId    = "-"
            Email     = $email
            Status    = "FAIL"
            Error     = $msg
        }
        Write-Host "    FAIL: $msg" -ForegroundColor Red
    }
}

Write-Host "`n===== Summary =====" -ForegroundColor Cyan
$results | Format-Table -AutoSize

$failures = $results | Where-Object { $_.Status -eq "FAIL" }
if ($failures) {
    Write-Host "`nFailed to create $($failures.Count) user(s)." -ForegroundColor Red
} else {
    Write-Host "`nAll users created successfully!" -ForegroundColor Green
}
