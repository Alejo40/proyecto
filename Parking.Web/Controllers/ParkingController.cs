using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parking.Core.Domain;
using Parking.Core.Services;
namespace Parking.Web.Controllers;

[Authorize(Roles = "Admin,Operador")]
public class ParkingController : Controller
{
    private readonly ParkingService _svc;
    private readonly IParkingRepository _repo;
    public ParkingController(ParkingService svc, IParkingRepository repo) { _svc = svc; _repo = repo; }

    public async Task<IActionResult> Index() => View(await _repo.GetSessionsAsync(true));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Entry(string plate, VehicleType type)
    {
        try { await _svc.EntryAsync(plate, type); } catch (Exception e) { TempData["Error"] = e.Message; }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Exit(string plate)
    {
        try { var s = await _svc.ExitAsync(plate); TempData["Ok"] = $"{s.Plate}: ${s.Total:N0}"; }
        catch (Exception e) { TempData["Error"] = e.Message; }
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Report() => View(await _svc.ReportAsync());
}
