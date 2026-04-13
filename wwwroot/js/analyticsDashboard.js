/**
 * analyticsDashboard.js
 * Extracted & enhanced from AnalyticsDashboard.cshtml inline JS.
 * Requires: Chart.js 4+, chartjs-plugin-datalabels, chartjs-plugin-annotation, Bootstrap 5
 */

/* global Chart, appUrl */

// ============================================================
// State
// ============================================================
var failRateChart = null;
var trendChart = null;
var analyticsAbort = null;
var _gainScoreData = null;
var _currentFailRateData = null;

var _config = {};
var _tabCache = {};
var _cacheInvalid = false;

var _summaryDonutChart = null;
var _gainTrendChart = null;

var _etSortCol = -1;
var _etSortDir = 'asc';

// ============================================================
// DOMContentLoaded
// ============================================================
document.addEventListener('DOMContentLoaded', function () {
    // Read config from data attributes
    var cfgEl = document.getElementById('analyticsConfig');
    if (cfgEl) {
        _config.getAnalyticsSummaryUrl = cfgEl.dataset.summaryUrl;
        _config.getFailRateUrl = cfgEl.dataset.failRateUrl;
        _config.getTrendUrl = cfgEl.dataset.trendUrl;
        _config.getEtBreakdownUrl = cfgEl.dataset.etBreakdownUrl;
        _config.getExpiringSoonUrl = cfgEl.dataset.expiringSoonUrl;
        _config.getDrillDownUrl = cfgEl.dataset.drillDownUrl;
        _config.exportFailRateUrl = cfgEl.dataset.exportFailRateUrl;
        _config.exportTrendUrl = cfgEl.dataset.exportTrendUrl;
        _config.exportEtBreakdownUrl = cfgEl.dataset.exportEtBreakdownUrl;
        _config.getCascadeUnitsUrl = cfgEl.dataset.cascadeUnitsUrl;
        _config.getCascadeSubKategoriUrl = cfgEl.dataset.cascadeSubKategoriUrl;
        _config.getPrePostAssessmentListUrl = cfgEl.dataset.assessmentListUrl;
        _config.getItemAnalysisUrl = cfgEl.dataset.itemAnalysisUrl;
        _config.getGainScoreUrl = cfgEl.dataset.gainScoreUrl;
        _config.exportItemAnalysisUrl = cfgEl.dataset.exportItemAnalysisUrl;
        _config.exportGainScoreUrl = cfgEl.dataset.exportGainScoreUrl;
    }

    // Set default dates
    var today = new Date();
    var oneYearAgo = new Date(today.getFullYear() - 1, today.getMonth(), today.getDate());
    document.getElementById('filterStart').value = oneYearAgo.toISOString().slice(0, 10);
    document.getElementById('filterEnd').value = today.toISOString().slice(0, 10);

    // Load summary cards
    loadSummaryCards();

    // Load first tab (fail rate)
    loadTabData('failrate');

    // Setup event listeners
    setupEventListeners();
});

// ============================================================
// getFilterParams — build URLSearchParams from filter inputs
// ============================================================
function getFilterParams() {
    var params = new URLSearchParams();
    var bagian = document.getElementById('filterBagian').value;
    var unit = document.getElementById('filterUnit').value;
    var kategori = document.getElementById('filterKategori').value;
    var subKategori = document.getElementById('filterSubKategori').value;
    var periodeStart = document.getElementById('filterStart').value;
    var periodeEnd = document.getElementById('filterEnd').value;

    if (bagian) params.set('bagian', bagian);
    if (unit) params.set('unit', unit);
    if (kategori) params.set('kategori', kategori);
    if (subKategori) params.set('subKategori', subKategori);
    if (periodeStart) params.set('periodeStart', periodeStart);
    if (periodeEnd) params.set('periodeEnd', periodeEnd);

    return params;
}

// ============================================================
// loadSummaryCards — fetch summary data, render skeleton → real
// ============================================================
function loadSummaryCards() {
    var skeletonIds = ['summaryTotalSessions', 'summaryPassRate', 'summaryExpiring', 'summaryAvgGain'];
    skeletonIds.forEach(function (id) {
        var el = document.getElementById(id);
        if (el) el.innerHTML = '<div class="skeleton" style="width:60px;">&nbsp;</div>';
    });

    var params = getFilterParams();
    var url = _config.getAnalyticsSummaryUrl
        ? _config.getAnalyticsSummaryUrl + '?' + params.toString()
        : appUrl('/CMP/GetAnalyticsSummary?' + params.toString());

    fetch(url)
        .then(function (r) { return r.json(); })
        .then(function (data) {
            var setCard = function (id, value) {
                var el = document.getElementById(id);
                if (el) el.textContent = value;
            };
            setCard('summaryTotalSessions', (data.totalSessions || 0).toLocaleString('id-ID'));
            setCard('summaryPassRate', (data.passRate || 0).toFixed(1) + '%');
            setCard('summaryExpiring', (data.expiringCount || 0).toLocaleString('id-ID'));
            setCard('summaryAvgGain', (data.avgGainScore || 0).toFixed(1) + '%');
        })
        .catch(function (e) {
            console.error('Summary fetch error:', e);
            skeletonIds.forEach(function (id) {
                var el = document.getElementById(id);
                if (el) el.textContent = '-';
            });
        });
}

