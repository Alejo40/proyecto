namespace Parking.Data;

/// <summary>Ruta única de la BD: la web y el escritorio comparten el mismo archivo.</summary>
public static class DbPaths
{
    public static string ConnectionString
    {
        get
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Parqueadero");
            Directory.CreateDirectory(dir);
            return $"Data Source={Path.Combine(dir, "parking.db")}";
        }
    }
}
