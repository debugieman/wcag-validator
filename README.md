# WCAG Analyzer

A web application for validating website accessibility against WCAG (Web Content Accessibility Guidelines) standards.

## Tech Stack

- **Backend**: ASP.NET Core Web API (.NET 10)
- **Frontend**: Angular
- **Database**: SQLite with Entity Framework Core
- **Architecture**: Clean Architecture (Domain, Application, Infrastructure, Api)

## Project Structure

```
src/
├── Api/             # ASP.NET Core Web API
├── Application/     # Business logic, MediatR handlers, validators
├── Domain/          # Entities, enums, repository interfaces
├── Infrastructure/  # EF Core, repository implementations
└── frontend/        # Angular application
```

## Getting Started

### Prerequisites

- .NET 10 SDK
- Node.js & npm

### Run Backend

```bash
dotnet run --project src/Api
```

### Run Frontend

```bash
cd src/frontend
npm start
```
