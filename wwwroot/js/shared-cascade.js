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
