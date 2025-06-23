#!/bin/bash

dotnet test --no-build --filter "CreateDatabaseIntegrationTest" -p:TestTfmsInParallel=false
dotnet test --no-build --filter "Test" -p:TestTfmsInParallel=false --logger "trx;logfilename=TestResults.trx"

find $(pwd) -name "TestResults.trx" -type f -print0 | xargs -0 -I{} bash -c 'cp {} /host/$(basename $(dirname $(dirname {}))).trx'