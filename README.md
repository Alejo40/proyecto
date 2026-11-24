# Sistema de gestión de parqueadero (.NET 8)
Proyectos: Parking.Core (dominio) · Parking.Data (EF Core + Identity) · Parking.Web (MVC) · Parking.Desktop (WinForms)

## Ejecutar
    dotnet run --project Parking.Web        # http://localhost:5000
    dotnet run --project Parking.Desktop    # (Windows) misma base de datos
Login: admin@parking.com / Admin123!  · BD: %LOCALAPPDATA%\Parqueadero\parking.db
Roles: Admin (reportes + usuarios) y Operador (entradas/salidas). Los registros nuevos reciben Operador.

## Estructuras de datos
| Necesidad | Elegida | Alternativa | Justificación |
|---|---|---|---|
| Buscar sesión activa por placa | Índice en BD sobre Plate | List + Find | O(log n) vs O(n) |
| Cupos libres por tipo | Consulta indexada de Slot | Stack<int> en memoria | Persistente y compartido web/escritorio |
| Unicidad de cupo | Índice único (Number, Type) | validación manual | La BD garantiza integridad |
| Reportes | LINQ GroupBy + record | bucles manuales | Declarativo e inmutable |
| Tarifa | Func<TimeSpan,VehicleType,decimal> | switch fijo | Intercambiable sin tocar el servicio |
