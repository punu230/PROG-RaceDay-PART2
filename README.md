# RaceDay System

A full-stack web application for managing road running, walking, and cycling events in South Africa.

## Features
- **RESTful API:** ASP.NET Core 8 Web API with Entity Framework Core and SQL Server.
- **MVC Frontend:** Razor views with session management, role-based navigation, and responsive Bootstrap UI.
- **Role-Based Access:** Distinct interfaces for Organisers (manage events, categories, results) and Participants (browse, enrol, view results).
- **Azure Blob Storage:** Organisers can upload event banner images securely to Azure. Images are displayed on the event pages.
- **Dockerized MVC:** The frontend runs inside a Docker container.

## Tech Stack
- ASP.NET Core 8 (API & MVC)
- Entity Framework Core
- SQL Server
- JWT Authentication
- Azure Blob Storage (`Azure.Storage.Blobs`)
- Docker
- xUnit & Moq

## Setup Instructions

### Prerequisites
- .NET 8.0 SDK
- SQL Server (LocalDB or Express)
- Docker Desktop

### Running Locally
1. Clone the repository.
2. Open the solution `RACE_day.sln` in Visual Studio 2022.
3. Update the `appsettings.json` in `RACE_day` with your JWT Key and Azure Connection String.
4. In the Package Manager Console, run `Update-Database`.
5. Set Multiple Startup Projects: `RACE_day` and `RaceDay.MVC`.
6. Press F5.

### Running with Docker
To run the MVC application inside a Docker container:

1. Build the Docker image from the root of the repository:
   ```bash
   docker build -t raceday-mvc -f RaceDay.MVC/Dockerfile .