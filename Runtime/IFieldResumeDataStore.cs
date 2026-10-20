namespace RPGFramework.Field
{
    /// <summary>
    /// What the field keeps of itself across a battle, which rebuilds it: set as the field leaves, taken as it enters.
    /// </summary>
    public interface IFieldResumeDataStore
    {
        /// <summary>Takes what was kept, emptying the store; the default when nothing was.</summary>
        T Get<T>();

        /// <summary>Keeps the data until the next <see cref="Get{T}" />, replacing anything kept before.</summary>
        void Set<T>(T data);
    }

    public sealed class FieldResumeDataStore : IFieldResumeDataStore
    {
        private object m_Data;

        T IFieldResumeDataStore.Get<T>()
        {
            T data = (T)m_Data;
            m_Data = null;

            return data;
        }

        void IFieldResumeDataStore.Set<T>(T data)
        {
            m_Data = data;
        }
    }
}