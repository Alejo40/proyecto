using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
namespace Parking.Web.Controllers;

public record UserRow(string Id, string Email, string Role);

[Authorize(Roles = "Admin")]
public class UsersController : Controller
{
    private readonly UserManager<IdentityUser> _um;
    public UsersController(UserManager<IdentityUser> um) => _um = um;

    public async Task<IActionResult> Index()
    {
        var rows = new List<UserRow>();
        foreach (var u in _um.Users.OrderBy(u => u.Email).ToList())
            rows.Add(new UserRow(u.Id, u.Email ?? u.UserName ?? "", (await _um.GetRolesAsync(u)).FirstOrDefault() ?? "—"));
        return View(rows);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetRole(string id, string role)
    {
        var u = await _um.FindByIdAsync(id);
        if (u is null || role is not ("Admin" or "Operador")) return RedirectToAction(nameof(Index));
        if (u.Id == _um.GetUserId(User)) { TempData["Error"] = "No puedes cambiar tu propio rol."; return RedirectToAction(nameof(Index)); }
        await _um.RemoveFromRolesAsync(u, await _um.GetRolesAsync(u));
        await _um.AddToRoleAsync(u, role);
        return RedirectToAction(nameof(Index));
    }
}
