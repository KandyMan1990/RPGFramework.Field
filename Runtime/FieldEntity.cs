namespace RPGFramework.Field
{
    internal sealed class FieldEntity
    {
        internal ushort EntityId { get; }

        private readonly FieldEntityScripts m_Scripts;

        private FieldVmContext m_VmContext;
        private bool           m_InitCompleted;

        internal FieldEntity(ushort entityId, FieldEntityScripts scripts, FieldVmContext vmContext)
        {
            EntityId        = entityId;
            m_Scripts       = scripts;
            m_VmContext     = vmContext;
            m_InitCompleted = false;
        }

        internal void Update(FieldVm vm, float deltaTime)
        {
            vm.Update(ref m_VmContext, deltaTime);

            if (!m_VmContext.IsActive)
            {
                if (!m_InitCompleted)
                {
                    m_InitCompleted = true;
                    StartScript(m_Scripts.InitScript);
                }
                else
                {
                    StartScript(m_Scripts.IdleScript);
                }
            }
        }

        private void StartScript(ushort scriptId)
        {
            FieldScriptInfo script = m_VmContext.Field.GetScript(scriptId);

            m_VmContext.InstructionPointer = (int)script.Offset;
            m_VmContext.IsActive           = true;
            m_VmContext.IsSuspended        = false;
            m_VmContext.WaitSeconds        = 0f;

            m_VmContext.ResetBlockingState();
        }
    }
}