param(
    [string] $ProfileName = "dreamlens-dev",
    [string] $Region = "us-east-1"
)

$ErrorActionPreference = "Stop"

$aws = Get-Command aws -ErrorAction SilentlyContinue
if (-not $aws) {
    $fallback = "$env:LOCALAPPDATA\Programs\Amazon\AWSCLIV2\aws.exe"
    if (Test-Path $fallback) {
        $aws = [pscustomobject]@{ Source = $fallback }
    }
}

if (-not $aws) {
    throw "AWS CLI was not found. Open a new terminal or reinstall AWS CLI v2."
}

Write-Host "Configuring AWS profile '$ProfileName' in region '$Region'."
Write-Host "Use a rotated access key. Do not reuse any key that was pasted into chat, logs, or tickets."

$accessKeyId = Read-Host "AWS access key id"
$secretKey = Read-Host "AWS secret access key" -AsSecureString
$bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secretKey)

try {
    $plainSecret = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)

    & $aws.Source configure set aws_access_key_id $accessKeyId --profile $ProfileName
    & $aws.Source configure set aws_secret_access_key $plainSecret --profile $ProfileName
    & $aws.Source configure set region $Region --profile $ProfileName
    & $aws.Source configure set output json --profile $ProfileName
}
finally {
    if ($bstr -ne [IntPtr]::Zero) {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
    }
}

Write-Host "Verifying profile identity."
& $aws.Source sts get-caller-identity --profile $ProfileName
