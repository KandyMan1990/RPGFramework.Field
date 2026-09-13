using UnityEngine;

namespace RPGFramework.Field
{
    [CreateAssetMenu(menuName = "RPG Framework/Field/Compiled Script", fileName = "FieldCompiledScript")]
    public sealed class FieldCompiledScript : ScriptableObject
    {
        /// <summary>
        /// The bytecode layout this build of the framework reads.<br /><br />
        /// <b>Bump this whenever the bytes an opcode occupies change</b>
        /// </summary>
        public const uint CURRENT_FORMAT_VERSION = 1;

        [Tooltip("Stable identifier used by the VM")]
        public int ScriptId;

        [Tooltip("The bytecode layout this was compiled for. 0 means it predates the check and has to be recompiled")]
        public uint FormatVersion;

        [Tooltip("Compiled bytecode")]
        public byte[] Bytecode;
    }
}