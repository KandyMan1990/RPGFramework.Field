using System.Collections.Generic;

namespace RPGFramework.Field.Editor
{
    /// <summary>What a block's picker offers for one of its arguments, each as it is written in the script.</summary>
    internal interface IArgumentChoiceProvider
    {
        IReadOnlyList<string> ChoicesFor(FieldScriptBlock block, int argumentIndex);
    }
}
