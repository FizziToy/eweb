using eweb.Domain.Entities;
using eweb.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eweb.Web.Controllers;

[Authorize(Roles = "Admin")]
public class CategoriesController : Controller
{
    private readonly ApplicationDbContext _context;

    public CategoriesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // INDEX 
    public async Task<IActionResult> Index()
    {
        var categories = await _context.LessonCategories
            .Include(c => c.Lessons)
            .OrderBy(c => c.Name)
            .ToListAsync();

        return View(categories);
    }

    // CREATE
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name)
    {
        try
        {
            var category = new LessonCategory(name);

            _context.LessonCategories.Add(category);

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
        }

        return RedirectToAction(nameof(Index));
    }

    // EDIT
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, string name)
    {
        var category = await _context.LessonCategories.FindAsync(id);

        if (category == null)
            return NotFound();

        try
        {
            category.Rename(name);

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
        }

        return RedirectToAction(nameof(Index));
    }

    // DELETE
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _context.LessonCategories
            .Include(c => c.Lessons)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null)
            return NotFound();

        if (category.Lessons.Any())
        {
            TempData["Error"] = "Категорію не можна видалити, бо вона використовується в уроках.";
            return RedirectToAction(nameof(Index));
        }

        _context.LessonCategories.Remove(category);

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}