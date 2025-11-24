using FeuerwerkLager.Data;
using FeuerwerkLager.Logs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FeuerwerkLager.Pages.Admin;

public class IndexModel : PageModel
{
    private readonly FireworksContext _context;
    private readonly IWebHostEnvironment _env;

    public IndexModel(FireworksContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    public List<string> BackupFiles { get; set; } = new();

    public void OnGet()
    {
        LoadBackups();
    }

    private void LoadBackups()
    {
        var backupDir = Path.Combine(_env.ContentRootPath, "Backups");
        if (Directory.Exists(backupDir))
        {
            BackupFiles = Directory.GetFiles(backupDir, "fireworks_*.db")
                .OrderByDescending(f => f)
                .Select(f => Path.GetFileName(f) ?? string.Empty)
                .ToList();
        }
        else
        {
            BackupFiles = new List<string>();
        }
    }

    public async Task<IActionResult> OnPostResetAsync()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.Database.MigrateAsync();

        PlainFileLogger.Clear();
        PlainFileLogger.Log("Datenbank-Reset durchgeführt.");

        return RedirectToPage();
    }

    private string GetDatabasePath()
    {
        var connectionString = _context.Database.GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return Path.Combine(_env.ContentRootPath, "fireworks.db");
        }

        var builder = new SqliteConnectionStringBuilder(connectionString);
        var dataSource = builder.DataSource;

        if (!Path.IsPathRooted(dataSource))
        {
            dataSource = Path.Combine(_env.ContentRootPath, dataSource);
        }

        return dataSource;
    }

    public async Task<IActionResult> OnPostBackup()
    {
        var dbPath = GetDatabasePath();
        var backupDir = Path.Combine(_env.ContentRootPath, "Backups");

        if (!System.IO.File.Exists(dbPath))
        {
            ModelState.AddModelError(string.Empty, "Keine Datenbankdatei gefunden.");
            LoadBackups();
            return Page();
        }

        Directory.CreateDirectory(backupDir);

        var fileName = $"fireworks_{DateTime.Now:yyyyMMdd_HHmmss}.db";
        var destPath = Path.Combine(backupDir, fileName);

        try
        {
            await using var source = new SqliteConnection($"Data Source={dbPath}");
            await source.OpenAsync();

            await using var destination = new SqliteConnection($"Data Source={destPath}");
            await destination.OpenAsync();

            source.BackupDatabase(destination);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Backup fehlgeschlagen: {ex.Message}");
            LoadBackups();
            return Page();
        }

        PlainFileLogger.Log($"Backup erstellt: {fileName}");

        return RedirectToPage();
    }
}
