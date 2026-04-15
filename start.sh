#!/bin/bash
echo "Starting backend..."
dotnet run --project src/Api/WcagAnalyzer.Api.csproj --launch-profile https &

echo "Starting frontend..."
cd src/frontend && npm start
