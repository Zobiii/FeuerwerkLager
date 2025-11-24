using FeuerwerkLager.Data;
using FeuerwerkLager.Logs;
using FeuerwerkLager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FeuerwerkLager.Pages.Articles;

public class CreateModel : PageModel
{
    private readonly FireworksContext _context;

    public CreateModel(FireworksContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Article Article { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        // Name / Firma säubern
        Article.Name = Article.Name?.Trim() ?? string.Empty;
        Article.Company = Article.Company?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(Article.Name) || string.IsNullOrWhiteSpace(Article.Company))
        {
            ModelState.AddModelError(string.Empty, "Name und Firma dürfen nicht leer sein.");
            return Page();
        }

        // Doppelter Artikel? (Name + Firma schon vorhanden)
        var exists = await _context.Articles.AnyAsync(a =>
            a.Name.ToLower() == Article.Name.ToLower() &&
            a.Company.ToLower() == Article.Company.ToLower());

        if (exists)
        {
            ModelState.AddModelError(
                string.Empty,
                "Ein Artikel mit diesem Namen und dieser Firma existiert bereits."
            );
            return Page();
        }

        _context.Articles.Add(Article);
        await _context.SaveChangesAsync();

        PlainFileLogger.Log(
            $"Artikel erstellt: {Article.Name}, Firma: {Article.Company}, Kategorie: {Article.Category}");

        // Info für nächste Seite
        SuccessMessage = $"Artikel \"{Article.Name}\" wurde gespeichert.";

        // Bleibt auf /Articles/Create (PRG-Pattern)
        return RedirectToPage();
    }
}
