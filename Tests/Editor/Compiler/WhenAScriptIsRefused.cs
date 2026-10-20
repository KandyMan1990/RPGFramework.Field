using System;
using NUnit.Framework;
using RPGFramework.Field.Editor;

namespace RPGFramework.Field.Tests.Compiler
{
    /// <summary>A script the compiler cannot write is refused, saying what is wrong and where.</summary>
    internal sealed class WhenAScriptIsRefused
    {
        [TestCase("YIELD\nGOTO_JUMP -10",                         "not a byte offset")]
        [TestCase("YIELD\nGOTO_JUMP Nowhere",                     "no LABEL in this script names")]
        [TestCase("LABEL Twice\nYIELD\nLABEL Twice\nGOTO_JUMP Twice", "already in this script")]
        [TestCase("GOTO_JUMP End\nYIELD\nLABEL End",              "nothing after it to run")]
        [TestCase("LABEL 3rd\nRETURN",                            "LABEL needs one name")]
        public void ThenAJumpThatCannotLandSaysWhy(string source, string why)
        {
            StringAssert.Contains(why, Refusal(source));
        }

        [TestCase("SHOW_DIALOGUE_WINDOW 0",                          "missing its localisation key")]
        [TestCase("SHOW_DIALOGUE_WINDOW",                            "missing its dialogue channel")]
        [TestCase("CREATE_DIALOGUE_WINDOW 8 0 0 100 100",            "dialogue channel from 0 to 7")]
        [TestCase("SET_DIALOGUE_WINDOW_STYLE 0 Whisper",             "needs a window style")]
        [TestCase("SET_DIALOGUE_WINDOW_STYLE 0 1",                   "needs a window style")]
        [TestCase("SET_MESSAGE_VARIABLE 8 1",                        "message variable from 0 to 7")]
        [TestCase("ASK_PLAYER_TO_MAKE_A_CHOICE $answer question",    "at least one answer")]
        public void ThenDialogueItCannotShowSaysWhy(string source, string why)
        {
            StringAssert.Contains(why, Refusal(source));
        }

        [TestCase("REQUEST_SCRIPT 3 7 12",   "takes 2 argument(s)")]
        [TestCase("START_BATTLE 0 3 512 15", "includes DISABLE_GAME_OVER_ON_DEFEAT, which START_BATTLE does not read")]
        [TestCase("SET_BATTLE_MODE 513",     "includes DISABLE_ESCAPE, which SET_BATTLE_MODE does not read")]
        public void ThenAnArgumentItCannotUseSaysWhy(string source, string why)
        {
            StringAssert.Contains(why, Refusal(source));
        }

        [TestCase("IF_BYTE 1 == 1\nYIELD",           "no END_IF")]
        [TestCase("YIELD\nEND_IF",                   "no IF open")]
        [TestCase("IF_BYTE 1 ~~ 1\nEND_IF",          "is not a comparison")]
        [TestCase("IF_BYTE 1 == 1",                  "no END_IF")]
        [TestCase("IF_BOOL true > false\nEND_IF",    "only compare with == or !=")]
        [TestCase("IF_BOOL 1 == true\nEND_IF",       "expected true, false or a $variable")]
        [TestCase("IF_BYTE 256 == 1\nEND_IF",        "a whole number from 0 to 255")]
        [TestCase("IF_BYTE -1 == 1\nEND_IF",         "a whole number from 0 to 255")]
        [TestCase("IF_FLOAT 1 bit_set 2\nEND_IF",    "cannot test bits")]
        [TestCase("ELSE\nRETURN",                    "ELSE with no IF open")]
        [TestCase("IF_BYTE 1 == 1\nELSE\nELSE\nEND_IF", "a second ELSE")]
        [TestCase("IF_BYTE 1 == 1\nELSE\nYIELD",     "no END_IF")]
        public void ThenABlockItCannotCloseSaysWhy(string source, string why)
        {
            StringAssert.Contains(why, Refusal(source));
        }

        [TestCase("PLAY_MUSIC \"Music Sample AllStems",            "line 1: the quote before [Music Sample AllStems] is never closed")]
        [TestCase("CROSSFADE_MUSIC Music Sample AllStems 2.5 0.25", "double quotes")]
        public void ThenANameItCannotReadSaysHowToWriteIt(string source, string why)
        {
            StringAssert.Contains(why, Refusal(source));
        }

        private static string Refusal(string source)
        {
            Exception refused = Assert.Catch(() => FieldScriptCompiler.Compile(source));

            return refused.Message;
        }
    }
}
