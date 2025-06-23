$EnvName = $args[0]
$Bucket = $args[1]
$Prefix = $args[2]

######### INTRO
if (!$EnvName) {
    Write-Error "Please provide $EnvName as first argument"
    exit 1
}

if (!$EnvName) {
    Write-Error "Please provide bucket name as second argument"
}

if (!$Prefix) {
    $Prefix = ("MANUAL/$(Get-Date -Format 'yyyy-MM-dd_HH-mm')")
}

Write-Output "Backing up databases used in $EnvName to bucket $Bucket"

######### LOAD AWS MODULE

if (!(Get-Module -ListAvailable | where { $_.Name -eq "AWSPowerShell.NetCore" })) {
    Write-Host "AWSPowerShell.NetCore Module is not installed"
    Set-PSRepository -Name PSGallery -InstallationPolicy Trusted
    Find-Module AWSPowerShell.NetCore | Install-Module -Confirm:$False
}
else {
    Write-Host "Module AWSPowerShell.NetCore is installed, loading..."
}

Import-Module -Name AWSPowerShell.NetCore -ErrorAction Stop

####### LOAD AWS EBS CONNECTION STRINGS

$AwsProfile = "friendsfactory"
$AwsRegion = "eu-central-1"

$EbsEnvs = Get-EBEnvironment -ProfileName $AwsProfile -Region $AwsRegion `
| Where-Object { $_.EnvironmentName.ToLower().EndsWith("-${EnvName}".ToLower()) }

$ConnectionStrings = @()

foreach ($e in $EbsEnvs) {
    $envVars = Get-EBConfigurationSetting `
        -ApplicationName $e.ApplicationName `
        -EnvironmentName $e.EnvironmentName `
        -ProfileName $AwsProfile `
        -Region $AwsRegion `
    | Select-Object -ExpandProperty OptionSettings `
    | Where-Object { $_.Namespace -eq "aws:elasticbeanstalk:application:environment" }
    | Where-Object { $_.OptionName.ToLower().StartsWith("ConnectionStrings:".ToLower()) }
    | Where-Object { $_.Value.ToLower().IndexOf("Host=".ToLower()) -ne -1 }

    foreach ($cs in $envVars) {
        if (!$ConnectionStrings.Contains($cs.Value)) {
            $ConnectionStrings = $ConnectionStrings + $cs.Value
        }
    }
}

# Write-Host ($ConnectionStrings | Format-List | Out-String)

###### PREPARE BACKUP FOLDER

$BackupDir = "db-backups"
if (!(Test-Path $BackupDir -PathType Container)) {
    try {
        New-Item -Path $BackupDir -ItemType Directory -ErrorAction Stop | Out-Null
    }
    catch {
        Write-Error -Message "Unable to create directory $BackupDir. Error was: $_" -ErrorAction Stop
        exit 1
    }
}

Remove-Item (Join-Path -Path $BackupDir -ChildPath "*.*")

###### BACKUP

foreach ($cs in $ConnectionStrings) {
    pwsh ./ci/backup-database.ps1 $cs "$BackupDir/"

    if (!$?) {
        Write-Error "Error backing up $cs"
        exit 1
    }
}

####### UPLOAD TO BUCKET
Write-S3Object -BucketName $Bucket `
    -KeyPrefix $Prefix `
    -Folder $BackupDir `
    -ProfileName $AwsProfile `
    -Region $AwsRegion