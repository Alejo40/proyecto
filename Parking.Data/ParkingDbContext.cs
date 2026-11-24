using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Parking.Core.Domain;
using Parking.Core.Services;
namespace Parking.Data;

public class ParkingDbContext : IdentityDbContext<IdentityUser>
{
    public ParkingDbContext(DbContextOptions<ParkingDbContext> o) : base(o) { }
    public DbSet<Slot> Slots => Set<Slot>();
    public DbSet<ParkingSession> Sessions => Set<ParkingSession>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.Entity<Slot>().HasIndex(s => new { s.Number, s.Type }).IsUnique();
        b.Entity<ParkingSession>().HasIndex(s => s.Plate);
        b.Entity<ParkingSession>().Property(s => s.Total).HasColumnType("decimal(18,2)");
        // Datos semilla: 20 carros, 10 motos, 10 bicicletas
        int id = 1;
        foreach (var (t, n) in new[] { (VehicleType.Carro, 20), (VehicleType.Moto, 10), (VehicleType.Bicicleta, 10) })
            for (int i = 1; i <= n; i++)
                b.Entity<Slot>().HasData(new Slot { Id = id++, Number = i, Type = t });
    }
}

public class EfParkingRepository : IParkingRepository
{
    private readonly ParkingDbContext _db;
    public EfParkingRepository(ParkingDbContext db) => _db = db;

    public Task<ParkingSession?> GetActiveByPlateAsync(string plate) =>
        _db.Sessions.Include(s => s.Slot).FirstOrDefaultAsync(s => s.Plate == plate && s.ExitTime == null);
    public Task<Slot?> GetFreeSlotAsync(VehicleType t) =>
        _db.Slots.Where(s => s.Type == t && !s.IsOccupied).OrderBy(s => s.Number).FirstOrDefaultAsync();
    public Task<List<ParkingSession>> GetSessionsAsync(bool active) =>
        _db.Sessions.Include(s => s.Slot).Where(s => (s.ExitTime == null) == active)
           .OrderByDescending(s => s.EntryTime).ToListAsync();
    public async Task AddAsync(ParkingSession s) => await _db.Sessions.AddAsync(s);
    public Task SaveAsync() => _db.SaveChangesAsync();
}
