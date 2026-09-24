using System.Collections.Generic;
using UnityEngine;

namespace RPGFramework.Field
{
    /// <summary>
    /// Every entity in the field, kept on the field prefab's root: its id, its scripts, and its body when it has one.
    /// <br /><br />
    /// The prefab being edited holds the scripts as text. Exporting compiles them into a copy of the prefab — the one
    /// that is bundled and loaded — with the text and names removed, so bytecode is never kept where it could fall
    /// behind the text it came from.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FieldEntities : MonoBehaviour
    {
        /// <summary>
        /// The bytecode layout this build of the framework reads. <b>Bump it whenever the bytes an opcode occupies
        /// change</b>, so a bundle built by an older framework is refused rather than read as garbage.
        /// </summary>
        internal const uint BYTECODE_FORMAT_VERSION = 1;

        [SerializeField]
        [Tooltip("Whether this field is built in 3D or 2D. Entities made here follow it: which kind of collider and rigidbody they get")]
        private FieldDimension m_Dimension;

        [SerializeField]
        private List<FieldEntityRecord> m_Entities = new List<FieldEntityRecord>();

        [SerializeField]
        [HideInInspector]
        private uint m_FormatVersion;

        [SerializeField]
        [HideInInspector]
        private List<CompiledFieldEntity> m_Compiled = new List<CompiledFieldEntity>();

        [SerializeField]
        [HideInInspector]
        private List<string> m_AnimationNames = new List<string>();

        internal FieldDimension                     Dimension      => m_Dimension;
        internal List<FieldEntityRecord>            Entities       => m_Entities;
        internal IReadOnlyList<CompiledFieldEntity> Compiled       => m_Compiled;
        internal uint                               FormatVersion  => m_FormatVersion;
        internal IReadOnlyList<string>              AnimationNames => m_AnimationNames;

        /// <summary>
        /// Export only: replace the authored records with what they compiled to.
        /// </summary>
        internal void SetCompiled(List<CompiledFieldEntity> compiled, List<string> animationNames)
        {
            m_Compiled       = compiled;
            m_AnimationNames = animationNames;
            m_FormatVersion  = BYTECODE_FORMAT_VERSION;

            m_Entities.Clear();
        }
    }
}
