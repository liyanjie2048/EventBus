using System.Data.Entity;

namespace Liyanjie.EventBus;

/// <summary>
/// 
/// </summary>
public class EFContext : DbContext
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="nameOrConnectionString"></param>
    public EFContext(string nameOrConnectionString)
        : base(nameOrConnectionString) { }

    /// <summary>
    /// 
    /// </summary>
    public IDbSet<EFEvent> Events { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="modelBuilder"></param>
    protected override void OnModelCreating(DbModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var entity = modelBuilder.Entity<EFEvent>();
        entity.HasKey(_ => _.Id);
        entity.HasIndex(_ => _.Name);
        entity.HasIndex(_ => _.IsHandled);
    }
}
