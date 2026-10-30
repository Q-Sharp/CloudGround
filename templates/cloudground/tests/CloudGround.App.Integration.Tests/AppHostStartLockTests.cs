namespace CloudGround.App.Integration.Tests;

public sealed class AppHostStartLockTests
{
    private static readonly TimeSpan StillWaiting = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan Released = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task A_second_start_waits_until_the_first_releases_the_lock()
    {
        var cancellation = TestContext.Current.CancellationToken;
        var lockFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.lock");
        var first = await AppHostStartLock.AcquireAsync(lockFile, cancellation);

        var second = AppHostStartLock.AcquireAsync(lockFile, cancellation);
        await Task.Delay(StillWaiting, cancellation);
        Assert.False(second.IsCompleted);

        await first.DisposeAsync();
        await (await second.WaitAsync(Released, cancellation)).DisposeAsync();
        File.Delete(lockFile);
    }
}
