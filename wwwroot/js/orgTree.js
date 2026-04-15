// Organization Tree — JS utilities
// Phase 292: AJAX utility | Phase 293: tree rendering | Phase 294: AJAX CRUD + kebab dropdown
// v2: Portal-consistent redesign with color-coded icons, dashed connectors, stats bar

let _flatUnits = [];
let _expandedIds = new Set();

function getAntiForgeryToken() {
    const input = document.querySelector('input[name="__RequestVerificationToken"]');
    return input ? input.value : '';
}

async function ajaxPost(url, data = {}) {
    const params = new URLSearchParams(data);
    params.append('__RequestVerificationToken', getAntiForgeryToken());
    const res = await fetch(url, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
            'X-Requested-With': 'XMLHttpRequest'
        },
        body: params.toString()
    });
    if (!res.ok) throw new Error(`HTTP ${res.status}`);
    return res.json();
}

async function ajaxGet(url) {
    const res = await fetch(url, {
        headers: { 'X-Requested-With': 'XMLHttpRequest' }
    });
    if (!res.ok) throw new Error(`HTTP ${res.status}`);
    return res.json();
}

// Utility

function escapeHtml(str) {
    return String(str)
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;');
}

function buildTree(flatList) {
    const map = new Map();
    const roots = [];
    flatList.forEach(u => { u.children = []; map.set(u.id, u); });
    flatList.forEach(u => {
        if (u.parentId === null) {
            roots.push(u);
        } else {
            const parent = map.get(u.parentId);
            if (parent) parent.children.push(u);
        }
    });
    return roots;
}

// Expand state

function saveExpandState() {
    _expandedIds = new Set();
    document.querySelectorAll('.tree-node[data-expanded="true"]').forEach(n => {
        _expandedIds.add(parseInt(n.dataset.id));
    });
}

function setDefaultExpandState() {
    _flatUnits.forEach(u => {
        if (u.level < 2) _expandedIds.add(u.id);
    });
}

// Stats bar

function renderStats(flatList) {
    const bar = document.getElementById('org-stats-bar');
    if (!bar) return;
    const roots = flatList.filter(u => u.parentId === null).length;
    const total = flatList.length;
    const active = flatList.filter(u => u.isActive).length;
    bar.innerHTML = `
        <div class="org-stat">
            <div class="org-stat-icon blue"><i class="bi bi-building"></i></div>
            <div>
                <div class="org-stat-value">${roots}</div>
                <div class="org-stat-label">Bagian</div>
            </div>
        </div>
        <div class="org-stat">
            <div class="org-stat-icon purple"><i class="bi bi-diagram-3"></i></div>
            <div>
                <div class="org-stat-value">${total}</div>
                <div class="org-stat-label">Total Unit</div>
            </div>
        </div>
        <div class="org-stat">
            <div class="org-stat-icon green"><i class="bi bi-check-circle"></i></div>
            <div>
                <div class="org-stat-value">${active}</div>
                <div class="org-stat-label">Aktif</div>
            </div>
        </div>`;
}

// Tree rendering

function countChildren(node) {
    if (!node.children) return 0;
    let count = node.children.length;
    node.children.forEach(c => { count += countChildren(c); });
    return count;
}

