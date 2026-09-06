namespace RPGFramework.Field
{
    /// <summary>
    /// Field script opcodes.<br /><br />
    /// Add new opcodes at the end of their category — each category has a 256-value block with room to
    /// grow — and never reuse the value of a removed opcode.<br /><br />
    /// The engine owns <c>0x0000</c>-<c>0x7FFF</c>. A game defines its own opcodes from
    /// <see cref="FieldScriptOpCodeRanges.GAME_BASE" /> upward, so the framework never has to carry a
    /// vocabulary that belongs to one game.
    /// </summary>
    /// <remarks>
    /// <b>Reading the argument comments.</b> A <c>sources</c> byte packs where two operands come from,
    /// one nibble each — high nibble the first operand (the destination, for anything that writes), low
    /// nibble the second:
    /// <code>
    /// 0 = immediate, the value follows inline
    /// 1 = Global bank    2 = Session bank    3 = Temp bank
    /// </code>
    /// So <c>byte immediate | ushort sourceAddress</c> means: an inline literal byte when that operand's
    /// nibble is 0, otherwise a ushort address into the named bank. Bank addresses are always ushort, so
    /// a script can reach any variable the <c>VariableMapAsset</c> declares.
    /// </remarks>
    public enum FieldScriptOpCode : ushort
    {
        // Script flow and control (0x0000)
        Return                                  = 0x0000, //
        RunAnotherEntityScriptUnlessBusy        = 0x0001, // byte targetEntityId, byte targetScriptId
        RunAnotherEntityScriptWaitUntilStarted  = 0x0002, // byte targetEntityId, byte targetScriptId
        RunAnotherEntityScriptWaitUntilFinished = 0x0003, // byte targetEntityId, byte targetScriptId
        ReturnToAnotherScript                   = 0x0004, // byte targetScriptId
        GotoJump                                = 0x0005, // int offset
        GotoDirectly                            = 0x0006, // int index
        CompareTwoByteValues                    = 0x0007, // byte sources, byte immediate | ushort addressA, byte immediate | ushort addressB, byte comparisonType, byte jumpAmount
        CompareTwoIntValues                     = 0x0008, // byte sources, int immediate | ushort addressA, int immediate | ushort addressB, byte comparisonType, byte jumpAmount
        Yield                                   = 0x0009, // blocks until the next frame
        WaitSeconds                             = 0x000A, // float seconds
        IfKeyIsDown                             = 0x000B,
        IfKeyWasJustPressed                     = 0x000C,
        IfKeyWasJustReleased                    = 0x000D,
        DoNothing                               = 0x000E, //
        IfCharacterIsInParty                    = 0x000F,
        IfCharacterIsAvailable                  = 0x0010,
        DebugLog                                = 0x0011, // ulong messageId - authoring aid, writes to the console

        // System and module control (0x0100)
        SpecialOp                = 0x0100,
        RunMinigame              = 0x0101,
        SetBattleModeOptions     = 0x0102, // ushort arena, ushort enemyGroup, ushort flags, byte enemyLevel
        LoadResultOfLastBattle   = 0x0103,
        SetBattleEncounterTable  = 0x0104,
        JumpToAnotherMap         = 0x0105, // string targetFieldId, int spawnIndex
        GetLastFieldMap          = 0x0106,
        SetJumpFieldID           = 0x0107,
        StartBattle              = 0x0108, //
        RandomEncounters         = 0x0109,
        GatewayTriggerActivation = 0x010A, // bool active
        GameOver                 = 0x010B,
        WorldMapJump             = 0x010C, // int spawnIndex
        SetSaveEnabled           = 0x010D, // bool enabled

        // Assignment and mathematics (0x0200)
        AssignValue8Bit         = 0x0200, // byte sources, ushort destinationAddress, byte immediate | ushort sourceAddress
        AssignValue16Bit        = 0x0201, // byte sources, ushort destinationAddress, ushort immediate | ushort sourceAddress
        SetBit                  = 0x0202, // byte sources, ushort destinationAddress, byte immediate | ushort bitIndexAddress (0-7)
        UnsetBit                = 0x0203, // byte sources, ushort destinationAddress, byte immediate | ushort bitIndexAddress (0-7)
        Addition8Bit            = 0x0204, // byte sources, ushort destinationAddress, byte immediate | ushort sourceAddress
        Addition16Bit           = 0x0205, // byte sources, ushort destinationAddress, ushort immediate | ushort sourceAddress
        Addition8BitClamped     = 0x0206, // byte sources, ushort destinationAddress, byte immediate | ushort sourceAddress
        Addition16BitClamped    = 0x0207, // byte sources, ushort destinationAddress, ushort immediate | ushort sourceAddress
        Subtraction8Bit         = 0x0208, // byte sources, ushort destinationAddress, byte immediate | ushort sourceAddress
        Subtraction16Bit        = 0x0209, // byte sources, ushort destinationAddress, ushort immediate | ushort sourceAddress
        Subtraction8BitClamped  = 0x020A, // byte sources, ushort destinationAddress, byte immediate | ushort sourceAddress
        Subtraction16BitClamped = 0x020B, // byte sources, ushort destinationAddress, ushort immediate | ushort sourceAddress
        Multiplication8Bit      = 0x020C, // byte sources, ushort destinationAddress, byte immediate | ushort sourceAddress
        Multiplication16Bit     = 0x020D, // byte sources, ushort destinationAddress, ushort immediate | ushort sourceAddress
        Division8Bit            = 0x020E, // byte sources, ushort destinationAddress, byte immediate | ushort sourceAddress
        Division16Bit           = 0x020F, // byte sources, ushort destinationAddress, ushort immediate | ushort sourceAddress
        Remainder8Bit           = 0x0210, // byte sources, ushort destinationAddress, byte immediate | ushort sourceAddress
        Remainder16Bit          = 0x0211, // byte sources, ushort destinationAddress, ushort immediate | ushort sourceAddress
        BitwiseAnd8Bit          = 0x0212, // byte sources, ushort destinationAddress, byte immediate | ushort sourceAddress
        BitwiseAnd16Bit         = 0x0213, // byte sources, ushort destinationAddress, ushort immediate | ushort sourceAddress
        BitwiseOr8Bit           = 0x0214, // byte sources, ushort destinationAddress, byte immediate | ushort sourceAddress
        BitwiseOr16Bit          = 0x0215, // byte sources, ushort destinationAddress, ushort immediate | ushort sourceAddress
        BitwiseXor8Bit          = 0x0216, // byte sources, ushort destinationAddress, byte immediate | ushort sourceAddress
        BitwiseXor16Bit         = 0x0217, // byte sources, ushort destinationAddress, ushort immediate | ushort sourceAddress
        Increment8Bit           = 0x0218, // byte sources (high nibble only), ushort destinationAddress
        Increment16Bit          = 0x0219, // byte sources (high nibble only), ushort destinationAddress
        Increment8BitClamped    = 0x021A, // byte sources (high nibble only), ushort destinationAddress
        Increment16BitClamped   = 0x021B, // byte sources (high nibble only), ushort destinationAddress
        Decrement8Bit           = 0x021C, // byte sources (high nibble only), ushort destinationAddress
        Decrement16Bit          = 0x021D, // byte sources (high nibble only), ushort destinationAddress
        Decrement8BitClamped    = 0x021E, // byte sources (high nibble only), ushort destinationAddress
        Decrement16BitClamped   = 0x021F, // byte sources (high nibble only), ushort destinationAddress
        GetRandomNumber         = 0x0220, // byte sources, ushort destinationAddress, byte immediate | ushort exclusiveMaximumAddress (0 means full byte range)
        RandomNumberSeed        = 0x0221, // byte sources (low nibble only), int immediate | ushort seedAddress
        GetLowByte              = 0x0222,
        GetHighByte             = 0x0223,
        GetTwoBytes             = 0x0224,
        Sine                    = 0x0225,
        Cosine                  = 0x0226,

        // Windowing and menu (0x0300)
        RunTutorial             = 0x0300,
        CloseWindow             = 0x0301,
        ResizeWindow            = 0x0302,
        CreateSpecialWindow     = 0x0303,
        SetNumberInWindow       = 0x0304,
        SetTimeInWindow         = 0x0305,
        ShowDialogueWindow      = 0x0306, // ulong dialogueId, bool blockMovement
        SetWindowTextValue      = 0x0307,
        SetWindowTextValue16Bit = 0x0308,
        SetMapNameInMenu        = 0x0309,
        AskPlayerToMakeAChoice  = 0x030A, // byte bank, byte addressToStoreChoice, ulong dialogueId, byte answerCount, ulong[] answerIds
        MenuOperations          = 0x030B,
        MainMenuAccessibility   = 0x030C, // bool enabled
        CreateDialogueWindow    = 0x030D, // ulong dialogueId, int x, int y, int width, int height
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
        JoinPartyToLeader                = 0x0500,
        SplitPartyFromLeader             = 0x0501,
        CharacterGraphicsOp              = 0x0502,
        WaitForGraphicsOp                = 0x0503,
        MoveToPartyMember                = 0x0504,
        SlipAgainstWalls                 = 0x0505,
        LockInput                        = 0x0506, // bool locked
        TurnToPartyMember                = 0x0507,
        CollisionDetection               = 0x0508,
        GetPartyMemberDirection          = 0x0509,
        GetPartyMemberPosition           = 0x050A,
        InteractionTriggerActivation     = 0x050B, // bool isActive
        InitAsCharacter                  = 0x050C, //
        PlayAnimationLooping             = 0x050D,
        PlayAnimationOnceAndWait         = 0x050E,
        Visibility                       = 0x050F, // bool isVisible
        SetEntityPosition                = 0x0510, // float x, float y, float z
        MoveEntityToXYWalkAnimation      = 0x0511, // moves an entity using walk animation (if available) to x,y,z at speed set by SetMovementSpeed
        MoveEntityToXYNoAnimation        = 0x0512, // as above but doesn't animate or rotate
        MoveEntityToAnotherEntity        = 0x0513, // navigates to another entity stopping once it reaches its collision
        TurnEntityToAnotherEntity        = 0x0514, // int entityId, byte rotationDirection (0 clockwise, 1 anti-clockwise, 2 closest), float duration (calls SetEntityRotationAsync with smooth)
        WaitForAnimation                 = 0x0515, // waits for the animation to complete that has been previously played using any of the animation opcodes.
        MoveFieldObject                  = 0x0516, // MoveEntityToXYNoAnimation but rotates
        PlayAnimationAsync               = 0x0517,
        PlayAnimationOnceAsync           = 0x0518,
        PlayPartialAnimation             = 0x0519,
        SetMovementSpeed                 = 0x051A, // float movementSpeed
        SetEntityRotation                = 0x051B, // float x, float y, float z
        SetEntityRotationAsync           = 0x051C, // float x, float y, float z, byte direction (0 clockwise, 1 counterclockwise, 2 closest), float duration, byte rotationType (0 linear, 1 smooth)
        SetDirectionToFaceEntity         = 0x051D, // byte targetEntityId
        GetEntityDirection               = 0x051E,
        PlayAnimationStopOnLastFrameWait = 0x051F,
        SetAnimationSpeed                = 0x0520,
        SetEntityAsControllableCharacter = 0x0521,
        MakeEntityJump                   = 0x0522,
        GetEntityPosition                = 0x0523,
        ClimbLadder                      = 0x0524,
        TransposeObjectVisualizationOnly = 0x0525,
        WaitForTranspose                 = 0x0526,
        SetInteractionRange              = 0x0527, // float size
        SetCollisionRadius               = 0x0528,
        Collidability                    = 0x0529,
        LineTriggerInitialization        = 0x052A,
        LineTriggerActivation            = 0x052B,
        SetLine                          = 0x052C,
        FixFacingForward                 = 0x052D,
        SetAnimationID                   = 0x052E,
        StopAnimation                    = 0x052F,
        FlushMovement                    = 0x0530, // drop any queued movement for this entity
        SetRunningEnabled                = 0x0531, // bool enabled
        SetFootstepSound                 = 0x0532, // byte surfaceKey, byte soundSetId - per-surface footsteps for this entity
        LockWalkmeshRegion               = 0x0533, // int regionId - nothing may walk over it
        UnlockWalkmeshRegion             = 0x0534, // int regionId
        InitialiseHeadFacing             = 0x0535, // begin head tracking on this entity, before any SetHeadFacing*
        SetHeadFacingEntity              = 0x0536, // int targetEntityId, int frameCount - turn the head only, body unchanged
        SetHeadFacingPlayer              = 0x0537, // int frameCount - track the player entity with the head only
        SetHeadFacingLimit               = 0x0538, // byte yawLimit, byte pitchLimit, byte rollLimit - stop the head over-rotating
        SetHeadPose                      = 0x0539, // byte pose (0 neutral, 1 up, 2 down)
        StopHeadFacing                   = 0x053A, // return the head to neutral and leave tracking

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
        MusicOperation        = 0x0800,
        PlayMusic             = 0x0801, // int id
        PlaySound             = 0x0802, // int id
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