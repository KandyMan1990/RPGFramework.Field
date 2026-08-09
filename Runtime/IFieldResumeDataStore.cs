namespace RPGFramework.Field
{
    public interface IFieldResumeDataStore
    {
        T    Get<T>();
        void Set<T>(T data);
    }

    public class FieldResumeDataStore : IFieldResumeDataStore
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