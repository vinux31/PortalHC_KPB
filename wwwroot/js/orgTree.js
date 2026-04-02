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
