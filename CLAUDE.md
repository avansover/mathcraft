# Mathcraft — Claude Context

## What is this?
A turn-based math RPG for kids. Players fight monster waves by answering math questions.
Harder skill = harder math question = bigger effect.
Built by Amir as a personal portfolio project.

## Repo Structure
```
mathcraft/
├── CLAUDE.md          ← you are here
├── docs/
│   └── game-design.md ← full game design document
├── client/            ← React frontend (not started yet)
└── server/            ← .NET backend (not started yet)
```

## Tech Stack (decided)
- **Frontend:** React
- **Backend:** .NET
- **Realtime (phase 2):** SignalR
- **LLM:** Claude API (monster AI)
- **DB:** TBD

## Current Status
Design phase — no code written yet.
`docs/game-design.md` has everything decided so far.

## What's decided
- 3 classes: Warrior, Mage, Priest
- Skills unlock by level (lv1, lv3, lv6)
- Stats grow each level (HP, attack/healing)
- Math Engine is a fully separate system — game calls `GetQuestion(difficulty, playerAge)`, never specifies math operations directly
- Monster AI powered by LLM (movement + skill choice + funny quotes)
- Phase 1: hot-seat multiplayer. Phase 2: online via SignalR
- Persistent characters, XP, loot system

## What's NOT decided yet (continue here tomorrow)
- [ ] Skill effects (what does Charge do exactly? Does Heal target one or full party? Does Divine Shield reflect?)
- [ ] Monster types and design
- [ ] Loot system
- [ ] DB schema
- [ ] Architecture doc

## GitHub
https://github.com/avansover/mathcraft
