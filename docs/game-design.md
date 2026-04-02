# Mathcraft — Game Design Document

## Concept
A turn-based RPG for kids where math powers your skills.
Players choose a class and fight waves of monsters by answering math questions.
Harder skill = harder question = bigger reward.

---

## Core Mechanics

### Combat Loop
1. Player selects a skill
2. Math Engine generates a question based on skill difficulty + player age
3. Correct answer → skill hits, effect applies
4. Wrong answer → skill misses
5. Monster turn → LLM selects monster move, target, and generates a quote
6. Repeat until wave is cleared or party is wiped

### Risk / Reward
- Easy skill → easy question → small effect
- Hard skill → hard question → big effect
- Players naturally push themselves to attempt harder questions

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

## Target Audience
- Primary: Kids aged 9-11 (designer's daughters)
- Designed to scale: ages 6-12 via Math Engine age mapping