function renderNode(node, level) {
    const hasChildren = node.children && node.children.length > 0;
    const dimmed = !node.isActive ? 'style="opacity:0.5"' : '';

    const iconClass = level === 0 ? 'bi-building' : level === 1 ? 'bi-diagram-3' : 'bi-gear';
    const levelClass = level <= 2 ? `level-${level}` : 'level-2';

    const badge = node.isActive
        ? '<span class="badge rounded-pill bg-success" style="font-size:0.72rem;">Aktif</span>'
        : '<span class="badge rounded-pill bg-danger" style="font-size:0.72rem;">Nonaktif</span>';

    const chevron = hasChildren
        ? `<div class="tree-chevron${_expandedIds.has(node.id) ? ' expanded' : ''}"><i class="bi bi-chevron-right"></i></div>`
        : '<span class="tree-chevron-placeholder"></span>';

    const childCount = hasChildren
        ? `<span class="org-child-count">${node.children.length} unit</span>`
        : '';

    const toggleLabel = node.isActive ? 'Nonaktifkan' : 'Aktifkan';
    const toggleIcon = node.isActive ? 'bi-toggle-off' : 'bi-toggle-on';

    const dropdown = `
        <div class="dropdown ms-auto flex-shrink-0 action-dropdown">
            <button class="btn btn-sm btn-link text-muted p-0 lh-1" type="button" data-bs-toggle="dropdown" aria-expanded="false" onclick="event.stopPropagation()">
                <i class="bi bi-three-dots-vertical"></i>
            </button>
            <ul class="dropdown-menu dropdown-menu-end">
                <li><a class="dropdown-item" href="#" onclick="event.preventDefault(); openAddModal(${node.id})"><i class="bi bi-plus-circle me-2"></i>Tambah Sub-unit</a></li>
                <li><a class="dropdown-item" href="#" onclick="event.preventDefault(); openEditModal(${node.id})"><i class="bi bi-pencil me-2"></i>Edit</a></li>
                <li><a class="dropdown-item" href="#" onclick="event.preventDefault(); doToggle(${node.id})"><i class="bi ${toggleIcon} me-2"></i>${toggleLabel}</a></li>
                <li><hr class="dropdown-divider"></li>
                <li><a class="dropdown-item text-danger" href="#" onclick="event.preventDefault(); openDeleteModal(${node.id}, '${escapeHtml(node.name)}', ${hasChildren ? node.children.length : 0})"><i class="bi bi-trash me-2"></i>Hapus</a></li>
            </ul>
        </div>`;

    let childrenHtml = '';
    if (hasChildren) {
        const childItems = node.children.map(c => renderNode(c, level + 1)).join('');
        const isExpanded = _expandedIds.has(node.id);
        const display = isExpanded ? '' : 'style="display:none"';
        childrenHtml = `<ul class="tree-children" ${display}>${childItems}</ul>`;
    }

    const isExpanded = hasChildren && _expandedIds.has(node.id);

    return `
        <li class="tree-node" data-expanded="${isExpanded}" data-id="${node.id}">
            <div class="tree-row d-flex align-items-center gap-2" ${dimmed}>
                <div class="org-row-glow"></div>
                <i class="bi bi-grip-vertical drag-handle"></i>
                ${chevron}
                <div class="org-node-icon ${levelClass}"><i class="bi ${iconClass}"></i></div>
                <span class="tree-label">${escapeHtml(node.name)}</span>
                ${childCount}
                ${badge}
                ${dropdown}
            </div>
            ${childrenHtml}
        </li>`;
}

function updateExpandAllButton() {
    const btn = document.getElementById('btn-expand-all');
    if (!btn) return;
    const nodes = document.querySelectorAll('.tree-node');
    const hasCollapsed = Array.from(nodes).some(n => n.dataset.expanded === 'false' && n.querySelector('.tree-children'));
    btn.innerHTML = hasCollapsed
        ? '<i class="bi bi-arrows-expand me-1"></i>Expand All'
        : '<i class="bi bi-arrows-collapse me-1"></i>Collapse All';
}

// Init / refresh tree

async function initTree() {
    const container = document.getElementById('org-tree-container');
    if (!container) return;

    const isRefresh = container.querySelector('.tree-node') !== null;
    if (isRefresh) saveExpandState();

    container.innerHTML = `
        <div class="text-center py-5">
            <div class="spinner-border text-primary" role="status"></div>
            <div class="mt-2 text-muted small">Memuat struktur organisasi...</div>
        </div>`;

    try {
        const flat = await ajaxGet('GetOrganizationTree');
        _flatUnits = flat;
        if (!isRefresh) setDefaultExpandState();
        renderStats(flat);
        const roots = buildTree(flat);
        if (roots.length === 0) {
            container.innerHTML = `
                <div class="text-center text-muted py-5">
                    <i class="bi bi-diagram-3 d-block mb-2 fs-3"></i>
                    <strong>Belum ada unit organisasi</strong>
                    <div class="small mt-1">Struktur organisasi masih kosong. Tambah Bagian baru untuk memulai.</div>
                </div>`;
            return;
        }
        const html = `<ul class="tree-root list-unstyled mb-0">${roots.map(r => renderNode(r, 0)).join('')}</ul>`;
        container.innerHTML = html;
        updateExpandAllButton();
        initSortable();
    } catch (err) {
        container.innerHTML = `
            <div class="alert alert-danger">
                <i class="bi bi-exclamation-triangle me-2"></i>
                Gagal memuat struktur organisasi. Periksa koneksi dan muat ulang halaman.
            </div>`;
    }
}

