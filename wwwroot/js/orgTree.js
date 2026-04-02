// Organization Tree — JS utilities
// Phase 292: AJAX utility | Phase 293+: tree rendering & CRUD

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

// Phase 293: Tree rendering functions

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

function renderNode(node, level) {
    const isExpanded = level < 2;
    const hasChildren = node.children && node.children.length > 0;
    const dimmed = !node.isActive ? 'style="opacity:0.5"' : '';

    const icon = level === 0 ? 'bi-building' : level === 1 ? 'bi-diagram-3' : 'bi-dot';
    const badge = node.isActive
        ? '<span class="badge rounded-pill bg-success badge-status">Aktif</span>'
        : '<span class="badge rounded-pill bg-danger badge-status">Nonaktif</span>';

    const chevron = hasChildren
        ? `<i class="bi bi-chevron-right tree-chevron${isExpanded ? ' expanded' : ''}"></i>`
        : '<span class="tree-chevron-placeholder"></span>';

    let childrenHtml = '';
    if (hasChildren) {
        const childItems = node.children.map(c => renderNode(c, level + 1)).join('');
        const display = isExpanded ? '' : 'style="display:none"';
        childrenHtml = `<ul class="tree-children" ${display}>${childItems}</ul>`;
    }

    return `
        <li class="tree-node" data-expanded="${isExpanded}" data-id="${node.id}">
            <div class="tree-row d-flex align-items-center gap-2" ${dimmed}>
                ${chevron}
                <i class="bi ${icon}"></i>
                <span class="tree-label">${escapeHtml(node.name)}</span>
                ${badge}
            </div>
            ${childrenHtml}
        </li>`;
}

function updateExpandAllButton() {
    const btn = document.getElementById('btn-expand-all');
    if (!btn) return;
    const nodes = document.querySelectorAll('.tree-node');
    const hasCollapsed = Array.from(nodes).some(n => n.dataset.expanded === 'false' && n.querySelector('.tree-children'));
    btn.textContent = hasCollapsed ? 'Expand All' : 'Collapse All';
}

async function initTree() {
    const container = document.getElementById('org-tree-container');
    if (!container) return;

    container.innerHTML = `
        <div class="text-center py-5">
            <div class="spinner-border text-primary" role="status"></div>
            <div class="mt-2 text-muted small">Memuat struktur organisasi...</div>
        </div>`;

    try {
        const flat = await ajaxGet('/Organization/GetOrganizationTree');
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
    } catch (err) {
        container.innerHTML = `
            <div class="alert alert-danger">
                <i class="bi bi-exclamation-triangle me-2"></i>
                Gagal memuat struktur organisasi. Periksa koneksi dan muat ulang halaman.
            </div>`;
    }
}

// Event listeners — guard: hanya aktif jika halaman ini punya org-tree-container
document.addEventListener('DOMContentLoaded', function () {
    const container = document.getElementById('org-tree-container');
    if (!container) return;

    // Expand/collapse per node via event delegation
    container.addEventListener('click', function (e) {
        const row = e.target.closest('.tree-row');
        if (!row) return;
        const node = row.closest('.tree-node');
        const children = node.querySelector('.tree-children');
        if (!children) return;
        const isExpanded = node.dataset.expanded === 'true';
        children.style.display = isExpanded ? 'none' : '';
        node.dataset.expanded = isExpanded ? 'false' : 'true';
        row.querySelector('.tree-chevron')?.classList.toggle('expanded', !isExpanded);
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
                children.style.display = expand ? '' : 'none';
                node.dataset.expanded = expand ? 'true' : 'false';
                node.querySelector('.tree-row .tree-chevron')?.classList.toggle('expanded', expand);
            });
            this.textContent = hasCollapsed ? 'Collapse All' : 'Expand All';
        });
    }
});
