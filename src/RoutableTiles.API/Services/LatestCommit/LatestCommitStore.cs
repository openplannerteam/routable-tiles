using Microsoft.EntityFrameworkCore;
using RoutableTiles.API.Db;
using RoutableTiles.API.Db.Tables;

namespace RoutableTiles.API.Services.LatestCommit;

public class LatestCommitStore
{
    private SnapshotCommit? _commit = null;

    public async Task<SnapshotCommit?> GetLatest(OsmDbContext context)
    {
        if (_commit == null)
        {
            _commit = await context.SnapshotCommits
                .OrderByDescending(x => x.Sequence)
                .FirstOrDefaultAsync();
        }

        return _commit;
    }

    public async Task Refresh(OsmDbContext context)
    {
        _commit = await context.SnapshotCommits
            .OrderByDescending(x => x.Sequence)
            .FirstOrDefaultAsync();
    }
}