#!/bin/bash
echo "Starting backend..."
dotnet run --project src/Api/WcagAnalyzer.Api.csproj &

echo "Starting frontend..."
cd src/frontend && npm start
