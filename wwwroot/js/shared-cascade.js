/**
 * Shared Section → Unit cascading dropdown.
 *
 * Usage:
 *   initSectionUnitCascade({
 *       sectionUnits: { "RFCC": ["Unit A", "Unit B"], ... },
 *       sectionId: 'sectionSelect',
 *       unitId: 'unitSelect',
 *       currentSection: '',
 *       currentUnit: ''
 *   });
 */
function initSectionUnitCascade(opts) {
    var sectionSelect = document.getElementById(opts.sectionId);
    var unitSelect = document.getElementById(opts.unitId);
    var sectionUnits = opts.sectionUnits || {};
    var currentUnit = opts.currentUnit || '';

    if (!sectionSelect || !unitSelect) return;

    Object.keys(sectionUnits).forEach(function (section) {
        var opt = document.createElement('option');
        opt.value = section;
        opt.textContent = section;
        sectionSelect.appendChild(opt);
    });

    function updateUnits(section) {
        unitSelect.innerHTML = '<option value="">-- Pilih Unit --</option>';
        if (section && sectionUnits[section]) {
            sectionUnits[section].forEach(function (unit) {
                var opt = document.createElement('option');
                opt.value = unit;
                opt.textContent = unit;
                if (unit === currentUnit) opt.selected = true;
                unitSelect.appendChild(opt);
            });
        }
    }

    if (opts.currentSection) {
        sectionSelect.value = opts.currentSection;
        updateUnits(opts.currentSection);
    }

    sectionSelect.addEventListener('change', function () {
        updateUnits(this.value);
    });
}

/**
 * Multi-select variant: Section (single <select>) → Unit checkbox-list + "Primary" radio per row.
 *
 * Phase 399 (MU-01/MU-02). Bagian tetap single <select id=sectionSelect>; Unit jadi container
 * checkbox+radio yang di-render ulang client-side dari dict (ViewBag.SectionUnitsJson, no AJAX).
 * Mengikuti UI-SPEC §A state machine PERSIS.
 *
 * Usage:
 *   initSectionUnitMultiCascade({
 *       sectionUnits: { "RFCC": ["Unit A", "Unit B"], ... },
 *       sectionId: 'sectionSelect',
 *       containerId: 'unitMultiContainer',
 *       currentSection: '',          // Edit: Bagian tersimpan
 *       selectedUnits: ['Unit A'],   // Edit: unit ter-centang
 *       primaryUnit: 'Unit A'        // Edit: unit utama (radio ter-pilih)
 *   });
 *
 * Model-binding: banyak checkbox name="Units" → List<string> Units; satu radio name="PrimaryUnit"
 * yang ter-check → string? PrimaryUnit (standar ASP.NET Core MVC).
 */
