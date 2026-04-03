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
6. Wrong answer → skill fails, AP is still spent
7. Turn ends, next character in initiative queue acts

**Universal rule:** ALL skills require a math question — attack, defend, and heal alike.
A wrong answer always wastes the AP. No free actions.

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
Each skill has base stats and 3 ranks. Ranks inherit base stats unless they explicitly override them.

```
Skill
├── name
├── base stats
│   ├── range        (tiles)
│   ├── hitbox       (single, 2×2, line, cross, etc.)
│   └── AP cost
└── ranks [1, 2, 3]
    ├── math difficulty   (Easy / Medium / Hard)
    ├── damage/effect multiplier
    └── overrides (optional): range, hitbox, AP cost, AoE size
```

### Skill Ranks

**Rank states (per skill per character):**

| State | Meaning |
|-------|---------|
| Locked | Not yet available — must be earned |
| Unlocked | Available to equip in loadout |
| Equipped | The rank used in combat |

**Unlock rule:**
- Age determines starting unlocked rank at character creation (older kids begin with higher ranks already unlocked)
- Complete a dungeon with rank X equipped → rank X+1 unlocks for that skill
- Ranks unlock per skill individually — a kid can have rank 2 Slash and rank 1 Frost Bolt simultaneously
- Unlocked ≠ Equipped — kids choose which rank to equip before each dungeon in the loadout screen
- A kid may keep rank 1 equipped even after unlocking rank 2 if rank 2 math feels too hard

**What rank changes:**
- Damage / effect multiplier — always scales with rank
- Other properties (range, hitbox, AoE, AP cost) — optional per-skill config overrides

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
**Role:** Tank — high HP, physical damage, frontline fighter, party protector
**Stats per level:** HP++, Power+

| Level | Skill | Type | Notes |
|-------|-------|------|-------|
| 1 | Slash | Attack | Single target melee |
| 1 | Shield Block | Defend | Correct → 50% dmg reduction + taunt; Wrong → no effect, AP wasted |
| 3 | Charge | Attack | Melee with positional component |
| 6 | Whirlwind | Attack | AoE melee |

---

### Mage 🧙
**Role:** Glass cannon — low HP, high magic damage, ranged
**Stats per level:** HP+, Power++

| Level | Skill | Type | Notes |
|-------|-------|------|-------|
| 1 | Frost Bolt | Attack | Single target ranged |
| 3 | Fireball | Attack | AoE ranged |
| 6 | Blizzard | Attack | Large AoE ranged |

---

### Priest ✨
**Role:** Healer/Support — keeps party alive, utility skills
**Stats per level:** HP+, Power++

| Level | Skill | Type | Notes |
|-------|-------|------|-------|
| 1 | Heal | Heal | Single target heal; Wrong → heal fails, AP wasted |
| 3 | Smite | Attack | Ranged holy damage |
| 6 | Divine Shield | Defend | Protects target ally; details TBD |

---

## Character System

### Per Profile
- Max **3 characters** per player profile
- Each character belongs to one class (Warrior, Mage, Priest)
- Characters are persistent — saved between sessions

### Stats

| Stat | Purpose |
|------|---------|
| HP | Survivability |
| Power | Damage AND healing output — scales with level and gear |
| Speed | Turn order (higher acts first, heroes beat monsters on tie) |
| Move Points (MP) | Tiles moved per turn |
| Attack Points (AP) | Skills used per turn |
| Potion Points (PP) | Potions used per turn |

### Starting Stats (Level 1)

| Stat | Warrior | Mage | Priest |
|------|---------|------|--------|
| HP | 120 | 60 | 80 |
| Power | 15 | 20 | 18 |
| Speed | 8 | 12 | 10 |
| MP | 4 | 3 | 3 |
| AP | 2 | 2 | 2 |
| PP | 1 | 1 | 1 |

### Progression
- Max level: **10**
- Each level up: all stats increase (amounts TBD per class)
- New skills unlock at specific levels (see class tables below)
- Max level is a configuration value — can be adjusted without code changes

