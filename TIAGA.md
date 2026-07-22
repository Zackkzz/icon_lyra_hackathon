# ICON Lyra Hackathon — Fridge Meal Planner Prototype

## Purpose
A full-stack hackathon prototype that helps users track fridge contents, plan weekly meals, generate shopping lists, and chat with an LLM about their food — all powered by real-time fridge data.

## Stack
- **Mobile:** React Native Expo (Expo Router) — cross-platform mobile app with warm food-app UI
- **Backend:** ASP.NET Web API (.NET 10) with Entity Framework Core + Npgsql for PostgreSQL
- **Database:** PostgreSQL 16 via Docker Compose
- **LLM:** OpenRouter API with tool-calling (function calling) wired to database operations
- **Language:** C# (backend), TypeScript (mobile)

## Project Structure
```
prototype/
├── mobile/          — React Native Expo app
├── backend/
│   └── FridgeMealPlanner/  — ASP.NET Web API (.NET 10)
│       ├── Models/          — EF Core entities
│       ├── Data/            — AppDbContext with seed data
│       ├── Controllers/     — REST API controllers
│       ├── Services/        — OpenRouter LLM + ToolExecutor
│       ├── DTOs/            — Data transfer objects
│       ├── Enums/           — Unit, MealType, Source
│       └── Migrations/      — EF Core migrations
└── database/        — Docker Compose (PostgreSQL 16)
```

## Quick Start
1. Start database: `cd prototype/database && docker compose up -d`
2. Run backend: `cd prototype/backend/FridgeMealPlanner && dotnet run`
3. Run mobile: `cd prototype/mobile && npx expo start`

## Build & Test Commands
- Backend build: `dotnet build` (from prototype/backend/FridgeMealPlanner)
- EF migrations: `export PATH="$PATH:$HOME/.dotnet/tools" && dotnet ef migrations add <Name>` then `dotnet ef database update`
- Mobile type-check: `npx tsc --noEmit` (from prototype/mobile)
- Database: `docker compose up -d` / `docker compose down`

## Conventions
- Conventional Commits for git
- C# models match database schema exactly
- API routes follow RESTful patterns
- Mobile uses warm palette: creams, greens, rounded corners, soft shadows
