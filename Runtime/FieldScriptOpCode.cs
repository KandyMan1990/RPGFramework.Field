namespace RPGFramework.Field
{
    public enum FieldScriptOpCode : ushort
    {
        // Script flow and control (0x0000)
        [FieldOpCode("RETURN", ArgumentLayout.Sequential, Summary = "End this script and free its priority slot")]
        Return = 0x0000,

        [FieldOpCode("REQUEST_SCRIPT", ArgumentLayout.Sequential, Summary = "Run one of another entity's scripts. Does nothing if that priority slot is busy")]
        [Argument(0, "targetEntityId", ArgumentType.EntityId)]
        [Argument(1, "priority",       ArgumentType.Priority, Description = "0-7, 7 most urgent")]
        [Argument(2, "targetEventId",  ArgumentType.EventId,  Description = "the script's index within that entity")]
        RunAnotherEntityScriptUnlessBusy = 0x0001,

        [FieldOpCode("REQUEST_SCRIPT_WAIT_START", ArgumentLayout.Sequential, Summary = "Run one of another entity's scripts and wait until it starts. Does nothing if that priority slot is busy")]
        [Argument(0, "targetEntityId", ArgumentType.EntityId)]
        [Argument(1, "priority",       ArgumentType.Priority, Description = "0-7, 7 most urgent")]
        [Argument(2, "targetEventId",  ArgumentType.EventId,  Description = "the script's index within that entity")]
        RunAnotherEntityScriptWaitUntilStarted = 0x0002,

        [FieldOpCode("REQUEST_SCRIPT_WAIT_END", ArgumentLayout.Sequential, Summary = "Run one of another entity's scripts and wait for it to finish. Does nothing if that priority slot is busy")]
        [Argument(0, "targetEntityId", ArgumentType.EntityId)]
        [Argument(1, "priority",       ArgumentType.Priority, Description = "0-7, 7 most urgent")]
        [Argument(2, "targetEventId",  ArgumentType.EventId,  Description = "the script's index within that entity")]
        RunAnotherEntityScriptWaitUntilFinished = 0x0003,

        [FieldOpCode("RETURN_TO_SCRIPT", ArgumentLayout.Sequential, Summary = "Hand this priority slot to another of this entity's scripts")]
        [Argument(0, "targetEventId", ArgumentType.EventId)]
        ReturnToAnotherScript = 0x0004,

        [FieldOpCode("GOTO_JUMP", ArgumentLayout.Sequential, Summary = "Jump forwards or backwards, counted from the end of this instruction")]
        [Argument(0, "offset", ArgumentType.JumpDistance, Description = "relative to the byte after this instruction")]
        GotoJump = 0x0005,

        [FieldOpCode("GOTO_DIRECTLY", ArgumentLayout.Sequential, Summary = "Jump to an absolute position in this script")]
        [Argument(0, "target", ArgumentType.JumpTarget, Description = "absolute byte offset")]
        GotoDirectly = 0x0006,

        [FieldOpCode("IF_BYTE", ArgumentLayout.BankCompare, OpensBlock = true, Summary = "Run the instructions inside only when the comparison holds")]
        [Argument(0, "a",          ArgumentType.Value8)]
        [Argument(1, "comparison", ArgumentType.Comparison)]
        [Argument(2, "b",          ArgumentType.Value8)]
        CompareTwoByteValues = 0x0007,

        [FieldOpCode("IF_INT", ArgumentLayout.BankCompare, OpensBlock = true, Summary = "Run the instructions inside only when the comparison holds")]
        [Argument(0, "a",          ArgumentType.ValueInt)]
        [Argument(1, "comparison", ArgumentType.Comparison)]
        [Argument(2, "b",          ArgumentType.ValueInt)]
        CompareTwoIntValues = 0x0008,

        [FieldOpCode("YIELD", ArgumentLayout.Sequential, Summary = "Give up the rest of this frame and resume next frame")]
        Yield = 0x0009,

        [FieldOpCode("WAIT_SECONDS", ArgumentLayout.Sequential, Summary = "Pause this script for a length of time")]
        [Argument(0, "seconds", ArgumentType.Float)]
        WaitSeconds = 0x000A,

        IfKeyIsDown          = 0x000B,
        IfKeyWasJustPressed  = 0x000C,
        IfKeyWasJustReleased = 0x000D,

        [FieldOpCode("NOP", ArgumentLayout.Sequential, Summary = "Does nothing. Useful as a placeholder while authoring")]
        DoNothing = 0x000E,

        IfCharacterIsInParty   = 0x000F,
        IfCharacterIsAvailable = 0x0010,
        DebugLog               = 0x0011, // ulong messageId - authoring aid, writes to the console

        [FieldOpCode("IF_BOOL", ArgumentLayout.BankCompare, OpensBlock = true, Summary = "Run the instructions inside only when the comparison holds")]
        [Argument(0, "a",          ArgumentType.ValueBool)]
        [Argument(1, "comparison", ArgumentType.Comparison, Description = "== or !=")]
        [Argument(2, "b",          ArgumentType.ValueBool)]
        CompareTwoBoolValues = 0x0012,

        // System and module control (0x0100)
        SpecialOp   = 0x0100,
        RunMinigame = 0x0101,

        [FieldOpCode("SET_BATTLE_MODE_OPTIONS", ArgumentLayout.Sequential, Summary = "Choose the arena, enemies and rules for the next battle")]
        [Argument(0, "arena",      ArgumentType.UShort)]
        [Argument(1, "enemyGroup", ArgumentType.UShort)]
        [Argument(2, "flags",      ArgumentType.UShort)]
        [Argument(3, "enemyLevel", ArgumentType.Byte)]
        SetBattleModeOptions = 0x0102,

        LoadResultOfLastBattle  = 0x0103,
        SetBattleEncounterTable = 0x0104,

        [FieldOpCode("JUMP_TO_MAP", ArgumentLayout.Sequential, Summary = "Leave for another field, entering at one of its spawn points")]
        [Argument(0, "field",      ArgumentType.FieldName)]
        [Argument(1, "spawnPoint", ArgumentType.SpawnId, Description = "the id on a SpawnPoint in the field being entered")]
        JumpToAnotherMap = 0x0105,

        GetLastFieldMap = 0x0106,
        SetJumpFieldID  = 0x0107,

        [FieldOpCode("START_BATTLE", ArgumentLayout.Sequential, Summary = "Begin the battle set up by SET_BATTLE_MODE_OPTIONS")]
        StartBattle = 0x0108,

        RandomEncounters = 0x0109,

        [FieldOpCode("GATEWAY_TRIGGER_ACTIVATION", ArgumentLayout.Sequential, Summary = "Turn this field's gateway triggers on or off")]
        [Argument(0, "active", ArgumentType.Bool)]
        GatewayTriggerActivation = 0x010A,

        GameOver       = 0x010B,
        WorldMapJump   = 0x010C, // int spawnIndex
        SetSaveEnabled = 0x010D, // bool enabled

        // Assignment and mathematics (0x0200)
        [FieldOpCode("ASSIGN_BYTE", ArgumentLayout.BankBinary, Summary = "Set a byte variable")]
        [Argument(0, "destination", ArgumentType.Variable8)]
        [Argument(1, "value",       ArgumentType.Value8)]
        AssignValue8Bit = 0x0200,

        [FieldOpCode("ASSIGN_SHORT", ArgumentLayout.BankBinary, Summary = "Set a short variable")]
        [Argument(0, "destination", ArgumentType.Variable16)]
        [Argument(1, "value",       ArgumentType.Value16)]
        AssignValue16Bit = 0x0201,

        [FieldOpCode("SET_BIT", ArgumentLayout.BankBinary, Summary = "Set one bit of a byte variable")]
        [Argument(0, "destination", ArgumentType.Variable8)]
        [Argument(1, "bitIndex",    ArgumentType.Value8, Description = "0-7")]
        SetBit = 0x0202,

        [FieldOpCode("UNSET_BIT", ArgumentLayout.BankBinary, Summary = "Clear one bit of a byte variable")]
        [Argument(0, "destination", ArgumentType.Variable8)]
        [Argument(1, "bitIndex",    ArgumentType.Value8, Description = "0-7")]
        UnsetBit = 0x0203,

        [FieldOpCode("ADD_BYTE", ArgumentLayout.BankBinary, Summary = "Add to a byte variable, wrapping if it overflows")]
        [Argument(0, "destination", ArgumentType.Variable8)]
        [Argument(1, "value",       ArgumentType.Value8)]
        Addition8Bit = 0x0204,

        [FieldOpCode("ADD_SHORT", ArgumentLayout.BankBinary, Summary = "Add to a short variable, wrapping if it overflows")]
        [Argument(0, "destination", ArgumentType.Variable16)]
        [Argument(1, "value",       ArgumentType.Value16)]
        Addition16Bit = 0x0205,

        [FieldOpCode("ADD_BYTE_CLAMPED", ArgumentLayout.BankBinary, Summary = "Add to a byte variable, stopping at its limit rather than wrapping")]
        [Argument(0, "destination", ArgumentType.Variable8)]
        [Argument(1, "value",       ArgumentType.Value8)]
        Addition8BitClamped = 0x0206,

        [FieldOpCode("ADD_SHORT_CLAMPED", ArgumentLayout.BankBinary, Summary = "Add to a short variable, stopping at its limit rather than wrapping")]
        [Argument(0, "destination", ArgumentType.Variable16)]
        [Argument(1, "value",       ArgumentType.Value16)]
        Addition16BitClamped = 0x0207,

        [FieldOpCode("SUB_BYTE", ArgumentLayout.BankBinary, Summary = "Subtract from a byte variable, wrapping if it overflows")]
        [Argument(0, "destination", ArgumentType.Variable8)]
        [Argument(1, "value",       ArgumentType.Value8)]
        Subtraction8Bit = 0x0208,

        [FieldOpCode("SUB_SHORT", ArgumentLayout.BankBinary, Summary = "Subtract from a short variable, wrapping if it overflows")]
        [Argument(0, "destination", ArgumentType.Variable16)]
        [Argument(1, "value",       ArgumentType.Value16)]
        Subtraction16Bit = 0x0209,

        [FieldOpCode("SUB_BYTE_CLAMPED", ArgumentLayout.BankBinary, Summary = "Subtract from a byte variable, stopping at its limit rather than wrapping")]
        [Argument(0, "destination", ArgumentType.Variable8)]
        [Argument(1, "value",       ArgumentType.Value8)]
        Subtraction8BitClamped = 0x020A,

        [FieldOpCode("SUB_SHORT_CLAMPED", ArgumentLayout.BankBinary, Summary = "Subtract from a short variable, stopping at its limit rather than wrapping")]
        [Argument(0, "destination", ArgumentType.Variable16)]
        [Argument(1, "value",       ArgumentType.Value16)]
        Subtraction16BitClamped = 0x020B,

        [FieldOpCode("MUL_BYTE", ArgumentLayout.BankBinary, Summary = "Multiply a byte variable, wrapping if it overflows")]
        [Argument(0, "destination", ArgumentType.Variable8)]
        [Argument(1, "value",       ArgumentType.Value8)]
        Multiplication8Bit = 0x020C,

        [FieldOpCode("MUL_SHORT", ArgumentLayout.BankBinary, Summary = "Multiply a short variable, wrapping if it overflows")]
        [Argument(0, "destination", ArgumentType.Variable16)]
        [Argument(1, "value",       ArgumentType.Value16)]
        Multiplication16Bit = 0x020D,

        [FieldOpCode("DIV_BYTE", ArgumentLayout.BankBinary, Summary = "Divide a byte variable, rounding down. Dividing by zero leaves it unchanged")]
        [Argument(0, "destination", ArgumentType.Variable8)]
        [Argument(1, "value",       ArgumentType.Value8)]
        Division8Bit = 0x020E,

        [FieldOpCode("DIV_SHORT", ArgumentLayout.BankBinary, Summary = "Divide a short variable, rounding down. Dividing by zero leaves it unchanged")]
        [Argument(0, "destination", ArgumentType.Variable16)]
        [Argument(1, "value",       ArgumentType.Value16)]
        Division16Bit = 0x020F,

        [FieldOpCode("MOD_BYTE", ArgumentLayout.BankBinary, Summary = "Replace a byte variable with its remainder after dividing. Dividing by zero leaves it unchanged")]
        [Argument(0, "destination", ArgumentType.Variable8)]
        [Argument(1, "value",       ArgumentType.Value8)]
        Remainder8Bit = 0x0210,

        [FieldOpCode("MOD_SHORT", ArgumentLayout.BankBinary, Summary = "Replace a short variable with its remainder after dividing. Dividing by zero leaves it unchanged")]
        [Argument(0, "destination", ArgumentType.Variable16)]
        [Argument(1, "value",       ArgumentType.Value16)]
        Remainder16Bit = 0x0211,

        [FieldOpCode("AND_BYTE", ArgumentLayout.BankBinary, Summary = "Bitwise AND into a byte variable")]
        [Argument(0, "destination", ArgumentType.Variable8)]
        [Argument(1, "value",       ArgumentType.Value8)]
        BitwiseAnd8Bit = 0x0212,

        [FieldOpCode("AND_SHORT", ArgumentLayout.BankBinary, Summary = "Bitwise AND into a short variable")]
        [Argument(0, "destination", ArgumentType.Variable16)]
        [Argument(1, "value",       ArgumentType.Value16)]
        BitwiseAnd16Bit = 0x0213,

        [FieldOpCode("OR_BYTE", ArgumentLayout.BankBinary, Summary = "Bitwise OR into a byte variable")]
        [Argument(0, "destination", ArgumentType.Variable8)]
        [Argument(1, "value",       ArgumentType.Value8)]
        BitwiseOr8Bit = 0x0214,

        [FieldOpCode("OR_SHORT", ArgumentLayout.BankBinary, Summary = "Bitwise OR into a short variable")]
        [Argument(0, "destination", ArgumentType.Variable16)]
        [Argument(1, "value",       ArgumentType.Value16)]
        BitwiseOr16Bit = 0x0215,

        [FieldOpCode("XOR_BYTE", ArgumentLayout.BankBinary, Summary = "Bitwise XOR into a byte variable")]
        [Argument(0, "destination", ArgumentType.Variable8)]
        [Argument(1, "value",       ArgumentType.Value8)]
        BitwiseXor8Bit = 0x0216,

        [FieldOpCode("XOR_SHORT", ArgumentLayout.BankBinary, Summary = "Bitwise XOR into a short variable")]
        [Argument(0, "destination", ArgumentType.Variable16)]
        [Argument(1, "value",       ArgumentType.Value16)]
        BitwiseXor16Bit = 0x0217,

        [FieldOpCode("INC_BYTE", ArgumentLayout.BankUnary, Summary = "Add one to a byte variable, wrapping")]
        [Argument(0, "destination", ArgumentType.Variable8)]
        Increment8Bit = 0x0218,

        [FieldOpCode("INC_SHORT", ArgumentLayout.BankUnary, Summary = "Add one to a short variable, wrapping")]
        [Argument(0, "destination", ArgumentType.Variable16)]
        Increment16Bit = 0x0219,

        [FieldOpCode("INC_BYTE_CLAMPED", ArgumentLayout.BankUnary, Summary = "Add one to a byte variable, stopping at its limit")]
        [Argument(0, "destination", ArgumentType.Variable8)]
        Increment8BitClamped = 0x021A,

        [FieldOpCode("INC_SHORT_CLAMPED", ArgumentLayout.BankUnary, Summary = "Add one to a short variable, stopping at its limit")]
        [Argument(0, "destination", ArgumentType.Variable16)]
        Increment16BitClamped = 0x021B,

        [FieldOpCode("DEC_BYTE", ArgumentLayout.BankUnary, Summary = "Subtract one from a byte variable, wrapping")]
        [Argument(0, "destination", ArgumentType.Variable8)]
        Decrement8Bit = 0x021C,

        [FieldOpCode("DEC_SHORT", ArgumentLayout.BankUnary, Summary = "Subtract one from a short variable, wrapping")]
        [Argument(0, "destination", ArgumentType.Variable16)]
        Decrement16Bit = 0x021D,

        [FieldOpCode("DEC_BYTE_CLAMPED", ArgumentLayout.BankUnary, Summary = "Subtract one from a byte variable, stopping at its limit")]
        [Argument(0, "destination", ArgumentType.Variable8)]
        Decrement8BitClamped = 0x021E,

        [FieldOpCode("DEC_SHORT_CLAMPED", ArgumentLayout.BankUnary, Summary = "Subtract one from a short variable, stopping at its limit")]
        [Argument(0, "destination", ArgumentType.Variable16)]
        Decrement16BitClamped = 0x021F,

        [FieldOpCode("GET_RANDOM", ArgumentLayout.BankUnary, Summary = "Store a random byte, 0 to 255")]
        [Argument(0, "destination", ArgumentType.Variable8)]
        GetRandomNumber = 0x0220,

        [FieldOpCode("RANDOM_SEED", ArgumentLayout.BankSeed, Summary = "Reseed the random sequence, so a script repeats exactly")]
        [Argument(0, "seed", ArgumentType.ValueInt)]
        RandomNumberSeed = 0x0221,

        GetLowByte  = 0x0222,
        GetHighByte = 0x0223,
        GetTwoBytes = 0x0224,
        Sine        = 0x0225,
        Cosine      = 0x0226,

        [FieldOpCode("SET_BOOL", ArgumentLayout.BankBinary, Summary = "Set a bool variable")]
        [Argument(0, "destination", ArgumentType.VariableBool)]
        [Argument(1, "value",       ArgumentType.ValueBool)]
        AssignValueBool = 0x0227,

        // Windowing and menu (0x0300)
        RunTutorial         = 0x0300,
        CloseWindow         = 0x0301,
        ResizeWindow        = 0x0302,
        CreateSpecialWindow = 0x0303,
        SetNumberInWindow   = 0x0304,
        SetTimeInWindow     = 0x0305,

        [FieldOpCode("SHOW_DIALOGUE_WINDOW", ArgumentLayout.Sequential, Summary = "Show a line of dialogue and wait for the player to dismiss it")]
        [Argument(0, "dialogue",      ArgumentType.LocalisationKey)]
        [Argument(1, "blockMovement", ArgumentType.Bool)]
        ShowDialogueWindow = 0x0306,

        SetWindowTextValue      = 0x0307,
        SetWindowTextValue16Bit = 0x0308,
        SetMapNameInMenu        = 0x0309,

        [FieldOpCode("ASK_PLAYER_TO_MAKE_A_CHOICE", ArgumentLayout.Sequential, Summary = "Ask a question and store which answer the player chose")]
        [Argument(0, "destination", ArgumentType.Variable8)]
        [Argument(1, "question",    ArgumentType.LocalisationKey)]
        [Argument(2, "answers",     ArgumentType.LocalisationKeyList)]
        AskPlayerToMakeAChoice = 0x030A,

        MenuOperations = 0x030B,

        [FieldOpCode("MAIN_MENU_ACCESSIBILITY", ArgumentLayout.Sequential, Summary = "Allow or block the player opening the main menu")]
        [Argument(0, "enabled", ArgumentType.Bool)]
        MainMenuAccessibility = 0x030C,

        [FieldOpCode("CREATE_DIALOGUE_WINDOW", ArgumentLayout.Sequential, Summary = "Create a dialogue window at a position and size, without showing it")]
        [Argument(0, "dialogue", ArgumentType.LocalisationKey)]
        [Argument(1, "x",        ArgumentType.Int)]
        [Argument(2, "y",        ArgumentType.Int)]
        [Argument(3, "width",    ArgumentType.Int)]
        [Argument(4, "height",   ArgumentType.Int)]
        CreateDialogueWindow = 0x030D,

        SetWindowPosition       = 0x030E,
        SetWindowModes          = 0x030F,
        ResetWindow             = 0x0310,
        SetNumberOfRowsInWindow = 0x0311,
        SetMessageSpeed         = 0x0312, // float charactersPerSecond

        // Party and inventory (0x0400)
        ChangePartyMembers        = 0x0400,
        StorePartyMembers         = 0x0401,
        IncreaseGil               = 0x0402,
        DecreaseGil               = 0x0403,
        GetGilAmount              = 0x0404,
        RestoreHPMP               = 0x0405,
        IncreaseMP                = 0x0406,
        DecreaseMP                = 0x0407,
        IncreaseHP                = 0x0408,
        DecreaseHP                = 0x0409,
        AddItemToInventory        = 0x040A,
        RemoveItemFromInventory   = 0x040B,
        GetItemCountFromInventory = 0x040C,
        GetPartyMembersIdentity   = 0x040D,
        AddCharacterToParty       = 0x040E,
        RemoveCharacterFromParty  = 0x040F,
        SetAllPartyCharacters     = 0x0410,
        SetCharacterAvailability  = 0x0411,
        LockPartyMember           = 0x0412,
        UnlockPartyMember         = 0x0413,

        // Field models and animation (0x0500)
        JoinPartyToLeader    = 0x0500,
        SplitPartyFromLeader = 0x0501,
        CharacterGraphicsOp  = 0x0502,
        WaitForGraphicsOp    = 0x0503,
        MoveToPartyMember    = 0x0504,
        SlipAgainstWalls     = 0x0505,

        [FieldOpCode("LOCK_INPUT", ArgumentLayout.Sequential, Summary = "Take control away from the player, or give it back")]
        [Argument(0, "locked", ArgumentType.Bool)]
        LockInput = 0x0506,

        TurnToPartyMember       = 0x0507,
        CollisionDetection      = 0x0508,
        GetPartyMemberDirection = 0x0509,
        GetPartyMemberPosition  = 0x050A,

        [FieldOpCode("INTERACTION_TRIGGER_ACTIVATION", ArgumentLayout.Sequential, Summary = "Turn this entity's interaction trigger on or off")]
        [Argument(0, "enabled", ArgumentType.Bool)]
        InteractionTriggerActivation = 0x050B,

        [FieldOpCode("INIT_CHARACTER", ArgumentLayout.Sequential, Summary = "Mark this entity as the player character")]
        InitAsCharacter = 0x050C,

        PlayAnimationLooping     = 0x050D,
        PlayAnimationOnceAndWait = 0x050E,

        [FieldOpCode("VISIBILITY", ArgumentLayout.Sequential, Summary = "Show or hide this entity")]
        [Argument(0, "isVisible", ArgumentType.Bool)]
        Visibility = 0x050F,

        [FieldOpCode("SET_ENTITY_POSITION", ArgumentLayout.Sequential, Summary = "Move this entity immediately, with no animation")]
        [Argument(0, "x", ArgumentType.Float)]
        [Argument(1, "y", ArgumentType.Float)]
        [Argument(2, "z", ArgumentType.Float)]
        SetEntityPosition = 0x0510,

        MoveEntityToXYWalkAnimation = 0x0511, // moves an entity using walk animation (if available) to x,y,z at speed set by SetMovementSpeed
        MoveEntityToXYNoAnimation   = 0x0512, // as above but doesn't animate or rotate
        MoveEntityToAnotherEntity   = 0x0513, // navigates to another entity stopping once it reaches its collision
        TurnEntityToAnotherEntity   = 0x0514, // int entityId, byte rotationDirection (0 clockwise, 1 anti-clockwise, 2 closest), float duration (calls SetEntityRotationAsync with smooth)
        WaitForAnimation            = 0x0515, // waits for the animation to complete that has been previously played using any of the animation opcodes.
        MoveFieldObject             = 0x0516, // MoveEntityToXYNoAnimation but rotates
        PlayAnimationAsync          = 0x0517,
        PlayAnimationOnceAsync      = 0x0518,
        PlayPartialAnimation        = 0x0519,

        [FieldOpCode("SET_MOVEMENT_SPEED", ArgumentLayout.Sequential, Summary = "Set how fast this entity moves")]
        [Argument(0, "movementSpeed", ArgumentType.Float)]
        SetMovementSpeed = 0x051A,

        [FieldOpCode("SET_ENTITY_ROTATION", ArgumentLayout.Sequential, Summary = "Face this entity in a direction immediately")]
        [Argument(0, "x", ArgumentType.Float)]
        [Argument(1, "y", ArgumentType.Float)]
        [Argument(2, "z", ArgumentType.Float)]
        SetEntityRotation = 0x051B,

        [FieldOpCode("SET_ENTITY_ROTATION_ASYNC", ArgumentLayout.Sequential, Summary = "Turn this entity to face a direction over time")]
        [Argument(0, "x",            ArgumentType.Float)]
        [Argument(1, "y",            ArgumentType.Float)]
        [Argument(2, "z",            ArgumentType.Float)]
        [Argument(3, "direction",    ArgumentType.Byte, Description = "0 clockwise, 1 counterclockwise, 2 closest")]
        [Argument(4, "duration",     ArgumentType.Float)]
        [Argument(5, "rotationType", ArgumentType.Byte, Description = "0 linear, 1 smooth")]
        SetEntityRotationAsync = 0x051C,

        [FieldOpCode("SET_DIRECTION_TO_FACE_ENTITY", ArgumentLayout.Sequential, Summary = "Face this entity towards another")]
        [Argument(0, "targetEntityId", ArgumentType.EntityId)]
        SetDirectionToFaceEntity = 0x051D,

        GetEntityDirection               = 0x051E,
        PlayAnimationStopOnLastFrameWait = 0x051F,
        SetAnimationSpeed                = 0x0520,
        SetEntityAsControllableCharacter = 0x0521,
        MakeEntityJump                   = 0x0522,
        GetEntityPosition                = 0x0523,
        ClimbLadder                      = 0x0524,
        TransposeObjectVisualizationOnly = 0x0525,
        WaitForTranspose                 = 0x0526,

        [FieldOpCode("SET_INTERACTION_RANGE", ArgumentLayout.Sequential, Summary = "Set how close the player must be to interact with this entity")]
        [Argument(0, "radius", ArgumentType.Float)]
        SetInteractionRange = 0x0527,

        SetCollisionRadius        = 0x0528,
        Collidability             = 0x0529,
        LineTriggerInitialization = 0x052A,
        LineTriggerActivation     = 0x052B,
        SetLine                   = 0x052C,
        FixFacingForward          = 0x052D,
        SetAnimationID            = 0x052E,
        StopAnimation             = 0x052F,
        FlushMovement             = 0x0530, // drop any queued movement for this entity
        SetRunningEnabled         = 0x0531, // bool enabled
        SetFootstepSound          = 0x0532, // byte surfaceKey, byte soundSetId - per-surface footsteps for this entity
        LockWalkmeshRegion        = 0x0533, // int regionId - nothing may walk over it
        UnlockWalkmeshRegion      = 0x0534, // int regionId
        InitialiseHeadFacing      = 0x0535, // begin head tracking on this entity, before any SetHeadFacing*
        SetHeadFacingEntity       = 0x0536, // int targetEntityId, int frameCount - turn the head only, body unchanged
        SetHeadFacingPlayer       = 0x0537, // int frameCount - track the player entity with the head only
        SetHeadFacingLimit        = 0x0538, // byte yawLimit, byte pitchLimit, byte rollLimit - stop the head over-rotating
        SetHeadPose               = 0x0539, // byte pose (0 neutral, 1 up, 2 down)
        StopHeadFacing            = 0x053A, // return the head to neutral and leave tracking

        // Background and screen tint (0x0600)
        SetBackgroundDepth     = 0x0600,
        ScrollBackground       = 0x0601,
        BackgroundOn           = 0x0602,
        BackgroundOff          = 0x0603,
        BackgroundRollForward  = 0x0604,
        BackgroundRollBackward = 0x0605,
        BackgroundClear        = 0x0606,
        SetShadeLevel          = 0x0607, // float level
        SubtractiveScreenFade  = 0x0608, // float r, float g, float b, float duration

        // Camera and screen movement (0x0700)
        FadeScreen                        = 0x0700,
        FadeScreenWait                    = 0x0701,
        WaitForFade                       = 0x0702,
        ShakeScreen                       = 0x0703,
        ScrollScreen                      = 0x0704,
        ScrollScreenToEntity              = 0x0705,
        ScrollScreenToPosition            = 0x0706,
        ScrollScreenToLeader              = 0x0707,
        ScrollToPartyMember               = 0x0708,
        StartTheScreenToPositionEaseInOut = 0x0709,
        StartTheScreenToPositionLinear    = 0x070A,
        WaitForScrolling                  = 0x070B,

        // Audio (0x0800)
        MusicOperation = 0x0800,

        [FieldOpCode("PLAY_MUSIC", ArgumentLayout.Sequential, Summary = "Start a music track, layered as one of its stem states")]
        [Argument(0, "track", ArgumentType.MusicName,      Description = "the music asset's name, as it appears in the music provider")]
        [Argument(1, "state", ArgumentType.MusicStateName, Description = "which stem state it starts on")]
        PlayMusic = 0x0801,

        [FieldOpCode("PLAY_SOUND", ArgumentLayout.Sequential, Summary = "Play a sound effect once")]
        [Argument(0, "sound", ArgumentType.SoundName, Description = "the sound asset's name, as it appears in the SFX provider")]
        PlaySound = 0x0802,

        MusicLockMode         = 0x0803,
        SetBattleMusic        = 0x0804,
        CheckIfMusicIsPlaying = 0x0805,
        PlayAmbientLoop       = 0x0806, // int id - a looping ambience, distinct from a one-shot
        StopSound             = 0x0807, // int channel
        PreserveSoundChannel  = 0x0808, // int channel - survives the next field load
        SetSoundVolume        = 0x0809, // int channel, float volume
        FadeSoundVolume       = 0x080A, // int channel, float volume, float duration
        SetSoundPan           = 0x080B, // int channel, float pan
        FadeSoundPan          = 0x080C, // int channel, float pan, float duration
        SetAllSoundVolume     = 0x080D, // float volume
        FadeAllSoundVolume    = 0x080E, // float volume, float duration
        SetAllSoundPan        = 0x080F, // float pan
        FadeAllSoundPan       = 0x0810, // float pan, float duration

        [FieldOpCode("MUSIC_STEM_STATE", ArgumentLayout.Sequential, Summary = "Switch the music to one of the stem states its asset declares")]
        [Argument(0, "track",       ArgumentType.MusicNameHint,  Description = "which track's states to choose from — not compiled, it applies to whatever is playing")]
        [Argument(1, "state",       ArgumentType.MusicStateName, Description = "the state's name on the music asset")]
        [Argument(2, "fadeSeconds", ArgumentType.Float,          Description = "0 changes immediately")]
        SetMusicStemState = 0x0811,

        // Video (0x0900)
        PrepareMovie = 0x0900, // int id
        PlayMovie    = 0x0901, // int id
        WaitForMovie = 0x0902,

        // Timer (0x0A00)
        SetCountdownTimer  = 0x0A00, // int seconds
        ShowCountdownTimer = 0x0A01, // bool visible, int x, int y

        // Input and haptics (0x0B00)
        SetVibration  = 0x0B00, // float lowFrequency, float highFrequency, float duration
        SetKeyEnabled = 0x0B01, // byte controlSlot, bool enabled
    }

    /// <summary>
    /// Where the engine's opcode range ends and a game's begins.
    /// </summary>
    public static class FieldScriptOpCodeRanges
    {
        /// <summary>
        /// Highest value the framework will ever assign to one of its own opcodes.
        /// </summary>
        public const ushort ENGINE_MAX = 0x7FFF;

        /// <summary>
        /// First value available to a game. Dispatch tests this bit to choose between the framework's
        /// opcode table and the game's, so the two can never collide or renumber each other.
        /// </summary>
        public const ushort GAME_BASE = 0x8000;

        public static bool IsGameOpCode(ushort opCode)
        {
            bool isGameOpCode = opCode >= GAME_BASE;

            return isGameOpCode;
        }
    }
}