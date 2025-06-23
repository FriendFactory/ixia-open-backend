#!/bin/bash
DOTNET_DIR=/usr/local/share/dotnet/x64/ # replace this with dotnet if it's in PATH on your machine
DOTNET_TOOLS_DIR=/Users/$(whoami)/.dotnet/tools
CONNECTION_STRING="Host=127.0.0.1;Port=5434;Database=main;Username=ffadmin1;Password=n7LDn62J5aigyUu2zk"

TABLE=$1

export PATH="$PATH:${DOTNET_TOOLS_DIR}:${DOTNET_DIR}"

dotnet tool uninstall --global dotnet-ef
dotnet tool install --global dotnet-ef --version 5.0.6


dotnet ef dbcontext scaffold \
    --namespace Frever.Shared.MainDb.Entities \
    --context-namespace Frever.Shared.MainDb \
    --context TempContext \
    --table ${TABLE} \
    --context-dir . \
    --output-dir Entities \
    --force \
    --no-onconfiguring \
    "${CONNECTION_STRING}" \
    Npgsql.EntityFrameworkCore.PostgreSQL