# RPGFramework.Field

The field: the explorable places of an RPG Framework game. A field is a prefab, loaded from an asset bundle. Its
characters and objects are **entities**, and each runs **scripts**, authored in the Field Designer's block editor and
compiled to bytecode, for movement, dialogue, music, the menus, saving and starting battles. Fields can be 2D or 3D.

Requires Unity 6000.6 or newer, RPGFramework.Core and its shared types, the Field, Menu and Battle shared types,
RPGFramework.DI, RPGFramework.Audio, RPGFramework.Localisation, RPGFramework.Hashing, Unity.Mathematics and
Cinemachine.

---

## Installation

Install through the Package Manager from the repository's latest release tag, with the framework packages it depends
on.

---

## Setting up

1. **A field scene** holding a `FieldModuleMonoBehaviour` (GameObject > RPG Framework > Field > Field Module makes one,
   with its camera) with:
   - the **Cinemachine Brain** on the scene's camera, set to cut: it takes its view from each field's own camera
     (Cameras, below);
   - the `UIDocument` dialogue windows are drawn in;
   - the scene's `InputAdapter`, on your gameplay map: the field reads its `Confirm` (talking, and a dialogue window's
     pages), `Cancel` and `Menu` (the party menu), and moves with the action you name;
   - the game's up direction, `0,1,0` for a 3D field and `0,0,1` for a 2D one;
   - how wide the player's interaction angle is, 180° by default, and how far above or below them someone can be
     talked to, 2 by default.

   It also binds the field module, and its default presentation, loading fields from prefabs.
2. **A scene installer** binding `IFieldDatabase` to the generated `FieldDatabase` (see Export, below). To show fields
   your own way, bind `IFieldPresentation` there too, and the default is not used.
3. **In your global installer**, bind `IFieldArgsStore` to `FieldArgsStore`, `IGameOverFieldStore` to
   `GameOverFieldStore` and `IFieldResumeDataStore` to `FieldResumeDataStore`.
4. **Add the scene to your game's module and scene databases** under `FieldConstants.MODULE_ID`.
5. **Open your variable map.** Field's variables are added to it: the current field and spawn point, the player's
   saved position and facing, and the game-over field and its spawn point. Pick the field and spawn point a new game
   starts at as their defaults, the field a game over sends the player to, and the battle music a new game starts with,
   Core's `BattleMusic`, by name.

Field's variables:

| Variable | Bank | What it holds |
| --- | --- | --- |
| `CurrentField`, `CurrentSpawn` | Persistent | where the player is, by field and spawn point; their defaults are where a new game begins |
| `PlayerPositionX`, `PlayerPositionY`, `PlayerPositionZ`, `PlayerFacing` | Persistent | where the player stood, and which way they faced, when the game was saved |
| `GameOverField`, `GameOverSpawn` | Session | where a lost battle sends the player; only their defaults are used |
| `ScreenFade`, `ScreenFadeTransition`, `ScreenColour` | Persistent | how a script left the field's screen: faded, and by which transition, or tinted |

---

## Fields

A field is a prefab whose root has **`FieldEntities`**: whether the field is 2D or 3D, and every entity in it. Under the
root sit the entities' bodies, spawn points, blockers, cameras and scenery. A field is loaded whole, from its own bundle: large
areas aren't streamed.

**A tile field** is a 2D field with a **Walkable Tilemap** component on one of its tilemaps (GameObject > RPG Framework
> Field > Walkable Tilemap makes one): every character in it walks a tile at a time on that tilemap's tiles, in the four
directions of its grid, as the SNES-era games do. A 2D field without one is walked freely, sliding along what it meets.
A tile can be entered only where the tilemap has one, no blocker closes it and nothing solid stands on it — another
character, or a wall of colliders — and walking at one that cannot be entered turns the walker to face it; walking at a
character pushes it, as in a free field. Characters keep their rigidbodies, so triggers still notice them.

**A scripted move on a tile field** walks a tile at a time too: across the grid first, then up or down, to the tile its
point is in, so it always ends on a tile's centre. For another route, a script uses two moves. A tile it cannot enter is
waited at, the character facing it, until it clears or a script stops the move, and stopping finishes the step under
way. Walking up to an entity stops on the first tile within the stop distance, so a distance of 1 is the tile beside it.