// ============================================================
// loadTabData — lazy load per tab with caching
// ============================================================
function loadTabData(tabName, forceRefresh) {
    // Check cache
    if (!forceRefresh && !_cacheInvalid && _tabCache[tabName]) {
        applyTabData(tabName, _tabCache[tabName]);
        return;
    }

    var params = getFilterParams();

    if (analyticsAbort) analyticsAbort.abort();
    analyticsAbort = new AbortController();

    var url;
    switch (tabName) {
        case 'failrate':
            url = _config.getFailRateUrl
                ? _config.getFailRateUrl + '?' + params.toString()
                : appUrl('/CMP/GetFailRateData?' + params.toString());
            break;
        case 'trend':
            url = _config.getTrendUrl
                ? _config.getTrendUrl + '?' + params.toString()
                : appUrl('/CMP/GetTrendData?' + params.toString());
            break;
        case 'et':
            url = _config.getEtBreakdownUrl
                ? _config.getEtBreakdownUrl + '?' + params.toString()
                : appUrl('/CMP/GetEtBreakdownData?' + params.toString());
            break;
        case 'expiring':
            var thresholdEl = document.getElementById('expiringThreshold');
            var days = thresholdEl ? thresholdEl.value : '30';
            params.set('days', days);
            url = _config.getExpiringSoonUrl
                ? _config.getExpiringSoonUrl + '?' + params.toString()
                : appUrl('/CMP/GetExpiringSoonData?' + params.toString());
            break;
        case 'itemanalysis':
            var assessmentId = document.getElementById('filterAssessment').value;
            if (!assessmentId) {
                showItemAnalysisEmpty('Pilih assessment terlebih dahulu', 'Gunakan dropdown di atas untuk memilih assessment, lalu klik Terapkan Filter.');
                return;
            }
            params.set('assessmentGroupId', assessmentId);
            url = _config.getItemAnalysisUrl
                ? _config.getItemAnalysisUrl + '?' + params.toString()
                : appUrl('/CMP/GetItemAnalysisData?' + params.toString());
            break;
        case 'gainscore':
            var assessmentIdGs = document.getElementById('filterAssessment').value;
            if (!assessmentIdGs) {
                showGainScoreEmpty('Pilih assessment terlebih dahulu', 'Gunakan dropdown di atas untuk memilih assessment, lalu klik Terapkan Filter.');
                return;
            }
            params.set('assessmentGroupId', assessmentIdGs);
            url = _config.getGainScoreUrl
                ? _config.getGainScoreUrl + '?' + params.toString()
                : appUrl('/CMP/GetGainScoreData?' + params.toString());
            break;
        default:
            return;
    }

    // Show loading state on the tab pane
    var paneMap = {
        failrate: 'tabFailRate', trend: 'tabTrend', et: 'tabEtBreakdown',
        expiring: 'tabExpiring', itemanalysis: 'tabItemAnalysis', gainscore: 'tabGainScore'
    };
    var pane = document.getElementById(paneMap[tabName]);
    if (pane) pane.classList.add('dashboard-loading');

    fetch(url, { signal: analyticsAbort.signal })
        .then(function (r) { return r.json(); })
        .then(function (data) {
            _tabCache[tabName] = data;
            if (_cacheInvalid) _cacheInvalid = false;
            applyTabData(tabName, data);
        })
        .catch(function (e) {
            if (e.name !== 'AbortError') {
                console.error('Tab fetch error (' + tabName + '):', e);
            }
        })
        .finally(function () {
            if (pane) pane.classList.remove('dashboard-loading');
        });
}

function applyTabData(tabName, data) {
    switch (tabName) {
        case 'failrate':
            renderFailRate(data.failRate || data);
            break;
        case 'trend':
            renderTrend(data.trend || data);
            if (data.gainScoreTrend !== undefined) {
                renderGainScoreTrend(data.gainScoreTrend);
            }
            break;
        case 'et':
            renderEtTable(data.etBreakdown || data);
            break;
        case 'expiring':
            var thresholdEl = document.getElementById('expiringThreshold');
            var days = thresholdEl ? parseInt(thresholdEl.value, 10) : 30;
            renderExpiringTable(data.expiringSoon || data, days);
            break;
        case 'itemanalysis':
            renderItemAnalysis(data);
            break;
        case 'gainscore':
            renderGainScore(data);
            break;
    }
}

// ============================================================
// invalidateCache
// ============================================================
function invalidateCache() {
    _tabCache = {};
    _cacheInvalid = true;
}

// ============================================================
// renderFailRate — color-coded bars, datalabels, annotation, insight panel
// ============================================================
function renderFailRate(data) {
    var canvas = document.getElementById('failRateChart');
    var emptyEl = document.getElementById('failRateEmpty');
    var insightEl = document.getElementById('failRateInsight');

    if (!data || data.length === 0) {
        canvas.style.display = 'none';
        emptyEl.style.display = '';
        if (insightEl) insightEl.style.display = 'none';
        return;
    }
    canvas.style.display = '';
    emptyEl.style.display = 'none';

    _currentFailRateData = data;

    if (failRateChart) failRateChart.destroy();

    var labels = data.map(function (d) { return d.section + ' - ' + d.category; });
    var values = data.map(function (d) { return d.failRatePercent; });

    // Color-coded bars: green <30%, yellow 30-60%, red >60%
    var bgColors = values.map(function (v) {
        if (v > 60) return 'rgba(220,53,69,0.80)';
        if (v >= 30) return 'rgba(255,193,7,0.80)';
        return 'rgba(40,167,69,0.80)';
    });
    var borderColors = values.map(function (v) {
        if (v > 60) return '#dc3545';
        if (v >= 30) return '#ffc107';
        return '#198754';
    });

    failRateChart = new Chart(canvas, {
        type: 'bar',
        data: {
            labels: labels,
            datasets: [{
                label: 'Fail Rate (%)',
                data: values,
                backgroundColor: bgColors,
                borderColor: borderColors,
                borderWidth: 1
            }]
        },
        options: {
            responsive: true,
            onClick: function (evt) {
                var points = failRateChart.getElementsAtEventForMode(evt, 'nearest', { intersect: true }, true);
                if (points.length > 0) {
                    var idx = points[0].index;
                    var item = _currentFailRateData[idx];
                    fetchDrillDown(item.section, item.category);
                }
            },
            plugins: {
                legend: { display: false },
                datalabels: {
                    anchor: 'end',
                    align: 'top',
                    formatter: function (v) { return v.toFixed(1) + '%'; },
                    font: { size: 11, weight: 'bold' }
                },
                annotation: {
                    annotations: {
                        threshold: {
                            type: 'line',
                            yMin: 30,
                            yMax: 30,
                            borderColor: '#ffc107',
                            borderWidth: 2,
                            borderDash: [6, 6],
                            label: {
                                display: true,
                                content: 'Batas Perhatian (30%)',
                                position: 'start'
                            }
                        }
                    }
                },
                tooltip: {
                    callbacks: {
                        afterBody: function () {
                            return 'Klik bar untuk melihat detail';
                        }
                    }
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    max: 100,
                    ticks: {
                        callback: function (v) { return v + '%'; }
                    }
                }
            }
        }
    });

    // Build insight panel
    renderFailRateInsight(data);
}

// ============================================================
// renderFailRateInsight — summary table + donut beside chart
// ============================================================
function renderFailRateInsight(data) {
    var container = document.getElementById('failRateInsight');
    var legendEl = document.getElementById('failRateLegend');
    if (!container) return;

    container.style.display = '';
    if (legendEl) legendEl.style.display = '';

    var totalSesi = data.reduce(function (s, d) { return s + d.total; }, 0);
    var totalGagal = data.reduce(function (s, d) { return s + d.failed; }, 0);
    var totalLulus = totalSesi - totalGagal;
    var highest = data.reduce(function (a, b) { return a.failRatePercent > b.failRatePercent ? a : b; });

    // Left: summary text
    var textEl = document.getElementById('failRateInsightText');
    if (textEl) {
        textEl.innerHTML =
            'Total Sesi: <strong>' + totalSesi + '</strong> | ' +
            'Lulus: <strong class="text-success">' + totalLulus + '</strong> | ' +
            'Gagal: <strong class="text-danger">' + totalGagal + '</strong> | ' +
            'Fail Rate Tertinggi: <strong class="text-danger">' + escapeHtml(highest.section + ' - ' + highest.category) +
            ' (' + highest.failRatePercent.toFixed(1) + '%)</strong>';
    }

}

