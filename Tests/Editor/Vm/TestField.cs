using RPGFramework.Core;
using RPGFramework.Core.SharedTypes;

namespace RPGFramework.Field.Tests.Vm
{
    /// <summary>
    /// A field's VM over memory of its own. Scripts and entities are registered as a loaded field registers them, and
    /// each entity is run a frame at a time, as the field module runs it.
    /// </summary>
    internal sealed class TestField
    {
        internal const int TEMP_BYTES = 64;

        private int m_NextScriptId;
        private int m_NextEntityId;

        internal TestField()
        {
            Memory = new FakeMemoryService(1024, 256);
            Vm     = new FieldVM(Memory, TEMP_BYTES);

            m_NextScriptId = 1;
            m_NextEntityId = 100;
        }

        internal IMemoryService Memory { get; }
        internal FieldVM        Vm     { get; }

        internal int Add(Script script)
        {
            int scriptId = m_NextScriptId++;

            Vm.RegisterScript(scriptId, FieldEntities.BYTECODE_FORMAT_VERSION, script.ToArray());

            return scriptId;
        }

        /// <summary>
        /// An entity whose events run these scripts, each in the slot given for it. Event 0's, its init, starts in
        /// Main.
        /// </summary>
        internal FieldEntityRuntime AddEntity(int entityId, byte[] slotsByEvent, params int[] scriptIdsByEvent)
        {
            FieldEntityRuntime entity = new FieldEntityRuntime(entityId, scriptIdsByEvent, slotsByEvent);

            Vm.RegisterEntity(entityId, entity);

            return entity;
        }

        /// <summary>An entity running the script in Main.</summary>
        internal FieldEntityRuntime Start(Script script)
        {
            return AddEntity(m_NextEntityId++, new byte[1], Add(script));
        }

        /// <summary>Runs the script in Main on an entity of its own, for one frame.</summary>
        internal FieldEntityRuntime Run(Script script)
        {
            FieldEntityRuntime entity = Start(script);

            Tick(entity);

            return entity;
        }

        /// <summary>How many times a <see cref="Script.Count" /> at <paramref name="address" /> has run.</summary>
        internal int CountAt(ushort address)
        {
            return Memory.ReadInt(MemoryBank.Persistent, address);
        }

        internal void Tick(FieldEntityRuntime entity, int frames = 1)
        {
            for (int i = 0; i < frames; i++)
            {
                entity.Update(Vm);
            }
        }
    }
}
