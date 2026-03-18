# Baskent Yasam - University Platform

A comprehensive university life platform for Baskent University students and staff.

## Quick Start (Docker)

### Prerequisites
- Docker Desktop installed
- Git

### Setup Steps

1. **Clone the repository**
   ```bash
   git clone <repo-url>
   cd baskent-yasam
   ```

2. **Set up environment variables**
   ```bash
   cp .env.example .env
   # Edit .env with your real values (SMTP credentials, JWT secret, etc.)
   ```

3. **Run with Docker**
   ```bash
   docker-compose up --build
   ```

4. **Access the application**
   - Frontend: http://localhost:3000
   - API: http://localhost:5283/api
   - Swagger: http://localhost:5283/swagger
   - Database: localhost:5432

## Project Structure

```
/baskent-yasam/
├── README.md                    # This file
├── docker-compose.yml           # Container orchestration
├── .env.example                 # Environment variables template
├── BaskentYasam.sln             # .NET Solution file
│
├── docs/                        # Documentation
│   ├── docker-setup.md          # Docker setup guide
│   ├── database-setup.md        # Database setup guide
│   ├── testing-guide.md         # Testing guide
│   └── ...
│
├── backend/                     # .NET API
│   ├── ApiProject.csproj
│   ├── Program.cs
│   ├── appsettings.json
│   ├── Dockerfile
│   ├── Controllers/
│   ├── Services/
│   ├── Models/
│   ├── Data/
│   ├── Hubs/                    # SignalR Hubs
│   ├── Migrations/
│   └── scripts/                 # SQL scripts
│       ├── setup_database.sql
│       ├── migrations/
│       └── checks/
│
└── frontend/                    # React Frontend
    ├── package.json
    ├── Dockerfile
    ├── nginx.conf
    ├── public/
    └── src/
```

## Features

- User authentication with email verification
- Role-based access control (Student, Academic Staff, Admin)
- Appointment booking system
- Cafeteria ordering system
- Real-time notifications (SignalR)
- Instructor schedule management

## Development

### Local Development (without Docker)

**Backend:**
```bash
cd backend
dotnet run
```

**Frontend:**
```bash
cd frontend
npm install
npm start
```

### Environment Variables

See `.env.example` for all available environment variables.

## Documentation

- [Docker Setup](docs/docker-setup.md)
- [Database Setup](docs/database-setup.md)
- [Testing Guide](docs/testing-guide.md)
- [Team Instructions](docs/team-instructions.md)

## Tech Stack

- **Frontend:** React 19, TypeScript, Tailwind CSS
- **Backend:** .NET 8, ASP.NET Core Web API
- **Database:** PostgreSQL 16
- **Real-time:** SignalR
- **Containerization:** Docker, Docker Compose

## License

This project is for educational purposes at Baskent University.
