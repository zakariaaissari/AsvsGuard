using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ASVSGuard.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<IdentityUser> _users;
    private readonly SignInManager<IdentityUser> _signIn;

    public AccountController(UserManager<IdentityUser> users, SignInManager<IdentityUser> signIn)
    {
        _users = users;
        _signIn = signIn;
    }

    [HttpGet]
    public IActionResult Register() => View();

    [HttpPost]
    public async Task<IActionResult> Register(string email, string password)
    {
        var user = new IdentityUser { UserName = email, Email = email };
        var result = await _users.CreateAsync(user, password);
        if (result.Succeeded)
        {
            await _signIn.SignInAsync(user, isPersistent: false);
            return RedirectToAction("Index", "Exigence");
        }
        foreach (var e in result.Errors)
            ModelState.AddModelError(string.Empty, e.Description);
        return View();
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string email, string password, string? returnUrl = null)
    {
        var result = await _signIn.PasswordSignInAsync(email, password, isPersistent: true, lockoutOnFailure: true);
        if (result.Succeeded)
            return LocalRedirect(returnUrl ?? "/Exigence");
        ModelState.AddModelError(string.Empty, "Invalid email or password.");
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await _signIn.SignOutAsync();
        return RedirectToAction("Index", "Exigence");
    }
}
