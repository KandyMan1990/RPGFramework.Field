namespace RPGFramework.Field
{
    internal sealed class FieldEntityRuntime
    {
        internal int  EntityId { get; }
        internal bool IsActive { get; private set; }

        private int  m_CurrentScriptId;
        private bool m_ScriptRequested;

        internal FieldEntityRuntime(int entityId, int initScriptId)
        {
            EntityId          = entityId;
            m_CurrentScriptId = initScriptId;
            IsActive          = true;
            m_ScriptRequested = true;
        }

        internal void RequestScript(int scriptId)
        {
            m_CurrentScriptId = scriptId;
            m_ScriptRequested = true;
        }

        internal void Update(FieldVM vm)
        {
            if (!IsActive || !m_ScriptRequested)
            {
                return;
            }

            vm.Execute(EntityId, m_CurrentScriptId, this);
        }

        internal void OnScriptFinished()
        {
            m_ScriptRequested = false;
        }
    }
}