using System;

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
        /// Arguments written in order, each at its type's fixed width.
        /// </summary>
        Literal,

        /// <summary>
        /// A sources byte, a destination address, then one argument that is either an immediate or a
        /// second bank address.
        /// </summary>
        BankBinary,

        /// <summary>
        /// A sources byte using only its high nibble, then a destination address. For opcodes that read
        /// and write one variable, such as an increment.
        /// </summary>
        BankUnary,

        /// <summary>
        /// A sources byte, two arguments, a comparison type and a jump distance.
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

        /// <summary>A byte-wide variable being written to. Contributes a nibble to the source's byte.</summary>
        Variable8,

        /// <summary>A 16 bit variable being written to.</summary>
        Variable16,

        /// <summary>A byte-wide value: either an immediate or a variable to read.</summary>
        Value8,

        /// <summary>A 16 bit value: either an immediate or a variable to read.</summary>
        Value16,

        /// <summary>A 32 bit value: either an immediate or a variable to read.</summary>
        ValueInt,

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
        public int          Index { get; }
        public string       Name  { get; }
        public ArgumentType Type  { get; }

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
    }
}