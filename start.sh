#!/bin/bash
echo "Starting WcagAnalyzer..."
echo "Backend: http://localhost:5042"
echo "Frontend: http://localhost:4200"
echo ""

dotnet run --project src/Api/WcagAnalyzer.Api.csproj &
cd src/frontend && npx ng serve
