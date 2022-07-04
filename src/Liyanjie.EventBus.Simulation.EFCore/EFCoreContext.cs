
using Microsoft.EntityFrameworkCore;

namespace Liyanjie.EventBus;

/// <summary>
/// 
/// </summary>
public class EFCoreContext : DbContext
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="options"></param>
    public EFCoreContext(DbContextOptions<EFCoreContext> options)
        : base(options) { }

    /// <summary>
    /// 
    /// </summary>
    public DbSet<EFCoreEvent> Events { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="modelBuilder"></param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var entity = modelBuilder.Entity<EFCoreEvent>();
        entity.HasKey(_ => _.Id);
        entity.HasIndex(_ => _.Name);
        entity.HasIndex(_ => _.IsHandled);
    }
}
