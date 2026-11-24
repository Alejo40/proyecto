namespace Parking.Core.Domain;

public enum VehicleType { Carro, Moto, Bicicleta }

public class Slot
{
    public int Id { get; set; }
    public int Number { get; set; }
    public VehicleType Type { get; set; }
    public bool IsOccupied { get; set; }
}

public class ParkingSession
{
    public int Id { get; set; }
    public string Plate { get; set; } = "";
    public VehicleType Type { get; set; }
    public int SlotId { get; set; }
    public Slot? Slot { get; set; }
    public DateTime EntryTime { get; set; }
    public DateTime? ExitTime { get; set; }
    public decimal? Total { get; set; }
    public bool IsActive => ExitTime is null;
}

// record: resultado inmutable para reportes (value semantics)
public record DailyReport(DateOnly Day, int Vehicles, decimal Income);
