using System.Data.Entity;
using SQLite.CodeFirst;

namespace LGCallRecorder.Database
{
    public class MainModel : DbContext
    {
        public MainModel()
            : base("name=MainModel")
        {
        }

        public virtual DbSet<CallRecord> CallRecords { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            SqliteCreateDatabaseIfNotExists<MainModel> sqliteConnectionInitializer =
                new SqliteCreateDatabaseIfNotExists<MainModel>(modelBuilder);
            System.Data.Entity.Database.SetInitializer(sqliteConnectionInitializer);

            modelBuilder.Entity<CallRecord>().HasKey(x => new {x.StationNumber, x.Dialed, x.Start});
        }
    }
}