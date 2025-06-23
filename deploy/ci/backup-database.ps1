$ConnectionString = $args[0]
$OutFilePrefix = $args[1]

if (!$ConnectionString) {
    Write-Error "Please provide .NET-compatible Postgres connection string as first argument"
    exit 1
}

if (!$OutFilePrefix) {
    $OutFilePrefix = "$(Get-Date -Format 'yyyy-MM-dd_HH-mm')__"
}

function ParseConnectionString() {
    param (
        [string]$ConnectionString
    )

    $Parts = $ConnectionString.Split(";")


    $result = @{}

    foreach ($p in $Parts) {
        $kv = $p.Split("=")
        $result[$kv[0].ToLower()] = $kv[1]
    }

    return $result
}

pg_dump --help > /dev/null

if (!$?) {
    Write-Error "pg_dump command is not found"
    exit 1
}

$DbConnectionInfo = ParseConnectionString -ConnectionString $ConnectionString
$DbName = $DbConnectionInfo["database"]
$Username = $DbConnectionInfo["username"]
$Password = $DbConnectionInfo["password"]
$DbHost = $DbConnectionInfo["host"]

# Write-Host ($DbConnectionInfo | Format-List | Out-String)

$OutFile = "${OutFilePrefix}${DbName}.sql"

$PostgresConnectionString = "postgres://${Username}:${Password}@${DbHost}:5432/${DbName}"

pg_dump $PostgresConnectionString `
    --clean `
    --if-exists `
    --file=$OutFile `

Write-Host "Backed up to $OutFile"