namespace APILogger.Persistence
{
    public abstract class LogPersistence
    {
        public abstract void SaveLog( string messages, bool custom = false );
    }
}