<!-- Sync Impact Report
Version change: N/A → 1.0.0 (initial ratification)
Added sections: Core Principles (5), Tech Stack, Development Workflow, Governance
Removed sections: N/A (initial version)
Templates requiring updates:
  ✅ plan-template.md — Constitution Check gates align with principles below
  ✅ spec-template.md — Scope/requirements align with age-first and math-separation principles
  ✅ tasks-template.md — Task categories reflect Math Engine, LLM, and progressive delivery
Follow-up TODOs: None — all placeholders resolved.
-->

# Mathcraft Constitution

## Core Principles

### I. Math Engine Independence (NON-NEGOTIABLE)
The Math Engine MUST be a fully decoupled system, completely separate from all game logic.
Game systems MUST NOT reference specific math operations (addition, division, etc.) directly.
All game code MUST interact with the Math Engine only through a single interface: `GetQuestion(rank)`.
The Math Engine owns the mapping of `rank → question type + complexity`. Age determines starting unlocked rank at character creation only — it does not affect question generation.

**Rationale**: This separation is the core architectural decision of Mathcraft. It allows the game
to evolve independently of its educational content, and enables the math difficulty to be tuned
per player without touching game logic.

### II. Age-First Design
Every feature, skill, and game mechanic MUST be designed to support the full age range of 6–12.
No feature may hardcode a math operation, difficulty label, or numeric range for a specific age.
Age configuration MUST be a first-class player attribute, set at character creation and adjustable.

**Rationale**: The game is designed for Amir's daughters (9–11) but must scale to siblings,
classmates, and future users. Building age-awareness in from the start prevents costly retrofits.

### III. Spec Before Code
No implementation work MUST begin without a completed spec for that feature.
Specs MUST include: user stories with acceptance scenarios, functional requirements, and success criteria.
Code that exists without a corresponding spec is considered technical debt and MUST be specced retroactively.

**Rationale**: Spec-Driven Development ensures we build the right thing before building it right.
For a solo developer, this prevents scope creep and wasted implementation effort.

### IV. Progressive Delivery
Features MUST be built in phases, each phase delivering independent, testable value.
Phase 1 (hot-seat multiplayer) MUST be fully playable before Phase 2 (online multiplayer) begins.
The LLM monster AI MUST be optional — the game MUST be fully playable without it.

**Rationale**: A working game at each phase ensures the project always has something to show.
This is critical for a portfolio project and for maintaining motivation as a solo developer.

### V. Simplicity First (YAGNI)
No feature may be built speculatively. Every feature MUST map to a defined user story.
Abstractions are only permitted when the same logic is needed in 3+ distinct places.
Complexity MUST be justified in the plan's Complexity Tracking table.

**Rationale**: Portfolio projects are at risk of over-engineering. Every hour spent on unused
abstractions is an hour not spent on working, demonstrable features.

## Tech Stack

- **Backend**: .NET (C#)
- **Frontend**: React (TypeScript)
- **Real-time (Phase 2)**: SignalR
- **Database**: PostgreSQL
- **LLM**: Claude API (monster AI — optional enhancement)
- **Hosting**: Railway (dev/staging), scalable to AWS
- **Monorepo structure**: `client/` + `server/` under one repo

All stack decisions are locked for Phase 1. Changes require a constitution amendment.

## Development Workflow

- All features follow the spec-kit flow: `/speckit-specify` → `/speckit-plan` → `/speckit-tasks` → `/speckit-implement`
- Each user story MUST be independently testable before moving to the next
- Commits MUST be made after each completed task or logical group
- Secrets MUST use environment variables — never committed to the repo
- `.claude/settings.local.json` MUST remain in `.gitignore`

## Governance

This constitution supersedes all other practices and informal decisions for the Mathcraft project.
Amendments require: a clear rationale, an update to `LAST_AMENDED_DATE`, and a version bump.
All implementation plans MUST include a Constitution Check gate before Phase 0 research.

Version bump rules:
- MAJOR: Principle removed, renamed, or fundamentally redefined
- MINOR: New principle or section added
- PATCH: Clarification, wording fix, non-semantic refinement

**Version**: 1.0.1 | **Ratified**: 2026-04-03 | **Last Amended**: 2026-04-08
