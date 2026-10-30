namespace CloudGround.App.Integration.Tests;

public sealed class AppHostStartLockTests
{
    private static readonly TimeSpan StillWaiting = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan Released = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task A_second_start_waits_until_the_first_releases_the_lock()
    {
        var cancellation = TestContext.Current.CancellationToken;
        var first = await AppHostStartLock.AcquireAsync(cancellation);

        var second = AppHostStartLock.AcquireAsync(cancellation);
        await Task.Delay(StillWaiting, cancellation);
        Assert.False(second.IsCompleted);

        await first.DisposeAsync();
        await using var acquired = await second.WaitAsync(Released, cancellation);
    }
}
