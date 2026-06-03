using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HcPortal.Models;
using HcPortal.Data;
using HcPortal.Services;

namespace HcPortal.Controllers
{
    [Route("Admin/[action]")]
    public class OrganizationController : AdminBaseController
    {
        public OrganizationController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            AuditLogService auditLog,
            IWebHostEnvironment env)
            : base(context, userManager, auditLog, env)
        {
        }

        // Override View resolution to use Views/Admin/ folder (controller name is Organization, but views stay in Admin/)
        protected new ViewResult View() => base.View("~/Views/Admin/" + ControllerContext.ActionDescriptor.ActionName + ".cshtml");
        protected new ViewResult View(object? model) => base.View("~/Views/Admin/" + ControllerContext.ActionDescriptor.ActionName + ".cshtml", model);
        protected new ViewResult View(string viewName) => base.View(viewName.StartsWith("~/") ? viewName : "~/Views/Admin/" + viewName + ".cshtml");
        protected new ViewResult View(string viewName, object? model) => base.View(viewName.StartsWith("~/") ? viewName : "~/Views/Admin/" + viewName + ".cshtml", model);

        // GET /Admin/ManageOrganization
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> ManageOrganization(int? editId)
        {
            var roots = await _context.OrganizationUnits
                .Include(u => u.Children.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name))
                    .ThenInclude(c => c.Children.OrderBy(gc => gc.DisplayOrder).ThenBy(gc => gc.Name))
                .Where(u => u.ParentId == null)
                .OrderBy(u => u.DisplayOrder)
                .ThenBy(u => u.Name)
                .ToListAsync();

            ViewBag.PotentialParents = await _context.OrganizationUnits
                .Where(u => u.IsActive)
                .OrderBy(u => u.Level)
                .ThenBy(u => u.DisplayOrder)
                .ToListAsync();

            if (editId.HasValue)
            {
                ViewBag.EditUnit = await _context.OrganizationUnits.FindAsync(editId.Value);
            }