// ============================================================
// fetchDrillDown — click bar → modal with detail table
// ============================================================
function fetchDrillDown(section, category) {
    var params = getFilterParams();
    params.set('section', section);
    params.set('category', category);

    var url = _config.getDrillDownUrl
        ? _config.getDrillDownUrl + '?' + params.toString()
        : appUrl('/CMP/GetFailRateDrillDown?' + params.toString());

    var modalLabel = document.getElementById('drillDownModalLabel');
    var modalBody = document.getElementById('drillDownBody');

    if (modalLabel) modalLabel.textContent = 'Detail Fail Rate — ' + section + ' / ' + category;
    if (modalBody) modalBody.innerHTML = '<div class="text-center py-4"><div class="spinner-border text-primary" role="status"></div></div>';

    // Show modal
    var modalEl = document.getElementById('drillDownModal');
    if (modalEl) {
        var modal = bootstrap.Modal.getOrCreateInstance(modalEl);
        modal.show();
    }

    fetch(url)
        .then(function (r) { return r.json(); })
        .then(function (data) {
            if (!data || data.length === 0) {
                modalBody.innerHTML = '<p class="text-muted text-center py-3">Tidak ada data detail untuk kombinasi ini.</p>';
                return;
            }

            var rows = data.map(function (d) {
                var statusBadge = d.status === 'Lulus'
                    ? '<span class="badge bg-success">Lulus</span>'
                    : '<span class="badge bg-danger">Gagal</span>';
                return '<tr>' +
                    '<td>' + escapeHtml(d.namaPekerja) + '</td>' +
                    '<td>' + (d.skor !== null && d.skor !== undefined ? d.skor.toFixed(1) : '-') + '</td>' +
                    '<td>' + statusBadge + '</td>' +
                    '<td>' + (d.tanggalAssessment ? new Date(d.tanggalAssessment).toLocaleDateString('id-ID') : '-') + '</td>' +
                    '</tr>';
            }).join('');

            modalBody.innerHTML =
                '<div class="table-responsive">' +
                '<table class="table table-sm table-hover">' +
                '<thead class="table-light"><tr>' +
                '<th>Nama Pekerja</th><th>Skor</th><th>Status</th><th>Tanggal</th>' +
                '</tr></thead>' +
                '<tbody>' + rows + '</tbody>' +
                '</table>' +
                '</div>';
        })
        .catch(function (e) {
            console.error('Drilldown fetch error:', e);
            modalBody.innerHTML = '<p class="text-danger text-center py-3">Gagal memuat data. Coba lagi nanti.</p>';
        });
}

// ============================================================
// renderTrend — line chart pass/fail with larger axis font
// ============================================================
function renderTrend(data) {
    var canvas = document.getElementById('trendChart');
    var emptyEl = document.getElementById('trendEmpty');

    if (!data || data.length === 0) {
        canvas.style.display = 'none';
        emptyEl.style.display = '';
        return;
    }
    canvas.style.display = '';
    emptyEl.style.display = 'none';

    if (trendChart) trendChart.destroy();

    var labels = data.map(function (d) { return d.label; });

    trendChart = new Chart(canvas, {
        type: 'line',
        data: {
            labels: labels,
            datasets: [
                {
                    label: 'Pass',
                    data: data.map(function (d) { return d.passed; }),
                    borderColor: '#198754',
                    backgroundColor: 'rgba(25,135,84,0.1)',
                    fill: false,
                    tension: 0.3,
                    pointRadius: 4
                },
                {
                    label: 'Fail',
                    data: data.map(function (d) { return d.failed; }),
                    borderColor: '#dc3545',
                    backgroundColor: 'rgba(220,53,69,0.1)',
                    fill: false,
                    tension: 0.3,
                    pointRadius: 4
                }
            ]
        },
        options: {
            responsive: true,
            plugins: {
                legend: { display: true },
                datalabels: { display: false }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    ticks: { font: { size: 13 } }
                },
                x: {
                    ticks: { font: { size: 13 } }
                }
            }
        }
    });

    // Trend insight (best/worst month, direction)
    renderTrendInsight(data);
}

// ============================================================
// renderGainScoreTrend — handle single point, line chart, insight row
// ============================================================
function renderGainScoreTrend(gainScoreTrend) {
    var canvas = document.getElementById('gainScoreTrendChart');
    var emptyMsg = document.getElementById('gainTrendEmpty');
    var insightEl = document.getElementById('gainTrendInsight');

    if (_gainTrendChart) _gainTrendChart.destroy();

    if (!gainScoreTrend || gainScoreTrend.length === 0) {
        canvas.style.display = 'none';
        emptyMsg.style.display = '';
        if (insightEl) insightEl.style.display = 'none';
        return;
    }

    emptyMsg.style.display = 'none';

    // Single data point: show metric card instead of chart
    if (gainScoreTrend.length === 1) {
        canvas.style.display = 'none';
        var single = gainScoreTrend[0];
        var parent = canvas.parentElement;
        var metricId = 'gainTrendSingleMetric';
        var existing = document.getElementById(metricId);
        if (existing) existing.remove();

        var metricDiv = document.createElement('div');
        metricDiv.id = metricId;
        metricDiv.className = 'text-center py-3';
        metricDiv.innerHTML =
            '<div class="card border-0 bg-light d-inline-block px-4 py-3">' +
            '<div class="text-muted small">' + escapeHtml(single.label) + '</div>' +
            '<div class="fs-2 fw-bold text-primary">' + single.avgGainScore.toFixed(1) + '%</div>' +
            '<div class="text-muted small">Sample: ' + single.sampleCount + ' pasangan</div>' +
            '</div>' +
            '<p class="text-muted small mt-2">Hanya 1 bulan data tersedia. Perlu minimal 2 bulan untuk menampilkan trend.</p>';
        parent.insertBefore(metricDiv, canvas.nextSibling);
        return;
    }

    // Remove single metric card if exists
    var existingMetric = document.getElementById('gainTrendSingleMetric');
    if (existingMetric) existingMetric.remove();

    canvas.style.display = '';

    var labels = gainScoreTrend.map(function (d) { return d.label; });
    var values = gainScoreTrend.map(function (d) { return d.avgGainScore; });
    var counts = gainScoreTrend.map(function (d) { return d.sampleCount; });

    _gainTrendChart = new Chart(canvas.getContext('2d'), {
        type: 'line',
        data: {
            labels: labels,
            datasets: [{
                label: 'Avg Gain Score (%)',
                data: values,
                borderColor: '#6f42c1',
                backgroundColor: 'rgba(111,66,193,0.1)',
                fill: true,
                tension: 0.3,
                pointRadius: 4,
                pointHoverRadius: 6
            }]
        },
        options: {
            responsive: true,
            plugins: {
                tooltip: {
                    callbacks: {
                        afterLabel: function (ctx) {
                            return 'Sample: ' + counts[ctx.dataIndex] + ' pasangan';
                        }
                    }
                },
                datalabels: { display: false }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    title: { display: true, text: 'Avg Gain Score (%)' },
                    ticks: { font: { size: 13 } }
                },
                x: {
                    title: { display: true, text: 'Bulan' },
                    ticks: { font: { size: 13 } }
                }
            }
        }
    });

    // Gain trend insight
    renderGainTrendInsight(gainScoreTrend);
}

