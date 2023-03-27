using Microsoft.EntityFrameworkCore;
using RoutableTiles.API.Db.Caches;
using RoutableTiles.API.Db.Conversions;
using RoutableTiles.API.Db.Tables;

namespace RoutableTiles.API.Db;

public class OsmDbContext : DbContext
{
    public OsmDbContext(DbContextOptions<OsmDbContext> options,
        SnapshotCommitIdsCache snapshotCommitIdsCache, SnapshotCommitsByTimestampCache snapshotCommitsByTimestampCache) : base(options)
    {
        this.SnapshotCommitIdsCache = snapshotCommitIdsCache;
        SnapshotCommitsByTimestampCache = snapshotCommitsByTimestampCache;
    }

    public SnapshotCommitIdsCache SnapshotCommitIdsCache { get; }
    
    public SnapshotCommitsByTimestampCache SnapshotCommitsByTimestampCache { get; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SnapshotNode>()
            .HasKey(c => new { c.Id, c.Version, c.CommitId });
        modelBuilder.Entity<SnapshotWay>()
            .HasKey(c => new { c.Id, c.Version, c.CommitId });
        modelBuilder.Entity<SnapshotWayNodes>()
            .HasKey(c => new { c.SnapshotWayId, c.SnapshotWayVersion, c.SnapshotWayCommitId, c.Index });
        modelBuilder.Entity<SnapshotRelation>()
            .HasKey(c => new { c.Id, c.Version, c.CommitId });
        modelBuilder.Entity<SnapshotRelationMember>()
            .HasKey(c => new { c.SnapshotRelationId, c.SnapshotRelationVersion, c.SnapshotRelationCommitId, c.Order });
        modelBuilder.Entity<SnapshotRelationMember>()
            .Property(x => x.MemberType)
            .HasConversion<int>(new OsmGeoTypeValueConverter());

        modelBuilder.Entity<SnapshotWay>()
            .HasMany(sw => sw.SnapshotWayNodes)
            .WithOne(swn => swn.Way)
            .HasForeignKey(x => new { x.SnapshotWayId, x.SnapshotWayVersion, x.SnapshotWayCommitId });

        modelBuilder.Entity<SnapshotRelation>()
            .HasMany(r => r.Members)
            .WithOne(m => m.SnapshotRelation)
            .HasForeignKey(x => new { x.SnapshotRelationId, x.SnapshotRelationVersion, x.SnapshotRelationCommitId });
    }

    public DbSet<Snapshot> Snapshots { get; set; } = null!;
    public DbSet<SnapshotCommit> SnapshotCommits { get; set; } = null!;
    public DbSet<SnapshotCommitTiles> SnapshotCommitTiles { get; set; } = null!;
    public DbSet<SnapshotNode> SnapshotNodes { get; set; } = null!;
    public DbSet<SnapshotWay> SnapshotWays { get; set; } = null!;

    public DbSet<SnapshotWayNodes> SnapshotWayNodes { get; set; } = null!;
    public DbSet<SnapshotRelation> SnapshotRelations { get; set; } = null!;
    public DbSet<SnapshotRelationMember> SnapshotRelationMembers { get; set; } = null!;
}