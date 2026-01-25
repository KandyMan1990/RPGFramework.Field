using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using RPGFramework.Audio;
using RPGFramework.Core;
using RPGFramework.Core.Data;
using RPGFramework.Core.Input;
using RPGFramework.Core.PlayerLoop;
using RPGFramework.Core.SaveDataService;
using RPGFramework.Core.SharedTypes;
using RPGFramework.DI;
using RPGFramework.Field.Helpers;
using RPGFramework.Field.SharedTypes;
using RPGFramework.Field.StreamingAssetLoader;
using RPGFramework.Menu.SharedTypes;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RPGFramework.Field
{
    public class FieldModule : IFieldModule, IUpdatable, IInputContext
    {
        private struct FieldFileInfo
        {
            public readonly long Offset;
            public readonly int  Length;

            public FieldFileInfo(long offset, int length)
            {
                Offset = offset;
                Length = length;
            }
        }

        private const byte MAGIC_0 = (byte)'F';
        private const byte MAGIC_1 = (byte)'I';
        private const byte MAGIC_2 = (byte)'D';
        private const byte MAGIC_3 = (byte)'X';

        private static readonly byte[] m_FieldIndexMagic =
        {
                MAGIC_0,
                MAGIC_1,
                MAGIC_2,
                MAGIC_3
        };

        private readonly ICoreModule       m_CoreModule;
        private readonly IDIResolver       m_DIResolver;
        private readonly IMenuTypeProvider m_MenuTypeProvider;
        private readonly ISaveDataService  m_SaveDataService;
        private readonly IMusicPlayer      m_MusicPlayer;
        private readonly ISfxPlayer        m_SfxPlayer;
        private readonly IInputRouter      m_InputRouter;

        private InputAdapter m_InputAdapter;

        private readonly Dictionary<ulong, FieldFileInfo> m_FieldTableOfContents;

        private FieldVm       m_Vm;
        private FieldInstance m_Field;

        public FieldModule(ICoreModule       coreModule,
                           IDIResolver       diResolver,
                           IMenuTypeProvider menuTypeProvider,
                           ISaveDataService  saveDataService,
                           IMusicPlayer      musicPlayer,
                           ISfxPlayer        sfxPlayer,
                           IInputRouter      inputRouter)
        {
            m_CoreModule       = coreModule;
            m_DIResolver       = diResolver;
            m_MenuTypeProvider = menuTypeProvider;
            m_SaveDataService  = saveDataService;
            m_MusicPlayer      = musicPlayer;
            m_SfxPlayer        = sfxPlayer;
            m_InputRouter      = inputRouter;

            m_FieldTableOfContents = new Dictionary<ulong, FieldFileInfo>();
        }

        async Task IModule.OnEnterAsync(IModuleArgs args)
        {
            await LoadFieldInfos();

            m_InputAdapter = Object.FindFirstObjectByType<InputAdapter>();
            m_DIResolver.InjectInto(m_InputAdapter);

            ulong fieldId = ((IFieldModuleArgs)args).GetFieldId;

            //await LoadFieldAsync(fieldId);

            FieldFileHeader header = new FieldFileHeader
                                     {
                                             Version = 1
                                     };

            byte[] script = BuildPhase4Test(out uint idleOffset);
            Dictionary<ushort, FieldScriptInfo> scripts = new Dictionary<ushort, FieldScriptInfo>
                                                          {
                                                                  { 0, new FieldScriptInfo(0, 0,          idleOffset) },
                                                                  { 1, new FieldScriptInfo(1, idleOffset, (uint)(script.Length - idleOffset)) }
                                                          };

            m_Field = new FieldInstance(header, script, scripts);

            m_Field.CreateEntity(0,
                                 new FieldEntityScripts
                                 {
                                         InitScript = 0,
                                         IdleScript = 1
                                 });

            m_Vm = new FieldVm(m_MusicPlayer, m_SfxPlayer);
            m_InputAdapter.Enable();

            UpdateManager.RegisterUpdatable(this);

            m_InputRouter.Push(this);
        }

        Task IModule.OnExitAsync()
        {
            m_InputRouter.Pop(this);

            UpdateManager.UnregisterUpdatable(this);

            m_InputAdapter.Disable();

            m_CoreModule.ResetModule<IFieldModule, FieldModule>();

            return Task.CompletedTask;
        }

        Task IFieldModule.LoadMenuModuleAsync(byte menuId)
        {
            Type            type = m_MenuTypeProvider.GetType(menuId);
            IMenuModuleArgs args = new MenuModuleArgs(type);

            return m_CoreModule.LoadModuleAsync<IMenuModule>(args);
        }

        void IUpdatable.Update()
        {
            foreach (FieldEntity entity in m_Field.Entities)
            {
                entity.Update(m_Vm, Time.deltaTime);
            }
        }

        private void SetResumeData(RuntimeResumeData data)
        {
            SaveSection<RuntimeResumeData> newData = new SaveSection<RuntimeResumeData>(1, data);

            m_SaveDataService.SetSection(FrameworkSaveSectionDatabase.RESUME_DATA, newData);
        }

        private async Task LoadFieldInfos()
        {
            IStreamingAssetLoader assetLoader = StreamingAssetLoaderProvider.Get();

            string path = HelperFunctions.CombinePath(Application.streamingAssetsPath, "Field", "Field.idx");

            byte[] bytes = await assetLoader.LoadAsync(path);

            using BinaryReader br = new BinaryReader(new MemoryStream(bytes));

            byte[] magic = br.ReadBytes(4);
            for (int i = 0; i < magic.Length; i++)
            {
                if (magic[i] != m_FieldIndexMagic[i])
                {
                    throw new InvalidDataException($"{nameof(IFieldModule)}::{nameof(LoadFieldInfos)} Incorrect File Format");
                }
            }

            int fieldCount = br.ReadInt32();
            m_FieldTableOfContents.Clear();

            for (int i = 0; i < fieldCount; i++)
            {
                ulong hash   = br.ReadUInt64();
                int   offset = br.ReadInt32();
                int   length = br.ReadInt32();

                FieldFileInfo fieldFileInfo = new FieldFileInfo(offset, length);

                m_FieldTableOfContents.Add(hash, fieldFileInfo);
            }
        }

        private async Task LoadFieldAsync(ulong hash)
        {
            FieldFileInfo fieldFileInfo = m_FieldTableOfContents[hash];

            string path = HelperFunctions.CombinePath(Application.streamingAssetsPath, "Field", "Field.bin");

            await using FileStream fieldFileStream       = new FileStream(path, FileMode.Open, FileAccess.Read);
            using BinaryReader     fieldFileBinaryReader = new BinaryReader(fieldFileStream);

            fieldFileBinaryReader.BaseStream.Seek(fieldFileInfo.Offset, SeekOrigin.Begin);
            byte[] fieldBytes = fieldFileBinaryReader.ReadBytes(fieldFileInfo.Length);

            using MemoryStream fieldMemoryStream = new MemoryStream(fieldBytes);
            using BinaryReader fieldBinaryReader = new BinaryReader(fieldMemoryStream);
            // field script info (entities, their scripts)
            // TODO: read in a FieldScriptInfo
            // navigation (2d walk mesh equivalent)
            // TODO: read in a FieldNavigationInfo
            // encounter data (data TBD)
            // TODO: read in an EncounterTableInfo
            // background (tiles in V1, tileId, position)
            // TODO: read in a BackgroundInfo

            // old way of doing things
            // uint scriptCount = fieldFileBinaryReader.ReadUInt32();
            //
            // Dictionary<ushort, FieldScriptInfo> scripts = new Dictionary<ushort, FieldScriptInfo>();
            //
            // for (int i = 0; i < scriptCount; i++)
            // {
            //     ushort id     = fieldFileBinaryReader.ReadUInt16();
            //     uint   offset = fieldFileBinaryReader.ReadUInt32();
            //     uint   length = fieldFileBinaryReader.ReadUInt32();
            //
            //     scripts[id] = new FieldScriptInfo
            //                   {
            //                           ScriptId = id,
            //                           Offset   = offset,
            //                           Length   = length
            //                   };
            // }
            //
            // byte[] scriptData = fieldFileBinaryReader.ReadBytes((int)(header.ScriptBlockSize - (ms.Position - header.ScriptBlockOffset)));
            //
            // return new FieldInstance(header, scriptData, scripts);
        }

        private static byte[] BuildTestScript(out uint idleOffset, out uint script2Offset)
        {
            using MemoryStream ms = new MemoryStream();
            using BinaryWriter bw = new BinaryWriter(ms);

            bw.Write((ushort)FieldScriptOpcode.PlayBgm);
            bw.Write(0);

            bw.Write((ushort)FieldScriptOpcode.WaitSeconds);
            bw.Write(2.0f);

            bw.Write((ushort)FieldScriptOpcode.End);

            idleOffset = (uint)ms.Position;
            long idleStart = ms.Position;

            bw.Write((ushort)FieldScriptOpcode.WaitSeconds);
            bw.Write(1.0f);

            bw.Write((ushort)FieldScriptOpcode.Call);
            bw.Write((ushort)2);

            bw.Write((ushort)FieldScriptOpcode.Jump);
            bw.Write((int)(idleStart - (ms.Position + sizeof(int))));

            script2Offset = (uint)ms.Position;

            bw.Write((ushort)FieldScriptOpcode.PlaySfx);
            bw.Write(4);

            bw.Write((ushort)FieldScriptOpcode.End);

            return ms.ToArray();
        }

        private static byte[] BuildPhase4Test(out uint idleOffset)
        {
            using MemoryStream ms = new();
            using BinaryWriter bw = new(ms);

            // init
            bw.Write((ushort)FieldScriptOpcode.PlayBgm);
            bw.Write(0);

            bw.Write((ushort)FieldScriptOpcode.SetVar);
            bw.Write((ushort)0);
            bw.Write(0);

            bw.Write((ushort)FieldScriptOpcode.End);

            idleOffset = (uint)ms.Position;
            long idleStart = ms.Position;

            bw.Write((ushort)FieldScriptOpcode.AddVar);
            bw.Write((ushort)0);
            bw.Write(1);

            bw.Write((ushort)FieldScriptOpcode.IfVarEqual);
            bw.Write((ushort)0);
            bw.Write(60);
            bw.Write(sizeof(ushort) + sizeof(ushort) + sizeof(int)); // jump over WaitForSeconds, Jump, new position

            // Yield to prevent freeze
            bw.Write((ushort)FieldScriptOpcode.Yield);

            bw.Write((ushort)FieldScriptOpcode.Jump);
            bw.Write((int)(idleStart - (ms.Position + sizeof(int))));

            bw.Write((ushort)FieldScriptOpcode.PlaySfx);
            bw.Write(4);

            bw.Write((ushort)FieldScriptOpcode.End);

            return ms.ToArray();
        }

        bool IInputContext.Handle(ControlSlot slot)
        {
            if (slot == ControlSlot.Tertiary)
            {
                byte menuType = (byte)MenuType.Config;

                ((IFieldModule)this).LoadMenuModuleAsync(menuType).FireAndForget();

                return true;
            }

            return false;
        }
    }
}