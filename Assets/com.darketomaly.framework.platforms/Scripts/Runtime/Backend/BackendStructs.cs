using System.Collections.Generic;

namespace Framework.Platforms
{
    public enum State
    {
        Uninitialized,
        LoggingIn,
        FetchingTitleNews,
        Error
    }

    [System.Serializable]
    public class UserData
    {
        public List<int> m_SavesSent;

        public void Validate()
        {
            if (m_SavesSent == null)
            {
                m_SavesSent = new List<int>();
            }
        }
    }

    [System.Serializable]
    public class PersistentUserData
    {
        public bool m_Developer;
    }
    
    public struct TitleNews
    {
        public string m_Alert;
        public string m_FeedbackLink;
    }
}