            return View("ManageOrganization", roots);
        }

        // GET /Admin/GetOrganizationTree
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> GetOrganizationTree()
        {
            var units = await _context.OrganizationUnits
                .OrderBy(u => u.Level)
                .ThenBy(u => u.DisplayOrder)
                .ThenBy(u => u.Name)
                .Select(u => new { u.Id, u.Name, u.ParentId, u.Level, u.DisplayOrder, u.IsActive })
                .ToListAsync();
            return Json(units);
        }

        // POST /Admin/AddOrganizationUnit
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddOrganizationUnit(string name, int? parentId)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, message = "Nama tidak boleh kosong." });
                TempData["Error"] = "Nama tidak boleh kosong.";
                TempData["ShowAddForm"] = true;
                return RedirectToAction("ManageOrganization");
            }

            bool duplicate = await _context.OrganizationUnits
                .AnyAsync(u => u.Name == name.Trim() && u.ParentId == parentId);
            if (duplicate)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, message = "Nama unit sudah digunakan. Gunakan nama yang berbeda." });
                TempData["Error"] = "Nama unit sudah digunakan. Gunakan nama yang berbeda.";
                TempData["ShowAddForm"] = true;
                return RedirectToAction("ManageOrganization");
            }

            int level = 0;
            if (parentId.HasValue)
            {
                var parent = await _context.OrganizationUnits.FindAsync(parentId.Value);
                level = parent != null ? parent.Level + 1 : 0;
            }

            int maxOrder = await _context.OrganizationUnits
                .Where(u => u.ParentId == parentId)
                .MaxAsync(u => (int?)u.DisplayOrder) ?? 0;

            var unit = new OrganizationUnit
            {
                Name = name.Trim(),
                ParentId = parentId,
                Level = level,
                DisplayOrder = maxOrder + 1,
                IsActive = true
            };

            _context.OrganizationUnits.Add(unit);
            await _context.SaveChangesAsync();

            if (IsAjaxRequest())
                return Json(new { success = true, message = "Unit berhasil ditambahkan." });
            TempData["Success"] = "Unit berhasil ditambahkan.";
            return RedirectToAction("ManageOrganization");
        }

        // POST /Admin/EditOrganizationUnit
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditOrganizationUnit(int id, string name, int? parentId)
        {
            var unit = await _context.OrganizationUnits.FindAsync(id);
            if (unit == null)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, message = "Unit tidak ditemukan." });
                return NotFound();
            }

            string oldName = unit.Name;
            int? oldParentId = unit.ParentId;

            if (string.IsNullOrWhiteSpace(name))
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, message = "Nama tidak boleh kosong." });
                TempData["Error"] = "Nama tidak boleh kosong.";
                return RedirectToAction("ManageOrganization", new { editId = id });
            }

            bool duplicate = await _context.OrganizationUnits
                .AnyAsync(u => u.Name == name.Trim() && u.ParentId == parentId && u.Id != id);
            if (duplicate)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, message = "Nama unit sudah digunakan. Gunakan nama yang berbeda." });
                TempData["Error"] = "Nama unit sudah digunakan. Gunakan nama yang berbeda.";
                return RedirectToAction("ManageOrganization", new { editId = id });
            }

            if (parentId.HasValue)
            {
                if (parentId.Value == id)
                {
                    if (IsAjaxRequest())
                        return Json(new { success = false, message = "Tidak dapat memindahkan unit ke sub-unitnya sendiri (circular reference)." });
                    TempData["Error"] = "Tidak dapat memindahkan unit ke sub-unitnya sendiri (circular reference).";
                    return RedirectToAction("ManageOrganization", new { editId = id });
                }

                bool isDescendant = await IsDescendantAsync(id, parentId.Value);
                if (isDescendant)
                {
                    if (IsAjaxRequest())
                        return Json(new { success = false, message = "Tidak dapat memindahkan unit ke sub-unitnya sendiri (circular reference)." });
                    TempData["Error"] = "Tidak dapat memindahkan unit ke sub-unitnya sendiri (circular reference).";
                    return RedirectToAction("ManageOrganization", new { editId = id });
                }
            }

            if (unit.ParentId != parentId)
            {
                int newLevel = 0;
                if (parentId.HasValue)
                {
                    var newParent = await _context.OrganizationUnits.FindAsync(parentId.Value);
                    newLevel = newParent != null ? newParent.Level + 1 : 0;
                }
                unit.ParentId = parentId;
                unit.Level = newLevel;
                await UpdateChildrenLevelsAsync(unit);
            }

            // Cascade rename and reparent to denormalized fields
            int cascadedUsers = 0;
            int cascadedMappings = 0;

            // Cascade rename
            if (oldName != name.Trim())
            {
                if (unit.Level == 0)
                {
                    var affectedUsers = await _context.Users.Where(u => u.Section == oldName).ToListAsync();
                    foreach (var u in affectedUsers) u.Section = name.Trim();
                    cascadedUsers += affectedUsers.Count;

                    var affectedMappings = await _context.CoachCoacheeMappings.Where(m => m.AssignmentSection == oldName).ToListAsync();
                    foreach (var m in affectedMappings) m.AssignmentSection = name.Trim();
                    cascadedMappings += affectedMappings.Count;

                    var affectedKompetensi = await _context.ProtonKompetensiList.Where(k => k.Bagian == oldName).ToListAsync();
                    foreach (var k in affectedKompetensi) k.Bagian = name.Trim();

                    var affectedGuidance = await _context.CoachingGuidanceFiles.Where(g => g.Bagian == oldName).ToListAsync();
                    foreach (var g in affectedGuidance) g.Bagian = name.Trim();
                }
                else
                {
                    var affectedUsers = await _context.Users.Where(u => u.Unit == oldName).ToListAsync();
                    foreach (var u in affectedUsers) u.Unit = name.Trim();
                    cascadedUsers += affectedUsers.Count;

                    var affectedMappings = await _context.CoachCoacheeMappings.Where(m => m.AssignmentUnit == oldName).ToListAsync();
                    foreach (var m in affectedMappings) m.AssignmentUnit = name.Trim();
                    cascadedMappings += affectedMappings.Count;

                    var affectedKompetensi = await _context.ProtonKompetensiList.Where(k => k.Unit == oldName).ToListAsync();
                    foreach (var k in affectedKompetensi) k.Unit = name.Trim();

                    var affectedGuidance = await _context.CoachingGuidanceFiles.Where(g => g.Unit == oldName).ToListAsync();
                    foreach (var g in affectedGuidance) g.Unit = name.Trim();
                }
            }

            // Cascade reparent — update Section for users in this unit when parent changes
            if (oldParentId != parentId && unit.Level >= 1)
            {
                // Find root ancestor (Level 0) from new parent
                string newSectionName = "";
                if (parentId.HasValue)
                {
                    var ancestor = await _context.OrganizationUnits.FindAsync(parentId.Value);
                    while (ancestor != null && ancestor.Level > 0 && ancestor.ParentId.HasValue)
                    {
                        ancestor = await _context.OrganizationUnits.FindAsync(ancestor.ParentId.Value);
                    }
                    if (ancestor != null) newSectionName = ancestor.Name;
                }

                if (!string.IsNullOrEmpty(newSectionName))
                {
                    var reparentUsers = await _context.Users.Where(u => u.Unit == oldName).ToListAsync();
                    foreach (var u in reparentUsers) u.Section = newSectionName;
                    cascadedUsers += reparentUsers.Count;

                    var reparentMappings = await _context.CoachCoacheeMappings.Where(m => m.AssignmentUnit == oldName).ToListAsync();
                    foreach (var m in reparentMappings) m.AssignmentSection = newSectionName;
                    cascadedMappings += reparentMappings.Count;

                    var reparentKompetensi = await _context.ProtonKompetensiList.Where(k => k.Unit == oldName).ToListAsync();
                    foreach (var k in reparentKompetensi) k.Bagian = newSectionName;

                    var reparentGuidance = await _context.CoachingGuidanceFiles.Where(g => g.Unit == oldName).ToListAsync();
                    foreach (var g in reparentGuidance) g.Bagian = newSectionName;
                }
            }

            unit.Name = name.Trim();
            await _context.SaveChangesAsync();

            var msg = (cascadedUsers > 0 || cascadedMappings > 0)
                ? $"Unit berhasil diperbarui. {cascadedUsers} user dan {cascadedMappings} mapping terupdate."
                : "Unit berhasil diperbarui.";
            if (IsAjaxRequest())
                return Json(new { success = true, message = msg });
            TempData["Success"] = msg;
            return RedirectToAction("ManageOrganization");
        }

        // POST /Admin/PreviewEditCascade — read-only dampak cascade sebelum apply Edit (ORG-TREE-07)
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PreviewEditCascade(int id, string name, int? parentId)
        {
            var unit = await _context.OrganizationUnits.FindAsync(id);
            if (unit == null) return Json(new { error = "Unit tidak ditemukan." });

            var trimmed = (name ?? "").Trim();
            bool nameChanged = !string.IsNullOrEmpty(trimmed) && unit.Name != trimmed;
            bool parentChanged = unit.ParentId != parentId;   // server authoritative (Pitfall 6)

            if (!nameChanged && !parentChanged)
                return Json(new { nameChanged = false, parentChanged = false });   // D-04 early-return

            int affectedUsers = 0, affectedMappings = 0, affectedKompetensi = 0, affectedGuidance = 0;
            string oldName = unit.Name;

            if (nameChanged)
            {
                if (unit.Level == 0)
                {
                    affectedUsers      += await _context.Users.CountAsync(u => u.Section == oldName);
                    affectedMappings   += await _context.CoachCoacheeMappings.CountAsync(m => m.AssignmentSection == oldName);
                    affectedKompetensi += await _context.ProtonKompetensiList.CountAsync(k => k.Bagian == oldName);
                    affectedGuidance   += await _context.CoachingGuidanceFiles.CountAsync(g => g.Bagian == oldName);
                }
                else
                {
                    affectedUsers      += await _context.Users.CountAsync(u => u.Unit == oldName);
                    affectedMappings   += await _context.CoachCoacheeMappings.CountAsync(m => m.AssignmentUnit == oldName);
                    affectedKompetensi += await _context.ProtonKompetensiList.CountAsync(k => k.Unit == oldName);
                    affectedGuidance   += await _context.CoachingGuidanceFiles.CountAsync(g => g.Unit == oldName);
                }
            }

            // A1 FULL-ACCURACY: reparent meng-update KEEMPAT field-pair di EditOrganizationUnit (L249-261),
            // jadi count KEEMPAT (BUKAN affectedUsers saja seperti spec L433-438) agar preview == actual.
            if (parentChanged && unit.Level >= 1 && !nameChanged)
            {
                affectedUsers      += await _context.Users.CountAsync(u => u.Unit == oldName);
                affectedMappings   += await _context.CoachCoacheeMappings.CountAsync(m => m.AssignmentUnit == oldName);
                affectedKompetensi += await _context.ProtonKompetensiList.CountAsync(k => k.Unit == oldName);
                affectedGuidance   += await _context.CoachingGuidanceFiles.CountAsync(g => g.Unit == oldName);
            }

            return Json(new {
                nameChanged,
                parentChanged,
                affectedUsersCount = affectedUsers,
                affectedMappingsCount = affectedMappings,
                affectedKompetensiCount = affectedKompetensi,
                affectedGuidanceCount = affectedGuidance
            });
        }

        private async Task<bool> IsDescendantAsync(int nodeId, int targetId)
        {
            var current = await _context.OrganizationUnits.FindAsync(targetId);
            while (current != null && current.ParentId.HasValue)
            {
                if (current.ParentId.Value == nodeId) return true;
                current = await _context.OrganizationUnits.FindAsync(current.ParentId.Value);
            }
            return false;
        }

        private async Task UpdateChildrenLevelsAsync(OrganizationUnit unit)
        {
            var children = await _context.OrganizationUnits
                .Where(u => u.ParentId == unit.Id)
                .ToListAsync();

            foreach (var child in children)
            {
                child.Level = unit.Level + 1;
                await UpdateChildrenLevelsAsync(child);
            }
        }

        // POST /Admin/ToggleOrganizationUnitActive
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleOrganizationUnitActive(int id)
        {
            var unit = await _context.OrganizationUnits
                .Include(u => u.Children)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (unit == null)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, message = "Unit tidak ditemukan." });
                return NotFound();
            }

            if (unit.IsActive && unit.Children.Any(c => c.IsActive))
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, message = "Nonaktifkan semua unit di bawahnya terlebih dahulu." });
                TempData["Error"] = "Nonaktifkan semua unit di bawahnya terlebih dahulu.";
                return RedirectToAction("ManageOrganization");
            }

            if (unit.IsActive)
            {
                bool hasActiveUsers;
                if (unit.Level == 0)
                    hasActiveUsers = await _context.Users.AnyAsync(u => u.Section == unit.Name);
                else
                    hasActiveUsers = await _context.Users.AnyAsync(u => u.Unit == unit.Name);

                if (hasActiveUsers)
                {
                    if (IsAjaxRequest())
                        return Json(new { success = false, message = "Tidak dapat menonaktifkan unit. Masih ada user aktif yang terdaftar di unit ini. Pindahkan semua user terlebih dahulu." });
                    TempData["Error"] = "Tidak dapat menonaktifkan unit. Masih ada user aktif yang terdaftar di unit ini. Pindahkan semua user terlebih dahulu.";
                    return RedirectToAction("ManageOrganization");
                }
            }

            unit.IsActive = !unit.IsActive;
            await _context.SaveChangesAsync();

            var toggleMsg = $"Status berhasil diubah menjadi {(unit.IsActive ? "Aktif" : "Nonaktif")}.";
            if (IsAjaxRequest())
                return Json(new { success = true, message = toggleMsg });
            TempData["Success"] = toggleMsg;
            return RedirectToAction("ManageOrganization");
        }

        // POST /Admin/DeleteOrganizationUnit
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteOrganizationUnit(int id)
        {
            var unit = await _context.OrganizationUnits
                .Include(u => u.Children)
                .Include(u => u.KkjFiles)
                .Include(u => u.CpdpFiles)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (unit == null)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, message = "Unit tidak ditemukan." });
                return NotFound();
            }

            if (unit.Children.Any(c => c.IsActive))
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, message = "Hapus atau nonaktifkan unit di bawahnya terlebih dahulu." });
                TempData["Error"] = "Hapus atau nonaktifkan unit di bawahnya terlebih dahulu.";
                return RedirectToAction("ManageOrganization");
            }

            if (unit.KkjFiles.Any() || unit.CpdpFiles.Any())
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, message = "Unit ini masih memiliki file KKJ/CPDP yang ter-assign. Hapus file terlebih dahulu." });
                TempData["Error"] = "Unit ini masih memiliki file KKJ/CPDP yang ter-assign. Hapus file terlebih dahulu.";
                return RedirectToAction("ManageOrganization");
            }

            bool hasUsers = await _context.Users.AnyAsync(u => u.Section == unit.Name || u.Unit == unit.Name);
            if (hasUsers)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, message = "Unit ini masih memiliki pekerja yang ter-assign. Pindahkan pekerja terlebih dahulu." });
                TempData["Error"] = "Unit ini masih memiliki pekerja yang ter-assign. Pindahkan pekerja terlebih dahulu.";
                return RedirectToAction("ManageOrganization");
            }

            bool hasProtonData = unit.Level == 0
                ? await _context.ProtonKompetensiList.AnyAsync(k => k.Bagian == unit.Name)
                    || await _context.CoachingGuidanceFiles.AnyAsync(g => g.Bagian == unit.Name)
                : await _context.ProtonKompetensiList.AnyAsync(k => k.Unit == unit.Name)
                    || await _context.CoachingGuidanceFiles.AnyAsync(g => g.Unit == unit.Name);
            if (hasProtonData)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, message = "Unit ini masih memiliki data Proton (Silabus/Guidance) yang ter-assign. Pindahkan data terlebih dahulu." });
                TempData["Error"] = "Unit ini masih memiliki data Proton (Silabus/Guidance) yang ter-assign. Pindahkan data terlebih dahulu.";
                return RedirectToAction("ManageOrganization");
            }

            try
            {
                _context.OrganizationUnits.Remove(unit);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, message = "Tidak bisa hapus unit: masih ada data yang berelasi." });
                TempData["Error"] = "Tidak bisa hapus unit: masih ada data yang berelasi.";
                return RedirectToAction("ManageOrganization");
            }

            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var actorName = string.IsNullOrWhiteSpace(currentUser?.NIP)
                    ? (currentUser?.FullName ?? "Unknown")
                    : $"{currentUser.NIP} - {currentUser.FullName}";
                await _auditLog.LogAsync(currentUser?.Id ?? "", actorName, "DeleteOrganizationUnit",
                    $"Deleted organization unit '{unit.Name}' [ID={unit.Id}] (Level={unit.Level})",
                    unit.Id, "OrganizationUnit");
            }
            catch { /* audit log failure tidak block response */ }

            if (IsAjaxRequest())
                return Json(new { success = true, message = "Unit berhasil dihapus." });
            TempData["Success"] = "Unit berhasil dihapus.";
            return RedirectToAction("ManageOrganization");
        }

        // POST /Admin/ReorderOrganizationUnit
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReorderOrganizationUnit(int id, string direction)
        {
            var unit = await _context.OrganizationUnits.FindAsync(id);
            if (unit == null)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, message = "Unit tidak ditemukan." });
                return NotFound();
            }

            var siblings = await _context.OrganizationUnits
                .Where(u => u.ParentId == unit.ParentId)
                .OrderBy(u => u.DisplayOrder)
                .ToListAsync();

            int index = siblings.FindIndex(u => u.Id == id);

            if (direction == "up" && index > 0)
            {
                var prev = siblings[index - 1];
                (unit.DisplayOrder, prev.DisplayOrder) = (prev.DisplayOrder, unit.DisplayOrder);
            }
            else if (direction == "down" && index < siblings.Count - 1)
            {
                var next = siblings[index + 1];
                (unit.DisplayOrder, next.DisplayOrder) = (next.DisplayOrder, unit.DisplayOrder);
            }

            await _context.SaveChangesAsync();
            if (IsAjaxRequest())
                return Json(new { success = true, message = "Urutan berhasil diubah." });
            return RedirectToAction("ManageOrganization");
        }

        // POST /Admin/ReorderBatch
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReorderBatch(int? parentId, string orderedIds)
        {
            if (string.IsNullOrWhiteSpace(orderedIds))
                return Json(new { success = false, message = "Data urutan tidak valid." });

            var ids = orderedIds.Split(',')
                .Where(s => int.TryParse(s.Trim(), out _))
                .Select(s => int.Parse(s.Trim()))
                .ToArray();

            if (ids.Length == 0)
                return Json(new { success = false, message = "Data urutan tidak valid." });

            var siblings = await _context.OrganizationUnits
                .Where(u => u.ParentId == parentId)
                .OrderBy(u => u.DisplayOrder)
                .ToListAsync();

            if (ids.Length != siblings.Count)
                return Json(new { success = false, message = "Jumlah item tidak sesuai." });

            var siblingMap = siblings.ToDictionary(u => u.Id);
            foreach (var id in ids)
            {
                if (!siblingMap.ContainsKey(id))
                    return Json(new { success = false, message = "ID tidak valid atau bukan sibling yang sama." });
            }

            for (int i = 0; i < ids.Length; i++)
            {
                siblingMap[ids[i]].DisplayOrder = i + 1;
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Urutan berhasil diubah." });
        }
    }
}
