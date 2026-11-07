using System;
using System.Collections.Generic;
using System.Reflection;
using RPGFramework.Core.Memory;

namespace RPGFramework.Field.Editor
{
    /// <summary>
    /// One argument of an opcode, resolved from its attribute.
    /// </summary>
    internal sealed class FieldArgumentInfo
    {
        public string        Name        { get; }
        public ArgumentType  Type        { get; }
        public VariableWidth Width       { get; }
        public string        Description { get; }
        public Type          EnumType    { get; }
        public long          EnumMask    { get; }
        public string        Default     { get; }

        internal FieldArgumentInfo(ArgumentAttribute attribute)
        {
            Name        = attribute.Name;
            Type        = attribute.Type;
            Width       = attribute.Width;
            Description = attribute.Description;
            EnumType    = attribute.EnumType;
            EnumMask    = attribute.EnumMask;
            Default     = attribute.Default;
        }
    }

    /// <summary>
    /// One opcode an author can use, with everything needed to offer it as a block: what to call it,
    /// what it does, and one entry per input it takes.
    /// </summary>
    internal sealed class FieldOpCodeInfo
    {
        public FieldScriptOpCode OpCode        { get; }
        public string            ScriptName    { get; }
        public ArgumentLayout    Layout        { get; }
        public string            Summary       { get; }
        public bool              OpensBlock    { get; }
        public bool              StopsInit     { get; }
        public bool              NeedsBody     { get; }
        public bool              TargetsBody   { get; }
        public bool              NeedsAnimator { get; }
        public string            Package       { get; }

        /// <summary>One of the game's own, from <c>0x8000</c> up, run by the handler the game added for it.</summary>
        public bool IsGame => FieldScriptOpCodeRanges.IsGameOpCode((ushort)OpCode);

        public IReadOnlyList<FieldArgumentInfo> Arguments { get; }

        internal FieldOpCodeInfo(FieldScriptOpCode opCode, FieldOpCodeAttribute attribute, FieldArgumentInfo[] arguments)
        {
            OpCode        = opCode;
            ScriptName    = attribute.ScriptName;
            Layout        = attribute.Layout;
            Summary       = attribute.Summary;
            OpensBlock    = attribute.OpensBlock;
            StopsInit     = attribute.StopsInit;
            NeedsBody     = attribute.NeedsBody;
            TargetsBody   = attribute.TargetsBody;
            NeedsAnimator = attribute.NeedsAnimator;
            Package       = attribute.Package;
            Arguments     = arguments;
        }
    }

    /// <summary>
    /// Every opcode an author can use, read from the attributes on <see cref="FieldScriptOpCode" /> and on any game's own
    /// enum of opcodes, from <c>0x8000</c> up.
    /// <br /><br />
    /// The enums are the single source of truth for what the engine and the game have. This just reads them, so a block
    /// palette, the compiler and a disassembler all describe the same opcodes without any of them keeping a second list
    /// in step. An opcode with no <see cref="FieldOpCodeAttribute" /> is a declaration of intent and is not offered.
    /// </summary>
    internal sealed class FieldOpCodeCatalogue
    {
        // The words the compiler reads before it looks an opcode up, which an opcode's own name would never reach.
        private static readonly string[] m_CompilerWords = { "LABEL", "ELSE", "END_IF" };

        private static FieldOpCodeCatalogue m_Project;

        private readonly Type[]                                        m_Tables;
        private readonly Dictionary<FieldScriptOpCode, FieldOpCodeInfo> m_ByOpCode;
        private readonly Dictionary<string, FieldOpCodeInfo>            m_ByScriptName;
        private readonly FieldOpCodeInfo[]                              m_All;

        /// <summary>The project's: the framework's opcodes and those of every enum a player builds.</summary>
        public static FieldOpCodeCatalogue Project => m_Project ??= new FieldOpCodeCatalogue(FieldOpCodeTables.Find(false));

        public IReadOnlyList<FieldOpCodeInfo> All => m_All;

