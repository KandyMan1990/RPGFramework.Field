using System.Collections.Generic;
using System.Reflection;
using RPGFramework.Core.Memory;

namespace RPGFramework.Field.Editor
{
    /// <summary>
    /// One argument of an opcode, resolved from its attribute.
    /// </summary>
    public sealed class FieldArgumentInfo
    {
        public string        Name        { get; }
        public ArgumentType  Type        { get; }
        public VariableWidth Width       { get; }
        public string        Description { get; }

        internal FieldArgumentInfo(ArgumentAttribute attribute)
        {
            Name        = attribute.Name;
            Type        = attribute.Type;
            Width       = attribute.Width;
            Description = attribute.Description;
        }
    }

    /// <summary>
    /// One opcode an author can use, with everything needed to offer it as a block: what to call it,
    /// what it does, and one entry per input it takes.
    /// </summary>
    public sealed class FieldOpCodeInfo
    {
        public FieldScriptOpCode OpCode     { get; }
        public string            ScriptName { get; }
        public ArgumentLayout    Layout     { get; }
        public string            Summary    { get; }

        /// <summary>
        /// True when instructions nest inside this one, such as an <c>IF</c>.
        /// </summary>
        public bool OpensBlock { get; }

        public IReadOnlyList<FieldArgumentInfo> Arguments { get; }

        internal FieldOpCodeInfo(FieldScriptOpCode opCode, FieldOpCodeAttribute attribute, List<FieldArgumentInfo> arguments)
        {
            OpCode     = opCode;
            ScriptName = attribute.ScriptName;
            Layout     = attribute.Layout;
            Summary    = attribute.Summary;
            OpensBlock = attribute.OpensBlock;
            Arguments  = arguments;
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
    public static class FieldOpCodeCatalogue
    {
        private static Dictionary<FieldScriptOpCode, FieldOpCodeInfo> s_ByOpCode;
        private static Dictionary<string, FieldOpCodeInfo>            s_ByScriptName;
        private static List<FieldOpCodeInfo>                          s_All;

        public static IReadOnlyList<FieldOpCodeInfo> All
        {
            get
            {
                EnsureBuilt();

                return s_All;
            }
        }

        public static bool TryGet(FieldScriptOpCode opCode, out FieldOpCodeInfo info)
        {
            EnsureBuilt();

            bool found = s_ByOpCode.TryGetValue(opCode, out info);

            return found;
        }

        public static bool TryGet(string scriptName, out FieldOpCodeInfo info)
        {
            EnsureBuilt();

            bool found = s_ByScriptName.TryGetValue(scriptName, out info);

            return found;
        }

        /// <summary>
        /// Problems that would make the catalogue unusable: a duplicate scriptName, or argument indices
        /// that are not 0..n-1. Argument order decides how a block's inputs become bytecode, so a gap or
        /// a repeat there is not something to discover at runtime.
        /// </summary>
        public static string[] Validate()
        {
            List<string>               problems    = new List<string>();
            Dictionary<string, string> scriptNames = new Dictionary<string, string>();

            foreach (FieldInfo field in typeof(FieldScriptOpCode).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
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

                foreach (ArgumentAttribute argument in arguments)
                {
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
            if (s_All != null)
            {
                return;
            }

            s_All          = new List<FieldOpCodeInfo>();
            s_ByOpCode     = new Dictionary<FieldScriptOpCode, FieldOpCodeInfo>();
            s_ByScriptName = new Dictionary<string, FieldOpCodeInfo>();

            foreach (FieldInfo field in typeof(FieldScriptOpCode).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                FieldOpCodeAttribute opCodeAttribute = field.GetCustomAttribute<FieldOpCodeAttribute>();

                if (opCodeAttribute == null)
                {
                    continue;
                }

                List<ArgumentAttribute> argumentAttributes = new List<ArgumentAttribute>(field.GetCustomAttributes<ArgumentAttribute>());

                // Sorted rather than taken as they come: GetCustomAttributes does not promise an order,
                // and the order is what turns a block's inputs into correct bytecode.
                argumentAttributes.Sort((a, b) => a.Index.CompareTo(b.Index));

                List<FieldArgumentInfo> arguments = new List<FieldArgumentInfo>(argumentAttributes.Count);

                foreach (ArgumentAttribute argumentAttribute in argumentAttributes)
                {
                    arguments.Add(new FieldArgumentInfo(argumentAttribute));
                }

                FieldScriptOpCode opCode = (FieldScriptOpCode)field.GetRawConstantValue();
                FieldOpCodeInfo   info   = new FieldOpCodeInfo(opCode, opCodeAttribute, arguments);

                s_All.Add(info);
                s_ByOpCode[opCode]                         = info;
                s_ByScriptName[opCodeAttribute.ScriptName] = info;
            }
        }
    }
}