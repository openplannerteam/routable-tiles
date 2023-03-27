using System.Collections.Concurrent;
using RoutableTiles.API.Db.Tables;

namespace RoutableTiles.API.Db.Caches;

public class SnapshotCommitsByTimestampCache
{
    private readonly ConcurrentDictionary<DateTime, SnapshotCommit> _commits = new();

    public async Task<SnapshotCommit?> GetFor(DateTime dateTime)
    {
        if (!_commits.TryGetValue(dateTime.FloorMinute(), out var commit)) return null;
        
        return commit;
    }

    public void Set(SnapshotCommit snapshotCommit)
    {
        _commits.TryAdd(snapshotCommit.Timestamp.FloorMinute(), snapshotCommit);
    }
}