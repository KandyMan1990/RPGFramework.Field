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
        public string        Default     { get; }

        internal FieldArgumentInfo(ArgumentAttribute attribute)
        {
            Name        = attribute.Name;
            Type        = attribute.Type;
            Width       = attribute.Width;
            Description = attribute.Description;
            EnumType    = attribute.EnumType;
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
        public bool              NeedsAnimator { get; }
        public string            Package       { get; }

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
            NeedsAnimator = attribute.NeedsAnimator;
            Package       = attribute.Package;
            Arguments     = arguments;
        }
    }

    /// <summary>
    /// Every opcode an author can use, read from the attributes on <see cref="FieldScriptOpCode" />.
    /// <br /><br />
    /// The enum is the single source of truth for what the engine has. This just reads it, so a block
    /// palette, the compiler and a disassembler all describe the same opcodes without any of them
    /// keeping a second list in step. An opcode with no <see cref="FieldOpCodeAttribute" /> is a
    /// declaration of intent and is not offered.
    /// </summary>
    internal static class FieldOpCodeCatalogue
    {
        private static Dictionary<FieldScriptOpCode, FieldOpCodeInfo> m_ByOpCode;
        private static Dictionary<string, FieldOpCodeInfo>            m_ByScriptName;
        private static FieldOpCodeInfo[]                              m_All;

        public static IReadOnlyList<FieldOpCodeInfo> All
        {
            get
            {
                EnsureBuilt();

                return m_All;
            }
        }

        public static bool TryGet(FieldScriptOpCode opCode, out FieldOpCodeInfo info)
        {
            EnsureBuilt();

            bool found = m_ByOpCode.TryGetValue(opCode, out info);

            return found;
        }

        public static bool TryGet(string scriptName, out FieldOpCodeInfo info)
        {
            EnsureBuilt();

            bool found = m_ByScriptName.TryGetValue(scriptName, out info);

            return found;
        }

        /// <summary>
        /// Problems that would make the catalogue unusable: a duplicate script name, argument indices that are not
        /// 0..n-1, a width where none belongs, or an enum argument whose number the encoding cannot carry. Argument
        /// order decides how a block's inputs become bytecode, so a gap or a repeat there is not something to discover
        /// at runtime
        /// </summary>
        public static string[] Validate()
        {
            List<string>               problems    = new List<string>();
            Dictionary<string, string> scriptNames = new Dictionary<string, string>();

            FieldInfo[] fields = typeof(FieldScriptOpCode).GetFields(BindingFlags.Public | BindingFlags.Static);

            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo field = fields[i];

                FieldOpCodeAttribute opCodeAttribute = field.GetCustomAttribute<FieldOpCodeAttribute>();
                ArgumentAttribute[]  arguments       = (ArgumentAttribute[])field.GetCustomAttributes<ArgumentAttribute>();

                if (opCodeAttribute == null)
                {
                    if (arguments.Length > 0)
                    {
                        problems.Add($"'{field.Name}' declares arguments but has no {nameof(FieldOpCodeAttribute)}, so nothing will offer it");
                    }

                    continue;
                }

                if (string.IsNullOrWhiteSpace(opCodeAttribute.ScriptName))
                {
                    problems.Add($"'{field.Name}' has a blank scriptName");
                }
                else if (scriptNames.TryGetValue(opCodeAttribute.ScriptName, out string owner))
                {
                    problems.Add($"'{field.Name}' and '{owner}' both use the scriptName {opCodeAttribute.ScriptName}");
                }
                else
                {
                    scriptNames.Add(opCodeAttribute.ScriptName, field.Name);
                }

                bool[] seen = new bool[arguments.Length];

                for (int j = 0; j < arguments.Length; j++)
                {
                    ArgumentAttribute argument = arguments[j];

                    // The editor offers an enum's names but writes the number, so the number has to be one the
                    // argument's encoding can carry.
                    if (argument.EnumType != null && (!argument.EnumType.IsEnum || argument.Type != ArgumentType.Byte && argument.Type != ArgumentType.UShort))
                    {
                        problems.Add($"'{field.Name}' argument '{argument.Name}' names {argument.EnumType.Name}, which must be an enum on a {nameof(ArgumentType.Byte)} or {nameof(ArgumentType.UShort)} argument");
                    }

                    if (argument.Index < 0 || argument.Index >= arguments.Length)
                    {
                        problems.Add($"'{field.Name}' argument '{argument.Name}' has index {argument.Index}, outside 0..{arguments.Length - 1}");
                        continue;
                    }

                    if (seen[argument.Index])
                    {
                        problems.Add($"'{field.Name}' has two arguments at index {argument.Index}");
                    }

                    seen[argument.Index] = true;

                    bool needsWidth = argument.Type == ArgumentType.Variable || argument.Type == ArgumentType.Value;

                    if (needsWidth != argument.HasWidth)
                    {
                        problems.Add(needsWidth
                                         ? $"'{field.Name}' argument '{argument.Name}' is a {argument.Type} with no width"
                                         : $"'{field.Name}' argument '{argument.Name}' declares a width, which only {nameof(ArgumentType.Variable)} and {nameof(ArgumentType.Value)} use");
                    }
                }
            }

            return problems.ToArray();
        }

        private static void EnsureBuilt()
        {
            if (m_All != null)
            {
                return;
            }

            List<FieldOpCodeInfo> all = new List<FieldOpCodeInfo>();

            m_ByOpCode     = new Dictionary<FieldScriptOpCode, FieldOpCodeInfo>();
            m_ByScriptName = new Dictionary<string, FieldOpCodeInfo>();

            FieldInfo[] fields = typeof(FieldScriptOpCode).GetFields(BindingFlags.Public | BindingFlags.Static);

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
                    ArgumentAttribute argumentAttribute = argumentAttributes[j];

                    arguments[j] = new FieldArgumentInfo(argumentAttribute);
                }

                FieldScriptOpCode opCode = (FieldScriptOpCode)field.GetRawConstantValue();
                FieldOpCodeInfo   info   = new FieldOpCodeInfo(opCode, opCodeAttribute, arguments);

                all.Add(info);
                m_ByOpCode[opCode]                         = info;
                m_ByScriptName[opCodeAttribute.ScriptName] = info;
            }

            m_All = all.ToArray();
        }
    }
}