---

## Math Engine (Separate System)

> The game never specifies a math operation.
> It only specifies a skill rank (1, 2, or 3).
> The Math Engine translates rank → question type and complexity.

### Key Design Principle
The Math Engine is a standalone module.
The game calls: `GetQuestion(rank)` → receives a question + correct answer.
No other game system needs to know about math operations.

### Rank → Math Mapping (draft)

| Rank | Math Difficulty | Example |
|------|----------------|---------|
| 1 | Easy | Addition / subtraction under 20 |
| 2 | Medium | Multiplication tables |
| 3 | Hard | Division, multi-step |

### Role of Age
Age does NOT affect question generation.
Age determines the **starting unlocked rank** at character creation:

| Age | Starting Unlocked Rank |
|-----|----------------------|
| 6-7 | Rank 1 only |
| 8-9 | Ranks 1-2 |
| 10+ | Ranks 1-3 |

A younger kid earns higher ranks through play. An older kid starts with more options immediately.

---

## Dungeon System

### Map Structure
- Each dungeon is a generated map of connected rooms
- Rooms are connected by doors
- The full map layout is visible to players at all times

### Visibility (Fog of War)
- **Current room** — fully lit, fully interactive
- **Adjacent rooms** — visible but darkened, no interaction
- **Undiscovered rooms** — hidden until an adjacent room is entered
- Room contents (monsters, loot) are always hidden until entered — full mystery, no door hints

### Room Types
| Type | Description |
|------|-------------|
| Start | Party spawns here, no monsters |
| Normal | Contains monsters + possible loot |
| Empty | No monsters, may contain a chest |
| Boss | End of dungeon, boss enemy, guaranteed rare loot |

### Party Movement
- All players occupy the same room at all times — no splitting
- After combat is resolved, the **party leader** (first player in party) chooses which door to take
- Entire party moves together into the chosen room
- On entering a new room: room lights up, adjacent rooms become dimly visible

### Combat Trigger
- Entering a room with monsters immediately starts combat
- Players cannot leave the room until all monsters are defeated

### Dungeon Generation
- Map layout is procedurally generated per dungeon run
- Boss room is always the furthest room from the start
- Exact generation rules TBD (guaranteed path to boss, room count range, etc.)

---

## Monster System

### Monster Structure
```
Monster
├── name
├── type          (regular / boss)
├── dungeon theme (forest / cave / castle — can appear in multiple)
├── stats
│   ├── HP
│   ├── Power
│   ├── Speed
│   ├── MP
│   └── AP        (no PP — heroes only)
├── skills []     (same structure as character skills, details filled per monster)
└── loot table
    ├── gold      (min, max)
    ├── XP reward
    └── item drops [] (item, drop chance %)
```

### Monster Roster (Phase 1)

| Monster | Type | Dungeon |
|---------|------|---------|
| Goblin Archer | Regular | Forest |
| Orc Fighter | Regular | Forest |
| Ogre | Boss | Forest |
| Kobold | Regular | Cave |
| Gnoll | Regular | Cave |
| Troll | Boss | Cave |
| Skeleton | Regular | Castle |
| Zombie | Regular | Castle |
| Leech | Boss | Castle |

Some monsters may appear in multiple dungeon themes — same entity, same stats.

### Boss Mechanics
Bosses are not just stronger regulars — they have unique skills that create special mechanics:
- **Ogre** — Knockback skill (repositions heroes on the grid)
- **Troll** — Regenerate skill (recovers HP each turn)
- **Leech** — Lifesteal skill (heals itself when hitting a player)

All boss mechanics implemented as skills, no special-casing in the engine.

### Monster Turns (No Math)
Monsters do not answer math questions. The LLM selects their move + skill + quote each turn.
Monsters always hit when they attack — no accuracy roll.

### Loot
- Regular monsters drop gold (random range), XP, and items by chance
- Boss monsters drop guaranteed rare loot + higher gold and XP
- Specific drop tables defined per monster in config

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
