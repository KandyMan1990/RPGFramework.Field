using System.Collections.Generic;
using RPGFramework.Field.Editor;

namespace RPGFramework.Field.Tests.Catalogue
{
    /// <summary>One choice for every picked argument, a spawn point's id or a name, so a new block compiles.</summary>
    internal sealed class OneChoiceEach : IArgumentChoiceProvider
    {
        private static readonly string[] m_Spawn = { "0" };
        private static readonly string[] m_Name  = { "Name" };

        IReadOnlyList<string> IArgumentChoiceProvider.ChoicesFor(FieldScriptBlock block, int argumentIndex)
        {
            return block.OpCode.Arguments[argumentIndex].Type == ArgumentType.SpawnId ? m_Spawn : m_Name;
        }
    }
}