### Cameras

**Each field is seen through its own Cinemachine camera**, placed in its prefab, whose view the field scene's camera
takes. **GameObject > RPG Framework > Field > Camera** makes one for each way a field is seen:

| Camera | For | What it is |
| --- | --- | --- |
| 2D Fixed | a 2D field that fits the screen | a camera at the centre of the field's tilemaps |
| 2D Follow | a 2D field larger than the screen | a camera locked onto the player, stopping where its view meets the edge of its bounds, which start out covering the tilemaps |
| 3D Fixed | a 3D field seen from one angle | a camera where the scene view looks from |
| 3D Follow | a 3D field the view slides across | a camera at a fixed angle, locked onto the player and moving only within its bounds |

A follow camera carries **Follow Player**, which keeps it on whoever is the player, and its bounds are a trigger
collider, so nothing walks into them. **A script can scroll a follow camera's view** to a point or onto another entity,
for a cutscene that shows something away from the player, then hand it back by scrolling onto the player; the bounds
still keep the view inside the field, and a field whose camera does not follow refuses a scroll at export. Everything
else is Cinemachine's: give a camera damping, a dead zone, or URP's Pixel Perfect Camera with Cinemachine's
pixel-perfect extension for pixel art. The field moves the camera from its own update, so it stops with the field under
the menu.

**A field can have several cameras**, one looked through at a time. Each carries a **Field Camera** naming it by an id
unique in the field, which every camera the menu makes is given; a field entered afresh starts on the lowest, and after
the menu or a battle it is on the one it was. `SET_CAMERA` changes it, cutting at once or blending over seconds. A
**camera zone** — an area whose enter script is a `SET_CAMERA` — changes it as the player walks in: give each part of
the field a zone of its own, overlapping where they meet, as the physics may report leaving one after entering the next.
A direction held through a cut follows the new camera at once.

### Entities

An entity is an id, a name, its scripts and, if it is something in the world, a **body**. An entity with no body is
only its scripts, for one that plays music or sets a flag.

Bodies are built from **presets** named for what they are for:

| Preset | Built | Scripts it starts with |
| --- | --- | --- |
| Gateway | an unseen trigger area | a gateway script to write the jump in |
| Character | a kinematic rigidbody, a capsule — standing in 3D, in 2D a footprint at its feet a cell wide — and an interaction trigger | shows itself |
| Player character | the same, without the interaction trigger | makes itself the player |
| Interactable object | a solid box and an interaction trigger | — |
| Examine point | an interaction trigger only, so it doesn't block | — |
| Area | a trigger area | — |
| Camera zone | a trigger area | changes to the field's newest camera as the player walks in |
| Save point | a trigger area, with placeholder visuals | allows saving while the player stands in it |
| Plain | nothing: seen and moved by scripts, walked through and never talked to | — |

A body's visuals are your own prefab, placed as its visible object.

### The Field Designer

**RPG Framework > Field Designer Window** lists your game's fields. For each one you set:

- **Prefab:** which prefab the field is, and its **exit and entry transitions** — what covers the screen as the player
  leaves for another field, and what reveals this one as they arrive — or the game's defaults.
- **Text:** the localisation sheets it loads, and its location name, chosen from those sheets' keys.
- **Scripts:** every entity and its scripts, opened from the prefab.
  - Add and delete entities, scripts and blockers, and give an entity a body built from a preset.
  - Edit the selected script in the **block editor**, check it compiles, and save the field.

Entities, blockers, spawn points and cameras can also be made from **GameObject > RPG Framework > Field** while
editing a field's prefab.

### Export

**Export** checks every field and writes nothing unless all of them pass. Among other things, it refuses:

- a script that doesn't compile;
- a request for an entity or script the field lacks;
- a body of the wrong dimension, or a dynamic rigidbody;
- an animation state the entity's Animator lacks;
- a music track or sound no music or SFX provider lists, or a stem state its track doesn't have;
- a dialogue key none of the field's sheets has;
- a jump to a spawn point its destination lacks;
- a Walkable Tilemap in a 3D field, or two in one field, and in a tile field a spawn point or character off a tile's
  centre, or a move to a written point off a tile's centre or where there is no tile;
