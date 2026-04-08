# Mathcraft — Claude Context

## What is this?
A turn-based math RPG for kids. Players fight monster waves by answering math questions.
Harder skill = harder math question = bigger effect.
Built by Amir as a personal portfolio project.

## Repo Structure
```
mathcraft/
├── CLAUDE.md                     <- you are here
├── docs/
│   └── game-design.md            <- full game design document
├── specs/
│   └── 001-user-auth/            <- Authentication & User Management
│       ├── spec.md               <- feature specification (complete)
│       ├── plan.md               <- implementation plan (complete)
│       ├── research.md           <- tech decisions (complete)
│       ├── data-model.md         <- entities and fields (complete)
│       ├── quickstart.md         <- validation steps (complete)
│       ├── contracts/api.md      <- REST endpoints (complete)
│       └── tasks.md              <- NEXT: generate with /speckit-tasks
├── .specify/
│   └── memory/constitution.md   <- project principles v1.0.1
├── client/                       <- React frontend (not started yet)
└── server/                       <- .NET backend (not started yet)
```

## Current Status
**Branch:** `001-user-auth`
**Last completed:** `/speckit-plan` ✅
**Next step:** `/speckit-tasks` — generate task list for 001-user-auth

## Tech Stack (locked for Phase 1)
- **Backend:** .NET 8 (C#) — ASP.NET Core, Entity Framework Core (code-first), MediatR (full CQRS)
- **Frontend:** React 18 (TypeScript) — React Router, Axios
- **Database:** PostgreSQL
- **Auth:** JWT — refresh token in HttpOnly cookie, access token in memory
- **Email:** SendGrid free tier (password reset)
- **Realtime (Phase 2):** SignalR
- **LLM:** Claude API (monster AI — optional, Phase 2)
- **Hosting:** Railway (dev/staging)
- **Monorepo:** `client/` + `server/` under one repo

## Architecture (decided)
- **Full MediatR CQRS** — all controllers dispatch via `IMediator`, no service layer
- **Unified response:** all handlers return `Result<T>` wrapper
- **Pipeline behaviors:** ValidationBehavior → LoggingBehavior → ErrorHandlingBehavior
- **EF Core code-first** — entities in C#, migrations version-controlled

## Key Design Decisions
- Family account (parent) owns multiple player profiles (kids)
- Kids select profile by tapping — no password at profile level
- Age lives on player profile — determines starting unlocked skill ranks
- Skills have 3 ranks (locked/unlocked/equipped) — rank drives math difficulty
- Math Engine is fully decoupled: game calls `GetQuestion(rank)` only
- All skills require math — attack, defend, heal alike. Wrong answer = AP wasted
- Turn order by Speed stat. Heroes beat monsters on tie.
- Move Points (MP), Attack Points (AP), Potion Points (PP) — independent per turn
- One large scrollable map per dungeon, room-by-room with fog of war
- Party leader chooses doors. Full party moves together — no splitting.
- Equal XP and loot for all party members regardless of kill count

## GitHub
https://github.com/avansover/mathcraft
Active branch: https://github.com/avansover/mathcraft/tree/001-user-auth
