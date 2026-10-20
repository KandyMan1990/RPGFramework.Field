using NUnit.Framework;
using RPGFramework.Field.Editor;

namespace RPGFramework.Field.Tests.Catalogue
{
    /// <summary>
    /// Export refuses an opcode where its flags say it cannot go, so a flag missing is a mistake export lets through.
    /// Anything that gains one of these behaviours is added here and to its attribute.
    /// </summary>
    internal sealed class WhenExportReadsAnOpcodesFlags
    {
        private static readonly string[] m_StopsInit =
        {
            "REQUEST_SCRIPT", "REQUEST_SCRIPT_WAIT_START", "REQUEST_SCRIPT_WAIT_END", "YIELD", "WAIT_SECONDS", "JUMP_TO_MAP",
            "SHOW_DIALOGUE_WINDOW", "SHOW_DIALOGUE_WINDOW_NO_WAIT", "WAIT_FOR_DIALOGUE_WINDOW", "ASK_PLAYER_TO_MAKE_A_CHOICE",
            "SET_ENTITY_ROTATION_OVER_TIME", "PLAY_ANIMATION_AND_WAIT", "PLAY_ANIMATION_AND_HOLD", "PLAY_PART_OF_ANIMATION_AND_WAIT",
            "PLAY_PART_OF_ANIMATION_AND_HOLD", "WAIT_FOR_ANIMATION", "MOVE_ENTITY_AND_WAIT", "GLIDE_ENTITY_AND_WAIT",
            "SLIDE_ENTITY_AND_WAIT", "MOVE_ENTITY_TO_ENTITY_AND_WAIT", "WAIT_FOR_MOVEMENT", "START_BATTLE", "OPEN_SAVE_MENU"
        };

        private static readonly string[] m_NeedsBody =
        {
            "SET_PLAYER_ENTITY", "VISIBILITY", "INTERACTION_TRIGGER_ACTIVATION", "SET_INTERACTION_RANGE", "SET_ENTITY_POSITION",
            "SET_MOVEMENT_SPEED", "SET_ENTITY_ROTATION", "SET_ENTITY_ROTATION_OVER_TIME", "SET_DIRECTION_TO_FACE_ENTITY",
            "COLLISION_TRIGGER_ACTIVATION", "SET_BASE_ANIMATION", "SET_ANIMATION_SPEED", "STOP_ANIMATION", "PLAY_ANIMATION_AND_WAIT",
            "PLAY_ANIMATION_AND_HOLD", "PLAY_ANIMATION", "PLAY_ANIMATION_HOLDING", "PLAY_ANIMATION_LOOPING",
            "PLAY_PART_OF_ANIMATION_AND_WAIT", "PLAY_PART_OF_ANIMATION_AND_HOLD", "PLAY_PART_OF_ANIMATION",
            "PLAY_PART_OF_ANIMATION_HOLDING", "PLAY_PART_OF_ANIMATION_LOOPING", "WAIT_FOR_ANIMATION", "PUSH_ANIMATION_STATE",
            "POP_ANIMATION_STATE", "MOVE_ENTITY_AND_WAIT", "MOVE_ENTITY", "GLIDE_ENTITY_AND_WAIT", "GLIDE_ENTITY",
            "SLIDE_ENTITY_AND_WAIT", "SLIDE_ENTITY", "MOVE_ENTITY_TO_ENTITY_AND_WAIT", "MOVE_ENTITY_TO_ENTITY", "WAIT_FOR_MOVEMENT",
            "STOP_MOVEMENT", "SOLIDITY", "PUSH_SCRIPT_ACTIVATION"
        };

        private static readonly string[] m_NeedsAnimator =
        {
            "SET_BASE_ANIMATION", "SET_ANIMATION_SPEED", "STOP_ANIMATION", "PLAY_ANIMATION_AND_WAIT", "PLAY_ANIMATION_AND_HOLD",
            "PLAY_ANIMATION", "PLAY_ANIMATION_HOLDING", "PLAY_ANIMATION_LOOPING", "PLAY_PART_OF_ANIMATION_AND_WAIT",
            "PLAY_PART_OF_ANIMATION_AND_HOLD", "PLAY_PART_OF_ANIMATION", "PLAY_PART_OF_ANIMATION_HOLDING",
            "PLAY_PART_OF_ANIMATION_LOOPING", "WAIT_FOR_ANIMATION", "PUSH_ANIMATION_STATE", "POP_ANIMATION_STATE"
        };

        [TestCaseSource(nameof(m_StopsInit))]
        public void ThenOneThatBlocksOrEndsTheFrameStopsInit(string scriptName)
        {
            Assert.IsTrue(Described(scriptName).StopsInit);
        }

        [TestCaseSource(nameof(m_NeedsBody))]
        public void ThenOneThatActsOnABodyNeedsOne(string scriptName)
        {
            Assert.IsTrue(Described(scriptName).NeedsBody);
        }

        [TestCaseSource(nameof(m_NeedsAnimator))]
        public void ThenOneThatDrivesAnimationNeedsAnAnimator(string scriptName)
        {
            Assert.IsTrue(Described(scriptName).NeedsAnimator);
        }

        private static FieldOpCodeInfo Described(string scriptName)
        {
            Assert.IsTrue(FieldOpCodeCatalogue.TryGet(scriptName, out FieldOpCodeInfo info), $"{scriptName} is not in the catalogue");

            return info;
        }
    }
}
