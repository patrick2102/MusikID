using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Framework
{
    public interface IdrfingerprintsContext : IDisposable
    {
        DbSet<Errors> Errors { get; set; }
        DbSet<Files> Files { get; set; }
        DbSet<FingerTaskQueue> FingerTaskQueue { get; set; }
        DbSet<Job> Job { get; set; }
        DbSet<LivestreamResults> LivestreamResults { get; set; }
        DbSet<OnDemandResults> OnDemandResults { get; set; }
        DbSet<RadioTaskQueue> RadioTaskQueue { get; set; }
        DbSet<Songs> Songs { get; set; }
        DbSet<Stations> Stations { get; set; }
        DbSet<Subfingerid> Subfingerid { get; set; }
        DbSet<TaskQueue> TaskQueue { get; set; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}