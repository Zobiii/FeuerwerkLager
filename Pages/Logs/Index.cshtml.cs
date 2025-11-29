using FeuerwerkLager.Logs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FeuerwerkLager.Pages.Logs;

public class IndexModel : PageModel
{
    public string[] Lines { get; set; } = Array.Empty<string>();

    [BindProperty(SupportsGet = true)]
    public int Limit { get; set; } = 200;

    [BindProperty(SupportsGet = true)]
    public string? Contains { get; set; }

    public void OnGet()
    {
        var all = PlainFileLogger.ReadAllLines();

        if (!string.IsNullOrWhiteSpace(Contains))
        {
            Lines = all
                .Where(l => l.Contains(Contains, StringComparison.OrdinalIgnoreCase))
                .Reverse()
                .Take(Limit > 0 ? Limit : 200)
                .ToArray();
        }
        else
        {
            Lines = all
                .Reverse()
                .Take(Limit > 0 ? Limit : 200)
                .ToArray();
        }
    }
}
