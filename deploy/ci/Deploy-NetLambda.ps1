$FunctionName = $args[0]
$FunctionPath = $args[1]

$AwsProfile = "default"
$AwsRegion = "eu-central-1"


dotnet lambda help > /dev/null

if (!$?) {
    Write-Host ".NET Core AWS Lambda tools is not installed, installing..."
    dotnet tool install -g Amazon.Lambda.Tools
}

if (!(Get-Module -ListAvailable | Where-Object { $_.Name -eq "AWSPowerShell.NetCore" })) {
    Write-Host "AWSPowerShell.NetCore Module is not installed"
    Set-PSRepository -Name PSGallery -InstallationPolicy Trusted
    Find-Module AWSPowerShell.NetCore | Install-Module -Confirm:$False
}
else {
    Write-Host "Module AWSPowerShell.NetCore is installed, loading..."
}

Import-Module -Name AWSPowerShell.NetCore -ErrorAction Stop

Set-Location $FunctionPath

dotnet lambda deploy-function $FunctionName `
    --function-runtime ".netcore3.1"
# --s3-bucket frever-deploy `
# --s3-prefix "media-convert-lambda/"

if (!$?) {
    Write-Error "Error publishing lambda"
    exit 1
}

Write-Host $FunctionName