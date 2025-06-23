#!/usr/local/bin/pwsh

$envName = $args[0]
$sshKeyFile = $args[1]

# https://docs.microsoft.com/en-us/powershell/scripting/install/installing-powershell-on-macos?view=powershell-7.2

if (!$envName) {
    Write-Error "Env name should be specified as first argument"
    exit 1
}

if (!$sshKeyFile) {
    Write-Error "SSH key file should be specified as second argument"
    exit 1
}

$redishosts = @{ }

$redishosts["dev-1"] = "xxxxxxxxx"
$redishosts["dev-2"] = "xxxxxxxxx"
$redishosts["content-test"] = "xxxxxxxxx"
$redishosts["content-stage"] = "xxxxxxxxx"
$redishosts["content-prod"] = "xxxxxxxxx"

[Console]::TreatControlCAsInput = $true

if ([System.Environment]::OSVersion.Platform -eq "Unix") {
    chmod 400 ${sshKeyFile}
}

$port = 6377

Write-Host "Running SSH tunnels to ${envName}"
Write-Host ""

$redisHost = $redishosts[$envName]

### Starting connection to redis

$a = Start-Process `
    -FilePath "ssh" `
    -ArgumentList "-o IdentitiesOnly=yes -i ${sshKeyFile} -N -L ${port}:${redisHost}:6379 ec2-user@ssh-${envName}.frever-api.com" `
    -PassThru

sleep 3

redis-cli -h 127.0.0.1 -p ${port} -c

Write-Host "Exiting..."

$a.Kill()