function initSectionUnitMultiCascade(opts) {
    var sectionSelect = document.getElementById(opts.sectionId);
    var container = document.getElementById(opts.containerId);
    var sectionUnits = opts.sectionUnits || {};
    // Snapshot pilihan awal (Edit pre-check); setelah operator berinteraksi, state hidup di DOM.
    var selected = (opts.selectedUnits || []).slice();
    var primary = opts.primaryUnit || '';
    var placeholder = (container && container.getAttribute('data-placeholder')) ||
        'Pilih Bagian dahulu untuk menampilkan daftar Unit.';

    if (!sectionSelect || !container) return;

    // HTML-escape nama unit sebelum masuk innerHTML (defense T-399-03-04; nama unit admin-curated
    // tapi tetap di-escape supaya karakter <,>,&," tidak merusak markup).
    function esc(s) {
        return String(s).replace(/[&<>"']/g, function (c) {
            return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c];
        });
    }

    // Populate Section <select> options dari dict (idiom sama initSectionUnitCascade:21-26),
    // jangan dobel bila view sudah meng-isi.
    Object.keys(sectionUnits).forEach(function (section) {
        var exists = false;
        for (var k = 0; k < sectionSelect.options.length; k++) {
            if (sectionSelect.options[k].value === section) { exists = true; break; }
        }
        if (!exists) {
            var opt = document.createElement('option');
            opt.value = section;
            opt.textContent = section;
            sectionSelect.appendChild(opt);
        }
    });

    function rows() { return container.querySelectorAll('.uu-check'); }

    // State: tepat 1 radio ter-check saat ≥1 checkbox ter-centang (UI-SPEC §A "Primary selected").
    // Bila belum ada primary tercentang → default = unit tercentang PERTAMA (D-02).
    function syncPrimaryDefault() {
        var checks = container.querySelectorAll('.uu-check');
        var anyPrimaryChecked = false;
        var firstCheckedRadio = null;
        for (var i = 0; i < checks.length; i++) {
            var chk = checks[i];
            var radio = chk.parentNode.querySelector('.uu-primary');
            if (chk.checked) {
                radio.disabled = false;
                if (firstCheckedRadio === null) firstCheckedRadio = radio;
                if (radio.checked) anyPrimaryChecked = true;
            } else {
                radio.checked = false;
                radio.disabled = true;
            }
        }
        if (!anyPrimaryChecked && firstCheckedRadio !== null) {
            firstCheckedRadio.checked = true; // default primary = first checked (D-02)
        }
    }

    function onCheckChange(chk) {
        var radio = chk.parentNode.querySelector('.uu-primary');
        if (chk.checked) {
            radio.disabled = false;
            // Bila belum ada primary di grup → jadikan unit ini primary (default first-checked, D-02).
            var anyPrimary = container.querySelector('.uu-primary:checked');
            if (!anyPrimary) radio.checked = true;
        } else {
            var wasPrimary = radio.checked;
            radio.checked = false;
            radio.disabled = true;
            // Bila ia primary yang di-uncheck → promote ke unit tercentang pertama yang tersisa.
            if (wasPrimary) {
                var checks = container.querySelectorAll('.uu-check');
                for (var i = 0; i < checks.length; i++) {
                    if (checks[i].checked) {
                        checks[i].parentNode.querySelector('.uu-primary').checked = true;
                        break;
                    }
                }
            }
        }
    }

    function render(section) {
        container.innerHTML = '';
        var units = sectionUnits[section] || [];
        if (!units.length) {
            container.innerHTML = '<span class="text-muted small">' + esc(placeholder) + '</span>';
            return;
        }
        units.forEach(function (unit, i) {
            var checked = selected.indexOf(unit) >= 0;
            var isPrim = checked && unit === primary;
            var row = document.createElement('div');
            row.className = 'd-flex align-items-center gap-2 mb-1';
            var eu = esc(unit);
            row.innerHTML =
                '<input type="checkbox" name="Units" value="' + eu + '" id="uu-chk-' + i + '" class="form-check-input uu-check mt-0"' + (checked ? ' checked' : '') + '>' +
                '<input type="radio" name="PrimaryUnit" value="' + eu + '" id="uu-prim-' + i + '" class="form-check-input uu-primary mt-0"' + (isPrim ? ' checked' : '') + (checked ? '' : ' disabled') + '>' +
                '<label for="uu-chk-' + i + '" class="form-check-label small flex-grow-1">' + eu + '</label>' +
                '<label for="uu-prim-' + i + '" class="form-check-label small text-success">Utama</label>';
            container.appendChild(row);
        });
        // Wire per-row checkbox change (UI-SPEC §A): enable/disable+clear radio, default/promote primary.
        var checks = container.querySelectorAll('.uu-check');
        checks.forEach(function (chk) {
            chk.addEventListener('change', function () { onCheckChange(chk); });
        });
        // Pasca-render: pastikan invariant 1-primary-saat-≥1-checked (default first checked, D-02).
        syncPrimaryDefault();
    }

    if (opts.currentSection) {
        sectionSelect.value = opts.currentSection;
        render(opts.currentSection);
    } else {
        render('');
    }

    // Ganti Bagian → reset pilihan lama; unit harus anak Bagian baru (invariant #1).
    sectionSelect.addEventListener('change', function () {
        selected = [];
        primary = '';
        render(this.value);
    });
}

/**
 * Shared password visibility toggle.
 *
 * Usage: onclick="togglePassword('passwordField', this)"
 */
function togglePassword(fieldId, btn) {
    var field = document.getElementById(fieldId);
    var icon = btn.querySelector('i');
    if (field.type === 'password') {
        field.type = 'text';
        icon.className = 'bi bi-eye-slash';
    } else {
        field.type = 'password';
        icon.className = 'bi bi-eye';
    }
}
