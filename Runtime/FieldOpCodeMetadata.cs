using System;
using RPGFramework.Core.Memory;

namespace RPGFramework.Field
{
    // A note on the words used here, since they come from assembly language and are not obvious:
    //
    //   opcode      the numeric id of an instruction - what the VM switches on
    //   script name the text an author writes for it, such as ADD_BYTE
    //   argument    a value the instruction takes, exactly like a function parameter
    //   layout      how those arguments are packed into bytes
    //
    // So an opcode is a function, its arguments are its parameters, the script name is what you type to
    // call it, and the layout is the calling convention.

    /// <summary>
    /// How an opcode's arguments are laid out in bytecode. The enum's argument list says what an author
    /// supplies; the layout says how those values become bytes.
    /// </summary>
    public enum ArgumentLayout
    {
        /// <summary>
        /// Arguments written in order. Every argument that can come from a variable is preceded by a sources
        /// byte shared with the next such argument, high nibble first, and is written as an immediate at its
        /// type's width or as a ushort bank address.
        /// </summary>
        Sequential,

        /// <summary>
        /// A sources byte, a destination address, then one argument: an immediate at the argument's width, or
        /// the width of the variable being read followed by its address.
        /// </summary>
        BankBinary,

        /// <summary>
        /// A sources byte using only its high nibble, then a destination address. For opcodes that read
        /// and write one variable, such as an increment.
        /// </summary>
        BankUnary,

        /// <summary>
        /// A sources byte, two arguments encoded as in <see cref="BankBinary" />, a comparison type and an int
        /// jump distance.
        /// </summary>
        BankCompare,

        /// <summary>
        /// A sources byte using only its low nibble, then one argument. For opcodes that read a value
        /// without writing one back.
        /// </summary>
        BankSeed
    }

    /// <summary>
    /// What one argument is — both how it encodes and what an authoring tool should offer for it. The
    /// semantic types encode identically to their underlying width but tell the field editor to show a
    /// dropdown rather than a bare number field.
    /// </summary>
    public enum ArgumentType
    {
        Byte,
        UShort,
        Int,
        Float,
        Bool,

        /// <summary>A jump relative to the next instruction. Always literal, so the compiler can check where it lands.</summary>
        JumpDistance,

        /// <summary>A jump to an absolute position in the script. Always literal, for the same reason.</summary>
        JumpTarget,

        /// <summary>An entity in the current field.</summary>
        EntityId,

        /// <summary>A script's index within its entity.</summary>
        EventId,

        /// <summary>A priority slot, 0-7.</summary>
        Priority,

        /// <summary>A field, authored by name and encoded as that name's 64-bit hash.</summary>
        FieldName,

        /// <summary>A music track, authored by asset name and encoded as that name's 64-bit hash.</summary>
        MusicName,

        /// <summary>A sound effect, authored by asset name and encoded as that name's 64-bit hash.</summary>
        SoundName,

        /// <summary>A music stem state, authored by name and encoded as that name's 64-bit hash.</summary>
        MusicStateName,

        /// <summary>
        /// A music track named only so that tooling knows whose stem states to offer. <b>Authored into the
        /// script text and never emitted to bytecode.</b><br /><br />
        /// <c>MUSIC_STEM_STATE</c> applies to whatever is playing, so the bytecode has no business
        /// carrying a track. Without the author saying which one they have in mind, though, the only
        /// honest list of states is the union across every track, which includes states the one that
        /// matters does not have. This narrows it, and costs a word of script text.
        /// </summary>
        MusicNameHint,

        /// <summary>
        /// A spawn point in the field being jumped to, encoded as the id on its
        /// <c>SpawnPoint</c> component. Which spawn points exist depends on the field named by the
        /// argument before it, so tooling has to read that argument to offer these.
        /// </summary>
        SpawnId,

        /// <summary>A localisation key, authored as text and encoded as its 64-bit hash.</summary>
        LocalisationKey,

        /// <summary>A variable-length run of localisation keys, preceded by a count byte.</summary>
        LocalisationKeyList,

        /// <summary>A variable being written to, of exactly the argument's width.</summary>
        Variable,

        /// <summary>
        /// A value at the argument's width: an immediate, or a variable no wider than it — see
        /// <see cref="ArgumentTypes.CanRead" />.
        /// </summary>
        Value,

        /// <summary>How an IF compares its two values — see <see cref="ScriptComparison" />.</summary>
        Comparison
    }

