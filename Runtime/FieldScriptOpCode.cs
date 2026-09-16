using RPGFramework.Core.Memory;

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

        [FieldOpCode("IF_BOOL", ArgumentLayout.BankCompare, OpensBlock = true, Summary = "Run the instructions inside only when the comparison holds")]
        [Argument(0, "a",          ArgumentType.Value, VariableWidth.Bool)]
        [Argument(1, "comparison", ArgumentType.Comparison, Description = "== or !=")]
        [Argument(2, "b",          ArgumentType.Value, VariableWidth.Bool)]
        CompareBool = 0x0007,

        [FieldOpCode("IF_SBYTE", ArgumentLayout.BankCompare, OpensBlock = true, Summary = "Run the instructions inside only when the comparison holds")]
        [Argument(0, "a",          ArgumentType.Value, VariableWidth.SByte)]
        [Argument(1, "comparison", ArgumentType.Comparison)]
        [Argument(2, "b",          ArgumentType.Value, VariableWidth.SByte)]
        CompareSByte = 0x0008,

        [FieldOpCode("IF_BYTE", ArgumentLayout.BankCompare, OpensBlock = true, Summary = "Run the instructions inside only when the comparison holds")]
        [Argument(0, "a",          ArgumentType.Value, VariableWidth.Byte)]
        [Argument(1, "comparison", ArgumentType.Comparison)]
        [Argument(2, "b",          ArgumentType.Value, VariableWidth.Byte)]
        CompareByte = 0x0009,

        [FieldOpCode("IF_SHORT", ArgumentLayout.BankCompare, OpensBlock = true, Summary = "Run the instructions inside only when the comparison holds")]
        [Argument(0, "a",          ArgumentType.Value, VariableWidth.Short)]
        [Argument(1, "comparison", ArgumentType.Comparison)]
        [Argument(2, "b",          ArgumentType.Value, VariableWidth.Short)]
        CompareShort = 0x000A,

        [FieldOpCode("IF_USHORT", ArgumentLayout.BankCompare, OpensBlock = true, Summary = "Run the instructions inside only when the comparison holds")]
        [Argument(0, "a",          ArgumentType.Value, VariableWidth.UShort)]
        [Argument(1, "comparison", ArgumentType.Comparison)]
        [Argument(2, "b",          ArgumentType.Value, VariableWidth.UShort)]
        CompareUShort = 0x000B,

        [FieldOpCode("IF_INT", ArgumentLayout.BankCompare, OpensBlock = true, Summary = "Run the instructions inside only when the comparison holds")]
        [Argument(0, "a",          ArgumentType.Value, VariableWidth.Int)]
        [Argument(1, "comparison", ArgumentType.Comparison)]
        [Argument(2, "b",          ArgumentType.Value, VariableWidth.Int)]
        CompareInt = 0x000C,

        [FieldOpCode("IF_UINT", ArgumentLayout.BankCompare, OpensBlock = true, Summary = "Run the instructions inside only when the comparison holds")]
        [Argument(0, "a",          ArgumentType.Value, VariableWidth.UInt)]
        [Argument(1, "comparison", ArgumentType.Comparison)]
        [Argument(2, "b",          ArgumentType.Value, VariableWidth.UInt)]
        CompareUInt = 0x000D,

        [FieldOpCode("IF_LONG", ArgumentLayout.BankCompare, OpensBlock = true, Summary = "Run the instructions inside only when the comparison holds")]
        [Argument(0, "a",          ArgumentType.Value, VariableWidth.Long)]
        [Argument(1, "comparison", ArgumentType.Comparison)]
        [Argument(2, "b",          ArgumentType.Value, VariableWidth.Long)]
        CompareLong = 0x000E,

        [FieldOpCode("IF_ULONG", ArgumentLayout.BankCompare, OpensBlock = true, Summary = "Run the instructions inside only when the comparison holds")]
        [Argument(0, "a",          ArgumentType.Value, VariableWidth.ULong)]
        [Argument(1, "comparison", ArgumentType.Comparison)]
        [Argument(2, "b",          ArgumentType.Value, VariableWidth.ULong)]
        CompareULong = 0x000F,

        [FieldOpCode("IF_FLOAT", ArgumentLayout.BankCompare, OpensBlock = true, Summary = "Run the instructions inside only when the comparison holds")]
        [Argument(0, "a",          ArgumentType.Value, VariableWidth.Float)]
        [Argument(1, "comparison", ArgumentType.Comparison, Description = "== != > < >= <=")]
        [Argument(2, "b",          ArgumentType.Value, VariableWidth.Float)]
        CompareFloat = 0x0010,

        IfCharacterIsInParty   = 0x0011,
        IfCharacterIsAvailable = 0x0012,

        [FieldOpCode("YIELD", ArgumentLayout.Sequential, Summary = "Give up the rest of this frame and resume next frame")]
        Yield = 0x0013,

        [FieldOpCode("WAIT_SECONDS", ArgumentLayout.Sequential, Summary = "Pause this script for a length of time")]
        [Argument(0, "seconds", ArgumentType.Float)]
        WaitSeconds = 0x0014,

        [FieldOpCode("NOP", ArgumentLayout.Sequential, Summary = "Does nothing. Useful as a placeholder while authoring")]
        DoNothing = 0x0015,

        DebugLog = 0x0016, // ulong messageId - authoring aid, writes to the console

        // System and module control (0x0100)
        [FieldOpCode("SET_BATTLE_MODE_OPTIONS", ArgumentLayout.Sequential, Summary = "Choose the arena, enemies and rules for the next battle")]
        [Argument(0, "arena",      ArgumentType.UShort)]
        [Argument(1, "enemyGroup", ArgumentType.UShort)]
        [Argument(2, "flags",      ArgumentType.UShort)]
        [Argument(3, "enemyLevel", ArgumentType.Byte)]
        SetBattleModeOptions = 0x0100,

        LoadResultOfLastBattle  = 0x0101,
        SetBattleEncounterTable = 0x0102,

        [FieldOpCode("JUMP_TO_MAP", ArgumentLayout.Sequential, Summary = "Leave for another field, entering at one of its spawn points")]
        [Argument(0, "field",      ArgumentType.FieldName)]
        [Argument(1, "spawnPoint", ArgumentType.SpawnId, Description = "the id on a SpawnPoint in the field being entered")]
        JumpToAnotherMap = 0x0103,

        GetLastFieldMap = 0x0104,
        SetJumpFieldID  = 0x0105,

        [FieldOpCode("START_BATTLE", ArgumentLayout.Sequential, Summary = "Begin the battle set up by SET_BATTLE_MODE_OPTIONS")]
        StartBattle = 0x0106,

        RandomEncounters = 0x0107,

        [FieldOpCode("GATEWAY_TRIGGER_ACTIVATION", ArgumentLayout.Sequential, Summary = "Turn this field's gateway triggers on or off")]
        [Argument(0, "active", ArgumentType.Bool)]
        GatewayTriggerActivation = 0x0108,

        GameOver       = 0x0109,
        WorldMapJump   = 0x010A, // int spawnIndex
        SetSaveEnabled = 0x010B, // bool enabled

        // Assignment and mathematics (0x0200)
        [FieldOpCode("SET_BOOL", ArgumentLayout.BankBinary, Summary = "Set a bool variable")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Bool)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.Bool)]
        SetBool = 0x0200,

        [FieldOpCode("SET_SBYTE", ArgumentLayout.BankBinary, Summary = "Set an sbyte variable")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.SByte)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.SByte)]
        SetSByte = 0x0201,

        [FieldOpCode("SET_BYTE", ArgumentLayout.BankBinary, Summary = "Set a byte variable")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Byte)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.Byte)]
        SetByte = 0x0202,

        [FieldOpCode("SET_SHORT", ArgumentLayout.BankBinary, Summary = "Set a short variable")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Short)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.Short)]
        SetShort = 0x0203,

        [FieldOpCode("SET_USHORT", ArgumentLayout.BankBinary, Summary = "Set a ushort variable")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.UShort)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.UShort)]
        SetUShort = 0x0204,

        [FieldOpCode("SET_INT", ArgumentLayout.BankBinary, Summary = "Set an int variable")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Int)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.Int)]
        SetInt = 0x0205,

        [FieldOpCode("SET_UINT", ArgumentLayout.BankBinary, Summary = "Set a uint variable")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.UInt)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.UInt)]
        SetUInt = 0x0206,

        [FieldOpCode("SET_LONG", ArgumentLayout.BankBinary, Summary = "Set a long variable")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Long)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.Long)]
        SetLong = 0x0207,

        [FieldOpCode("SET_ULONG", ArgumentLayout.BankBinary, Summary = "Set a ulong variable")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.ULong)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.ULong)]
        SetULong = 0x0208,

        [FieldOpCode("SET_FLOAT", ArgumentLayout.BankBinary, Summary = "Set a float variable")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Float)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.Float)]
        SetFloat = 0x0209,

        [FieldOpCode("ADD_SBYTE", ArgumentLayout.BankBinary, Summary = "Add to an sbyte variable, wrapping if it overflows")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.SByte)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.SByte)]
        AddSByte = 0x020A,

        [FieldOpCode("ADD_BYTE", ArgumentLayout.BankBinary, Summary = "Add to a byte variable, wrapping if it overflows")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Byte)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.Byte)]
        AddByte = 0x020B,

        [FieldOpCode("ADD_SHORT", ArgumentLayout.BankBinary, Summary = "Add to a short variable, wrapping if it overflows")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Short)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.Short)]
        AddShort = 0x020C,

        [FieldOpCode("ADD_USHORT", ArgumentLayout.BankBinary, Summary = "Add to a ushort variable, wrapping if it overflows")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.UShort)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.UShort)]
        AddUShort = 0x020D,

        [FieldOpCode("ADD_INT", ArgumentLayout.BankBinary, Summary = "Add to an int variable, wrapping if it overflows")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Int)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.Int)]
        AddInt = 0x020E,

        [FieldOpCode("ADD_UINT", ArgumentLayout.BankBinary, Summary = "Add to a uint variable, wrapping if it overflows")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.UInt)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.UInt)]
        AddUInt = 0x020F,

        [FieldOpCode("ADD_LONG", ArgumentLayout.BankBinary, Summary = "Add to a long variable, wrapping if it overflows")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Long)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.Long)]
        AddLong = 0x0210,

        [FieldOpCode("ADD_ULONG", ArgumentLayout.BankBinary, Summary = "Add to a ulong variable, wrapping if it overflows")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.ULong)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.ULong)]
        AddULong = 0x0211,

        [FieldOpCode("ADD_FLOAT", ArgumentLayout.BankBinary, Summary = "Add to a float variable")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Float)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.Float)]
        AddFloat = 0x0212,

        [FieldOpCode("ADD_SBYTE_CLAMPED", ArgumentLayout.BankBinary, Summary = "Add to an sbyte variable, stopping at its limit rather than wrapping")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.SByte)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.SByte)]
        AddSByteClamped = 0x0213,

        [FieldOpCode("ADD_BYTE_CLAMPED", ArgumentLayout.BankBinary, Summary = "Add to a byte variable, stopping at its limit rather than wrapping")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Byte)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.Byte)]
        AddByteClamped = 0x0214,

        [FieldOpCode("ADD_SHORT_CLAMPED", ArgumentLayout.BankBinary, Summary = "Add to a short variable, stopping at its limit rather than wrapping")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Short)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.Short)]
        AddShortClamped = 0x0215,

        [FieldOpCode("ADD_USHORT_CLAMPED", ArgumentLayout.BankBinary, Summary = "Add to a ushort variable, stopping at its limit rather than wrapping")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.UShort)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.UShort)]
        AddUShortClamped = 0x0216,

        [FieldOpCode("ADD_INT_CLAMPED", ArgumentLayout.BankBinary, Summary = "Add to an int variable, stopping at its limit rather than wrapping")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Int)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.Int)]
        AddIntClamped = 0x0217,

        [FieldOpCode("ADD_UINT_CLAMPED", ArgumentLayout.BankBinary, Summary = "Add to a uint variable, stopping at its limit rather than wrapping")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.UInt)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.UInt)]
        AddUIntClamped = 0x0218,

        [FieldOpCode("ADD_LONG_CLAMPED", ArgumentLayout.BankBinary, Summary = "Add to a long variable, stopping at its limit rather than wrapping")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Long)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.Long)]
        AddLongClamped = 0x0219,

        [FieldOpCode("ADD_ULONG_CLAMPED", ArgumentLayout.BankBinary, Summary = "Add to a ulong variable, stopping at its limit rather than wrapping")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.ULong)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.ULong)]
        AddULongClamped = 0x021A,

        [FieldOpCode("SUB_SBYTE", ArgumentLayout.BankBinary, Summary = "Subtract from an sbyte variable, wrapping if it overflows")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.SByte)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.SByte)]
        SubtractSByte = 0x021B,

        [FieldOpCode("SUB_BYTE", ArgumentLayout.BankBinary, Summary = "Subtract from a byte variable, wrapping if it overflows")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Byte)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.Byte)]
        SubtractByte = 0x021C,

        [FieldOpCode("SUB_SHORT", ArgumentLayout.BankBinary, Summary = "Subtract from a short variable, wrapping if it overflows")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Short)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.Short)]
        SubtractShort = 0x021D,

        [FieldOpCode("SUB_USHORT", ArgumentLayout.BankBinary, Summary = "Subtract from a ushort variable, wrapping if it overflows")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.UShort)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.UShort)]
        SubtractUShort = 0x021E,

        [FieldOpCode("SUB_INT", ArgumentLayout.BankBinary, Summary = "Subtract from an int variable, wrapping if it overflows")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Int)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.Int)]
        SubtractInt = 0x021F,

        [FieldOpCode("SUB_UINT", ArgumentLayout.BankBinary, Summary = "Subtract from a uint variable, wrapping if it overflows")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.UInt)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.UInt)]
        SubtractUInt = 0x0220,

        [FieldOpCode("SUB_LONG", ArgumentLayout.BankBinary, Summary = "Subtract from a long variable, wrapping if it overflows")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Long)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.Long)]
        SubtractLong = 0x0221,

        [FieldOpCode("SUB_ULONG", ArgumentLayout.BankBinary, Summary = "Subtract from a ulong variable, wrapping if it overflows")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.ULong)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.ULong)]
        SubtractULong = 0x0222,

        [FieldOpCode("SUB_FLOAT", ArgumentLayout.BankBinary, Summary = "Subtract from a float variable")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Float)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.Float)]
        SubtractFloat = 0x0223,

        [FieldOpCode("SUB_SBYTE_CLAMPED", ArgumentLayout.BankBinary, Summary = "Subtract from an sbyte variable, stopping at its limit rather than wrapping")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.SByte)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.SByte)]
        SubtractSByteClamped = 0x0224,

        [FieldOpCode("SUB_BYTE_CLAMPED", ArgumentLayout.BankBinary, Summary = "Subtract from a byte variable, stopping at its limit rather than wrapping")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Byte)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.Byte)]
        SubtractByteClamped = 0x0225,

        [FieldOpCode("SUB_SHORT_CLAMPED", ArgumentLayout.BankBinary, Summary = "Subtract from a short variable, stopping at its limit rather than wrapping")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Short)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.Short)]
        SubtractShortClamped = 0x0226,

        [FieldOpCode("SUB_USHORT_CLAMPED", ArgumentLayout.BankBinary, Summary = "Subtract from a ushort variable, stopping at its limit rather than wrapping")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.UShort)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.UShort)]
        SubtractUShortClamped = 0x0227,

        [FieldOpCode("SUB_INT_CLAMPED", ArgumentLayout.BankBinary, Summary = "Subtract from an int variable, stopping at its limit rather than wrapping")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Int)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.Int)]
        SubtractIntClamped = 0x0228,

        [FieldOpCode("SUB_UINT_CLAMPED", ArgumentLayout.BankBinary, Summary = "Subtract from a uint variable, stopping at its limit rather than wrapping")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.UInt)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.UInt)]
        SubtractUIntClamped = 0x0229,

        [FieldOpCode("SUB_LONG_CLAMPED", ArgumentLayout.BankBinary, Summary = "Subtract from a long variable, stopping at its limit rather than wrapping")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Long)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.Long)]
        SubtractLongClamped = 0x022A,

        [FieldOpCode("SUB_ULONG_CLAMPED", ArgumentLayout.BankBinary, Summary = "Subtract from a ulong variable, stopping at its limit rather than wrapping")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.ULong)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.ULong)]
        SubtractULongClamped = 0x022B,

        [FieldOpCode("MUL_SBYTE", ArgumentLayout.BankBinary, Summary = "Multiply an sbyte variable, wrapping if it overflows")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.SByte)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.SByte)]
        MultiplySByte = 0x022C,

        [FieldOpCode("MUL_BYTE", ArgumentLayout.BankBinary, Summary = "Multiply a byte variable, wrapping if it overflows")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Byte)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.Byte)]
        MultiplyByte = 0x022D,

        [FieldOpCode("MUL_SHORT", ArgumentLayout.BankBinary, Summary = "Multiply a short variable, wrapping if it overflows")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Short)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.Short)]
        MultiplyShort = 0x022E,

        [FieldOpCode("MUL_USHORT", ArgumentLayout.BankBinary, Summary = "Multiply a ushort variable, wrapping if it overflows")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.UShort)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.UShort)]
        MultiplyUShort = 0x022F,

        [FieldOpCode("MUL_INT", ArgumentLayout.BankBinary, Summary = "Multiply an int variable, wrapping if it overflows")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Int)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.Int)]
        MultiplyInt = 0x0230,

        [FieldOpCode("MUL_UINT", ArgumentLayout.BankBinary, Summary = "Multiply a uint variable, wrapping if it overflows")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.UInt)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.UInt)]
        MultiplyUInt = 0x0231,

        [FieldOpCode("MUL_LONG", ArgumentLayout.BankBinary, Summary = "Multiply a long variable, wrapping if it overflows")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Long)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.Long)]
        MultiplyLong = 0x0232,

        [FieldOpCode("MUL_ULONG", ArgumentLayout.BankBinary, Summary = "Multiply a ulong variable, wrapping if it overflows")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.ULong)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.ULong)]
        MultiplyULong = 0x0233,

        [FieldOpCode("MUL_FLOAT", ArgumentLayout.BankBinary, Summary = "Multiply a float variable")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Float)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.Float)]
        MultiplyFloat = 0x0234,

        [FieldOpCode("DIV_SBYTE", ArgumentLayout.BankBinary, Summary = "Divide an sbyte variable, rounding towards zero. Dividing by zero leaves it unchanged")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.SByte)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.SByte)]
        DivideSByte = 0x0235,

        [FieldOpCode("DIV_BYTE", ArgumentLayout.BankBinary, Summary = "Divide a byte variable, rounding towards zero. Dividing by zero leaves it unchanged")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Byte)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.Byte)]
        DivideByte = 0x0236,

        [FieldOpCode("DIV_SHORT", ArgumentLayout.BankBinary, Summary = "Divide a short variable, rounding towards zero. Dividing by zero leaves it unchanged")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Short)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.Short)]
        DivideShort = 0x0237,

        [FieldOpCode("DIV_USHORT", ArgumentLayout.BankBinary, Summary = "Divide a ushort variable, rounding towards zero. Dividing by zero leaves it unchanged")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.UShort)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.UShort)]
        DivideUShort = 0x0238,

        [FieldOpCode("DIV_INT", ArgumentLayout.BankBinary, Summary = "Divide an int variable, rounding towards zero. Dividing by zero leaves it unchanged")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Int)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.Int)]
        DivideInt = 0x0239,

        [FieldOpCode("DIV_UINT", ArgumentLayout.BankBinary, Summary = "Divide a uint variable, rounding towards zero. Dividing by zero leaves it unchanged")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.UInt)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.UInt)]
        DivideUInt = 0x023A,

        [FieldOpCode("DIV_LONG", ArgumentLayout.BankBinary, Summary = "Divide a long variable, rounding towards zero. Dividing by zero leaves it unchanged")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Long)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.Long)]
        DivideLong = 0x023B,

        [FieldOpCode("DIV_ULONG", ArgumentLayout.BankBinary, Summary = "Divide a ulong variable, rounding towards zero. Dividing by zero leaves it unchanged")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.ULong)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.ULong)]
        DivideULong = 0x023C,

        [FieldOpCode("DIV_FLOAT", ArgumentLayout.BankBinary, Summary = "Divide a float variable. Dividing by zero leaves it unchanged")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Float)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.Float)]
        DivideFloat = 0x023D,

        [FieldOpCode("MOD_SBYTE", ArgumentLayout.BankBinary, Summary = "Replace an sbyte variable with its remainder after dividing. Dividing by zero leaves it unchanged")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.SByte)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.SByte)]
        RemainderSByte = 0x023E,

        [FieldOpCode("MOD_BYTE", ArgumentLayout.BankBinary, Summary = "Replace a byte variable with its remainder after dividing. Dividing by zero leaves it unchanged")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Byte)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.Byte)]
        RemainderByte = 0x023F,

        [FieldOpCode("MOD_SHORT", ArgumentLayout.BankBinary, Summary = "Replace a short variable with its remainder after dividing. Dividing by zero leaves it unchanged")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Short)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.Short)]
        RemainderShort = 0x0240,

        [FieldOpCode("MOD_USHORT", ArgumentLayout.BankBinary, Summary = "Replace a ushort variable with its remainder after dividing. Dividing by zero leaves it unchanged")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.UShort)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.UShort)]
        RemainderUShort = 0x0241,

        [FieldOpCode("MOD_INT", ArgumentLayout.BankBinary, Summary = "Replace an int variable with its remainder after dividing. Dividing by zero leaves it unchanged")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Int)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.Int)]
        RemainderInt = 0x0242,

        [FieldOpCode("MOD_UINT", ArgumentLayout.BankBinary, Summary = "Replace a uint variable with its remainder after dividing. Dividing by zero leaves it unchanged")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.UInt)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.UInt)]
        RemainderUInt = 0x0243,

        [FieldOpCode("MOD_LONG", ArgumentLayout.BankBinary, Summary = "Replace a long variable with its remainder after dividing. Dividing by zero leaves it unchanged")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Long)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.Long)]
        RemainderLong = 0x0244,

        [FieldOpCode("MOD_ULONG", ArgumentLayout.BankBinary, Summary = "Replace a ulong variable with its remainder after dividing. Dividing by zero leaves it unchanged")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.ULong)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.ULong)]
        RemainderULong = 0x0245,

        [FieldOpCode("INC_SBYTE", ArgumentLayout.BankUnary, Summary = "Add one to an sbyte variable, wrapping")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.SByte)]
        IncrementSByte = 0x0246,

        [FieldOpCode("INC_BYTE", ArgumentLayout.BankUnary, Summary = "Add one to a byte variable, wrapping")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Byte)]
        IncrementByte = 0x0247,

        [FieldOpCode("INC_SHORT", ArgumentLayout.BankUnary, Summary = "Add one to a short variable, wrapping")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Short)]
        IncrementShort = 0x0248,

        [FieldOpCode("INC_USHORT", ArgumentLayout.BankUnary, Summary = "Add one to a ushort variable, wrapping")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.UShort)]
        IncrementUShort = 0x0249,

        [FieldOpCode("INC_INT", ArgumentLayout.BankUnary, Summary = "Add one to an int variable, wrapping")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Int)]
        IncrementInt = 0x024A,

        [FieldOpCode("INC_UINT", ArgumentLayout.BankUnary, Summary = "Add one to a uint variable, wrapping")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.UInt)]
        IncrementUInt = 0x024B,

        [FieldOpCode("INC_LONG", ArgumentLayout.BankUnary, Summary = "Add one to a long variable, wrapping")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Long)]
        IncrementLong = 0x024C,

        [FieldOpCode("INC_ULONG", ArgumentLayout.BankUnary, Summary = "Add one to a ulong variable, wrapping")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.ULong)]
        IncrementULong = 0x024D,

        [FieldOpCode("INC_SBYTE_CLAMPED", ArgumentLayout.BankUnary, Summary = "Add one to an sbyte variable, stopping at its limit")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.SByte)]
        IncrementSByteClamped = 0x024E,

        [FieldOpCode("INC_BYTE_CLAMPED", ArgumentLayout.BankUnary, Summary = "Add one to a byte variable, stopping at its limit")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Byte)]
        IncrementByteClamped = 0x024F,

        [FieldOpCode("INC_SHORT_CLAMPED", ArgumentLayout.BankUnary, Summary = "Add one to a short variable, stopping at its limit")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Short)]
        IncrementShortClamped = 0x0250,

        [FieldOpCode("INC_USHORT_CLAMPED", ArgumentLayout.BankUnary, Summary = "Add one to a ushort variable, stopping at its limit")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.UShort)]
        IncrementUShortClamped = 0x0251,

        [FieldOpCode("INC_INT_CLAMPED", ArgumentLayout.BankUnary, Summary = "Add one to an int variable, stopping at its limit")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Int)]
        IncrementIntClamped = 0x0252,

        [FieldOpCode("INC_UINT_CLAMPED", ArgumentLayout.BankUnary, Summary = "Add one to a uint variable, stopping at its limit")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.UInt)]
        IncrementUIntClamped = 0x0253,

        [FieldOpCode("INC_LONG_CLAMPED", ArgumentLayout.BankUnary, Summary = "Add one to a long variable, stopping at its limit")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Long)]
        IncrementLongClamped = 0x0254,

        [FieldOpCode("INC_ULONG_CLAMPED", ArgumentLayout.BankUnary, Summary = "Add one to a ulong variable, stopping at its limit")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.ULong)]
        IncrementULongClamped = 0x0255,

        [FieldOpCode("DEC_SBYTE", ArgumentLayout.BankUnary, Summary = "Subtract one from an sbyte variable, wrapping")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.SByte)]
        DecrementSByte = 0x0256,

        [FieldOpCode("DEC_BYTE", ArgumentLayout.BankUnary, Summary = "Subtract one from a byte variable, wrapping")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Byte)]
        DecrementByte = 0x0257,

        [FieldOpCode("DEC_SHORT", ArgumentLayout.BankUnary, Summary = "Subtract one from a short variable, wrapping")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Short)]
        DecrementShort = 0x0258,

        [FieldOpCode("DEC_USHORT", ArgumentLayout.BankUnary, Summary = "Subtract one from a ushort variable, wrapping")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.UShort)]
        DecrementUShort = 0x0259,

        [FieldOpCode("DEC_INT", ArgumentLayout.BankUnary, Summary = "Subtract one from an int variable, wrapping")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Int)]
        DecrementInt = 0x025A,

        [FieldOpCode("DEC_UINT", ArgumentLayout.BankUnary, Summary = "Subtract one from a uint variable, wrapping")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.UInt)]
        DecrementUInt = 0x025B,

        [FieldOpCode("DEC_LONG", ArgumentLayout.BankUnary, Summary = "Subtract one from a long variable, wrapping")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Long)]
        DecrementLong = 0x025C,

        [FieldOpCode("DEC_ULONG", ArgumentLayout.BankUnary, Summary = "Subtract one from a ulong variable, wrapping")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.ULong)]
        DecrementULong = 0x025D,

        [FieldOpCode("DEC_SBYTE_CLAMPED", ArgumentLayout.BankUnary, Summary = "Subtract one from an sbyte variable, stopping at its limit")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.SByte)]
        DecrementSByteClamped = 0x025E,

        [FieldOpCode("DEC_BYTE_CLAMPED", ArgumentLayout.BankUnary, Summary = "Subtract one from a byte variable, stopping at its limit")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Byte)]
        DecrementByteClamped = 0x025F,

        [FieldOpCode("DEC_SHORT_CLAMPED", ArgumentLayout.BankUnary, Summary = "Subtract one from a short variable, stopping at its limit")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Short)]
        DecrementShortClamped = 0x0260,

        [FieldOpCode("DEC_USHORT_CLAMPED", ArgumentLayout.BankUnary, Summary = "Subtract one from a ushort variable, stopping at its limit")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.UShort)]
        DecrementUShortClamped = 0x0261,

        [FieldOpCode("DEC_INT_CLAMPED", ArgumentLayout.BankUnary, Summary = "Subtract one from an int variable, stopping at its limit")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Int)]
        DecrementIntClamped = 0x0262,

        [FieldOpCode("DEC_UINT_CLAMPED", ArgumentLayout.BankUnary, Summary = "Subtract one from a uint variable, stopping at its limit")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.UInt)]
        DecrementUIntClamped = 0x0263,

        [FieldOpCode("DEC_LONG_CLAMPED", ArgumentLayout.BankUnary, Summary = "Subtract one from a long variable, stopping at its limit")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Long)]
        DecrementLongClamped = 0x0264,

        [FieldOpCode("DEC_ULONG_CLAMPED", ArgumentLayout.BankUnary, Summary = "Subtract one from a ulong variable, stopping at its limit")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.ULong)]
        DecrementULongClamped = 0x0265,

        [FieldOpCode("AND_SBYTE", ArgumentLayout.BankBinary, Summary = "Bitwise AND into an sbyte variable")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.SByte)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.SByte)]
        BitwiseAndSByte = 0x0266,

        [FieldOpCode("AND_BYTE", ArgumentLayout.BankBinary, Summary = "Bitwise AND into a byte variable")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Byte)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.Byte)]
        BitwiseAndByte = 0x0267,

        [FieldOpCode("AND_SHORT", ArgumentLayout.BankBinary, Summary = "Bitwise AND into a short variable")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Short)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.Short)]
        BitwiseAndShort = 0x0268,

        [FieldOpCode("AND_USHORT", ArgumentLayout.BankBinary, Summary = "Bitwise AND into a ushort variable")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.UShort)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.UShort)]
        BitwiseAndUShort = 0x0269,

        [FieldOpCode("AND_INT", ArgumentLayout.BankBinary, Summary = "Bitwise AND into an int variable")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Int)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.Int)]
        BitwiseAndInt = 0x026A,

        [FieldOpCode("AND_UINT", ArgumentLayout.BankBinary, Summary = "Bitwise AND into a uint variable")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.UInt)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.UInt)]
        BitwiseAndUInt = 0x026B,

        [FieldOpCode("AND_LONG", ArgumentLayout.BankBinary, Summary = "Bitwise AND into a long variable")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Long)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.Long)]
        BitwiseAndLong = 0x026C,

        [FieldOpCode("AND_ULONG", ArgumentLayout.BankBinary, Summary = "Bitwise AND into a ulong variable")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.ULong)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.ULong)]
        BitwiseAndULong = 0x026D,

        [FieldOpCode("OR_SBYTE", ArgumentLayout.BankBinary, Summary = "Bitwise OR into an sbyte variable")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.SByte)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.SByte)]
        BitwiseOrSByte = 0x026E,

        [FieldOpCode("OR_BYTE", ArgumentLayout.BankBinary, Summary = "Bitwise OR into a byte variable")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Byte)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.Byte)]
        BitwiseOrByte = 0x026F,

        [FieldOpCode("OR_SHORT", ArgumentLayout.BankBinary, Summary = "Bitwise OR into a short variable")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Short)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.Short)]
        BitwiseOrShort = 0x0270,

        [FieldOpCode("OR_USHORT", ArgumentLayout.BankBinary, Summary = "Bitwise OR into a ushort variable")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.UShort)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.UShort)]
        BitwiseOrUShort = 0x0271,

        [FieldOpCode("OR_INT", ArgumentLayout.BankBinary, Summary = "Bitwise OR into an int variable")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Int)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.Int)]
        BitwiseOrInt = 0x0272,

        [FieldOpCode("OR_UINT", ArgumentLayout.BankBinary, Summary = "Bitwise OR into a uint variable")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.UInt)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.UInt)]
        BitwiseOrUInt = 0x0273,

        [FieldOpCode("OR_LONG", ArgumentLayout.BankBinary, Summary = "Bitwise OR into a long variable")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Long)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.Long)]
        BitwiseOrLong = 0x0274,

        [FieldOpCode("OR_ULONG", ArgumentLayout.BankBinary, Summary = "Bitwise OR into a ulong variable")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.ULong)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.ULong)]
        BitwiseOrULong = 0x0275,

        [FieldOpCode("XOR_SBYTE", ArgumentLayout.BankBinary, Summary = "Bitwise XOR into an sbyte variable")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.SByte)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.SByte)]
        BitwiseXorSByte = 0x0276,

        [FieldOpCode("XOR_BYTE", ArgumentLayout.BankBinary, Summary = "Bitwise XOR into a byte variable")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Byte)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.Byte)]
        BitwiseXorByte = 0x0277,

        [FieldOpCode("XOR_SHORT", ArgumentLayout.BankBinary, Summary = "Bitwise XOR into a short variable")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Short)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.Short)]
        BitwiseXorShort = 0x0278,

        [FieldOpCode("XOR_USHORT", ArgumentLayout.BankBinary, Summary = "Bitwise XOR into a ushort variable")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.UShort)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.UShort)]
        BitwiseXorUShort = 0x0279,

        [FieldOpCode("XOR_INT", ArgumentLayout.BankBinary, Summary = "Bitwise XOR into an int variable")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Int)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.Int)]
        BitwiseXorInt = 0x027A,

        [FieldOpCode("XOR_UINT", ArgumentLayout.BankBinary, Summary = "Bitwise XOR into a uint variable")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.UInt)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.UInt)]
        BitwiseXorUInt = 0x027B,

        [FieldOpCode("XOR_LONG", ArgumentLayout.BankBinary, Summary = "Bitwise XOR into a long variable")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Long)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.Long)]
        BitwiseXorLong = 0x027C,

        [FieldOpCode("XOR_ULONG", ArgumentLayout.BankBinary, Summary = "Bitwise XOR into a ulong variable")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.ULong)]
        [Argument(1, "value",       ArgumentType.Value,    VariableWidth.ULong)]
        BitwiseXorULong = 0x027D,

        [FieldOpCode("SET_BIT_SBYTE", ArgumentLayout.BankBinary, Summary = "Set one bit of an sbyte variable")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.SByte)]
        [Argument(1, "bitIndex",    ArgumentType.Value,    VariableWidth.Byte, Description = "0-7")]
        SetBitSByte = 0x027E,

        [FieldOpCode("SET_BIT_BYTE", ArgumentLayout.BankBinary, Summary = "Set one bit of a byte variable")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Byte)]
        [Argument(1, "bitIndex",    ArgumentType.Value,    VariableWidth.Byte, Description = "0-7")]
        SetBitByte = 0x027F,

        [FieldOpCode("SET_BIT_SHORT", ArgumentLayout.BankBinary, Summary = "Set one bit of a short variable")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Short)]
        [Argument(1, "bitIndex",    ArgumentType.Value,    VariableWidth.Byte, Description = "0-15")]
        SetBitShort = 0x0280,

        [FieldOpCode("SET_BIT_USHORT", ArgumentLayout.BankBinary, Summary = "Set one bit of a ushort variable")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.UShort)]
        [Argument(1, "bitIndex",    ArgumentType.Value,    VariableWidth.Byte, Description = "0-15")]
        SetBitUShort = 0x0281,

        [FieldOpCode("SET_BIT_INT", ArgumentLayout.BankBinary, Summary = "Set one bit of an int variable")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Int)]
        [Argument(1, "bitIndex",    ArgumentType.Value,    VariableWidth.Byte, Description = "0-31")]
        SetBitInt = 0x0282,

        [FieldOpCode("SET_BIT_UINT", ArgumentLayout.BankBinary, Summary = "Set one bit of a uint variable")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.UInt)]
        [Argument(1, "bitIndex",    ArgumentType.Value,    VariableWidth.Byte, Description = "0-31")]
        SetBitUInt = 0x0283,

        [FieldOpCode("SET_BIT_LONG", ArgumentLayout.BankBinary, Summary = "Set one bit of a long variable")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Long)]
        [Argument(1, "bitIndex",    ArgumentType.Value,    VariableWidth.Byte, Description = "0-63")]
        SetBitLong = 0x0284,

        [FieldOpCode("SET_BIT_ULONG", ArgumentLayout.BankBinary, Summary = "Set one bit of a ulong variable")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.ULong)]
        [Argument(1, "bitIndex",    ArgumentType.Value,    VariableWidth.Byte, Description = "0-63")]
        SetBitULong = 0x0285,

        [FieldOpCode("UNSET_BIT_SBYTE", ArgumentLayout.BankBinary, Summary = "Clear one bit of an sbyte variable")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.SByte)]
        [Argument(1, "bitIndex",    ArgumentType.Value,    VariableWidth.Byte, Description = "0-7")]
        UnsetBitSByte = 0x0286,

        [FieldOpCode("UNSET_BIT_BYTE", ArgumentLayout.BankBinary, Summary = "Clear one bit of a byte variable")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Byte)]
        [Argument(1, "bitIndex",    ArgumentType.Value,    VariableWidth.Byte, Description = "0-7")]
        UnsetBitByte = 0x0287,

        [FieldOpCode("UNSET_BIT_SHORT", ArgumentLayout.BankBinary, Summary = "Clear one bit of a short variable")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Short)]
        [Argument(1, "bitIndex",    ArgumentType.Value,    VariableWidth.Byte, Description = "0-15")]
        UnsetBitShort = 0x0288,

        [FieldOpCode("UNSET_BIT_USHORT", ArgumentLayout.BankBinary, Summary = "Clear one bit of a ushort variable")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.UShort)]
        [Argument(1, "bitIndex",    ArgumentType.Value,    VariableWidth.Byte, Description = "0-15")]
        UnsetBitUShort = 0x0289,

        [FieldOpCode("UNSET_BIT_INT", ArgumentLayout.BankBinary, Summary = "Clear one bit of an int variable")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Int)]
        [Argument(1, "bitIndex",    ArgumentType.Value,    VariableWidth.Byte, Description = "0-31")]
        UnsetBitInt = 0x028A,

        [FieldOpCode("UNSET_BIT_UINT", ArgumentLayout.BankBinary, Summary = "Clear one bit of a uint variable")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.UInt)]
        [Argument(1, "bitIndex",    ArgumentType.Value,    VariableWidth.Byte, Description = "0-31")]
        UnsetBitUInt = 0x028B,

        [FieldOpCode("UNSET_BIT_LONG", ArgumentLayout.BankBinary, Summary = "Clear one bit of a long variable")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Long)]
        [Argument(1, "bitIndex",    ArgumentType.Value,    VariableWidth.Byte, Description = "0-63")]
        UnsetBitLong = 0x028C,

        [FieldOpCode("UNSET_BIT_ULONG", ArgumentLayout.BankBinary, Summary = "Clear one bit of a ulong variable")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.ULong)]
        [Argument(1, "bitIndex",    ArgumentType.Value,    VariableWidth.Byte, Description = "0-63")]
        UnsetBitULong = 0x028D,

        [FieldOpCode("GET_RANDOM", ArgumentLayout.BankUnary, Summary = "Store a random byte, 0 to 255")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Byte)]
        GetRandomNumber = 0x028E,

        [FieldOpCode("RANDOM_SEED", ArgumentLayout.BankSeed, Summary = "Reseed the random sequence, so a script repeats exactly")]
        [Argument(0, "seed", ArgumentType.Value, VariableWidth.Int)]
        RandomNumberSeed = 0x028F,

        // Windowing and menu (0x0300)
        RunTutorial         = 0x0300,
        CloseWindow         = 0x0301,
        CreateSpecialWindow = 0x0302,
        SetNumberInWindow   = 0x0303,
        SetTimeInWindow     = 0x0304,

        [FieldOpCode("SHOW_DIALOGUE_WINDOW", ArgumentLayout.Sequential, Summary = "Show a line of dialogue and wait for the player to dismiss it")]
        [Argument(0, "dialogue",      ArgumentType.LocalisationKey)]
        [Argument(1, "blockMovement", ArgumentType.Bool)]
        ShowDialogueWindow = 0x0305,

        SetMapNameInMenu = 0x0306,

        [FieldOpCode("ASK_PLAYER_TO_MAKE_A_CHOICE", ArgumentLayout.Sequential, Summary = "Ask a question and store which answer the player chose")]
        [Argument(0, "destination", ArgumentType.Variable, VariableWidth.Byte)]
        [Argument(1, "question",    ArgumentType.LocalisationKey)]
        [Argument(2, "answers",     ArgumentType.LocalisationKeyList)]
        AskPlayerToMakeAChoice = 0x0307,

        MenuOperations = 0x0308,

        [FieldOpCode("MAIN_MENU_ACCESSIBILITY", ArgumentLayout.Sequential, Summary = "Allow or block the player opening the main menu")]
        [Argument(0, "enabled", ArgumentType.Bool)]
        MainMenuAccessibility = 0x0309,

        [FieldOpCode("CREATE_DIALOGUE_WINDOW", ArgumentLayout.Sequential, Summary = "Create a dialogue window at a position and size, without showing it")]
        [Argument(0, "dialogue", ArgumentType.LocalisationKey)]
        [Argument(1, "x",        ArgumentType.Int)]
        [Argument(2, "y",        ArgumentType.Int)]
        [Argument(3, "width",    ArgumentType.Int)]
        [Argument(4, "height",   ArgumentType.Int)]
        CreateDialogueWindow = 0x030A,

        SetMessageSpeed = 0x030B, // float charactersPerSecond

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
        MoveToPartyMember    = 0x0502,

        [FieldOpCode("LOCK_INPUT", ArgumentLayout.Sequential, Summary = "Take control away from the player, or give it back")]
        [Argument(0, "locked", ArgumentType.Bool)]
        LockInput = 0x0503,

        TurnToPartyMember       = 0x0504,
        CollisionDetection      = 0x0505,
        GetPartyMemberDirection = 0x0506,
        GetPartyMemberPosition  = 0x0507,

        [FieldOpCode("INTERACTION_TRIGGER_ACTIVATION", ArgumentLayout.Sequential, Summary = "Turn this entity's interaction trigger on or off")]
        [Argument(0, "enabled", ArgumentType.Bool)]
        InteractionTriggerActivation = 0x0508,

        [FieldOpCode("INIT_CHARACTER", ArgumentLayout.Sequential, Summary = "Mark this entity as the player character")]
        InitAsCharacter = 0x0509,

        PlayAnimationLooping     = 0x050A,
        PlayAnimationOnceAndWait = 0x050B,

        [FieldOpCode("VISIBILITY", ArgumentLayout.Sequential, Summary = "Show or hide this entity")]
        [Argument(0, "isVisible", ArgumentType.Bool)]
        Visibility = 0x050C,

        [FieldOpCode("SET_ENTITY_POSITION", ArgumentLayout.Sequential, Summary = "Move this entity immediately, with no animation")]
        [Argument(0, "x", ArgumentType.Float)]
        [Argument(1, "y", ArgumentType.Float)]
        [Argument(2, "z", ArgumentType.Float)]
        SetEntityPosition = 0x050D,

        MoveEntityToXYWalkAnimation = 0x050E, // moves an entity using walk animation (if available) to x,y,z at speed set by SetMovementSpeed
        MoveEntityToXYNoAnimation   = 0x050F, // as above but doesn't animate or rotate
        MoveEntityToAnotherEntity   = 0x0510, // navigates to another entity stopping once it reaches its collision
        TurnEntityToAnotherEntity   = 0x0511, // int entityId, byte rotationDirection (0 clockwise, 1 anti-clockwise, 2 closest), float duration (calls SetEntityRotationAsync with smooth)
        WaitForAnimation            = 0x0512, // waits for the animation to complete that has been previously played using any of the animation opcodes.
        MoveFieldObject             = 0x0513, // MoveEntityToXYNoAnimation but rotates
        PlayAnimationAsync          = 0x0514,
        PlayAnimationOnceAsync      = 0x0515,
        PlayPartialAnimation        = 0x0516,

        [FieldOpCode("SET_MOVEMENT_SPEED", ArgumentLayout.Sequential, Summary = "Set how fast this entity moves")]
        [Argument(0, "movementSpeed", ArgumentType.Float)]
        SetMovementSpeed = 0x0517,

        [FieldOpCode("SET_ENTITY_ROTATION", ArgumentLayout.Sequential, Summary = "Face this entity in a direction immediately")]
        [Argument(0, "x", ArgumentType.Float)]
        [Argument(1, "y", ArgumentType.Float)]
        [Argument(2, "z", ArgumentType.Float)]
        SetEntityRotation = 0x0518,

        [FieldOpCode("SET_ENTITY_ROTATION_ASYNC", ArgumentLayout.Sequential, Summary = "Turn this entity to face a direction over time")]
        [Argument(0, "x",            ArgumentType.Float)]
        [Argument(1, "y",            ArgumentType.Float)]
        [Argument(2, "z",            ArgumentType.Float)]
        [Argument(3, "direction",    ArgumentType.Byte, Description = "0 clockwise, 1 counterclockwise, 2 closest")]
        [Argument(4, "duration",     ArgumentType.Float)]
        [Argument(5, "rotationType", ArgumentType.Byte, Description = "0 linear, 1 smooth")]
        SetEntityRotationAsync = 0x0519,

        [FieldOpCode("SET_DIRECTION_TO_FACE_ENTITY", ArgumentLayout.Sequential, Summary = "Face this entity towards another")]
        [Argument(0, "targetEntityId", ArgumentType.EntityId)]
        SetDirectionToFaceEntity = 0x051A,

        GetEntityDirection               = 0x051B,
        PlayAnimationStopOnLastFrameWait = 0x051C,
        SetAnimationSpeed                = 0x051D,
        SetEntityAsControllableCharacter = 0x051E,
        MakeEntityJump                   = 0x051F,
        GetEntityPosition                = 0x0520,
        ClimbLadder                      = 0x0521,
        TransposeObjectVisualizationOnly = 0x0522,
        WaitForTranspose                 = 0x0523,

        [FieldOpCode("SET_INTERACTION_RANGE", ArgumentLayout.Sequential, Summary = "Set how close the player must be to interact with this entity")]
        [Argument(0, "radius", ArgumentType.Float)]
        SetInteractionRange = 0x0524,

        SetCollisionRadius   = 0x0525,
        Collidability        = 0x0526,
        FixFacingForward     = 0x0527,
        SetAnimationID       = 0x0528,
        StopAnimation        = 0x0529,
        FlushMovement        = 0x052A, // drop any queued movement for this entity
        SetRunningEnabled    = 0x052B, // bool enabled
        SetFootstepSound     = 0x052C, // byte surfaceKey, byte soundSetId - per-surface footsteps for this entity
        InitialiseHeadFacing = 0x052D, // begin head tracking on this entity, before any SetHeadFacing*
        SetHeadFacingEntity  = 0x052E, // int targetEntityId, int frameCount - turn the head only, body unchanged
        SetHeadFacingPlayer  = 0x052F, // int frameCount - track the player entity with the head only
        SetHeadFacingLimit   = 0x0530, // byte yawLimit, byte pitchLimit, byte rollLimit - stop the head over-rotating
        SetHeadPose          = 0x0531, // byte pose (0 neutral, 1 up, 2 down)
        StopHeadFacing       = 0x0532, // return the head to neutral and leave tracking

        // Screen tint (0x0600)
        SetShadeLevel         = 0x0600, // float level
        SubtractiveScreenFade = 0x0601, // float r, float g, float b, float duration

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
        SetAllSoundVolume     = 0x0807, // float volume
        FadeAllSoundVolume    = 0x0808, // float volume, float duration

        [FieldOpCode("MUSIC_STEM_STATE", ArgumentLayout.Sequential, Summary = "Switch the music to one of the stem states its asset declares")]
        [Argument(0, "track",       ArgumentType.MusicNameHint,  Description = "which track's states to choose from — not compiled, it applies to whatever is playing")]
        [Argument(1, "state",       ArgumentType.MusicStateName, Description = "the state's name on the music asset")]
        [Argument(2, "fadeSeconds", ArgumentType.Float,          Description = "0 changes immediately")]
        SetMusicStemState = 0x0809,

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