function initSortable() {
    if (typeof Sortable === 'undefined') return;
    document.querySelectorAll('.tree-children, .tree-root').forEach(function(ul) {
        Sortable.create(ul, {
            handle: '.drag-handle',
            animation: 150,
            ghostClass: 'sortable-ghost',
            chosenClass: 'sortable-chosen',
            group: false,
            onEnd: function(evt) {
                if (evt.oldIndex === evt.newIndex) return;
                var container = evt.from;
                var parentNode = container.closest('.tree-node');
                var parentId = parentNode ? parentNode.dataset.id : '';
                var orderedIds = Array.from(container.children)
                    .map(function(li) { return li.dataset.id; })
                    .filter(function(id) { return id; })
                    .join(',');
                ajaxPost('ReorderBatch', { parentId: parentId, orderedIds: orderedIds })
                    .then(function(res) {
                        if (res.success) {
                            showToast(res.message, 'success');
                        } else {
                            showToast(res.message, 'danger');
                            initTree();
                        }
                    })
                    .catch(function() {
                        showToast('Gagal mengubah urutan.', 'danger');
                        initTree();
                    });
            }
        });
    });
}

// Modal helpers

function findUnit(id) {
    return _flatUnits.find(u => u.id === id);
}

function getDescendantIds(id) {
    const ids = new Set();
    function walk(parentId) {
        _flatUnits.filter(u => u.parentId === parentId).forEach(u => {
            ids.add(u.id);
            walk(u.id);
        });
    }
    walk(id);
    return ids;
}

function populateParentDropdown(excludeId) {
    const select = document.getElementById('unitModalParent');
    select.innerHTML = '<option value="">— Tidak ada (root) —</option>';
    const excludeIds = excludeId ? getDescendantIds(excludeId) : new Set();
    if (excludeId) excludeIds.add(excludeId);

    _flatUnits
        .filter(u => u.isActive && !excludeIds.has(u.id))
        .sort((a, b) => a.level - b.level || a.displayOrder - b.displayOrder)
        .forEach(u => {
            const indent = '\u00A0'.repeat(u.level * 4);
            const opt = document.createElement('option');
            opt.value = u.id;
            opt.textContent = indent + u.name;
            select.appendChild(opt);
        });
}

// CRUD modal functions

function openAddModal(parentId) {
    document.getElementById('unitModalId').value = '';
    document.getElementById('unitModalName').value = '';
    document.getElementById('unitModalLabel').textContent = 'Tambah Unit';
    document.getElementById('unitModalSubmit').textContent = 'Tambah';
    document.getElementById('unitModalWarning').classList.add('d-none');
    document.getElementById('unitModalName').classList.remove('is-invalid');
    populateParentDropdown(null);
    if (parentId) {
        document.getElementById('unitModalParent').value = parentId;
    }
    new bootstrap.Modal(document.getElementById('unitModal')).show();
}

function openEditModal(id) {
    const unit = findUnit(id);
    if (!unit) return;
    document.getElementById('unitModalId').value = id;
    document.getElementById('unitModalName').value = unit.name;
    document.getElementById('unitModalLabel').textContent = 'Edit Unit';
    document.getElementById('unitModalSubmit').textContent = 'Simpan Perubahan';
    document.getElementById('unitModalWarning').classList.add('d-none');
    document.getElementById('unitModalName').classList.remove('is-invalid');
    populateParentDropdown(id);
    document.getElementById('unitModalParent').value = unit.parentId || '';
    new bootstrap.Modal(document.getElementById('unitModal')).show();
}