        /// <summary>The opcodes these enums describe; the first a script name is described by wins.</summary>
        internal FieldOpCodeCatalogue(Type[] tables)
        {
            m_Tables       = tables;
            m_ByOpCode     = new Dictionary<FieldScriptOpCode, FieldOpCodeInfo>();
            m_ByScriptName = new Dictionary<string, FieldOpCodeInfo>();

            List<FieldOpCodeInfo> all = new List<FieldOpCodeInfo>();

            for (int t = 0; t < tables.Length; t++)
            {
                if (Enum.GetUnderlyingType(tables[t]) != typeof(ushort))
                {
                    continue;
                }

                FieldInfo[] fields = tables[t].GetFields(BindingFlags.Public | BindingFlags.Static);

                for (int i = 0; i < fields.Length; i++)
                {
                    FieldInfo field = fields[i];

                    FieldOpCodeAttribute opCodeAttribute = field.GetCustomAttribute<FieldOpCodeAttribute>();

                    if (opCodeAttribute == null)
                    {
                        continue;
                    }

                    List<ArgumentAttribute> argumentAttributes = new List<ArgumentAttribute>(field.GetCustomAttributes<ArgumentAttribute>());

                    // Sorted rather than taken as they come: GetCustomAttributes does not promise an order,
                    // and the order is what turns a block's inputs into correct bytecode.
                    argumentAttributes.Sort((a, b) => a.Index.CompareTo(b.Index));

                    FieldArgumentInfo[] arguments = new FieldArgumentInfo[argumentAttributes.Count];

                    for (int j = 0; j < argumentAttributes.Count; j++)
                    {
                        arguments[j] = new FieldArgumentInfo(argumentAttributes[j]);
                    }

                    FieldScriptOpCode opCode = (FieldScriptOpCode)(ushort)field.GetRawConstantValue();
                    FieldOpCodeInfo   info   = new FieldOpCodeInfo(opCode, opCodeAttribute, arguments);

                    all.Add(info);
                    m_ByOpCode.TryAdd(opCode, info);
                    m_ByScriptName.TryAdd(opCodeAttribute.ScriptName ?? string.Empty, info);
                }
            }

            m_All = all.ToArray();
        }

        public bool TryGet(FieldScriptOpCode opCode, out FieldOpCodeInfo info)
        {
            return m_ByOpCode.TryGetValue(opCode, out info);
        }

        public bool TryGet(string scriptName, out FieldOpCodeInfo info)
        {
            return m_ByScriptName.TryGetValue(scriptName, out info);
        }

        /// <summary>
        /// Problems that would make the catalogue unusable: a duplicate script name or value, argument indices that are not
        /// 0..n-1, a width where none belongs, or an enum argument whose number the encoding cannot carry. Argument order
        /// decides how a block's inputs become bytecode, so a gap or a repeat there is not something to discover at
        /// runtime. A game's opcodes must also be its own range's, sequential, and take only arguments its handler can
        /// read.
        /// </summary>
        public string[] Validate()
        {
            List<string>               problems    = new List<string>();
            Dictionary<string, string> scriptNames = new Dictionary<string, string>();
            Dictionary<ushort, string> values      = new Dictionary<ushort, string>();

            for (int t = 0; t < m_Tables.Length; t++)
            {
                Type table    = m_Tables[t];
                bool isEngine = table == typeof(FieldScriptOpCode);

                if (Enum.GetUnderlyingType(table) != typeof(ushort))
                {
                    problems.Add($"{table.Name} describes opcodes but is not a ushort enum: declare it 'enum {table.Name} : ushort'");
                    continue;
                }

                FieldInfo[] fields = table.GetFields(BindingFlags.Public | BindingFlags.Static);

                for (int i = 0; i < fields.Length; i++)
                {
                    ValidateMember(fields[i], $"{table.Name}.{fields[i].Name}", isEngine, problems, scriptNames, values);
                }
            }

            return problems.ToArray();
        }

