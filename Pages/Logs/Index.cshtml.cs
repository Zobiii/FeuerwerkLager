using FeuerwerkLager.Logs;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FeuerwerkLager.Pages.Logs;

public class IndexModel : PageModel
{
    public string[] Lines { get; set; } = Array.Empty<string>();

    public void OnGet()
    {
        Lines = PlainFileLogger.ReadAllLines();
    }
}
