using SQLite;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Rise.NewRepository
{
    internal static class SQLiteConnectionExtensions
    {
        public static int RemoveAll(this ISQLiteConnection connection, IEnumerable<object> items, bool runInTransaction = true)
        {
            int count = 0;

            if (runInTransaction)
            {
                connection.RunInTransaction(() =>
                {
                    foreach (var item in items)
                        count += connection.Delete(item);
                });
            }
            else
            {
                foreach (var item in items)
                    count += connection.Delete(item);
            }

            return count;
        }

        public static Task<int> RemoveAllAsync(this ISQLiteAsyncConnection connection, IEnumerable<object> items, bool runInTransaction = true)
            => connection.WriteAsync(conn => RemoveAll(conn, items, runInTransaction));

        public static int InsertOrReplaceAll(this ISQLiteConnection connection, IEnumerable<object> items, bool runInTransaction = true)
        {
            int count = 0;

            if (runInTransaction)
            {
                connection.RunInTransaction(() =>
                {
                    foreach (var item in items)
                        count += connection.InsertOrReplace(item);
                });
            }
            else
            {
                foreach (var item in items)
                    count += connection.InsertOrReplace(item);
            }

            return count;
        }

        public static Task<int> InsertOrReplaceAllAsync(this ISQLiteAsyncConnection connection, IEnumerable<object> items, bool runInTransaction = true)
            => connection.WriteAsync(conn => InsertOrReplaceAll(conn, items, runInTransaction));

        private static Task<T> WriteAsync<T>(this ISQLiteAsyncConnection connection, Func<SQLiteConnectionWithLock, T> write)
        {
            return Task.Factory.StartNew(() =>
            {
                SQLiteConnectionWithLock conn = connection.GetConnection();
                using (conn.Lock())
                    return write(conn);
            }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }
    }
}