function renderTrendInsight(trendData) {
    var container = document.getElementById('trendInsight');
    if (!container || !trendData || trendData.length === 0) return;

    container.style.display = '';

    var best = trendData.reduce(function (a, b) { return a.passed > b.passed ? a : b; });
    var worst = trendData.reduce(function (a, b) { return a.failed > b.failed ? a : b; });

    // Slope from last 3 months of pass rate
    var recent = trendData.slice(-3);
    var trendText = '<i class="bi bi-dash-circle-fill text-secondary"></i> Stabil';
    if (recent.length >= 2) {
        var first = recent[0].passed / (recent[0].passed + recent[0].failed || 1);
        var last = recent[recent.length - 1].passed / (recent[recent.length - 1].passed + recent[recent.length - 1].failed || 1);
        var diff = last - first;
        if (diff > 0.05) trendText = '<i class="bi bi-arrow-up-circle-fill text-success"></i> Naik';
        else if (diff < -0.05) trendText = '<i class="bi bi-arrow-down-circle-fill text-danger"></i> Turun';
    }

    var bestEl = document.getElementById('trendBestMonth');
    if (bestEl) bestEl.innerHTML = escapeHtml(best.label) + ' (' + best.passed + ' pass)';
    var worstEl = document.getElementById('trendWorstMonth');
    if (worstEl) worstEl.innerHTML = escapeHtml(worst.label) + ' (' + worst.failed + ' fail)';
    var dirEl = document.getElementById('trendDirection');
    if (dirEl) dirEl.innerHTML = trendText;
}

function renderGainTrendInsight(data) {
    if (!data || data.length < 2) return;

    var bestMonth = data.reduce(function (a, b) { return a.avgGainScore > b.avgGainScore ? a : b; });
    var worstMonth = data.reduce(function (a, b) { return a.avgGainScore < b.avgGainScore ? a : b; });

    // Simple linear slope for trend arrow
    var n = data.length;
    var sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;
    for (var i = 0; i < n; i++) {
        sumX += i;
        sumY += data[i].avgGainScore;
        sumXY += i * data[i].avgGainScore;
        sumX2 += i * i;
    }
    var slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
    var trendIcon = slope > 0.5 ? '<i class="bi bi-arrow-up-circle-fill text-success"></i> Naik'
        : (slope < -0.5 ? '<i class="bi bi-arrow-down-circle-fill text-danger"></i> Turun'
            : '<i class="bi bi-dash-circle-fill text-secondary"></i> Stabil');

    // Append below gain score chart
    var parent = document.getElementById('gainScoreTrendChart').parentElement;
    var existing = document.getElementById('gainTrendInsight');
    if (existing) existing.remove();

    var div = document.createElement('div');
    div.id = 'gainTrendInsight';
    div.className = 'row g-2 mt-2';
    div.innerHTML =
        '<div class="col-md-4"><div class="card border-0 bg-light"><div class="card-body py-2 px-3">' +
        '<small class="text-muted">Bulan Terbaik</small><br>' +
        '<span class="fw-semibold text-success">' + escapeHtml(bestMonth.label) + '</span> — ' + bestMonth.avgGainScore.toFixed(1) + '%' +
        '</div></div></div>' +
        '<div class="col-md-4"><div class="card border-0 bg-light"><div class="card-body py-2 px-3">' +
        '<small class="text-muted">Bulan Terburuk</small><br>' +
        '<span class="fw-semibold text-danger">' + escapeHtml(worstMonth.label) + '</span> — ' + worstMonth.avgGainScore.toFixed(1) + '%' +
        '</div></div></div>' +
        '<div class="col-md-4"><div class="card border-0 bg-light"><div class="card-body py-2 px-3">' +
        '<small class="text-muted">Trend Gain Score</small><br>' +
        '<span class="fw-semibold">' + trendIcon + '</span>' +
        '</div></div></div>';
    parent.appendChild(div);
}

// ============================================================
// renderEtTable — sortable, higher opacity heatmap, mini progress bars, insight cards
// ============================================================
function renderEtTable(data) {
    var container = document.getElementById('etTableContainer');
    var emptyEl = document.getElementById('etEmpty');

    if (!data || data.length === 0) {
        container.innerHTML = '';
        emptyEl.style.display = '';
        return;
    }
    emptyEl.style.display = 'none';

    // Store data for sorting
    container._etData = data;

    // Populate insight cards (elements defined in CSHTML)
    var totalElemen = data.length;
    var highest = data.reduce(function (a, b) { return a.avgPct > b.avgPct ? a : b; });
    var lowest = data.reduce(function (a, b) { return a.avgPct < b.avgPct ? a : b; });
    var belowThreshold = data.filter(function (d) { return d.avgPct < 60; }).length;

    var etInsight = document.getElementById('etInsight');
    if (etInsight) etInsight.style.display = '';
    var etBest = document.getElementById('etBestElement');
    if (etBest) etBest.textContent = highest.elemenTeknis + ' (' + highest.avgPct.toFixed(1) + '%)';
    var etWorst = document.getElementById('etWorstElement');
    if (etWorst) etWorst.textContent = lowest.elemenTeknis + ' (' + lowest.avgPct.toFixed(1) + '%)';
    var etSummary = document.getElementById('etSummaryText');
    if (etSummary) etSummary.textContent = 'Total Elemen: ' + totalElemen + ' | Di bawah 60%: ' + belowThreshold + ' elemen';

    var etLegend = document.getElementById('etLegend');
    if (etLegend) etLegend.style.display = '';

    container.innerHTML = buildEtTableHtml(data);
}

