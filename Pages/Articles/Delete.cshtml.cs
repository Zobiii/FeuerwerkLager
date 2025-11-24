using FeuerwerkLager.Data;
using FeuerwerkLager.Logs;
using FeuerwerkLager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FeuerwerkLager.Pages.Articles;

public class DeleteModel : PageModel
{
    private readonly FireworksContext _context;

    public DeleteModel(FireworksContext context)
    {
        _context = context;
    }

    public List<Article> Articles { get; set; } = new();

    [BindProperty]
    public int SelectedArticleId { get; set; }

    public async Task OnGetAsync()
    {
        Articles = await _context.Articles
            .OrderBy(a => a.Name)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Articles = await _context.Articles
            .OrderBy(a => a.Name)
            .ToListAsync();

        if (SelectedArticleId <= 0)
        {
            ModelState.AddModelError(string.Empty, "Bitte einen Artikel auswählen.");
            return Page();
        }

        var article = await _context.Articles
            .FirstOrDefaultAsync(a => a.Id == SelectedArticleId);

        if (article == null)
        {
            ModelState.AddModelError(string.Empty, "Der ausgewählte Artikel wurde nicht gefunden.");
            return Page();
        }

        var stockEntries = await _context.StockEntries
            .Where(s => s.ArticleId == SelectedArticleId)
            .ToListAsync();

        var totalQuantity = stockEntries.Sum(s => s.Quantity);
        var positionCount = stockEntries.Count;

        _context.StockEntries.RemoveRange(stockEntries);
        _context.Articles.Remove(article);

        await _context.SaveChangesAsync();

        PlainFileLogger.Log(
            $"Artikel gelöscht: {article.Name}, Firma: {article.Company}, " +
            $"Bestandszeilen entfernt: {positionCount}, Gesamtmenge: {totalQuantity}");

        return RedirectToPage("/Index");
    }
}
