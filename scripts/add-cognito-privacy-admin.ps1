param(
    [ValidateSet("dev", "qa", "prod")]
    [string] $Environment = "dev",
    [string] $Email = "ai.ro.dodoloata@gmail.com",
    [string] $Region = "us-east-1",
    [string] $GroupName = "dreamlens-admin"
)

$ErrorActionPreference = "Stop"

$poolName = "dreamlens-$Environment-users"
$pools = & aws cognito-idp list-user-pools --max-results 60 --region $Region | ConvertFrom-Json
$pool = $pools.UserPools | Where-Object Name -eq $poolName | Select-Object -First 1
if ($null -eq $pool) {
    throw "Cognito user pool '$poolName' does not exist. Apply the $Environment Terraform stack first."
}

$filter = 'email = "' + $Email + '"'
$users = & aws cognito-idp list-users --user-pool-id $pool.Id --filter $filter --region $Region | ConvertFrom-Json
$user = $users.Users | Select-Object -First 1
if ($null -eq $user) {
    throw "No Cognito user with email '$Email' exists in '$poolName'. Register and confirm that account first, then rerun this script."
}

& aws cognito-idp admin-add-user-to-group `
    --user-pool-id $pool.Id `
    --username $user.Username `
    --group-name $GroupName `
    --region $Region
if ($LASTEXITCODE -ne 0) {
    throw "Failed to add '$Email' to '$GroupName' in '$poolName'."
}

Write-Host "Added '$Email' to '$GroupName' in '$poolName'. Sign out and back in to receive an updated token."
