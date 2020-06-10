using Microsoft.EntityFrameworkCore;

namespace Framework
{
    public partial class drfingerprintsContext : DbContext, IdrfingerprintsContext
    {
        public drfingerprintsContext()
        {
        }

        public drfingerprintsContext(DbContextOptions<drfingerprintsContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Errors> Errors { get; set; }
        public virtual DbSet<Files> Files { get; set; }
        public virtual DbSet<FingerTaskQueue> FingerTaskQueue { get; set; }
        public virtual DbSet<Job> Job { get; set; }
        public virtual DbSet<LivestreamResults> LivestreamResults { get; set; }
        public virtual DbSet<OnDemandResults> OnDemandResults { get; set; }
        public virtual DbSet<RadioTaskQueue> RadioTaskQueue { get; set; }
        public virtual DbSet<Songs> Songs { get; set; }
        public virtual DbSet<Stations> Stations { get; set; }
        public virtual DbSet<Subfingerid> Subfingerid { get; set; }
        public virtual DbSet<TaskQueue> TaskQueue { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. See http://go.microsoft.com/fwlink/?LinkId=723263 for guidance on storing connection strings.
                optionsBuilder.UseMySql("server=10.101.183.183;port=3306;user=DBadmin;password=Passw0rd;database=drfingerprints;ConnectionTimeout=240;DefaultCommandTimeout=240;SslMode=None", x => x.EnableRetryOnFailure().ServerVersion("5.7.25-mysql"));
                
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Errors>(entity =>
            {
                entity.ToTable("errors");

                entity.Property(e => e.Id)
                    .HasColumnName("ID")
                    .HasColumnType("bigint(20)");

                entity.Property(e => e.ErrorMsg)
                    .HasColumnName("ERROR_MSG")
                    .HasColumnType("varchar(500)")
                    .HasCharSet("latin1")
                    .HasCollation("latin1_swedish_ci");

                entity.Property(e => e.JobId)
                    .HasColumnName("JOB_ID")
                    .HasColumnType("bigint(20)");

                entity.Property(e => e.LastUpdated)
                    .HasColumnName("LAST_UPDATED")
                    .HasColumnType("timestamp")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP")
                    .ValueGeneratedOnAddOrUpdate();
            });

            modelBuilder.Entity<Files>(entity =>
            {
                entity.ToTable("files");

                entity.Property(e => e.Id)
                    .HasColumnName("ID")
                    .HasColumnType("bigint(20)");

                entity.Property(e => e.Duration).HasColumnName("duration");

                entity.Property(e => e.FilePath)
                    .IsRequired()
                    .HasColumnName("FILE_PATH")
                    .HasColumnType("varchar(256)")
                    .HasCharSet("latin1")
                    .HasCollation("latin1_swedish_ci");

                entity.Property(e => e.FileType)
                    .IsRequired()
                    .HasColumnName("FILE_TYPE")
                    .HasColumnType("varchar(10)")
                    .HasCharSet("latin1")
                    .HasCollation("latin1_swedish_ci");

                entity.Property(e => e.Ref)
                    .HasColumnName("ref")
                    .HasColumnType("varchar(50)")
                    .HasCharSet("latin1")
                    .HasCollation("latin1_swedish_ci");
            });

            modelBuilder.Entity<FingerTaskQueue>(entity =>
            {
                entity.ToTable("finger_task_queue");

                entity.Property(e => e.Id)
                    .HasColumnName("ID")
                    .HasColumnType("bigint(20)");

                entity.Property(e => e.Arguments)
                    .IsRequired()
                    .HasColumnName("ARGUMENTS")
                    .HasColumnType("varchar(200)")
                    .HasCharSet("latin1")
                    .HasCollation("latin1_swedish_ci");

                entity.Property(e => e.JobId)
                    .HasColumnName("JOB_ID")
                    .HasColumnType("bigint(20)");

                entity.Property(e => e.LastUpdated)
                    .HasColumnName("LAST_UPDATED")
                    .HasColumnType("timestamp(6)")
                    .HasDefaultValueSql("'CURRENT_TIMESTAMP(6)'")
                    .ValueGeneratedOnAddOrUpdate();

                entity.Property(e => e.Machine)
                    .HasColumnName("MACHINE")
                    .HasColumnType("varchar(50)")
                    .HasDefaultValueSql("'None'")
                    .HasCharSet("latin1")
                    .HasCollation("latin1_swedish_ci");

                entity.Property(e => e.Started).HasColumnName("STARTED");

                entity.Property(e => e.TaskType)
                    .IsRequired()
                    .HasColumnName("TASK_TYPE")
                    .HasColumnType("varchar(50)")
                    .HasCharSet("latin1")
                    .HasCollation("latin1_swedish_ci");
            });

            modelBuilder.Entity<Job>(entity =>
            {
                entity.ToTable("job");

                entity.Property(e => e.Id)
                    .HasColumnName("ID")
                    .HasColumnType("bigint(20)");

                entity.Property(e => e.Arguments)
                    .HasColumnName("ARGUMENTS")
                    .HasColumnType("varchar(500)")
                    .HasCharSet("latin1")
                    .HasCollation("latin1_swedish_ci");

                entity.Property(e => e.FileId)
                    .HasColumnName("FILE_ID")
                    .HasColumnType("bigint(20)");

                entity.Property(e => e.JobType)
                    .IsRequired()
                    .HasColumnName("JOB_TYPE")
                    .HasColumnType("varchar(50)")
                    .HasCharSet("latin1")
                    .HasCollation("latin1_swedish_ci");

                entity.Property(e => e.LastUpdated)
                    .HasColumnName("LAST_UPDATED")
                    .HasColumnType("timestamp")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP")
                    .ValueGeneratedOnAddOrUpdate();

                entity.Property(e => e.Percentage).HasColumnName("PERCENTAGE");

                entity.Property(e => e.StartDate)
                    .HasColumnName("START_DATE")
                    .HasColumnType("timestamp")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP")
                    .ValueGeneratedOnAddOrUpdate();

                entity.Property(e => e.User)
                    .HasColumnName("user")
                    .HasColumnType("varchar(50)")
                    .HasCharSet("latin1")
                    .HasCollation("latin1_swedish_ci");

                entity.Property(e => e.StatusMessage)
                    .HasColumnName("STATUS_MESSAGE")
                    .HasColumnType("varchar(30)")
                    .HasCharSet("latin1")
                    .HasCollation("latin1_swedish_ci");
            });

            modelBuilder.Entity<LivestreamResults>(entity =>
            {
                entity.ToTable("livestream_results");

                entity.HasIndex(e => e.ChannelId)
                    .HasName("CHANNEL_ID");

                entity.HasIndex(e => e.SongId)
                    .HasName("SONG_ID");

                entity.Property(e => e.Id)
                    .HasColumnName("ID")
                    .HasColumnType("bigint(20)");

                entity.Property(e => e.Accuracy).HasColumnName("ACCURACY");

                entity.Property(e => e.ChannelId)
                    .IsRequired()
                    .HasColumnName("CHANNEL_ID")
                    .HasColumnType("varchar(19)")
                    .HasCharSet("latin1")
                    .HasCollation("latin1_swedish_ci");

                entity.Property(e => e.Duration)
                    .HasColumnName("DURATION")
                    .HasColumnType("int(11)");

                entity.Property(e => e.LastUpdated)
                    .HasColumnName("LAST_UPDATED")
                    .HasColumnType("timestamp")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP")
                    .ValueGeneratedOnAddOrUpdate();

                entity.Property(e => e.Offset)
                    .HasColumnName("OFFSET")
                    .HasColumnType("time");

                entity.Property(e => e.PlayDate)
                    .HasColumnName("PLAY_DATE")
                    .HasColumnType("timestamp")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP")
                    .ValueGeneratedOnAddOrUpdate();

                entity.Property(e => e.SongId)
                    .HasColumnName("SONG_ID")
                    .HasColumnType("int(11)");

                entity.Property(e => e.SongOffset).HasColumnName("song_offset");

                entity.HasOne(d => d.Channel)
                    .WithMany(p => p.LivestreamResults)
                    .HasForeignKey(d => d.ChannelId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("livestream_results_ibfk_2");

                entity.HasOne(d => d.Song)
                    .WithMany(p => p.LivestreamResults)
                    .HasForeignKey(d => d.SongId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("livestream_results_ibfk_1");
            });

            modelBuilder.Entity<OnDemandResults>(entity =>
            {
                entity.ToTable("on_demand_results");

                entity.HasIndex(e => e.FileId)
                    .HasName("FILE_ID");

                entity.HasIndex(e => e.SongId)
                    .HasName("SONG_ID");

                entity.Property(e => e.Id)
                    .HasColumnName("ID")
                    .HasColumnType("bigint(20)");

                entity.Property(e => e.Accuracy).HasColumnName("ACCURACY");

                entity.Property(e => e.Duration)
                    .HasColumnName("DURATION")
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("'-1'");

                entity.Property(e => e.FileId)
                    .HasColumnName("FILE_ID")
                    .HasColumnType("bigint(20)");

                entity.Property(e => e.LastUpdated)
                    .HasColumnName("LAST_UPDATED")
                    .HasColumnType("timestamp(6)")
                    .HasDefaultValueSql("'CURRENT_TIMESTAMP(6)'")
                    .ValueGeneratedOnAddOrUpdate();

                entity.Property(e => e.Offset)
                    .HasColumnName("OFFSET")
                    .HasColumnType("time");

                entity.Property(e => e.SongId)
                    .HasColumnName("SONG_ID")
                    .HasColumnType("int(11)");

                entity.Property(e => e.SongOffset).HasColumnName("song_offset");

                entity.HasOne(d => d.File)
                    .WithMany(p => p.OnDemandResults)
                    .HasForeignKey(d => d.FileId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("on_demand_results_ibfk_2");
            });

            modelBuilder.Entity<RadioTaskQueue>(entity =>
            {
                entity.ToTable("radio_task_queue");

                entity.Property(e => e.Id)
                    .HasColumnName("ID")
                    .HasColumnType("bigint(20)");

                entity.Property(e => e.ChannelId)
                    .IsRequired()
                    .HasColumnName("CHANNEL_ID")
                    .HasColumnType("varchar(50)")
                    .HasCharSet("latin1")
                    .HasCollation("latin1_swedish_ci");

                entity.Property(e => e.ChunkPath)
                    .IsRequired()
                    .HasColumnName("CHUNK_PATH")
                    .HasColumnType("varchar(200)")
                    .HasCharSet("latin1")
                    .HasCollation("latin1_swedish_ci");

                entity.Property(e => e.JobId)
                    .HasColumnName("JOB_ID")
                    .HasColumnType("bigint(20)");

                entity.Property(e => e.LastUpdated)
                    .HasColumnName("LAST_UPDATED")
                    .HasColumnType("timestamp(6)")
                    .HasDefaultValueSql("'CURRENT_TIMESTAMP(6)'")
                    .ValueGeneratedOnAddOrUpdate();

                entity.Property(e => e.Machine)
                    .HasColumnName("MACHINE")
                    .HasColumnType("varchar(50)")
                    .HasDefaultValueSql("'None'")
                    .HasCharSet("latin1")
                    .HasCollation("latin1_swedish_ci");

                entity.Property(e => e.Started).HasColumnName("STARTED");
            });

            modelBuilder.Entity<Songs>(entity =>
            {
                entity.ToTable("songs");

                entity.HasIndex(e => e.Reference)
                    .HasName("DK1_SONGS")
                    .IsUnique();

                entity.HasIndex(e => new { e.DrDiskoteksnr, e.Sidenummer, e.Sekvensnummer })
                    .HasName("DR_DISKOTEKSNR");

                entity.Property(e => e.Id)
                    .HasColumnName("ID")
                    .HasColumnType("int(11)");

                entity.Property(e => e.DateChanged)
                    .HasColumnName("DATE_CHANGED")
                    .HasColumnType("timestamp")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP")
                    .ValueGeneratedOnAddOrUpdate();

                entity.Property(e => e.DrDiskoteksnr)
                    .HasColumnName("DR_DISKOTEKSNR")
                    .HasColumnType("int(11)");

                entity.Property(e => e.Duration)
                    .HasColumnName("DURATION")
                    .HasColumnType("bigint(20)")
                    .HasDefaultValueSql("'-1'");

                entity.Property(e => e.Reference)
                    .IsRequired()
                    .HasColumnName("REFERENCE")
                    .HasColumnType("varchar(20)")
                    .HasCharSet("latin1")
                    .HasCollation("latin1_swedish_ci");

                entity.Property(e => e.Sekvensnummer)
                    .HasColumnName("SEKVENSNUMMER")
                    .HasColumnType("int(11)");

                entity.Property(e => e.Sidenummer)
                    .HasColumnName("SIDENUMMER")
                    .HasColumnType("int(11)");
            });

            modelBuilder.Entity<Stations>(entity =>
            {
                entity.HasKey(e => e.DrId)
                    .HasName("PRIMARY");

                entity.ToTable("stations");

                entity.Property(e => e.DrId)
                    .HasColumnName("DR_ID")
                    .HasColumnType("varchar(19)")
                    .HasCharSet("latin1")
                    .HasCollation("latin1_swedish_ci");

                entity.Property(e => e.ChannelName)
                    .IsRequired()
                    .HasColumnName("channel_name")
                    .HasColumnType("varchar(255)")
                    .HasCharSet("latin1")
                    .HasCollation("latin1_swedish_ci");

                entity.Property(e => e.ChannelType)
                    .IsRequired()
                    .HasColumnName("channel_type")
                    .HasColumnType("varchar(255)")
                    .HasCharSet("latin1")
                    .HasCollation("latin1_swedish_ci");

                entity.Property(e => e.Running)
                    .HasColumnName("running")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.StreamingUrl)
                    .IsRequired()
                    .HasColumnName("streaming_url")
                    .HasColumnType("varchar(255)")
                    .HasCharSet("latin1")
                    .HasCollation("latin1_swedish_ci");
            });

            modelBuilder.Entity<Subfingerid>(entity =>
            {
                entity.ToTable("subfingerid");

                entity.HasIndex(e => e.DateAdded)
                    .HasName("DK1_SUBFINGERID");

                entity.Property(e => e.Id)
                    .HasColumnName("ID")
                    .HasColumnType("int(11)");

                entity.Property(e => e.DateAdded)
                    .HasColumnName("DATE_ADDED")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.DateChanged)
                    .HasColumnName("DATE_CHANGED")
                    .HasColumnType("timestamp")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP")
                    .ValueGeneratedOnAddOrUpdate();

                entity.Property(e => e.Signature)
                    .IsRequired()
                    .HasColumnName("SIGNATURE");
            });

            modelBuilder.Entity<TaskQueue>(entity =>
            {
                entity.ToTable("task_queue");

                entity.Property(e => e.Id)
                    .HasColumnName("ID")
                    .HasColumnType("bigint(20)");

                entity.Property(e => e.Arguments)
                    .IsRequired()
                    .HasColumnName("ARGUMENTS")
                    .HasColumnType("varchar(200)")
                    .HasCharSet("latin1")
                    .HasCollation("latin1_swedish_ci");

                entity.Property(e => e.JobId)
                    .HasColumnName("JOB_ID")
                    .HasColumnType("bigint(20)");

                entity.Property(e => e.LastUpdated)
                    .HasColumnName("LAST_UPDATED")
                    .HasColumnType("timestamp(6)")
                    .HasDefaultValueSql("'CURRENT_TIMESTAMP(6)'")
                    .ValueGeneratedOnAddOrUpdate();

                entity.Property(e => e.Machine)
                    .HasColumnName("MACHINE")
                    .HasColumnType("varchar(50)")
                    .HasDefaultValueSql("'None'")
                    .HasCharSet("latin1")
                    .HasCollation("latin1_swedish_ci");

                entity.Property(e => e.Started).HasColumnName("STARTED");

                entity.Property(e => e.TaskType)
                    .IsRequired()
                    .HasColumnName("TASK_TYPE")
                    .HasColumnType("varchar(50)")
                    .HasCharSet("latin1")
                    .HasCollation("latin1_swedish_ci");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
