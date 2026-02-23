using Microsoft.EntityFrameworkCore;
using HcPortal.Models;

namespace HcPortal.Data
{
    /// <summary>
    /// Seeds the Proton ProtonTrack rows (Phase 33+).
    /// Catalog items (Kompetensi/SubKompetensi/Deliverable) are managed via the Phase 35 Catalog UI.
    /// Idempotent: skips if ProtonTrack data already exists.
    /// </summary>
    public static class SeedProtonData
    {
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            // Skip if ProtonTracks already seeded (migration handles initial seeding via MERGE)
            if (await context.ProtonTracks.AnyAsync())
            {
                Console.WriteLine("ProtonTrack data already exists, skipping seed...");
                return;
            }

            // Note: The CreateProtonTrackTable migration seeds the 6 ProtonTrack rows via MERGE.
            // This method is a no-op if the migration has been applied.
            // For fresh dev installs where the migration was not yet applied, seed here:
            var tracks = new[]
            {
                new ProtonTrack { TrackType = "Panelman", TahunKe = "Tahun 1", DisplayName = "Panelman - Tahun 1", Urutan = 1 },
                new ProtonTrack { TrackType = "Panelman", TahunKe = "Tahun 2", DisplayName = "Panelman - Tahun 2", Urutan = 2 },
                new ProtonTrack { TrackType = "Panelman", TahunKe = "Tahun 3", DisplayName = "Panelman - Tahun 3", Urutan = 3 },
                new ProtonTrack { TrackType = "Operator", TahunKe = "Tahun 1", DisplayName = "Operator - Tahun 1", Urutan = 4 },
                new ProtonTrack { TrackType = "Operator", TahunKe = "Tahun 2", DisplayName = "Operator - Tahun 2", Urutan = 5 },
                new ProtonTrack { TrackType = "Operator", TahunKe = "Tahun 3", DisplayName = "Operator - Tahun 3", Urutan = 6 },
            };

            await context.ProtonTracks.AddRangeAsync(tracks);
            await context.SaveChangesAsync();
            Console.WriteLine("Seeded 6 ProtonTrack rows successfully.");
        }
    }
}
