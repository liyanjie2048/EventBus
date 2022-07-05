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
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
    public EFContext(string nameOrConnectionString)
#pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
        : base(nameOrConnectionString) { }

    public IDbSet<EFChannel> Channels { get; set; }

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

        var entity_Channel = modelBuilder.Entity<EFChannel>();
        entity_Channel.Property(_ => _.Name).HasMaxLength(200);
        entity_Channel.HasKey(_ => _.Name);

        var entity_Event = modelBuilder.Entity<EFEvent>();
        entity_Event.Property(_ => _.Channel).HasMaxLength(200);
        entity_Event.HasKey(_ => _.Id);
        entity_Event.HasIndex(_ => _.Channel);
    }
}
