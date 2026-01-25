namespace RPGFramework.Field
{
    internal enum FieldScriptOpcode : ushort
    {
        // ------------------------------------------------------------
        // Core Control / Flow
        // ------------------------------------------------------------

        Nop               = 0x0000,
        End               = 0x0006, // End script / return
        Call              = 0x0001, // Call script (scriptId)
        Jump              = 0x0002, // Unconditional jump (offset)
        RunScriptOnEntity = 0x0003, // (entityId, script Id)

        // ------------------------------------------------------------
        // Conditionals (STACKLESS)
        // ------------------------------------------------------------

        IfFlagSet   = 0x0100, // (flagId, jumpOffset)
        IfFlagClear = 0x0101, // (flagId, jumpOffset)

        IfVarEqual        = 0x0102, // (varId, value, jumpOffset)
        IfVarNotEqual     = 0x0103,
        IfVarGreater      = 0x0104,
        IfVarGreaterEqual = 0x0105,
        IfVarLess         = 0x0106,
        IfVarLessEqual    = 0x0107,

        // ------------------------------------------------------------
        // Flags / Variables
        // ------------------------------------------------------------

        SetFlag   = 0x01D, // (flagId)
        ClearFlag = 0x01E,

        SetVar  = 0x0110, // (varId, value)
        AddVar  = 0x0111, // (varId, delta)
        CopyVar = 0x0112, // (srcVarId, dstVarId)

        RandomToVar = 0x0113, // (min, max, varId)

        // ------------------------------------------------------------
        // Timing / Flow Control
        // ------------------------------------------------------------

        WaitSeconds  = 0x03C, // (float seconds)
        WaitForInput = 0x051, // (inputMask)
        Yield        = 0x052, // (return execution to the vm)

        SetTimer = 0x09A, // (timerId, seconds)
        GetTimer = 0x0A4, // (timerId, varId)

        // ------------------------------------------------------------
        // Movement / Entity Control
        // ------------------------------------------------------------

        MoveNpc     = 0x03E, // (npcId, dx, dy)
        MoveNpcAbs  = 0x03F, // (npcId, x, y)
        FaceNpc     = 0x052, // (npcId, direction)
        SetNpcSpeed = 0x03D, // (npcId, speed)

        PlayerJump   = 0x023, // (fieldId, x, y)
        LockPlayer   = 0x050,
        UnlockPlayer = 0x051,

        // ------------------------------------------------------------
        // Audio
        // ------------------------------------------------------------

        PlayBgm = 0x021, // (bgmId)
        StopBgm = 0x0A6, // (fadeSeconds)
        PlaySfx = 0x022, // (sfxId)

        // ------------------------------------------------------------
        // Message / UI
        // ------------------------------------------------------------

        ShowText     = 0x047, // (textId)
        ShowTextSync = 0x048, // (textId)
        AskChoice    = 0x04A, // (choiceId, resultVarId)

        WindowSize   = 0x04B, // (w, h)
        WindowClose  = 0x04C,
        SetMessSpeed = 0x05F, // (speed)

        // ------------------------------------------------------------
        // Field / Visuals
        // ------------------------------------------------------------

        OffsetModel = 0x0F0,  // (entityId, x, y)
        Scroll2D    = 0x0064, // (dx, dy, seconds)
        ShakeScreen = 0x0A8,  // (intensity, seconds)
    }
    
    public enum OpCode
    {
        // Script Flow and Control
        Return,
        RunAnotherEntityScriptUnlessBusy,
        RunAnotherEntityScriptWaitUntilStarted,
        RunAnotherEntityScriptWaitUntilFinished,
        RunPartyMemberScriptUnlessBusy,
        RunPartyMemberScriptWaitUntilStarted,
        RunPartyMemberScriptWaitUntilFinished,
        ReturnToAnotherScript,
        GotoForward,
        GotoForwardLong,
        GotoBackward,
        GotoBackwardLong,
        CompareTwoValuesU8,
        CompareTwoValuesU16,
        CompareTwoValues16Bit,
        CompareTwoValues16BitBigJump,
        CompareTwoValuesU32,
        CompareTwoValuesU32BitBigJump,
        WaitForFrames,
        IfKeyIsDown,
        IfKeyWasJustPressed,
        IfKeyWasJustReleased,
        DoNothing,
        IfCharacterIsInParty,
        IfCharacterIsAvailable,

        // System and Module Control
        ChangeDiscs,
        SpecialOp,
        RunMinigame,
        SetBattleModeOptions,
        LoadResultOfLastBattle,
        SetBattleEncounterTable,
        JumpToAnotherMap,
        GetLastFieldMap,
        StartBattle,
        RandomEncounters,
        SetBattleModeOptionsAgain,
        GatewayTriggerActivation,
        GameOver,

        // Assignment and Mathematics
        Addition8BitClamped,
        Addition16BitClamped,
        Subtraction8BitClamped,
        Subtraction16BitClamped,
        Increment8BitClamped,
        Increment16BitClamped,
        Decrement8BitClamped,
        Decrement16BitClamped,
        RandomNumberSeed,
        AssignValue8Bit,
        AssignValue16Bit,
        SetBit,
        UnsetBit,
        Unused,
        Addition8Bit,
        Addition16Bit,
        Subtraction8Bit,
        Subtraction16Bit,
        Multiplication8Bit,
        Multiplication16Bit,
        Division8Bit,
        Division16Bit,
        Remainder8Bit,
        Remainder16Bit,
        BitwiseAnd8Bit,
        BitwiseAnd16Bit,
        BitwiseOr8Bit,
        BitwiseOr16Bit,
        BitwiseXor8Bit,
        BitwiseXor16Bit,
        Increment8Bit,
        Increment16Bit,
        Decrement8Bit,
        Decrement16Bit,
        GetRandomNumber,
        GetLowByte,
        GetHighByte,
        GetTwoBytes,
        Sine,
        Cosine,

        // Windowing and Menu
        RunTutorial,
        CloseWindow,
        ResizeWindow,
        CreateSpecialWindow,
        SetNumberInWindow,
        SetTimeInWindow,
        ShowWindowWithText,
        SetWindowTextValue,
        SetWindowTextValue16Bit,
        SetMapNameInMenu,
        AskPlayerToMakeAChoice,
        MenuOperations,
        MainMenuAccessibility,
        CreateWindow,
        SetWindowPosition,
        SetWindowModes,
        ResetWindow,
        CloseWindowAgain,
        SetNumberOfRowsInWindow,
        GetWindowColor,
        SetWindowColor,

        // Party and Inventory
        ChangePartyMembers,
        StorePartyMembers,
        IncreaseGil,
        DecreaseGil,
        GetGilAmount,
        Unused1,
        Unused2,
        RestoreHPMP,
        RestoreHPMPAgain,
        IncreaseMP,
        DecreaseMP,
        IncreaseHP,
        DecreaseHP,
        AddItemToInventory,
        RemoveItemFromInventory,
        GetItemCountFromInventory,
        AddMateriaToInventory,
        RemoveMateriaFromInventory,
        MateriaOpC,
        GetPartyMembersIdentity,
        AddCharacterToParty,
        RemoveCharacterFromParty,
        SetAllPartyCharacters,
        IfCharacterIsInPartyAgain,
        IfCharacterIsAvailableAgain,
        SetCharacterAvailability,
        LockPartyMember,
        UnlockPartyMember,

        // Field Models and Animation
        JoinPartyToLeader,
        SplitPartyFromLeader,
        EyeBlinkActivation,
        CharacterGraphicsOp,
        WaitForGraphicsOp,
        MoveToPartyMember,
        SlipAgainstWalls,
        PlayerMovability,
        FaceCharacter,
        TurnToPartyMember,
        CollisionDetection,
        GetPartyMemberDirection,
        GetPartyMemberPosition,
        Interactibility,
        InitAsFieldModel,
        InitAsCharacter,
        PlayAnimationLooping,
        PlayAnimationOnceAndWait,
        Visibility,
        SetEntityLocationXYZI,
        SetEntityLocationXYI,
        SetEntityLocationXYZ,
        MoveEntityToXYWalkAnimation,
        MoveEntityToXYNoAnimation,
        MoveEntityToAnotherEntity,
        TurnEntityToAnotherEntity,
        WaitForAnimation,
        MoveFieldObject,
        PlayAnimationAsync,
        PlayAnimationOnceAsync,
        PlayPartialAnimation,
        PlayPartialAnimationAgain,
        SetMovementSpeed,
        SetFacingDirection,
        RotateModel,
        RotateModelDeprecated,
        SetDirectionToFaceEntity,
        GetEntityDirection,
        GetEntityLocationXY,
        GetEntityDirectionI,
        PlayAnimationStopOnLastFrameWait,
        PlayAnimationToDo,
        PlayAnimationToDoAgain,
        SetAnimationSpeed,
        SetEntityAsControllableCharacter,
        MakeEntityJump,
        GetEntityPositionXYZI,
        ClimbLadder,
        TransposeObjectVisualizationOnly,
        WaitForTranspose,
        SetInteractibilityRadius,
        SetCollisionRadius,
        Collidability,
        LineTriggerInitialization,
        LineTriggerActivation,
        SetLine,
        SetInteractibilityRadiusAgain,
        SetCollisionRadiusAgain,
        FixFacingForward,
        SetAnimationID,
        StopAnimation,
        WaitForTurn,

        // Background and Palette
        BackgroundMovie,
        SetBackgroundDepth,
        ScrollBackground,
        MultiplyPaletteColors,
        BackgroundOn,
        BackgroundOff,
        BackgroundRollForward,
        BackgroundRollBackward,
        BackgroundClear,
        StorePalette,
        LoadPalette,
        CopyPalette,
        CopyPalettePartial,
        AddToPaletteColorValues,
        MultiplyPaletteColorValues,
        StorePaletteOffset,
        LoadPaletteOffset,
        CopyPaletteAgain,
        ReturnPalette,
        AddPalette,

        // Camera, Audio and Video
        FadeScreen,
        ShakeScreen,
        ScrollScreen,
        ScrollScreenAgain,
        ScrollScreenToEntity,
        ScrollScreenToPosition,
        ScrollScreenToLeader,
        StartTheScreenToPositionEaseInOut,
        WaitForScrolling,
        StartTheScreenToPositionLinear,
        FadeScreenWait,
        WaitForFade,
        ScrollToPartyMember,
        MusicOperation,
        PlayMusic,
        PlaySound,
        MusicOperationAgain,
        MusicVT,
        MusicVM,
        MusicLockMode,
        SetBattleMusic,
        Unknown,
        SetMovie,
        PlayMovie,
        GetCurrentMovieFrame,
        MovieCameraActivation,
        MusicOpF,
        MusicOpC,
        CheckIfMusicIsPlaying,
        
        // Uncategorized
        Something,
        SomethingAgain,
        SetX,
        GetX,
        SearchForValueInData,
        SetJumpFieldID,
        SetJumpFieldIDAgain,
    }
}