- a field with no Cinemachine camera, a camera without a Field Camera or two sharing an id, a script naming a camera the
  field lacks, and a follow camera's bounds missing or not a trigger;
- a scroll in a field whose camera does not follow, or onto an entity with no body.

It then writes **`FieldDatabase.cs`**, which maps each field to its bundle, sheets and location name; it is generated,
so don't edit it, and as it is C#, a field can't be added without a new build. It builds each field's asset bundle and
records the variable map's layout and the table of opcodes. **A player build is refused if the variable map or the
opcodes, the framework's or your own, have changed since the last export**, because compiled scripts address
variables where they were and hold the opcodes' values as they were.

**RPG Framework > Field > Draw Interaction and Trigger Debug** draws interaction ranges and which way each entity faces,
triggers, gateways and blockers in the Game view.

---

## Scripts

### When they run

| Type | Slot | Runs when |
| --- | --- | --- |
| `Init` | 0 Main | the field loads, before it is shown; every entity's first script |
| `Default` | 0 Main, in place of the init | once every entity's init has run, and may loop and wait |
| `OnEnter` / `OnLeave` | 2 Enter / 3 Leave | the player enters or leaves the entity's trigger; leaving interrupts an enter script still running |
| `Gateway` | 2 Enter | the player enters the entity's trigger, to leave the field; gateways can be switched off as a group |
| `OnInteraction` | 7 Interaction | the player faces the entity and presses confirm |
| `OnPush` | 6 Push | the player walks into the entity |
| `Requested` | its own choice, 1 if none | another script asks for it, by entity and script |

- **One script runs per entity at a time.** An entity has eight priority slots, 0 to 7, the most urgent 7, and a script
  runs in its type's slot however it starts. A more urgent script interrupts a less urgent one, which carries on where
  it left off once the urgent one returns. A requested script chooses its slot, as the slot decides what may interrupt
  it.
- **Each entity runs up to 16 instructions a frame**, so a loop can't stall the game. A long script simply carries on
  next frame.
- **Scratch values are Session or Temp variables.** A Temp variable is one script's own, zeroed as it starts. For a
  value two entities share — a roll one makes and another reads — declare a few Session variables as your game's shared
  scratch, and have each field set one before reading it: Session isn't saved, and nothing clears it as a field loads.

### Writing them

The block editor stacks blocks, and every argument that names something is picked from a list. Picked arguments include
entities and their scripts, fields and spawn points, music, sounds and stem states, animation states, dialogue keys and
variables. A new block starts on the first choice in each list, and changing a block's track or field moves its stem
state or spawn point to one the new choice has.

The script is kept as text, one instruction a line. A shopkeeper's interaction script might read:

```
IF_INT $story_progress >= 4
    SHOW_DIALOGUE_WINDOW 0 TestField/Welcome true
ELSE
    SHOW_DIALOGUE_WINDOW 0 TestField/Closed true
END_IF
RETURN
```

- **`$name` reads or writes a variable** from the variable map. `$items[3]` names an array's element, and
  `$leader.hp` a record's field. Any number, flag or id argument can be a variable.
- **`IF_` blocks** compare two values, with an optional `ELSE`, and close with `END_IF`. **`LABEL`s** mark where jumps
  go.
- **Names are hashed when compiled**, so reordering a list never repoints a script. A name with a space in it is
  written in double quotes, `PLAY_MUSIC "Town Theme" AllStems`, which the block editor does itself.