function buildEtTableHtml(data) {
    var sortIcon = function (col) {
        if (_etSortCol !== col) return ' <i class="bi bi-chevron-expand text-muted" style="font-size:0.7rem;"></i>';
        return _etSortDir === 'asc'
            ? ' <i class="bi bi-chevron-up"></i>'
            : ' <i class="bi bi-chevron-down"></i>';
    };

    var rows = data.map(function (d) {
        var heatStyle = '';
        if (d.avgPct >= 80) heatStyle = 'background-color:rgba(40,167,69,0.35);';
        else if (d.avgPct >= 60) heatStyle = 'background-color:rgba(255,193,7,0.30);';
        else heatStyle = 'background-color:rgba(220,53,69,0.30);';

        var barColor = d.avgPct >= 80 ? '#198754' : (d.avgPct >= 60 ? '#ffc107' : '#dc3545');
        var progressBar =
            '<div class="progress" style="height:6px;min-width:60px;">' +
            '<div class="progress-bar" style="width:' + d.avgPct + '%;background-color:' + barColor + ';"></div>' +
            '</div>';

        return '<tr>' +
            '<td>' + escapeHtml(d.elemenTeknis) + '</td>' +
            '<td>' + escapeHtml(d.category) + '</td>' +
            '<td style="' + heatStyle + '">' + d.avgPct.toFixed(1) + '% ' + progressBar + '</td>' +
            '<td>' + d.minPct.toFixed(1) + '%</td>' +
            '<td>' + d.maxPct.toFixed(1) + '%</td>' +
            '<td>' + d.sampleCount + '</td>' +
            '</tr>';
    }).join('');

    return '<table class="table table-sm table-hover" id="etTable">' +
        '<thead class="table-light"><tr>' +
        '<th scope="col" style="cursor:pointer;" onclick="sortEtTable(0)">Elemen Teknis' + sortIcon(0) + '</th>' +
        '<th scope="col" style="cursor:pointer;" onclick="sortEtTable(1)">Kategori' + sortIcon(1) + '</th>' +
        '<th scope="col" style="cursor:pointer;" onclick="sortEtTable(2)">Rata-rata' + sortIcon(2) + '</th>' +
        '<th scope="col" style="cursor:pointer;" onclick="sortEtTable(3)">Min' + sortIcon(3) + '</th>' +
        '<th scope="col" style="cursor:pointer;" onclick="sortEtTable(4)">Max' + sortIcon(4) + '</th>' +
        '<th scope="col" style="cursor:pointer;" onclick="sortEtTable(5)">Sesi' + sortIcon(5) + '</th>' +
        '</tr></thead>' +
        '<tbody>' + rows + '</tbody>' +
        '</table>';
}

// ============================================================
// sortEtTable — client-side sort toggle
// ============================================================
function sortEtTable(colIndex) {
    var container = document.getElementById('etTableContainer');
    var data = container._etData;
    if (!data) return;

    if (_etSortCol === colIndex) {
        _etSortDir = _etSortDir === 'asc' ? 'desc' : 'asc';
    } else {
        _etSortCol = colIndex;
        _etSortDir = 'asc';
    }

    var keyMap = {
        0: 'elemenTeknis',
        1: 'category',
        2: 'avgPct',
        3: 'minPct',
        4: 'maxPct',
        5: 'sampleCount'
    };
    var key = keyMap[colIndex];
    var isString = colIndex <= 1;

    var sorted = data.slice().sort(function (a, b) {
        var va = a[key], vb = b[key];
        if (isString) {
            va = (va || '').toLowerCase();
            vb = (vb || '').toLowerCase();
            if (va < vb) return _etSortDir === 'asc' ? -1 : 1;
            if (va > vb) return _etSortDir === 'asc' ? 1 : -1;
            return 0;
        }
        return _etSortDir === 'asc' ? va - vb : vb - va;
    });

    // Re-render only the table portion
    var tableStart = container.innerHTML.indexOf('<table');
    var tableEnd = container.innerHTML.indexOf('</table>') + '</table>'.length;
    var before = container.innerHTML.substring(0, tableStart);
    var after = container.innerHTML.substring(tableEnd);
    container.innerHTML = before + buildEtTableHtml(sorted) + after;
}

// ============================================================
// renderExpiringTable — "Sisa Hari" column, colored badges, alert, empty state
// ============================================================
function renderExpiringTable(data, days) {
    var container = document.getElementById('expiringTableContainer');
    var emptyEl = document.getElementById('expiringEmpty');
    days = days || 30;

    if (!data || data.length === 0) {
        container.innerHTML = '';
        emptyEl.style.display = '';
        emptyEl.innerHTML =
            '<i class="bi bi-shield-check fs-1 d-block mb-2 text-success"></i>' +
            '<span class="fw-semibold">Tidak ada sertifikat yang akan expired</span><br>' +
            '<small>Tidak ada sertifikat yang akan expired dalam ' + days + ' hari ke depan.</small>';
        return;
    }
    emptyEl.style.display = 'none';

    var now = new Date();
    var totalCount = data.length;

    // Alert banner
    var alertEl = document.getElementById('expiringAlert');
    if (alertEl) {
        if (totalCount >= 5) {
            alertEl.className = 'alert alert-danger py-2 mb-3';
            alertEl.innerHTML = '<i class="bi bi-exclamation-triangle-fill me-2"></i><strong>' + totalCount + '</strong> sertifikat akan expired dalam <strong>' + days + '</strong> hari ke depan. Segera lakukan perpanjangan.';
            alertEl.style.display = '';
        } else if (totalCount > 0) {
            alertEl.className = 'alert alert-warning py-2 mb-3';
            alertEl.innerHTML = '<i class="bi bi-exclamation-triangle me-2"></i><strong>' + totalCount + '</strong> sertifikat akan expired dalam <strong>' + days + '</strong> hari ke depan.';
            alertEl.style.display = '';
        } else {
            alertEl.style.display = 'none';
        }
    }

    var rows = data.map(function (d) {
        var tgl = new Date(d.tanggalExpired).toLocaleDateString('id-ID');
        var sisaHari = Math.ceil((new Date(d.tanggalExpired) - now) / (1000 * 60 * 60 * 24));
        var badgeClass = sisaHari <= 7 ? 'bg-danger'
            : (sisaHari <= 14 ? 'bg-warning text-dark' : 'bg-info text-dark');
        var sisaBadge = '<span class="badge ' + badgeClass + '">' + sisaHari + ' hari</span>';

        return '<tr>' +
            '<td>' + escapeHtml(d.namaPekerja) + '</td>' +
            '<td>' + escapeHtml(d.namaSertifikat) + '</td>' +
            '<td>' + tgl + '</td>' +
            '<td>' + sisaBadge + '</td>' +
            '<td>' + escapeHtml(d.sectionUnit) + '</td>' +
            '</tr>';
    }).join('');

    container.innerHTML =
        '<table class="table table-sm table-hover">' +
        '<thead class="table-light"><tr>' +
        '<th scope="col">Nama Pekerja</th>' +
        '<th scope="col">Sertifikat</th>' +
        '<th scope="col">Tgl Expired</th>' +
        '<th scope="col">Sisa Hari</th>' +
        '<th scope="col">Bagian / Unit</th>' +
        '</tr></thead>' +
        '<tbody>' + rows + '</tbody>' +
        '</table>';
}

