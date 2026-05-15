using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NanoAgent.Builder.Application.Saas;
using NanoAgent.Builder.Application.Security;
using NanoAgent.Builder.Infrastructure.Identity;

namespace NanoAgent.Builder.Pages.Account;

[AllowAnonymous]
public sealed class RegisterModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ISaasSubscriptionService _subscriptionService;

    public RegisterModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ISaasSubscriptionService subscriptionService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _subscriptionService = subscriptionService;
    }

    [BindProperty]
    public RegisterInput Input { get; set; } = new();

    [BindProperty]
    public string? ReturnUrl { get; set; }

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        ReturnUrl ??= Url.Content("~/");

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = new ApplicationUser
        {
            UserName = Input.Email,
            Email = Input.Email,
            EmailConfirmed = true,
            DisplayName = Input.DisplayName,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        var result = await _userManager.CreateAsync(user, Input.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }

        await _userManager.AddToRoleAsync(user, ApplicationRoles.User);
        await _subscriptionService.SubscribeUserAsync(user.Id, SaasPlanCodes.Free, cancellationToken);
        await _signInManager.SignInAsync(user, isPersistent: false);

        return LocalRedirect(GetSafeReturnUrl());
    }

    private string GetSafeReturnUrl() =>
        Url.IsLocalUrl(ReturnUrl) ? ReturnUrl! : Url.Content("~/");

    public sealed class RegisterInput
    {
        [Display(Name = "Display name")]
        [StringLength(100)]
        public string? DisplayName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 8)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare(nameof(Password), ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
