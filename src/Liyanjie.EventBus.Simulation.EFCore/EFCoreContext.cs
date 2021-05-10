using System;

using Microsoft.EntityFrameworkCore;

namespace Liyanjie.EventBus.Simulation.EFCore
{
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
        public DbSet<EFCoreEventWrapper> Events { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="modelBuilder"></param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            var entity = modelBuilder.Entity<EFCoreEventWrapper>();
            entity.HasKey(_ => _.Id);
            entity.HasIndex(_ => _.Name);
        }
    }
}
