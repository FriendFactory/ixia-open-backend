if (!(Get-Module -ListAvailable | Where-Object { $_.Name -eq "AWSPowerShell.NetCore" })) {
    Write-Host "AWSPowerShell.NetCore Module is not installed"
    Set-PSRepository -Name PSGallery -InstallationPolicy Trusted
    Find-Module AWSPowerShell.NetCore | Install-Module -Confirm:$False
}
else {
    Write-Host "Module AWSPowerShell.NetCore is installed, loading..."
}

Import-Module -Name AWSPowerShell.NetCore -ErrorAction Stop

Get-S3Object -BucketName "xxxxxxxxx" -KeyPrefix "s3-access" | Remove-S3Object -Force

# Remove-S3Object `
#     -BucketName "xxxxxxxxx"
#     -KeyPrefix