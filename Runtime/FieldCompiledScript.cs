using UnityEngine;

namespace RPGFramework.Field
{
    [CreateAssetMenu(menuName = "RPG Framework/Field/Compiled Script", fileName = "FieldCompiledScript")]
    public sealed class FieldCompiledScript : ScriptableObject
    {
        [Tooltip("Stable identifier used by the VM")]
        public int ScriptId;

        [Tooltip("Compiled bytecode")]
        public byte[] Bytecode;
    }
}