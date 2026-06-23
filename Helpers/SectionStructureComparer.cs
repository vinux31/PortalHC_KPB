using System.Collections.Generic;
using System.Linq;

namespace HcPortal.Helpers;

// Phase 415 M4 — single source of truth untuk perbandingan struktur Section antar-paket.
// Sebelumnya logika "GroupBy SectionNumber → bandingkan count per key" di-duplikat di
// AssessmentAdminController (validasi import) dan CMPController.StartExam (re-guard D-13) dengan
// bentuk yang sudah divergen. Helper ini menyatukannya: keduanya membandingkan dua dictionary
// count-per-SectionNumber (grup "Lainnya" = sentinel LainnyaKey) dan menerima daftar key yang berbeda.
//
// Null-safe key (validate-phase escalation): grup "Lainnya" punya SectionNumber == null. `Dictionary<int?, int>`
// TIDAK boleh key null (melempar ArgumentNullException), sehingga import/​guard untuk soal Lainnya dengan paket
// saudara akan 500 (bug laten sejak 415-03, langgar keystone backward-compat). Semua peta count memakai `int`
// dengan null dipetakan ke `LainnyaKey` via `KeyOf`.
public static class SectionStructureComparer
{
    // Sentinel untuk grup "Lainnya" (SectionNumber == null). SectionNumber riil selalu >= 1, jadi int.MinValue aman.
    public const int LainnyaKey = int.MinValue;

    // Petakan SectionNumber (nullable) ke key non-null: null → LainnyaKey.
    public static int KeyOf(int? sectionNumber) => sectionNumber ?? LainnyaKey;

    // Bandingkan dua peta count-per-SectionNumber (LainnyaKey = "Lainnya"). Mengembalikan daftar
    // key yang count-nya BERBEDA antara kedua sisi (urut: angka dulu, "Lainnya" terakhir).
    // Guard cukup cek .Any(); validasi import bisa render daftar lengkap.
    public static List<int> MismatchedSections(
        IDictionary<int, int> left, IDictionary<int, int> right)
    {
        var allKeys = left.Keys.Union(right.Keys)
            .OrderBy(k => k == LainnyaKey) // Lainnya terakhir
            .ThenBy(k => k)
            .ToList();

        var mismatched = new List<int>();
        foreach (var sn in allKeys)
        {
            int x = left.TryGetValue(sn, out var xc) ? xc : 0;
            int y = right.TryGetValue(sn, out var yc) ? yc : 0;
            if (x != y)
                mismatched.Add(sn);
        }
        return mismatched;
    }

    // Label tampilan key: LainnyaKey → "Lainnya", selain itu angka apa adanya.
    public static string SectionLabel(int sectionKey) => sectionKey == LainnyaKey ? "Lainnya" : sectionKey.ToString();
}
