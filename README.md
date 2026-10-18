# RPGFramework.Field

The field: the explorable places of an RPG Framework game. A field is a prefab, loaded from an asset bundle. Its
characters and objects are **entities**, and each runs **scripts**, authored in the Field Designer's block editor and
compiled to bytecode, for movement, dialogue, music, the menus, saving and starting battles. Fields can be 2D or 3D.

Requires Unity 6000.6 or newer, RPGFramework.Core and its shared types, the Field, Menu and Battle shared types,
RPGFramework.DI, RPGFramework.Audio, RPGFramework.Localisation, RPGFramework.Hashing and Unity.Mathematics.

---

## Setting up

1. **A field scene** holding a `FieldModuleMonoBehaviour` with:
   - the camera;
   - the `UIDocument` dialogue windows are drawn in;
   - the scene's `InputAdapter`;
   - the game's up direction, `0,1,0` for a 3D field and `0,0,1` for a 2D one;
   - how wide the player's interaction angle is.
2. **A scene installer** binding `IFieldPresentation` to `PrefabFieldPresentation`, `IFieldDatabase` to the generated
   `FieldDatabase` (see Export, below) and `IFieldModule` to `FieldModule`.
3. **In your global installer**, bind `IFieldArgsStore` to `FieldArgsStore`, `IGameOverFieldStore` to
   `GameOverFieldStore` and `IFieldResumeDataStore` to `FieldResumeDataStore`.
4. **Add the scene to your game's module and scene databases** under `FieldConstants.MODULE_ID`.
5. **Open your variable map.** Field's variables are added to it: the current field and spawn point, the player's
   saved position and facing, and the game-over field and its spawn point. Pick the field and spawn point a new game
   starts at as their defaults, the field a game over sends the player to, and the battle music a new game starts with,
   Core's `BattleMusic`, by name.

---

## Fields

A field is a prefab whose root has **`FieldEntities`**: whether the field is 2D or 3D, and every entity in it. Under the
root sit the entities' bodies, spawn points, blockers and scenery.

### Entities

An entity is an id, a name, its scripts and, if it is something in the world, a **body**. An entity with no body is
only its scripts, for one that plays music or sets a flag.

Bodies are built from **presets** named for what they are for:

| Preset | Built | Scripts it starts with |
| --- | --- | --- |
| Gateway | an unseen trigger area | a gateway script to write the jump in |
| Character | a kinematic rigidbody, a capsule and an interaction trigger | shows itself |
| Player character | the same, without the interaction trigger | makes itself the player |
| Interactable object | a solid box and an interaction trigger | — |
| Examine point | an interaction trigger only, so it doesn't block | — |
| Area | a trigger area | — |
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

Entities, blockers and spawn points can also be made from **GameObject > RPG Framework > Field** while editing a field's
prefab.

### Export

**Export** checks every field and writes nothing unless all of them pass. Among other things, it refuses:

- a script that doesn't compile;
- a request for an entity or script the field lacks;
- a body of the wrong dimension, or a dynamic rigidbody;
- an animation state the entity's Animator lacks;
- a dialogue key none of the field's sheets has;
- a jump to a spawn point its destination lacks.

It then writes **`FieldDatabase.cs`**, which maps each field to its bundle, sheets and location name; it is generated, so
don't edit it. It builds each field's asset bundle and records the variable map's layout. **A player build is refused if
the variable map has changed since the last export**, because compiled scripts address variables where they were.

**RPG Framework > Field > Draw Interaction and Trigger Debug** draws interaction ranges, triggers, gateways and blockers
in the Game view.

---

## Scripts

### When they run

| Type | Runs when |
| --- | --- |
| `Init` | the field loads, before it is shown; every entity's first script |
| `Default` | once every entity's init has run, and may loop and wait |
| `OnEnter` / `OnLeave` | the player enters or leaves the entity's trigger |
| `Gateway` | the player enters the entity's trigger, to leave the field; gateways can be switched off as a group |
| `OnInteraction` | the player faces the entity and presses confirm |
| `OnPush` | the player walks into the entity |
| `Requested` | another script asks for it, by entity and script |

- **One script runs per entity at a time.** An entity has eight priority slots, and a more urgent script interrupts a
  less urgent one, which carries on where it left off once the urgent one returns. A requested script chooses its slot.
- **Each entity runs up to 16 instructions a frame**, so a loop can't stall the game. A long script simply carries on
  next frame.

### Writing them

