using System.Threading;

namespace TestTaskSolution;

public static class Server
{
    private static int _count;
    private static readonly ReaderWriterLockSlim CountLock = new(LockRecursionPolicy.NoRecursion);

    public static int GetCount()
    {
        CountLock.EnterReadLock();
        try
        {
            return _count;
        }
        finally
        {
            CountLock.ExitReadLock();
        }
    }

    public static void AddToCount(int value)
    {
        CountLock.EnterWriteLock();
        try
        {
            checked
            {
                _count += value;
            }
        }
        finally
        {
            CountLock.ExitWriteLock();
        }
    }

    
    public static void ResetForTests(int value = 0)
    {
        CountLock.EnterWriteLock();
        try
        {
            _count = value;
        }
        finally
        {
            CountLock.ExitWriteLock();
        }
    }
}