// ============================================================
// renderItemAnalysis — with insight summary above table
// ============================================================
function renderItemAnalysis(data) {
    if (!data || !data.items || data.items.length === 0) {
        showItemAnalysisEmpty(
            'Belum ada data soal',
            'Assessment yang dipilih tidak memiliki soal atau belum ada peserta yang mengerjakan.'
        );
        return;
    }
    document.getElementById('itemAnalysisEmpty').style.display = 'none';
    document.getElementById('itemAnalysisContent').style.display = '';

    // Warning N<30
    document.getElementById('itemAnalysisWarning').style.display = data.isLowN ? '' : 'none';

    // Insight summary
    var totalSoal = data.items.length;
    var soalBaik = data.items.filter(function (i) {
        return i.difficultyIndex >= 0.30 && i.difficultyIndex <= 0.70 &&
            i.discriminationIndex !== null && i.discriminationIndex >= 0.3;
    }).length;
    var soalRevisi = data.items.filter(function (i) {
        return i.difficultyIndex < 0.30 || i.difficultyIndex > 0.70 ||
            (i.discriminationIndex !== null && i.discriminationIndex < 0.2);
    }).length;

    var insightContainer = document.getElementById('itemAnalysisInsight');
    if (!insightContainer) {
        insightContainer = document.createElement('div');
        insightContainer.id = 'itemAnalysisInsight';
        var content = document.getElementById('itemAnalysisContent');
        content.insertBefore(insightContainer, content.firstChild);
    }
    insightContainer.style.display = '';
    insightContainer.innerHTML =
        '<div class="row g-2 mb-3">' +
        '<div class="col-md-4"><div class="card border-0 bg-light"><div class="card-body py-2 px-3">' +
        '<small class="text-muted">Total Soal</small><div class="fw-bold fs-5">' + totalSoal + '</div>' +
        '</div></div></div>' +
        '<div class="col-md-4"><div class="card border-0 bg-light"><div class="card-body py-2 px-3">' +
        '<small class="text-muted">Soal Baik</small><div class="fw-bold fs-5 text-success">' + soalBaik + '</div>' +
        '</div></div></div>' +
        '<div class="col-md-4"><div class="card border-0 bg-light"><div class="card-body py-2 px-3">' +
        '<small class="text-muted">Soal Perlu Revisi</small><div class="fw-bold fs-5 text-danger">' + soalRevisi + '</div>' +
        '</div></div></div>' +
        '</div>';

    var tbody = document.querySelector('#itemAnalysisTable tbody');
    tbody.innerHTML = '';
    var distractorHtml = '';

    data.items.forEach(function (item) {
        var pBadge = '';
        if (item.difficultyIndex > 0.70) pBadge = '<span class="badge bg-success">Mudah</span>';
        else if (item.difficultyIndex >= 0.30) pBadge = '<span class="badge bg-warning text-dark">Sedang</span>';
        else pBadge = '<span class="badge bg-danger">Sulit</span>';

        var dVal = item.discriminationIndex !== null && item.discriminationIndex !== undefined
            ? item.discriminationIndex.toFixed(3) : '\u2014';
        var dClass = item.isLowN ? 'text-muted' : '';
        var dWarning = item.isLowN
            ? ' <span class="badge bg-warning text-dark" style="font-size:0.7rem;">Data belum cukup (N&lt;30)</span>' : '';

        tbody.innerHTML += '<tr data-q="' + item.questionNumber + '" style="cursor:pointer;" onclick="toggleDistractor(' + item.questionNumber + ')">' +
            '<td>' + item.questionNumber + '</td>' +
            '<td>' + escapeHtml(item.questionText.substring(0, 80)) + (item.questionText.length > 80 ? '...' : '') + '</td>' +
            '<td>' + item.difficultyIndex.toFixed(3) + '</td>' +
            '<td>' + pBadge + '</td>' +
            '<td class="' + dClass + '">' + dVal + dWarning + '</td>' +
            '<td>' + item.totalResponden + '</td>' +
            '</tr>';

        if (item.distractors && item.distractors.length > 0) {
            distractorHtml += '<div id="distractor-' + item.questionNumber + '" class="mb-3" style="display:none;">' +
                '<h6 class="fw-semibold">Soal ' + item.questionNumber + ' \u2014 Distractor Analysis</h6>' +
                '<table class="table table-sm table-bordered">' +
                '<thead><tr><th>Opsi</th><th>Jumlah Pemilih</th><th>Persentase</th><th>Keterangan</th></tr></thead><tbody>';
            item.distractors.forEach(function (d) {
                var rowClass = d.isCorrect ? 'table-success' : '';
                var keterangan = d.isCorrect ? '<span class="badge bg-success">Jawaban Benar</span>' : '';
                distractorHtml += '<tr class="' + rowClass + '"><td>' + escapeHtml(d.optionText) + '</td><td>' + d.count + '</td><td>' + d.percent + '%</td><td>' + keterangan + '</td></tr>';
            });
            distractorHtml += '</tbody></table></div>';
        }
    });

    document.getElementById('distractorDetails').innerHTML = distractorHtml;
}

function showItemAnalysisEmpty(title, message) {
    var empty = document.getElementById('itemAnalysisEmpty');
    empty.style.display = '';
    empty.innerHTML =
        '<i class="bi bi-clipboard-data fs-1 d-block mb-2 text-muted"></i>' +
        '<h6>' + escapeHtml(title) + '</h6>' +
        '<p>' + escapeHtml(message) + '</p>';
    document.getElementById('itemAnalysisContent').style.display = 'none';
}

