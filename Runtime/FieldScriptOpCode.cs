namespace RPGFramework.Field
{
    internal enum FieldScriptOpCode : ushort
    {
        // Script Flow and Control
        Return,                                  //
        RunAnotherEntityScriptUnlessBusy,        // byte targetEntityId, byte targetScriptId
        RunAnotherEntityScriptWaitUntilStarted,  // byte targetEntityId, byte targetScriptId
        RunAnotherEntityScriptWaitUntilFinished, // byte targetEntityId, byte targetScriptId
        ReturnToAnotherScript,                   // byte targetScriptId
        GotoJump,                                // int offset
        GotoDirectly,                            // int index
        CompareTwoByteValues,
        CompareTwoIntValues,
        Yield,       //
        WaitSeconds, // float seconds
        IfKeyIsDown,
        IfKeyWasJustPressed,
        IfKeyWasJustReleased,
        DoNothing, //
        IfCharacterIsInParty,
        IfCharacterIsAvailable,

        // System and Module Control
        SpecialOp,
        RunMinigame,
        SetBattleModeOptions,
        LoadResultOfLastBattle,
        SetBattleEncounterTable,
        JumpToAnotherMap, // string targetFieldId, int spawnIndex
        GetLastFieldMap,
        StartBattle,
        RandomEncounters,
        SetBattleModeOptionsAgain,
        GatewayTriggerActivation, // bool active
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
        ShowWindowWithText, // ulong dialogueId, bool blockMovement
        SetWindowTextValue,
        SetWindowTextValue16Bit,
        SetMapNameInMenu,
        AskPlayerToMakeAChoice,
        MenuOperations,
        MainMenuAccessibility, // bool enabled
        CreateWindow, // ulong dialogueId, int x, int y, int width, int height
        SetWindowPosition,
        SetWindowModes,
        ResetWindow,
        SetNumberOfRowsInWindow,

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
        CharacterGraphicsOp,
        WaitForGraphicsOp,
        MoveToPartyMember,
        SlipAgainstWalls,
        LockInput, // bool locked
        TurnToPartyMember,
        CollisionDetection,
        GetPartyMemberDirection,
        GetPartyMemberPosition,
        InteractionTriggerActivation, // bool isActive
        InitAsCharacter,              //
        PlayAnimationLooping,
        PlayAnimationOnceAndWait,
        Visibility,        // bool isVisible
        SetEntityPosition, // float x, float y, float z
        MoveEntityToXYWalkAnimation, // moves an entity using walk animation (if available) to x,y,z at speed set by SetMovementSpeed
        MoveEntityToXYNoAnimation, // as above but doesn't animate or rotate
        MoveEntityToAnotherEntity, // navigates to another entity stopping once it reaches its collision
        TurnEntityToAnotherEntity, // int entityId, byte rotationDirection (0 clockwise, 1 anti-clockwise, 2 closest), float duration (calls SetEntityRotationAsync with smooth)
        WaitForAnimation, // waits for the animation to complete that has been previously played using any of the animation opcodes.
        MoveFieldObject, // MoveEntityToXYNoAnimation but rotates
        PlayAnimationAsync,
        PlayAnimationOnceAsync,
        PlayPartialAnimation,
        PlayPartialAnimationAgain,
        SetMovementSpeed,         // float movementSpeed
        SetEntityRotation,        // float x, float y, float z
        SetEntityRotationAsync,   // float x, float y, float z, byte direction (0 clockwise, 1 counterclockwise, 2 closest), float duration, byte rotationType (0 linear, 1 smooth)
        SetDirectionToFaceEntity, // byte targetEntityId
        GetEntityDirection,
        PlayAnimationStopOnLastFrameWait,
        PlayAnimationToDo,
        PlayAnimationToDoAgain,
        SetAnimationSpeed,
        SetEntityAsControllableCharacter,
        MakeEntityJump,
        GetEntityPosition,
        ClimbLadder,
        TransposeObjectVisualizationOnly,
        WaitForTranspose,
        SetInteractionRange, // float size
        SetCollisionRadius,
        Collidability,
        LineTriggerInitialization,
        LineTriggerActivation,
        SetLine,
        FixFacingForward,
        SetAnimationID,
        StopAnimation,

        // Background and Palette
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
        PlayMusic, // int id
        PlaySound, // int id
        MusicOperationAgain,
        MusicVT,
        MusicVM,
        MusicLockMode,
        SetBattleMusic,
        Unknown,
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