    /// <summary>
    /// Marks an opcode as one an author can use, and gives the field editor what it needs to offer it:
    /// the scriptName the compiler accepts and a description for the block.<br /><br />
    /// An opcode without this attribute is a declaration of intent — it has no handler and no encoder,
    /// and the editor will not show it.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class FieldOpCodeAttribute : Attribute
    {
        public string         ScriptName { get; }
        public ArgumentLayout Layout     { get; }

        /// <summary>
        /// One line describing what the opcode does, shown on its block. Optional where the scriptName and
        /// arguments already say it.
        /// </summary>
        public string Summary { get; set; }

        /// <summary>
        /// True when the opcode opens a block that other instructions sit inside, such as an <c>IF</c>.
        /// The editor nests those instructions under it, and the compiler works out how far to jump to
        /// skip them — so a jump distance is never something an author writes.
        /// </summary>
        public bool OpensBlock { get; set; }

        public FieldOpCodeAttribute(string scriptName, ArgumentLayout layout)
        {
            ScriptName = scriptName;
            Layout     = layout;
        }
    }

    /// <summary>
    /// One argument of an opcode, in the order it is written.<br /><br />
    /// <see cref="Index" /> is explicit rather than relying on the order attributes come back in:
    /// <c>GetCustomAttributes</c> does not guarantee an order, and argument order is what turns a block's
    /// inputs into correct bytecode.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public sealed class ArgumentAttribute : Attribute
    {
        public int           Index    { get; }
        public string        Name     { get; }
        public ArgumentType  Type     { get; }
        public VariableWidth Width    { get; }
        public bool          HasWidth { get; }

        /// <summary>
        /// What the argument means, where the name alone is not enough — a range, or what a value of zero
        /// does. Shown as a tooltip on the input.
        /// </summary>
        public string Description { get; set; }

        public ArgumentAttribute(int index, string name, ArgumentType type)
        {
            Index = index;
            Name  = name;
            Type  = type;
        }

        /// <summary>
        /// For <see cref="ArgumentType.Variable" /> and <see cref="ArgumentType.Value" />, which take their width from here.
        /// </summary>
        public ArgumentAttribute(int index, string name, ArgumentType type, VariableWidth width)
        {
            Index    = index;
            Name     = name;
            Type     = type;
            Width    = width;
            HasWidth = true;
        }
    }

    public static class ArgumentTypes
    {
        /// <summary>
        /// Whether an argument of this type can be written as a variable, and the variable width it needs.
        /// <paramref name="declaredWidth" /> is the width an <see cref="ArgumentType.Variable" /> or
        /// <see cref="ArgumentType.Value" /> argument declares.
        /// </summary>
        public static bool TryGetVariableWidth(ArgumentType type, VariableWidth declaredWidth, out VariableWidth width)
        {
            switch (type)
            {
                case ArgumentType.Variable:
                case ArgumentType.Value:
                    width = declaredWidth;
                    return true;

                case ArgumentType.Bool:
                    width = VariableWidth.Bool;
                    return true;

                case ArgumentType.Byte:
                case ArgumentType.EntityId:
                case ArgumentType.Priority:
                    width = VariableWidth.Byte;
                    return true;

                case ArgumentType.UShort:
                case ArgumentType.EventId:
                    width = VariableWidth.UShort;
                    return true;

                case ArgumentType.Int:
                case ArgumentType.SpawnId:
                    width = VariableWidth.Int;
                    return true;

                case ArgumentType.Float:
                    width = VariableWidth.Float;
                    return true;

                default:
                    width = default;
                    return false;
            }
        }

        /// <summary>
        /// Whether a <see cref="ArgumentLayout.Sequential" /> argument of this type carries a source nibble.
        /// The bank layouts encode their own sources, so their variable types are not included.
        /// </summary>
        public static bool TakesSource(ArgumentType type)
        {
            bool takesSource = type != ArgumentType.Variable &&
                               type != ArgumentType.Value    &&
                               TryGetVariableWidth(type, default, out _);

            return takesSource;
        }

        /// <summary>
        /// Whether a <see cref="ArgumentType.Value" /> of <paramref name="argumentWidth" /> can read a variable of
        /// <paramref name="variableWidth" />. A bool reads only a bool. An integer reads any integer no wider than
        /// itself, converted as a cast would convert it. A float reads a float or an integer of up to four bytes.
        /// </summary>
        public static bool CanRead(VariableWidth argumentWidth, VariableWidth variableWidth)
        {
            bool canRead = argumentWidth switch
                           {
                               VariableWidth.Bool  => variableWidth == VariableWidth.Bool,
                               VariableWidth.Float => variableWidth == VariableWidth.Float ||
                                                      (variableWidth.IsInteger() && variableWidth.GetByteCount() <= 4),
                               _                   => variableWidth.IsInteger() && variableWidth.GetByteCount() <= argumentWidth.GetByteCount()
                           };

            return canRead;
        }
    }
}