        private static void ValidateMember(FieldInfo field, string member, bool isEngine, List<string> problems, Dictionary<string, string> scriptNames, Dictionary<ushort, string> values)
        {
            FieldOpCodeAttribute opCodeAttribute = field.GetCustomAttribute<FieldOpCodeAttribute>();
            ArgumentAttribute[]  arguments       = (ArgumentAttribute[])field.GetCustomAttributes<ArgumentAttribute>();

            if (opCodeAttribute == null)
            {
                if (arguments.Length > 0)
                {
                    problems.Add($"'{member}' declares arguments but has no {nameof(FieldOpCodeAttribute)}, so nothing will offer it");
                }

                return;
            }

            ushort value  = (ushort)field.GetRawConstantValue();
            bool   isGame = FieldScriptOpCodeRanges.IsGameOpCode(value);

            if (isEngine && isGame)
            {
                problems.Add($"'{member}' is 0x{value:X4}, in the range a game's opcodes use: the framework's are below 0x{FieldScriptOpCodeRanges.GAME_BASE:X4}");
            }
            else if (!isEngine && !isGame)
            {
                problems.Add($"'{member}' is 0x{value:X4}, among the framework's opcodes: a game's start at 0x{FieldScriptOpCodeRanges.GAME_BASE:X4}");
            }

            if (values.TryGetValue(value, out string valueOwner))
            {
                problems.Add($"'{member}' and '{valueOwner}' are both 0x{value:X4}");
            }
            else
            {
                values.Add(value, member);
            }

            if (string.IsNullOrWhiteSpace(opCodeAttribute.ScriptName))
            {
                problems.Add($"'{member}' has a blank scriptName");
            }
            else if (scriptNames.TryGetValue(opCodeAttribute.ScriptName, out string owner))
            {
                problems.Add($"'{member}' and '{owner}' both use the scriptName {opCodeAttribute.ScriptName}");
            }
            else if (Array.IndexOf(m_CompilerWords, opCodeAttribute.ScriptName) >= 0)
            {
                problems.Add($"'{member}' uses the scriptName {opCodeAttribute.ScriptName}, which the compiler reads as its own word");
            }
            else
            {
                scriptNames.Add(opCodeAttribute.ScriptName, member);
            }

            if (!isEngine)
            {
                if (opCodeAttribute.Layout != ArgumentLayout.Sequential)
                {
                    problems.Add($"'{member}' is {opCodeAttribute.Layout}: a game's opcodes are {nameof(ArgumentLayout.Sequential)}");
                }

                if (opCodeAttribute.OpensBlock)
                {
                    problems.Add($"'{member}' opens a block, which only the framework's IF opcodes do");
                }
            }

            bool[] seen = new bool[arguments.Length];

            for (int j = 0; j < arguments.Length; j++)
            {
                ArgumentAttribute argument = arguments[j];

                // The editor offers an enum's names but writes the number, so the number has to be one the
                // argument's encoding can carry.
                if (argument.EnumType != null && (!argument.EnumType.IsEnum || argument.Type != ArgumentType.Byte && argument.Type != ArgumentType.UShort))
                {
                    problems.Add($"'{member}' argument '{argument.Name}' names {argument.EnumType.Name}, which must be an enum on a {nameof(ArgumentType.Byte)} or {nameof(ArgumentType.UShort)} argument");
                }

                if (argument.EnumMask != 0 && (argument.EnumType == null || !argument.EnumType.IsDefined(typeof(FlagsAttribute), false)))
                {
                    problems.Add($"'{member}' argument '{argument.Name}' has an {nameof(ArgumentAttribute.EnumMask)}, which only a {nameof(FlagsAttribute)} enum's argument can have");
                }

                if (!isEngine && !ArgumentTypes.TryGetGameRead(argument.Type, argument.Width, out _))
                {
                    problems.Add(argument.Type == ArgumentType.Variable
                                     ? $"'{member}' argument '{argument.Name}' is a {argument.Width} variable: a game's opcode writes a Bool, Byte, UShort, Int or Float"
                                     : $"'{member}' argument '{argument.Name}' is a {argument.Type}, which a game's opcode handler cannot read");
                }

                if (argument.Index < 0 || argument.Index >= arguments.Length)
                {
                    problems.Add($"'{member}' argument '{argument.Name}' has index {argument.Index}, outside 0..{arguments.Length - 1}");
                    continue;
                }

                if (seen[argument.Index])
                {
                    problems.Add($"'{member}' has two arguments at index {argument.Index}");
                }

                seen[argument.Index] = true;

                bool needsWidth = argument.Type == ArgumentType.Variable || argument.Type == ArgumentType.Value;

                if (needsWidth != argument.HasWidth)
                {
                    problems.Add(needsWidth
                                     ? $"'{member}' argument '{argument.Name}' is a {argument.Type} with no width"
                                     : $"'{member}' argument '{argument.Name}' declares a width, which only {nameof(ArgumentType.Variable)} and {nameof(ArgumentType.Value)} use");
                }
            }
        }
    }
}
