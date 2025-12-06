using Microsoft.Data.Sqlite;

namespace Happy_Apps_Core.DataAccess;

public static class DatabaseExtensions
{
    /// <summary>
    /// Enables tracing of SQL statements executed on the specified SQLite connection using a custom trace delegate.
    /// </summary>
    /// <param name="connection">The SQLite connection on which to enable tracing. Cannot be null.</param>
    /// <param name="action">A delegate that is invoked for each SQL statement executed. Pass <c>null</c> to remove trace event. There is no need to remove the trace event if closing the connection.</param>
    public static void Trace(this SqliteConnection connection, SQLitePCL.strdelegate_trace action)
    {
        if (connection.Handle == null) return;
        SQLitePCL.raw.sqlite3_trace(connection.Handle, action, connection);
    }

    /// <summary>
    /// Enables tracing of SQL statements executed on the specified SQLite connection using a custom trace delegate.
    /// </summary>
    /// <param name="connection">The SQLite connection on which to enable tracing. Cannot be null.</param>
    /// <param name="action">A delegate that is invoked for each SQL statement executed. Pass <c>null</c> to remove trace event.</param>
    public static void Update(this SqliteConnection connection, SQLitePCL.delegate_update action)
    {
        if (connection.Handle == null) return;
        SQLitePCL.raw.sqlite3_update_hook(connection.Handle, action, connection);
    }
}