- **The palette only offers opcodes the field can run.** Those that act through an optional package, such as PS1 reverb,
  appear only once it is installed. Some opcodes are declared for what is still to come but don't run yet, and aren't
  offered. A game can add opcodes of its own (*A game's own opcodes*, below).

### The opcodes

What a script can do, by what it acts on. Each block in the palette says what it does.

| | |
| --- | --- |
| **Flow** | `RETURN` `REQUEST_SCRIPT` `REQUEST_SCRIPT_WAIT_START` `REQUEST_SCRIPT_WAIT_END` `RETURN_TO_SCRIPT` `RETURN_TO_PRIORITY` `CALL_SCRIPT` `RETURN_FROM_CALL` `GOTO_JUMP` `GOTO_DIRECTLY` `IF_BOOL` to `IF_FLOAT` `YIELD` `WAIT_SECONDS` `NOP` |
| **The field, saving, battles and game over** | `JUMP_TO_MAP` `GATEWAY_TRIGGER_ACTIVATION` `SET_FIELD_EXIT_FADE` `SAVE_ACCESSIBILITY` `SET_BATTLE_MODE` `START_BATTLE` `GET_LAST_BATTLE_RESULT` `GAME_OVER` |
| **Variables** | `SET_` at each width from `BOOL` to `FLOAT`; `ADD_`, `SUB_`, `MUL_` and `DIV_` from `SBYTE` to `FLOAT`; `MOD_`, `INC_`, `DEC_`, `AND_`, `OR_`, `XOR_`, `NOT_`, `SET_BIT_` and `UNSET_BIT_` from `SBYTE` to `ULONG`; `_CLAMPED` forms of `ADD_`, `SUB_`, `INC_` and `DEC_`; `GET_RANDOM` `RANDOM_SEED` |
| **Dialogue and menus** | `CREATE_DIALOGUE_WINDOW` `SHOW_DIALOGUE_WINDOW` `SHOW_DIALOGUE_WINDOW_NO_WAIT` `SHOW_DIALOGUE_WINDOW_NO_WAIT_DISMISSIBLE` `WAIT_FOR_DIALOGUE_WINDOW` `CLOSE_DIALOGUE_WINDOW` `SET_DIALOGUE_WINDOW_STYLE` `SET_MESSAGE_VARIABLE` `ASK_PLAYER_TO_MAKE_A_CHOICE` `MAIN_MENU_ACCESSIBILITY` `OPEN_SAVE_MENU` |
| **Money** | `ADD_MONEY` `SUBTRACT_MONEY` |
| **Entities** | `SET_PLAYER_ENTITY` `LOCK_INPUT` `VISIBILITY` `SOLIDITY` `PUSH_SCRIPT_ACTIVATION` `INTERACTION_TRIGGER_ACTIVATION` `SET_INTERACTION_RANGE` `COLLISION_TRIGGER_ACTIVATION` `SET_ENTITY_POSITION` `SET_ENTITY_ROTATION` `SET_ENTITY_ROTATION_OVER_TIME` `SET_DIRECTION_TO_FACE_ENTITY` |
| **Movement** | `SET_MOVEMENT_SPEED`; `MOVE_ENTITY` `GLIDE_ENTITY` `SLIDE_ENTITY` `MOVE_ENTITY_TO_ENTITY`, each also `_AND_WAIT`; `WAIT_FOR_MOVEMENT` `STOP_MOVEMENT` |
| **Animation** | `SET_BASE_ANIMATION`; `PLAY_ANIMATION` and `PLAY_PART_OF_ANIMATION`, each also `_AND_WAIT`, `_AND_HOLD`, `_HOLDING` and `_LOOPING`; `SET_ANIMATION_SPEED` `WAIT_FOR_ANIMATION` `STOP_ANIMATION` `PUSH_ANIMATION_STATE` `POP_ANIMATION_STATE` |
| **The camera** | `SCROLL_SCREEN_TO_POSITION` `SCROLL_SCREEN_TO_ENTITY` `WAIT_FOR_SCROLLING` `SET_CAMERA` |
| **The screen and blockers** | `FADE_SCREEN` `FADE_SCREEN_AND_WAIT` `WAIT_FOR_FADE` `SUBTRACT_SCREEN_COLOUR` `ADD_SCREEN_COLOUR` `WAIT_FOR_SCREEN_COLOUR` `BLOCKER_ACTIVATION` |
| **Music and sounds** | `PLAY_MUSIC` `CROSSFADE_MUSIC` `STOP_MUSIC` `MUSIC_STEM_STATE` `IS_MUSIC_PLAYING` `SET_MUSIC_VOLUME` `FADE_MUSIC_VOLUME` `FADE_MUSIC_VOLUME_FROM` `WAIT_FOR_MUSIC_VOLUME` `SET_BATTLE_MUSIC` `PLAY_SOUND` `SET_REVERB_PRESET` `SET_REVERB_VOLUME` |
| **The countdown** | `SET_COUNTDOWN_TIMER` `SHOW_COUNTDOWN_TIMER` `HIDE_COUNTDOWN_TIMER` `GET_COUNTDOWN_TIMER` |
| **Input** | `WAIT_FOR_BUTTON` |

### A game's own opcodes

What only your game has — its own data, kept in its variables, read or changed by C# a script can't express — can be an
opcode of its own, offered in the palette under **Game** and compiled and checked as the framework's are.
**Assets > Create > RPG Framework > Field > Game Opcodes** makes a script to start from: the enum, its example opcode at
`GAME_BASE`, and the database that runs it, in one file so it compiles at once; split them as it grows.

1. **Describe it** in a `ushort` enum of your own, with the attributes the framework's opcodes use. Values start at
   `FieldScriptOpCodeRanges.GAME_BASE`, `0x8000`; the framework's are below. Every exported field holds the values, so
   give each new opcode the next one and never change or reuse one.

   ```csharp
   internal enum GameOpCode : ushort
   {
       [FieldOpCode("COUNT_ITEMS", ArgumentLayout.Sequential, Summary = "Read how many of an item the player holds")]
       [Argument(0, "count", ArgumentType.Variable, VariableWidth.Int)]
       [Argument(1, "item",  ArgumentType.Byte)]
       CountItems = FieldScriptOpCodeRanges.GAME_BASE
   }
   ```

2. **Handle it** in a class of your own implementing `IFieldGameOpCodeDatabase`, which holds each opcode's value with the
   method that runs it, a dictionary as the framework's own handlers are; the field puts them in one table with its
   own as each field's VM is made, indexed by the opcode. A method reads every argument, in order, with the read its type takes — `ReadByte` for a
   `Byte`, an `EntityId` or a `CameraId`, `ReadUShort`, `ReadInt` (also for `Seconds`), `ReadFloat`, `ReadBool`,
   `ReadName` for a hashed name or key, and `ReadDestination` for a `Variable` — then acts. A number can come from a
   variable, as any opcode's can. It writes a result to a destination with `WriteInt` and the rest, at the width the
   destination declares: `Bool`, `Byte`, `UShort`, `Int` or `Float`. The class's constructor takes your game's services.

   ```csharp
   internal sealed class GameOpCodeDatabase : IFieldGameOpCodeDatabase
   {
       private readonly IItemStore                             m_Items;
       private readonly Dictionary<ushort, FieldOpCodeHandler> m_Handlers;

       internal GameOpCodeDatabase(IItemStore items)
       {
           m_Items    = items;
           m_Handlers = new Dictionary<ushort, FieldOpCodeHandler>
                        {
                            { (ushort)GameOpCode.CountItems, CountItems }
                        };
       }

       IReadOnlyDictionary<ushort, FieldOpCodeHandler> IFieldGameOpCodeDatabase.Handlers => m_Handlers;

       private void CountItems(FieldScriptContext context)
       {
           FieldScriptDestination count = context.ReadDestination();
           byte                   item  = context.ReadByte();

           context.WriteInt(count, m_Items.Count(item));
       }
   }
   ```

3. **Bind it** in the field scene's installer, where the field otherwise binds one holding none:

   ```csharp
   container.BindSingleton<IFieldGameOpCodeDatabase, GameOpCodeDatabase>();
   ```

A handler runs once and the script carries on, in the same frame. It acts on your game's own services, not on the field:
moving an entity, showing a message or opening a menu stay the framework's opcodes, written beside yours. Your enum
must be in an assembly a player builds.

**What is checked.** Export refuses a value below `0x8000` or one two opcodes share, a script name the framework or
another of yours uses, a layout other than `Sequential`, and an argument type a handler can't read: a jump, a
comparison, a bank layout's value, or one of dialogue's channels, styles and slots. As the field module enters in play,
the editor logs an opcode described but never added, and one added but never described or below `0x8000`. In the editor, every read a handler makes is checked against its attributes; a read of another type, past the
last argument or an argument left unread is logged, and the script stops there.

---

## At run time

- **Movement:**
  - In a tile field every body moves a tile at a time on its walkable tilemap. Elsewhere a body moves with a driver
    chosen from what it has: a 3D rigidbody, a 2D rigidbody, or a plain transform.
  - Bodies are kinematic. The rigidbody drivers sweep and slide along what they hit, rather than pushing or being
    pushed.
  - Scripts walk entities to a point, glide or slide them there, or have one follow another: straight there, or on a
    tile field a tile at a time.
  - There is no NavMesh or walkmesh: movement is physics, a tilemap or plain transforms.
- **Facing:** every entity with a body faces one way, which walking turns it to at once, scripts set or turn over time,
  and interaction, the saved game and its animation read.
  - A 3D entity faces by turning its body.
  - A 2D entity never turns its sprite: it keeps the way it faces and its animation shows it through `FacingX` and
    `FacingY`. A rotation a script gives is read as Unity's 2D rotations are — none faces up the screen, and `0 0 90`
    a quarter turn anticlockwise, to the left. It stands facing down the screen, towards the camera, until something
    turns it; the player arrives facing the way its spawn point is turned.
- **Blockers** are objects in the field with an id that a script switches on and off. Switching one switches its
  colliders, its tilemap of closed cells and its visuals together, to close off part of a field.
- **Interaction:**
  - Confirm talks to whoever is most nearly straight ahead of the player, among the entities within their own
    interaction range and the field's height limit. Distance only decides who is in range. An entity can be made to
    answer only when it faces the player, by narrowing its interaction angle.
  - Which entity is the player is a script's choice, and may be none, for a field that is all cutscene.
- **Animation:**
  - An entity's base animation is one Animator state.
  - The field writes a `Speed` parameter, and `FacingX` and `FacingY` — which way the entity faces, with up the screen
    positive — so your controller decides how idle, walking and running blend, and which way a sprite looks.
  - Scripts play other states over the base one, once or looping, and can wait for them.
  - Animation events, timeline cutscenes and animated turning aren't supported yet.
- **Dialogue:**
  - Windows are addressed by channel, 0 to 7, each keeping its position and style until the field changes, when
    every window closes.
  - A window can hold the player while it waits for confirm, or leave the player free to walk away. A background one
    stays up until a script closes it, or until a press does once its script waits for it.
  - The newest window has the focus, a question keeping it until answered: only it finishes typing, turns its page or
    answers. One press closes every window that has finished, so two people speaking at once both close together.
  - A background window turns its pages while it has the focus, and lets the press through, so the player can still
    talk to someone. A dismissible one also closes on a press once it has typed, as people chatting nearby do, their
    script closing each line after a while if the player does not.
  - A question's cursor starts on the answer its script names, and cancel moves it to its cancel answer, with the
    cancel sound; the player still confirms it.
  - Text types at the player's field message speed.
- **Scrolling:**
  - A scroll moves the view from wherever it is to a point or onto an entity, over seconds, steadily or easing in and
    out, or at once; `WAIT_FOR_SCROLLING` waits for it to arrive. A point stays put; an entity is followed as it moves,
    and the player is followed again after a scroll onto the player.
  - The player can still walk while the view is away, unless the script locks input.
  - A jump to another field starts with the view on the player; after the menu or a battle it is where the script left
    it.
- **Cameras:**
  - `SET_CAMERA` looks through another of the field's cameras: at once, or blending over seconds, steadily or easing in
    and out. A blend cut short by a battle comes back finished.
  - Every follow camera follows the same view, so a scroll carries on across a change between them.
- **Input:** `WAIT_FOR_BUTTON` waits for a press of a button action, picked by name from your input actions,
  made after the wait began — through a script's input lock, as a game-over screen waits for confirm.
- **The screen:**
  - Scripts fade the screen out and back in beneath the message windows, so a message can show over a black screen,
    through any transition or the field's own exit and entry transitions, or at once.
  - They take a colour from the screen or add one, at once or over seconds, as a tint or a flash.
  - A fade and a colour are one state, each replacing the other, and where a script leaves it is saved: a field left
    faded or tinted is still so after the menu, a battle or a load.
  - The colours, and the default subtractive fade, need URP (Core's README).
- **Leaving a field:** a gateway or a script jumps to another field and spawn point, the screen covered by the field's
  exit transition and revealed by the next field's entry transition, unless a script has turned the exit's cover off
  because it faded the screen itself. The player can walk and talk as a field fades in, whether arriving, back from
  the menu or a battle, or after a load. The menu button opens the party menu when the field allows it and the screen
  is clear — not while it fades in, nor while a script's fade plays or holds it covered — and a script can start a
  battle or end the game.
  - A battle carries its own arena, enemies, flags and level. Whether losing it, or a countdown running out, ends the
    game is the battle mode's, which a script sets and the game saves; after the battle, a script reads how it ended.
    There are no random encounters yet: a battle starts only when a script starts it.
  - A game over sends the player to the game-over field your map names, entered afresh: a field like any other, whose
    scripts decide what it shows and when the game goes back to the title.
  - Returning from the menu or a battle puts the field back as it was: positions, animations, visibility, blockers, the
    camera and more.
  - Arriving in a new field starts it afresh.
- **Saving:**
  - Saving is allowed where your game's policy says: anywhere, or only where a script allows it, such as at a save
    point.
  - The player's position and facing are saved with the game, and a loaded game resumes in the field it was saved in.
- **The countdown:**
  - Scripts set it in seconds and show it where they choose, as minutes and seconds with a blinking colon; a battle
    carrying it shows it in the same place. It counts down beside the play time, in every module, shown or not, and is
    saved with the game.
  - A battle won as the time runs out is won: the battle stops reacting to the countdown once the fight is over, and a
    script that hides the countdown straight after `START_BATTLE` ends nothing when it reaches zero.
  - Shown and at zero, it sends the player to the game-over field, as a lost battle does, unless the battle mode lets it
    run out. Hiding it ends a timed stretch early.
  - Restyle it through Core's `rpg-countdown` class, which battles share.
- **Music:**
  - Scripts play, crossfade and stop music, and switch its stem states.
  - They set or fade its volume, Core's `SongVolume`, which is saved so it outlasts the menu and battles, and plays
    under the player's own music volume.
  - They set the battle music, which every battle plays until a script sets another, and which is saved.
  - A battle pauses the field's music, unless it keeps it playing, and the field picks it up where it was on the
    return, fading it in over a second as the battle's fades out, with its echo and reverb as they were.
  - A battle that keeps the music but plays its victory fanfare leaves the fanfare playing; the script that started it
    fades it out and starts what comes next.
  - Looping sounds aren't built, so there are none to start again after a battle.

---

## Sample

**2D Field Sample**: a field module scene for a 2D game, `FieldModule`, and two fields joined by a gateway each way,
drawn with Unity's built-in sprites:

- `SampleField`, walked freely and larger than the screen: ground walled in, with pillars, a 2D Follow camera, a player
  character, a villager who turns to face whoever talks to them, and a save point;
- `SampleTileField`, a tile field walked a tile at a time, seen through a 2D Fixed camera: its ground the Walkable
  Tilemap, a wall across it, and a villager standing in the way, who steps aside when talked to, across and then up,
  and comes back.

The characters share an Animator controller whose blend tree reads `FacingX` and `FacingY` and moves a marker to the
side they face, as a sprite sheet's frames would. Its scene installer binds the `FieldDatabase` the Field Designer's
export wrote, and its input adapter uses the `Player` map of the RPG Framework Starter sample's actions: it runs as the field of the
Starter's game, whose README says how to set it up, exporting the fields among the steps.

---

## Tests

The tests are in `Tests/Editor`, on Unity's Test Framework. They compile only in a project that lists the package as
testable: add it to `testables` in `Packages/manifest.json`,

```json
"testables": [
  "com.rpgframework.field"
]
```

then open **Window › General › Test Runner** and run the **EditMode** tests. They run scripts' bytecode through the VM —
maths, comparisons, waits, priority slots, requests between scripts, init, and what scripts ask the field for — compile
and run every opcode's new block, and test the compiler, the block editor's text and a new block's choices, the opcode
list, who confirm talks to, 2D facing, walking a tile field under input and by script, and export's camera, audio and
tile checks. The field itself,
which needs loading in play mode, is not tested yet.
