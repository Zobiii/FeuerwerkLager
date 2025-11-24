using FeuerwerkLager.Data;
using FeuerwerkLager.Logs;
using FeuerwerkLager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FeuerwerkLager.Pages.Articles;

public class EditModel : PageModel
{
    private readonly FireworksContext _context;

    public EditModel(FireworksContext context)
    {
        _context = context;
    }

    public List<Article> Articles { get; set; } = new();

    [BindProperty]
    public Article Article { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        Articles = await _context.Articles
            .OrderBy(a => a.Name)
            .ToListAsync();

        if (!Articles.Any())
        {
            return Page();
        }

        if (!id.HasValue)
        {
            Article = Articles.First();
        }
        else
        {
            var article = await _context.Articles.FindAsync(id.Value);
            if (article == null)
            {
                return NotFound();
            }
            Article = article;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Für das Dropdown oben
        Articles = await _context.Articles
            .OrderBy(a => a.Name)
            .ToListAsync();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var existing = await _context.Articles.FindAsync(Article.Id);
        if (existing == null)
        {
            ModelState.AddModelError(string.Empty, "Artikel nicht gefunden.");
            return Page();
        }

        var oldName = existing.Name;
        var oldCompany = existing.Company;
        var oldNem = existing.NEM;
        var oldCategory = existing.Category;
        var oldIsMulti = existing.IsMultiPart;
        var oldPieces = existing.PiecesPerUnit;

        // ggf. Trimmen
        Article.Name = Article.Name?.Trim() ?? string.Empty;
        Article.Company = Article.Company?.Trim() ?? string.Empty;

        existing.Name = Article.Name;
        existing.Company = Article.Company;
        existing.ProductNumber = Article.ProductNumber;
        existing.NEM = Article.NEM;
        existing.Category = Article.Category;
        existing.IsMultiPart = Article.IsMultiPart;
        existing.PiecesPerUnit = Article.PiecesPerUnit;
        existing.Notes = Article.Notes;

        await _context.SaveChangesAsync();

        PlainFileLogger.Log(
            $"Artikel bearbeitet: {oldName} -> {existing.Name}, Firma: {oldCompany} -> {existing.Company}, " +
            $"NEM: {oldNem} -> {existing.NEM}, Kategorie: {oldCategory} -> {existing.Category}, " +
            $"Einzelteile: {oldIsMulti}/{oldPieces} -> {existing.IsMultiPart}/{existing.PiecesPerUnit}");

        SuccessMessage = $"Artikel \"{existing.Name}\" wurde aktualisiert.";

        // Bleibt auf /Articles/Edit für denselben Artikel
        return RedirectToPage(new { id = Article.Id });
    }
}
