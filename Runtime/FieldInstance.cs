using System.Collections.Generic;

namespace RPGFramework.Field
{
    internal sealed class FieldInstance
    {
        internal readonly FieldFileHeader Header;
        internal readonly byte[]          ScriptData;

        private readonly Dictionary<ushort, FieldScriptInfo> m_Scripts;
        private readonly List<FieldEntity>                   m_Entities;
        private readonly Dictionary<ushort, bool>            m_Flags;
        private readonly Dictionary<ushort, int>             m_Vars;

        internal IReadOnlyList<FieldEntity> Entities => m_Entities;

        internal FieldInstance(FieldFileHeader header, byte[] scriptData, Dictionary<ushort, FieldScriptInfo> scripts)
        {
            Header     = header;
            ScriptData = scriptData;
            m_Scripts  = scripts;
            m_Entities = new List<FieldEntity>();
            m_Flags    = new Dictionary<ushort, bool>();
            m_Vars     = new Dictionary<ushort, int>();
        }

        internal FieldScriptInfo GetScript(ushort scriptId) => m_Scripts[scriptId];

        internal void CreateEntity(ushort entityId, FieldEntityScripts scripts)
        {
            FieldScriptInfo initScript = GetScript(scripts.InitScript);

            FieldVmContext ctx = new FieldVmContext(ScriptData, (int)initScript.Offset, this);

            m_Entities.Add(new FieldEntity(entityId, scripts, ctx));
        }

        internal bool GetFlag(ushort id) => m_Flags.TryGetValue(id, out bool v) && v;

        internal void SetFlag(ushort id) => m_Flags[id] = true;

        internal void ClearFlag(ushort id) => m_Flags[id] = false;

        internal int GetVar(ushort id) => m_Vars.GetValueOrDefault(id, 0);

        internal void SetVar(ushort id, int value) => m_Vars[id] = value;

        internal void AddVar(ushort id, int delta) => m_Vars[id] = GetVar(id) + delta;
    }
}