using Parking.Core.Domain;
namespace Parking.Core.Services;

public interface IParkingRepository
{
    Task<ParkingSession?> GetActiveByPlateAsync(string plate);
    Task<Slot?> GetFreeSlotAsync(VehicleType type);
    Task<List<ParkingSession>> GetSessionsAsync(bool active);
    Task AddAsync(ParkingSession s);
    Task SaveAsync();
}

public class ParkingService
{
    private readonly IParkingRepository _repo;
    private readonly Func<TimeSpan, VehicleType, decimal> _tariff; // delegado (estrategia)
    public event Action<ParkingSession>? VehicleEntered;           // evento

    public ParkingService(IParkingRepository repo, Func<TimeSpan, VehicleType, decimal>? tariff = null)
    {
        _repo = repo;
        _tariff = tariff ?? DefaultTariff;
    }

    public static decimal DefaultTariff(TimeSpan t, VehicleType v)
    {
        var hours = (decimal)Math.Ceiling(Math.Max(t.TotalHours, 1));
        return hours * v switch { VehicleType.Carro => 4000m, VehicleType.Moto => 2000m, _ => 500m };
    }

    public async Task<ParkingSession> EntryAsync(string plate, VehicleType type)
    {
        plate = plate.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(plate)) throw new ArgumentException("Placa requerida");
        if (await _repo.GetActiveByPlateAsync(plate) is not null)
            throw new InvalidOperationException("El vehículo ya está dentro");
        var slot = await _repo.GetFreeSlotAsync(type)
                   ?? throw new InvalidOperationException("No hay cupos disponibles");
        slot.IsOccupied = true;
        var s = new ParkingSession { Plate = plate, Type = type, SlotId = slot.Id, EntryTime = DateTime.Now };
        await _repo.AddAsync(s);
        await _repo.SaveAsync();
        VehicleEntered?.Invoke(s);
        return s;
    }

    public async Task<ParkingSession> ExitAsync(string plate)
    {
        var s = await _repo.GetActiveByPlateAsync(plate.Trim().ToUpperInvariant())
                ?? throw new KeyNotFoundException("No hay sesión activa para esa placa");
        s.ExitTime = DateTime.Now;
        s.Total = _tariff(s.ExitTime.Value - s.EntryTime, s.Type);
        if (s.Slot is not null) s.Slot.IsOccupied = false;
        await _repo.SaveAsync();
        return s;
    }

    // LINQ + records
    public async Task<IEnumerable<DailyReport>> ReportAsync() =>
        (await _repo.GetSessionsAsync(false))
        .GroupBy(s => DateOnly.FromDateTime(s.ExitTime!.Value))
        .Select(g => new DailyReport(g.Key, g.Count(), g.Sum(x => x.Total ?? 0)))
        .OrderByDescending(r => r.Day);
}
