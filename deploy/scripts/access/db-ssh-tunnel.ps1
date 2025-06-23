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

$dbhosts = @{ }

$dbhosts["dev-1"] = @{ all = "xxxxxxxxx" }
$dbhosts["dev-2"] = @{ all = "xxxxxxxxx" }
$dbhosts["content-stage"] = [ordered]@{
    auth  = "xxxxxxxxx";
    main  = "xxxxxxxxx";
    video = "xxxxxxxxx";
}
$dbhosts["content-prod"] = [ordered]@{
    auth  = "xxxxxxxxx";
    main  = "xxxxxxxxx";
    video = "xxxxxxxxx";
}

[Console]::TreatControlCAsInput = $true

if ([System.Environment]::OSVersion.Platform -eq "Unix") {
    chmod 400 ${sshKeyFile}
}

$tunnels = @()
$port = 5433

Write-Host "Running SSH tunnels to ${envName}"
Write-Host ""

foreach ($db in $dbhosts[$envName].Keys) {

    $dbHost = $dbhosts[$envName][$db]

    ### Starting connection to each database

    $a = Start-Process `
        -FilePath "ssh" `
        -ArgumentList "-o IdentitiesOnly=yes -i ${sshKeyFile} -N -L ${port}:${dbHost}:5432 ec2-user@ssh-${envName}.frever-api.com" `
        -PassThru

    $tunnels = $tunnels + $a


    Write-Host "Tunnel to ${envName} ${db} (${dbHost}) -> use host and port below:"
    Write-Host "127.0.0.1:${port}"
    Write-Host ""

    $port = $port + 1
}

Write-Host "Press any key to stop..."
$k = [System.Console]::ReadKey()

Write-Host "Exiting..."

foreach ($process in $tunnels) {
    $process.Kill()
}
