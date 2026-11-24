using Microsoft.EntityFrameworkCore;
using Parking.Core.Domain;
using Parking.Core.Services;
using Parking.Data;

ApplicationConfiguration.Initialize();
var db = new ParkingDbContext(new DbContextOptionsBuilder<ParkingDbContext>().UseSqlite(DbPaths.ConnectionString).Options);
db.Database.EnsureCreated();   // misma BD que la web
var repo = new EfParkingRepository(db);
var svc = new ParkingService(repo);

var f = new Form { Text = "Parqueadero - Escritorio", Width = 680, Height = 480, StartPosition = FormStartPosition.CenterScreen };
var plate = new TextBox { Left = 10, Top = 10, Width = 130, PlaceholderText = "Placa" };
var type = new ComboBox { Left = 150, Top = 10, Width = 100, DropDownStyle = ComboBoxStyle.DropDownList, DataSource = Enum.GetValues<VehicleType>() };
var bIn = new Button { Text = "Entrada", Left = 260, Top = 9, Width = 80 };
var bOut = new Button { Text = "Salida", Left = 350, Top = 9, Width = 80 };
var bRep = new Button { Text = "Reporte", Left = 440, Top = 9, Width = 80 };
var grid = new DataGridView { Top = 45, Left = 0, Width = 660, Height = 395, ReadOnly = true, AllowUserToAddRows = false,
    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
    Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };

async Task Refresh() => grid.DataSource = (await repo.GetSessionsAsync(true))
    .Select(s => new { s.Plate, Tipo = s.Type, Cupo = s.Slot?.Number, Entrada = s.EntryTime }).ToList();
async Task Run(Func<Task> a) { try { await a(); await Refresh(); } catch (Exception e) { MessageBox.Show(e.Message, "Aviso"); } }

svc.VehicleEntered += s => f.Text = $"Parqueadero - último ingreso: {s.Plate}";
grid.CellClick += (_, e) => { if (e.RowIndex >= 0) plate.Text = grid.Rows[e.RowIndex].Cells["Plate"].Value?.ToString(); };
bIn.Click += async (_, _) => await Run(async () => { await svc.EntryAsync(plate.Text, (VehicleType)type.SelectedItem!); plate.Clear(); });
bOut.Click += async (_, _) => await Run(async () => { var s = await svc.ExitAsync(plate.Text); MessageBox.Show($"Total a cobrar: ${s.Total:N0}", "Cobro"); plate.Clear(); });
bRep.Click += async (_, _) => { var r = await svc.ReportAsync();
    MessageBox.Show(string.Join("\n", r.Select(x => $"{x.Day}: {x.Vehicles} vehículos - ${x.Income:N0}")) is { Length: > 0 } t ? t : "Sin datos", "Reporte"); };

f.Controls.AddRange(new Control[] { plate, type, bIn, bOut, bRep, grid });
f.Shown += async (_, _) => await Refresh();
Application.Run(f);
