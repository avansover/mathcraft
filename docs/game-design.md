# Mathcraft — Game Design Document

## Concept
A turn-based RPG for kids where math powers your skills.
Players choose a class and fight waves of monsters by answering math questions.
Harder skill = harder question = bigger reward.

---

## Core Mechanics

### Risk / Reward
- Easy skill → easy question → small effect
- Hard skill → hard question → big effect
- Players naturally push themselves to attempt harder questions

---

## Combat System

### Map
- One large scrollable grid map per dungeon (e.g. 16×10 tiles)
- Fixed viewport with camera auto-panning to active character
- Player can scroll manually
- Heroes start at one end, cross to the other fighting through monster encounters
- Boss room at the end of every dungeon

### Turn Order
- Initiative-based: characters act in order of their **Speed stat** (highest first)
- Heroes and monsters interleaved — fastest acts first regardless of side

### A Turn
1. **Move** — spend Move Points (MP) to reposition on the grid
2. **Act** — spend Attack Points (AP) to use a skill
3. Select skill → valid target tiles highlighted (based on skill range + hitbox)
4. Confirm target → Math Engine generates question
5. Correct answer → skill fires, hitbox applied, effects resolved
6. Wrong answer → skill misses
7. Turn ends, next character in initiative queue acts

### Character Stats

| Stat | Affects |
|------|---------|
| HP | Survivability |
| Attack | Physical damage output |
| Magic Attack | Spell damage output |
| Healing Power | Heal amount |
| Speed | Initiative / turn order |
| Move Points (MP) | Tiles moved per turn |
| Attack Points (AP) | Skills used per turn |
| Potion Points (PP) | Potions used per turn |

All stats grow on level up. MP, AP, and PP can also increase with level ups.
Each resource is independent — using a potion never costs an attack, and vice versa.

### Skill Properties
Each skill defines its own:
- **Range** — how far it can reach (in tiles)
- **Hitbox** — shape of affected area (single tile, 2×2, line, cross, etc.)
- **Difficulty** — Easy / Medium / Hard (feeds into Math Engine)
- **Effect** — damage, heal, status (status effects deferred to later phase)

### Death & Recovery
- A character that reaches 0 HP becomes **Downed** — stays on the map, cannot act
- Any other party member can spend **1 AP** to revive a Downed character outside of active combat
- Revived character returns with 30% HP
- If **all party members are Downed simultaneously** → dungeon resets to the beginning
- Characters keep their XP and items on reset — the dungeon run is lost, not the character

### Win Condition
- At least 1 hero standing when the last monster dies = dungeon cleared
- End of dungeon: boss drops rare loot, XP awarded to all surviving characters

---

## Loot & Economy

### Item Types
- **Potions** — consumable, used during combat (costs 1 AP) or outside combat
- **Gear** — equipment that improves stats (weapon, armor, etc.)
- **Gold** — currency spent at the Store between dungeons

### Drop Sources
- Regular monsters → common loot, XP, gold
- Dungeon boss → rare loot, more XP, more gold

### Store
- Accessible between dungeons
- Players spend gold on potions, gear, and other items
- Details TBD

---

## Multiplayer
- **Phase 1:** Hot-seat (same screen, pass the device)
- **Phase 2:** Online real-time multiplayer via SignalR

---

## Classes

### Warrior ⚔️
**Role:** Tank — high HP, physical damage, frontline fighter
**Stats per level:** HP++, Attack+

| Level | Skill | Difficulty |
|-------|-------|------------|
| 1 | Slash | Easy |
| 3 | Charge | Medium |
| 6 | Whirlwind | Hard |

---

### Mage 🧙
**Role:** Glass cannon — low HP, high magic damage
**Stats per level:** HP+, Magic Attack++

| Level | Skill | Difficulty |
|-------|-------|------------|
| 1 | Frost Bolt | Easy |
| 3 | Fireball | Medium |
| 6 | Blizzard | Hard |

---

### Priest ✨
**Role:** Healer/Support — keeps party alive, utility skills
**Stats per level:** HP+, Healing Power++

| Level | Skill | Difficulty |
|-------|-------|------------|
| 1 | Heal | Easy |
| 3 | Smite | Medium |
| 6 | Divine Shield | Hard |

---

## Progression System
- Characters gain XP from defeating monsters
- Each level up: stats increase (HP, attack/healing power)
- New skills unlock at specific levels (see class tables above)
- Loot drops from monsters (to be designed)
- Characters are persistent — saved between sessions

---

## Math Engine (Separate System)

> The game never specifies a math operation.
> It only specifies a difficulty level.
> The Math Engine translates difficulty + player age into an appropriate question.

### Difficulty Levels
- **Easy** — safe choice, small effect
- **Medium** — balanced risk/reward
- **Hard** — high risk, big payoff

### Age → Math Mapping (draft)

| Age | Easy | Medium | Hard |
|-----|------|--------|------|
| 6-7 | Addition (under 10) | Addition (under 20) | Subtraction (under 20) |
| 8-9 | Addition/Subtraction (under 50) | Multiplication (×2-×5) | Multiplication (×6-×9) |
| 10-11 | Multiplication (all) | Division (simple) | Long division |
| 12+ | Division | Multi-step | Multi-step + mixed |

### Key Design Principle
The Math Engine is a standalone module.
The game calls: `GetQuestion(difficulty, playerAge)` → receives a question + correct answer.
No other game system needs to know about math operations.

---

## Monster AI (LLM-Powered)
- Each monster turn, the LLM receives current game state:
  - Monster HP, available skills, position
  - Each player's HP, class, position
- LLM responds with structured output:
  ```json
  {
    "move": "position_x",
    "skill": "skill_name",
    "target": "player_id",
    "quote": "You call that arithmetic?! My grandmother does harder math!"
  }
  ```
- Monsters have personality — funny, dramatic, taunting
- LLM controls monster behavior completely (movement + skill choice + flavor)

---

## Authentication & User Management

### Account Structure
Two-level hierarchy:

```
Family Account (parent-owned)
└── Player Profiles (one per kid)
    └── Characters (one or more per profile)
```

### Family Account
- Created and managed by a parent
- Credentials: email + password
- Responsible for: billing, profile management, parental settings (future)
- One account per family

### Player Profile
- Created by parent under the family account
- No password — kids select their profile from a list on login
- Holds: display name, avatar, **age** (drives Math Engine difficulty)
- Has its own: characters, inventory, gold, progress
- Age is set by parent at profile creation, adjustable later

### Session Flow
1. Parent logs in with email + password
2. Profile selection screen shown — all family profiles listed
3. Kid taps their profile → enters the game (no password)
4. Optional: parent can PIN-protect the account settings to prevent kids from changing ages or deleting profiles

### Stay Logged In
- Family account session persists (remember me)
- Returning players go straight to profile selection screen

---

## Target Audience
- Primary: Kids aged 9-11 (designer's daughters)
- Designed to scale: ages 6-12 via Math Engine age mapping
