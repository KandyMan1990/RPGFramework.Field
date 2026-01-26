namespace RPGFramework.Field
{
    internal sealed class FieldEntityRuntime
    {
        private readonly byte[] m_EntityVars;

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
            m_EntityVars      = new byte[256];
        }

        internal void RequestScript(int scriptId)
        {
            m_CurrentScriptId = scriptId;
            m_ScriptRequested = true;
        }

        internal void Update(FieldVM vm)
        {
            if (!IsActive || !m_ScriptRequested)
                return;

            vm.Execute(EntityId, m_CurrentScriptId, this);
        }

        internal void Shutdown()
        {
            IsActive = false;
        }

        internal void OnScriptFinished()
        {
            m_ScriptRequested = false;
        }
        
        internal byte GetVar(byte offset)             => m_EntityVars[offset];
        internal void SetVar(byte offset, byte value) => m_EntityVars[offset] = value;
    }
}