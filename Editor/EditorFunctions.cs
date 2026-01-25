using System.IO;
using RPGFramework.Field.Editor.Helpers;
using UnityEditor;
using UnityEngine;

namespace RPGFramework.Field.Editor
{
    internal static class EditorFunctions
    {
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

        [MenuItem("RPG Framework/Test/Create Field Indices file")]
        public static void CreateFieldIndices()
        {
            string directory = Path.Combine(Application.streamingAssetsPath, "Field");
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string filePath = Path.Combine(directory, "Field.idx");

            using FileStream   fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            using BinaryWriter bw = new BinaryWriter(fs);

            bw.Write(m_FieldIndexMagic);

            // temp
            bw.Write(1); // field count
            ulong testFieldName = Fnv1a64.Hash("RPGFramework.TestField");
            bw.Write(testFieldName); // hash
            bw.Write(0);             // offset
            bw.Write(0);             // length

            bw.Flush();
        }
    }
}