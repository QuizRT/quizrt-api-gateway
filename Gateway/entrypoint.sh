#!/bin/bash

set -e
run_cmd="dotnet run --server.urls http://*:80"

>&2 echo "SQL Server is up - executing command"
exec $run_cmd