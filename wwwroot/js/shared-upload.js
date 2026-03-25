/**
 * Shared upload zone with drag-and-drop support.
 *
 * Usage:
 *   initUploadZone({
 *       zoneId: 'uploadZone',
 *       inputId: 'excelFileInput',
 *       textId: 'dropText',
 *       buttonId: 'btnImport',
 *       accept: ['.xlsx'],                    // file extensions
 *       showSize: false                       // show file size in drop text
 *   });
 */
function initUploadZone(opts) {
    var fileInput = document.getElementById(opts.inputId);
    var zone = document.getElementById(opts.zoneId);
    var dropText = document.getElementById(opts.textId);
    var btn = document.getElementById(opts.buttonId);
    var accept = opts.accept || ['.xlsx'];
    var showSize = opts.showSize || false;

    if (!fileInput || !zone || !dropText || !btn) return;

    function setSelected(file) {
        var label = 'File dipilih: ' + file.name;
        if (showSize) {
            label = 'File: ' + file.name + ' (' + (file.size / (1024 * 1024)).toFixed(2) + 'MB)';
        }
        dropText.textContent = label;
        zone.style.borderColor = '#16a34a';
        zone.style.backgroundColor = '#f0fdf4';
        btn.disabled = false;
    }

    function isAccepted(name) {
        var lower = name.toLowerCase();
        return accept.some(function (ext) { return lower.endsWith(ext); });
    }

    fileInput.addEventListener('change', function () {
        if (this.files.length > 0) setSelected(this.files[0]);
    });

    zone.addEventListener('dragover', function (e) {
        e.preventDefault();
        zone.classList.add('drag-over');
    });

    zone.addEventListener('dragleave', function () {
        zone.classList.remove('drag-over');
    });

    zone.addEventListener('drop', function (e) {
        e.preventDefault();
        zone.classList.remove('drag-over');
        var file = e.dataTransfer.files[0];
        if (file && isAccepted(file.name)) {
            var dt = new DataTransfer();
            dt.items.add(file);
            fileInput.files = dt.files;
            setSelected(file);
        } else {
            dropText.textContent = 'Format file tidak didukung!';
            zone.style.borderColor = '#dc2626';
            zone.style.backgroundColor = '#fef2f2';
        }
    });
}