// ============================================================
// renderGainScore — with insight summary
// ============================================================
function renderGainScore(data) {
    _gainScoreData = data;
    if (!data || (!data.perWorker.length && !data.perElemen.length)) {
        showGainScoreEmpty(
            'Belum ada data gain score',
            'Belum ada peserta yang menyelesaikan Pre-Test dan Post-Test untuk assessment ini.'
        );
        return;
    }
    document.getElementById('gainScoreEmpty').style.display = 'none';
    document.getElementById('gainScoreContent').style.display = '';

    // Insight summary
    var insightContainer = document.getElementById('gainScoreInsight');
    if (!insightContainer) {
        insightContainer = document.createElement('div');
        insightContainer.id = 'gainScoreInsight';
        var content = document.getElementById('gainScoreContent');
        content.insertBefore(insightContainer, content.firstChild);
    }

    if (data.perWorker.length > 0) {
        var gains = data.perWorker.map(function (w) { return w.gainScore; });
        var avgGain = gains.reduce(function (s, v) { return s + v; }, 0) / gains.length;
        var highestWorker = data.perWorker.reduce(function (a, b) { return a.gainScore > b.gainScore ? a : b; });
        var lowestWorker = data.perWorker.reduce(function (a, b) { return a.gainScore < b.gainScore ? a : b; });

        insightContainer.style.display = '';
        insightContainer.innerHTML =
            '<div class="row g-2 mb-3">' +
            '<div class="col-md-4"><div class="card border-0 bg-light"><div class="card-body py-2 px-3">' +
            '<small class="text-muted">Rata-rata Gain</small><div class="fw-bold fs-5">' + avgGain.toFixed(1) + '</div>' +
            '</div></div></div>' +
            '<div class="col-md-4"><div class="card border-0 bg-light"><div class="card-body py-2 px-3">' +
            '<small class="text-muted">Tertinggi</small><div class="fw-bold fs-5 text-success">' + escapeHtml(highestWorker.namaPekerja) + ' (' + highestWorker.gainScore + ')</div>' +
            '</div></div></div>' +
            '<div class="col-md-4"><div class="card border-0 bg-light"><div class="card-body py-2 px-3">' +
            '<small class="text-muted">Terendah</small><div class="fw-bold fs-5 text-danger">' + escapeHtml(lowestWorker.namaPekerja) + ' (' + lowestWorker.gainScore + ')</div>' +
            '</div></div></div>' +
            '</div>';
    } else {
        insightContainer.innerHTML = '';
    }

    // Per Worker
    var wBody = document.getElementById('gainWorkerBody');
    wBody.innerHTML = '';
    data.perWorker.forEach(function (w) {
        wBody.innerHTML += '<tr><td>' + escapeHtml(w.namaPekerja) + '</td><td>' + escapeHtml(w.nip) + '</td><td>' + w.preScore + '</td><td>' + w.postScore + '</td><td class="fw-semibold">' + w.gainScore + '</td></tr>';
    });

    // Per Elemen
    var eBody = document.getElementById('gainElemenBody');
    eBody.innerHTML = '';
    data.perElemen.forEach(function (e) {
        eBody.innerHTML += '<tr><td>' + escapeHtml(e.elemenTeknis) + '</td><td>' + e.avgPre + '</td><td>' + e.avgPost + '</td><td class="fw-semibold">' + e.avgGain + '</td></tr>';
    });

    // Group Comparison
    var gBody = document.getElementById('groupComparisonBody');
    gBody.innerHTML = '';
    data.groupComparison.forEach(function (g) {
        gBody.innerHTML += '<tr><td>' + escapeHtml(g.groupName) + '</td><td>' + g.workerCount + '</td><td>' + g.avgPreScore + '</td><td>' + g.avgPostScore + '</td><td class="fw-semibold">' + g.avgGainScore + '</td></tr>';
    });
}

function showGainScoreEmpty(title, message) {
    var empty = document.getElementById('gainScoreEmpty');
    empty.style.display = '';
    empty.innerHTML =
        '<i class="bi bi-bar-chart-steps fs-1 d-block mb-2 text-muted"></i>' +
        '<h6>' + escapeHtml(title) + '</h6>' +
        '<p>' + escapeHtml(message) + '</p>';
    document.getElementById('gainScoreContent').style.display = 'none';
}

// ============================================================
// toggleDistractor
// ============================================================
function toggleDistractor(qNum) {
    var el = document.getElementById('distractor-' + qNum);
    if (el) el.style.display = el.style.display === 'none' ? '' : 'none';
}

// ============================================================
// toggleGainView
// ============================================================
function toggleGainView(view) {
    document.getElementById('gainWorkerView').style.display = view === 'worker' ? '' : 'none';
    document.getElementById('gainElemenView').style.display = view === 'elemen' ? '' : 'none';
}

// ============================================================
// loadAssessmentList
// ============================================================
function loadAssessmentList() {
    var bagian = document.getElementById('filterBagian').value;
    var unit = document.getElementById('filterUnit') ? document.getElementById('filterUnit').value : '';

    var url = _config.getPrePostAssessmentListUrl
        ? _config.getPrePostAssessmentListUrl + '?bagian=' + encodeURIComponent(bagian) + '&unit=' + encodeURIComponent(unit)
        : appUrl('/CMP/GetPrePostAssessmentList?bagian=' + encodeURIComponent(bagian) + '&unit=' + encodeURIComponent(unit));

    fetch(url)
        .then(function (r) { return r.json(); })
        .then(function (list) {
            var sel = document.getElementById('filterAssessment');
            sel.innerHTML = '<option value="">\u2014 Pilih assessment \u2014</option>';
            if (!list || list.length === 0) {
                sel.innerHTML += '<option value="" disabled>Tidak ada assessment Pre-Post Test untuk filter ini</option>';
                return;
            }
            list.forEach(function (item) {
                sel.innerHTML += '<option value="' + item.linkedGroupId + '">' + escapeHtml(item.title) + ' (' + item.totalWorker + ' pekerja)</option>';
            });
        })
        .catch(function () {
            console.warn('Gagal memuat daftar assessment');
        });
}

// ============================================================
// escapeHtml
// ============================================================
function escapeHtml(str) {
    if (!str) return '';
    var div = document.createElement('div');
    div.textContent = str;
    return div.innerHTML;
}

// ============================================================
// Export functions
// ============================================================
function exportFailRate() {
    var params = getFilterParams();
    var url = _config.exportFailRateUrl
        ? _config.exportFailRateUrl + '?' + params.toString()
        : appUrl('/CMP/ExportFailRateExcel?' + params.toString());
    window.location.href = url;
}

function exportTrend() {
    var params = getFilterParams();
    var url = _config.exportTrendUrl
        ? _config.exportTrendUrl + '?' + params.toString()
        : appUrl('/CMP/ExportTrendExcel?' + params.toString());
    window.location.href = url;
}

function exportEtBreakdown() {
    var params = getFilterParams();
    var url = _config.exportEtBreakdownUrl
        ? _config.exportEtBreakdownUrl + '?' + params.toString()
        : appUrl('/CMP/ExportEtBreakdownExcel?' + params.toString());
    window.location.href = url;
}

function exportItemAnalysis() {
    var assessmentGroupId = document.getElementById('filterAssessment').value;
    if (!assessmentGroupId) { alert('Pilih assessment terlebih dahulu.'); return; }
    var url = _config.exportItemAnalysisUrl
        ? _config.exportItemAnalysisUrl + '?assessmentGroupId=' + assessmentGroupId
        : appUrl('/CMP/ExportItemAnalysisExcel?assessmentGroupId=' + assessmentGroupId);
    window.location.href = url;
}