async function submitUnitModal() {
    const id = document.getElementById('unitModalId').value;
    const name = document.getElementById('unitModalName').value.trim();
    const parentId = document.getElementById('unitModalParent').value;

    if (!name) {
        document.getElementById('unitModalName').classList.add('is-invalid');
        return;
    }

    const isEdit = id !== '';
    const url = isEdit ? 'EditOrganizationUnit' : 'AddOrganizationUnit';
    const data = isEdit
        ? { id: id, name: name, parentId: parentId }
        : { name: name, parentId: parentId };

    try {
        const result = await ajaxPost(url, data);
        bootstrap.Modal.getInstance(document.getElementById('unitModal')).hide();
        showToast(result.message, result.success ? 'success' : 'danger');
        if (result.success) await initTree();
    } catch (err) {
        showToast('Terjadi kesalahan. Silakan coba lagi.', 'danger');
    }
}

// Toggle

async function doToggle(id) {
    try {
        const result = await ajaxPost('ToggleOrganizationUnitActive', { id: id });
        showToast(result.message, result.success ? 'success' : 'danger');
        if (result.success) await initTree();
    } catch (err) {
        showToast('Terjadi kesalahan. Silakan coba lagi.', 'danger');
    }
}

// Delete

function openDeleteModal(id, name, childCount) {
    document.getElementById('deleteModalId').value = id;
    document.getElementById('deleteModalName').textContent = name;
    const warning = document.getElementById('deleteModalWarning');
    if (childCount > 0) {
        warning.textContent = 'Unit ini memiliki ' + childCount + ' sub-unit.';
        warning.classList.remove('d-none');
    } else {
        warning.classList.add('d-none');
    }
    new bootstrap.Modal(document.getElementById('deleteModal')).show();
}

async function submitDelete() {
    const id = document.getElementById('deleteModalId').value;
    try {
        const result = await ajaxPost('DeleteOrganizationUnit', { id: id });
        bootstrap.Modal.getInstance(document.getElementById('deleteModal')).hide();
        showToast(result.message, result.success ? 'success' : 'danger');
        if (result.success) await initTree();
    } catch (err) {
        showToast('Terjadi kesalahan. Silakan coba lagi.', 'danger');
    }
}

// Event listeners — guard: hanya aktif jika halaman ini punya org-tree-container
document.addEventListener('DOMContentLoaded', function () {
    const container = document.getElementById('org-tree-container');
    if (!container) return;

    // Expand/collapse per node via event delegation
    container.addEventListener('click', function (e) {
        if (e.target.closest('.action-dropdown')) return;
        const row = e.target.closest('.tree-row');
        if (!row) return;
        const node = row.closest('.tree-node');
        const children = node.querySelector('.tree-children');
        if (!children) return;

        const isExpanded = node.dataset.expanded === 'true';
        const chevron = row.querySelector('.tree-chevron');

        if (isExpanded) {
            children.style.maxHeight = children.scrollHeight + 'px';
            requestAnimationFrame(function () {
                children.style.maxHeight = '0';
                children.style.opacity = '0';
            });
            setTimeout(function () {
                children.style.display = 'none';
                children.style.maxHeight = '';
                children.style.opacity = '';
            }, 300);
        } else {
            children.style.display = '';
            children.style.maxHeight = '0';
            children.style.opacity = '0';
            requestAnimationFrame(function () {
                children.style.maxHeight = children.scrollHeight + 'px';
                children.style.opacity = '1';
            });
            setTimeout(function () { children.style.maxHeight = ''; }, 300);
        }

        node.dataset.expanded = isExpanded ? 'false' : 'true';
        if (chevron) chevron.classList.toggle('expanded', !isExpanded);
        updateExpandAllButton();
    });

    // Expand All / Collapse All
    const btnExpandAll = document.getElementById('btn-expand-all');
    if (btnExpandAll) {
        btnExpandAll.addEventListener('click', function () {
            const nodes = document.querySelectorAll('.tree-node');
            const hasCollapsed = Array.from(nodes).some(n => n.dataset.expanded === 'false' && n.querySelector('.tree-children'));
            nodes.forEach(function (node) {
                const children = node.querySelector('.tree-children');
                if (!children) return;
                const expand = hasCollapsed;
                if (expand) {
                    children.style.display = '';
                    children.style.maxHeight = '';
                    children.style.opacity = '';
                } else {
                    children.style.display = 'none';
                }
                node.dataset.expanded = expand ? 'true' : 'false';
                var chevron = node.querySelector('.tree-row .tree-chevron');
                if (chevron) chevron.classList.toggle('expanded', expand);
            });
            updateExpandAllButton();
        });
    }
});
