namespace RPGFramework.Field.FieldVmArgs
{
    /// <summary>
    /// A question for the player: its text and answers by key, the answer its cursor starts on, and the one cancel
    /// moves it to — the dialogue flow's none for none.
    /// </summary>
    internal readonly struct QuestionArgs
    {
        internal readonly byte    Channel;
        internal readonly ulong   QuestionId;
        internal readonly ulong[] AnswerIds;
        internal readonly byte    StartingAnswer;
        internal readonly byte    CancelAnswer;

        internal QuestionArgs(byte channel, ulong questionId, ulong[] answerIds, byte startingAnswer, byte cancelAnswer)
        {
            Channel        = channel;
            QuestionId     = questionId;
            AnswerIds      = answerIds;
            StartingAnswer = startingAnswer;
            CancelAnswer   = cancelAnswer;
        }
    }
}
