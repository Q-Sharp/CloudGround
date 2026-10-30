namespace CloudGround.App.Integration.Tests;

public static class AppHostStartLock
{
    private static readonly string LockFile = Path.Combine(Path.GetTempPath(), "cloudground-app-apphost-start.lock");
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(250);

    public static Task<FileStream> AcquireAsync(CancellationToken cancellation) => AcquireAsync(LockFile, cancellation);

    public static async Task<FileStream> AcquireAsync(string lockFile, CancellationToken cancellation)
    {
        while (true)
        {
            try
            {
                return new FileStream(lockFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {
                await Task.Delay(RetryDelay, cancellation);
            }
        }
    }
}