The block editor stacks blocks, and every argument that names something is picked from a list. Picked arguments include
entities and their scripts, fields and spawn points, music, sounds and stem states, animation states, dialogue keys and
variables.

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
- **`IF_` blocks** compare two values, with an optional `ELSE`, and close with `END_IF`. **`LABEL`s** mark where jumps go.
- **Names are hashed when compiled**, so reordering a list never repoints a script. A name with a space in it is
  written in double quotes, `PLAY_MUSIC "Town Theme" AllStems`, which the block editor does itself.
- **Opcodes cover:**
  - flow, waits and requesting other scripts;
  - variables, maths and random numbers;
  - dialogue windows, questions and the menus;
  - entities: showing, moving, turning, solidity and interaction;
  - animation;
  - blockers and gateways;
  - music, sounds and reverb;
  - waiting for a button;
  - saving, battles and game over.
- **The palette only offers opcodes the field can run.** Those that act through an optional package, such as PS1 reverb,
  appear only once it is installed.

---

## At run time

- **Movement:**
  - A body moves with a driver chosen from what it has: a 3D rigidbody, a 2D rigidbody, a walkable tilemap, or a plain
    transform.
  - Bodies are kinematic. The rigidbody drivers sweep and slide along what they hit, rather than pushing or being pushed.
  - Scripts walk entities to a point, glide or slide them there, or have one follow another.
- **Blockers** are objects in the field with an id that a script switches on and off. Switching one switches its
  colliders, its tilemap of closed cells and its visuals together, to close off part of a field.
- **Interaction:**
  - Confirm picks the closest entity within its own interaction range that the player faces and that faces the player.
  - Which entity is the player is a script's choice, and may be none, for a field that is all cutscene.
- **Animation:**
  - An entity's base animation is one Animator state.
  - The field writes a `Speed` parameter, and `FacingX` and `FacingY` for 2D, so your controller decides how idle,
    walking and running blend.
  - Scripts play other states over the base one, once or looping, and can wait for them.
- **Dialogue:**
  - Windows are addressed by channel, 0 to 7, each keeping its position and style.
  - A window can hold the player while it waits for confirm, or leave the player free to walk away. A background one
    stays up until a script closes it.
  - Text types at the player's field message speed.
- **The screen:**
  - Scripts fade the screen out and back in beneath the message windows, so a message can show over a black screen,
    through any transition or the field's own exit and entry transitions, or at once.
  - They take a colour from the screen or add one, at once or over seconds, as a tint or a flash.
  - A fade and a colour are one state, each replacing the other, and where a script leaves it is saved: a field left
    faded or tinted is still so after the menu, a battle or a load.
  - The colours, and the default subtractive fade, need URP (Core's README).
- **Leaving a field:** a gateway or a script jumps to another field and spawn point, the screen covered by the field's
  exit transition and revealed by the next field's entry transition, unless a script has turned the exit's cover off
  because it faded the screen itself. The menu button opens the party menu when the field allows it, and a script can
  start a battle or end the game.
  - A battle carries its own arena, enemies, flags and level. Whether losing it, or a countdown running out, ends the
    game is the battle mode's, which a script sets and the game saves; after the battle, a script reads how it ended.
  - A game over sends the player to the game-over field your map names, entered afresh: a field like any other, whose
    scripts decide what it shows and when the game goes back to the title.
  - Returning from the menu or a battle puts the field back as it was: positions, animations, visibility, blockers and
    more.
  - Arriving in a new field starts it afresh.
- **Saving:**
  - Saving is allowed where your game's policy says: anywhere, or only where a script allows it, such as at a save point.
  - The player's position and facing are saved with the game, and a loaded game resumes in the field it was saved in.
- **Music:**
  - Scripts play, crossfade and stop music, and switch its stem states.
  - They set or fade its volume, Core's `MusicVolume`, which is saved so it outlasts the menu and battles.
  - They set the battle music, which every battle plays until a script sets another, and which is saved.
  - A battle pauses the field's music, unless it keeps it playing, and the field picks it up where it was on the
    return, fading it in over a second as the battle's fades out, with its echo and reverb as they were.
  - A battle that keeps the music but plays its victory fanfare leaves the fanfare playing; the script that started it
    fades it out and starts what comes next.

---

## Not in this version

- **Random encounters** aren't built; a battle starts only when a script starts it.
- **Some opcodes are declared but not yet run**, and the editor doesn't offer them.
- **A game can't add opcodes of its own** yet.
- **The list of fields is generated C#**, so a field can't be added without a new build.
- **A field is loaded whole**; large areas aren't streamed.
- **No NavMesh or walkmesh**: movement is physics, a tilemap or plain transforms.
- **Animation events, timeline cutscenes and animated turning** aren't supported.
- **Looping sounds** aren't built, so none are started again after a battle.