function exportGainScore() {
    var assessmentGroupId = document.getElementById('filterAssessment').value;
    if (!assessmentGroupId) { alert('Pilih assessment terlebih dahulu.'); return; }
    var url = _config.exportGainScoreUrl
        ? _config.exportGainScoreUrl + '?assessmentGroupId=' + assessmentGroupId
        : appUrl('/CMP/ExportGainScoreExcel?assessmentGroupId=' + assessmentGroupId);
    window.location.href = url;
}

// ============================================================
// setupEventListeners — all event wiring
// ============================================================
function setupEventListeners() {
    // Tab shown → lazy load
    var tabsEl = document.getElementById('analyticsTabs');
    if (tabsEl) {
        tabsEl.addEventListener('shown.bs.tab', function (e) {
            var href = e.target.getAttribute('href');
            var tabMap = {
                '#tabFailRate': 'failrate',
                '#tabTrend': 'trend',
                '#tabEtBreakdown': 'et',
                '#tabExpiring': 'expiring',
                '#tabItemAnalysis': 'itemanalysis',
                '#tabGainScore': 'gainscore'
            };
            var tabName = tabMap[href];
            if (tabName) loadTabData(tabName);

            // Show/hide assessment dropdown for analysis tabs
            var isAnalysisTab = href === '#tabItemAnalysis' || href === '#tabGainScore';
            var assessmentGroup = document.getElementById('filterAssessmentGroup');
            if (assessmentGroup) {
                assessmentGroup.style.display = isAnalysisTab ? '' : 'none';
            }

            // If switching to analysis tab, load assessment list if empty
            if (isAnalysisTab) {
                var sel = document.getElementById('filterAssessment');
                if (sel && sel.options.length <= 1) {
                    loadAssessmentList();
                }
            }
        });
    }

    // Cascade: Bagian → Unit
    var bagianEl = document.getElementById('filterBagian');
    if (bagianEl) {
        bagianEl.addEventListener('change', function () {
            var val = this.value;
            var unitEl = document.getElementById('filterUnit');
            unitEl.innerHTML = '<option value="">Semua Unit</option>';
            if (!val) {
                unitEl.disabled = true;
                unitEl.classList.add('bg-light');
            } else {
                var url = _config.getCascadeUnitsUrl
                    ? _config.getCascadeUnitsUrl + '?bagian=' + encodeURIComponent(val)
                    : appUrl('/CMP/GetAnalyticsCascadeUnits?bagian=' + encodeURIComponent(val));

                fetch(url)
                    .then(function (r) { return r.json(); })
                    .then(function (units) {
                        units.forEach(function (u) {
                            var opt = document.createElement('option');
                            opt.value = u;
                            opt.textContent = u;
                            unitEl.appendChild(opt);
                        });
                        unitEl.disabled = false;
                        unitEl.classList.remove('bg-light');
                    });
            }

            // Reload assessment list if analysis tab active
            var activeLink = document.querySelector('#analyticsTabs .nav-link.active');
            if (activeLink) {
                var href = activeLink.getAttribute('href');
                if (href === '#tabItemAnalysis' || href === '#tabGainScore') {
                    loadAssessmentList();
                }
            }
        });
    }

    // Cascade: Kategori → SubKategori
    var kategoriEl = document.getElementById('filterKategori');
    if (kategoriEl) {
        kategoriEl.addEventListener('change', function () {
            var val = this.value;
            var subEl = document.getElementById('filterSubKategori');
            subEl.innerHTML = '<option value="">Semua SubKategori</option>';
            if (!val) {
                subEl.disabled = true;
                subEl.classList.add('bg-light');
                return;
            }
            var url = _config.getCascadeSubKategoriUrl
                ? _config.getCascadeSubKategoriUrl + '?kategori=' + encodeURIComponent(val)
                : appUrl('/CMP/GetAnalyticsCascadeSubKategori?kategori=' + encodeURIComponent(val));

            fetch(url)
                .then(function (r) { return r.json(); })
                .then(function (subs) {
                    subs.forEach(function (s) {
                        var opt = document.createElement('option');
                        opt.value = s;
                        opt.textContent = s;
                        subEl.appendChild(opt);
                    });
                    subEl.disabled = false;
                    subEl.classList.remove('bg-light');
                });
        });
    }

    // Apply filter
    var btnApply = document.getElementById('btnApply');
    if (btnApply) {
        btnApply.addEventListener('click', function () {
            invalidateCache();
            loadSummaryCards();

            var activeLink = document.querySelector('#analyticsTabs .nav-link.active');
            var activeHref = activeLink ? activeLink.getAttribute('href') : '';
            var tabMap = {
                '#tabFailRate': 'failrate',
                '#tabTrend': 'trend',
                '#tabEtBreakdown': 'et',
                '#tabExpiring': 'expiring',
                '#tabItemAnalysis': 'itemanalysis',
                '#tabGainScore': 'gainscore'
            };
            var tabName = tabMap[activeHref] || 'failrate';
            loadTabData(tabName, true);
        });
    }

    // Reset filter
    var btnReset = document.getElementById('btnReset');
    if (btnReset) {
        btnReset.addEventListener('click', function () {
            document.getElementById('filterBagian').value = '';
            var unitEl = document.getElementById('filterUnit');
            unitEl.innerHTML = '<option value="">Semua Unit</option>';
            unitEl.disabled = true;
            unitEl.classList.add('bg-light');

            document.getElementById('filterKategori').value = '';
            var subEl = document.getElementById('filterSubKategori');
            subEl.innerHTML = '<option value="">Semua SubKategori</option>';
            subEl.disabled = true;
            subEl.classList.add('bg-light');

            document.getElementById('filterAssessment').innerHTML = '<option value="">\u2014 Pilih assessment \u2014</option>';

            var today = new Date();
            var oneYearAgo = new Date(today.getFullYear() - 1, today.getMonth(), today.getDate());
            document.getElementById('filterStart').value = oneYearAgo.toISOString().slice(0, 10);
            document.getElementById('filterEnd').value = today.toISOString().slice(0, 10);

            invalidateCache();
            loadSummaryCards();
            loadTabData('failrate', true);
        });
    }

    // Expiring threshold dropdown
    var thresholdEl = document.getElementById('expiringThreshold');
    if (thresholdEl) {
        thresholdEl.addEventListener('change', function () {
            delete _tabCache['expiring'];
            loadTabData('expiring', true);
        });
    }

    // Mobile filter collapse
    var btnFilterToggle = document.getElementById('btnFilterToggle');
    if (btnFilterToggle) {
        btnFilterToggle.addEventListener('click', function () {
            var body = document.getElementById('filterCollapseBody');
            if (body) {
                var bsCollapse = bootstrap.Collapse.getOrCreateInstance(body);
                bsCollapse.toggle();
            }
        });
    }
}
