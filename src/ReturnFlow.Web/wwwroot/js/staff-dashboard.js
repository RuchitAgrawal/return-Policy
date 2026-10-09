document.addEventListener('DOMContentLoaded', () => {
    const kpiActiveRmas = document.getElementById('kpiActiveRmas');
    const kpiValueAtRisk = document.getElementById('kpiValueAtRisk');
    const kpiAutoResolved = document.getElementById('kpiAutoResolved');
    const kpiStockRecovered = document.getElementById('kpiStockRecovered');

    const filterSearchInput = document.getElementById('filterSearchInput');
    const filterFraudSelect = document.getElementById('filterFraudSelect');
    const filterChannelSelect = document.getElementById('filterChannelSelect');
    const statusPillTabs = document.getElementById('statusPillTabs');
    const btnExportCsv = document.getElementById('btnExportCsv');

    const rmaTableBody = document.getElementById('rmaTableBody');

    const inspectorPlaceholder = document.getElementById('inspectorPlaceholder');
    const inspectorDetails = document.getElementById('inspectorDetails');
    const inspTicketNumber = document.getElementById('inspTicketNumber');
    const inspProductName = document.getElementById('inspProductName');
    const inspCustomerOrder = document.getElementById('inspCustomerOrder');
    const inspStatusBadge = document.getElementById('inspStatusBadge');

    const inspDeliveredDate = document.getElementById('inspDeliveredDate');
    const inspDaysSince = document.getElementById('inspDaysSince');
    const inspReturnWindow = document.getElementById('inspReturnWindow');
    const inspWarrantyWindow = document.getElementById('inspWarrantyWindow');
    const inspRefundAmount = document.getElementById('inspRefundAmount');
    const inspRestockingFee = document.getElementById('inspRestockingFee');
    const inspComplaintText = document.getElementById('inspComplaintText');

    const inspFraudBadge = document.getElementById('inspFraudBadge');
    const inspCategoryBadge = document.getElementById('inspCategoryBadge');
    const inspActionBadge = document.getElementById('inspActionBadge');
    const inspRoutingBadge = document.getElementById('inspRoutingBadge');
    const inspSentimentScore = document.getElementById('inspSentimentScore');
    const inspSentimentFill = document.getElementById('inspSentimentFill');
    const inspReasoningText = document.getElementById('inspReasoningText');
    const inspEmailText = document.getElementById('inspEmailText');

    const btnApproveSync = document.getElementById('btnApproveSync');
    const btnApplyRestocking = document.getElementById('btnApplyRestocking');
    const btnRejectClaim = document.getElementById('btnRejectClaim');
    const btnInspPrintLabel = document.getElementById('btnInspPrintLabel');

    let ticketsCache = [];
    let selectedTicketId = null;
    let currentStatusFilter = 'All';

    let defectChartInstance = null;
    let channelChartInstance = null;

    fetchKpis();
    fetchTickets();

    if (btnExportCsv) {
        btnExportCsv.addEventListener('click', exportClaimsCsv);
    }

    if (statusPillTabs) {
        statusPillTabs.addEventListener('click', (e) => {
            const btn = e.target.closest('.pill-tab-item');
            if (!btn) return;

            statusPillTabs.querySelectorAll('.pill-tab-item').forEach(b => b.classList.remove('active'));
            btn.classList.add('active');

            currentStatusFilter = btn.getAttribute('data-status') || 'All';
            applyFilters();
        });
    }

    [filterSearchInput, filterFraudSelect, filterChannelSelect].forEach(el => {
        if (!el) return;
        el.addEventListener('input', applyFilters);
        el.addEventListener('change', applyFilters);
    });

    async function fetchKpis() {
        try {
            const res = await fetch('/api/inventory/kpis');
            if (!res.ok) return;
            const kpis = await res.json();

            kpiActiveRmas.textContent = kpis.totalActiveRmas;
            kpiValueAtRisk.textContent = `₹${Number(kpis.totalValueAtRisk).toLocaleString('en-IN', { minimumFractionDigits: 2 })}`;
            
            if (kpis.autoResolvedPercent > 0) {
                kpiAutoResolved.textContent = `${kpis.autoResolvedPercent}%`;
                kpiAutoResolved.className = 'fw-bold font-mono text-success';
            } else {
                kpiAutoResolved.textContent = '0%';
                kpiAutoResolved.className = 'fw-bold font-mono text-dark';
            }
            
            kpiStockRecovered.textContent = kpis.stockRecovered;
        } catch (e) {
            console.error('Failed to load KPIs:', e);
        }
    }

    async function fetchTickets() {
        try {
            const res = await fetch('/api/rma');
            if (!res.ok) throw new Error('Failed to load tickets');
            ticketsCache = await res.json();
            renderTableRows(ticketsCache);
            initOrUpdateCharts(ticketsCache);
        } catch (e) {
            rmaTableBody.innerHTML = `<tr><td colspan="7" class="text-center text-danger py-4">${e.message}</td></tr>`;
        }
    }

    function renderTableRows(tickets) {
        if (!tickets || tickets.length === 0) {
            rmaTableBody.innerHTML = `<tr><td colspan="7" class="text-center text-steel py-4">No RMA tickets found matching filter.</td></tr>`;
            return;
        }

        rmaTableBody.innerHTML = '';
        tickets.forEach(t => {
            const tr = document.createElement('tr');
            tr.id = `row_${t.id}`;
            tr.setAttribute('data-id', t.id);
            tr.setAttribute('data-status', t.status);
            tr.setAttribute('data-fraud', t.fraudRiskLevel || 'None');
            tr.setAttribute('data-channel', t.salesChannel);
            tr.setAttribute('data-search', `${t.ticketNumber} ${t.customerName} ${t.productName} ${t.sku}`.toLowerCase());

            if (selectedTicketId === t.id) {
                tr.classList.add('row-selected');
            }

            const statusClass = getStatusBadgeClass(t.status);
            const fraudClass = getFraudBadgeClass(t.fraudRiskLevel);

            tr.innerHTML = `
                <td class="font-mono fw-bold text-dark">${escapeHtml(t.ticketNumber)}</td>
                <td>
                    <div class="fw-semibold">${escapeHtml(t.customerName)}</div>
                    <div class="text-steel small font-mono">${escapeHtml(t.productName)} (${escapeHtml(t.sku)})</div>
                </td>
                <td><span class="badge-pill-neutral font-mono">${escapeHtml(t.salesChannel)}</span></td>
                <td><span class="badge-pill-neutral ${statusClass}" id="rowBadge_${t.id}">${escapeHtml(t.status)}</span></td>
                <td><span class="badge-pill-neutral ${fraudClass}">${escapeHtml(t.fraudRiskLevel || 'Low')}</span></td>
                <td class="fw-bold text-success font-mono">₹${Number(t.calculatedRefundAmount).toLocaleString('en-IN', { minimumFractionDigits: 2 })}</td>
                <td class="text-steel small">${t.daysSinceDelivery}d ago</td>
            `;

            tr.addEventListener('click', () => {
                selectTicketRow(t.id);
            });

            rmaTableBody.appendChild(tr);
        });

        applyFilters();
    }

    function applyFilters() {
        const query = (filterSearchInput?.value || '').trim().toLowerCase();
        const fraud = filterFraudSelect?.value || 'All';
        const channel = filterChannelSelect?.value || 'All';

        const rows = rmaTableBody.querySelectorAll('tr[data-id]');
        rows.forEach(row => {
            const rSearch = row.getAttribute('data-search') || '';
            const rStatus = row.getAttribute('data-status') || '';
            const rFraud = row.getAttribute('data-fraud') || '';
            const rChannel = row.getAttribute('data-channel') || '';

            const matchQuery = !query || rSearch.includes(query);
            const matchStatus = currentStatusFilter === 'All' || rStatus.toLowerCase() === currentStatusFilter.toLowerCase();
            const matchFraud = fraud === 'All' || rFraud.toLowerCase() === fraud.toLowerCase();
            const matchChannel = channel === 'All' || rChannel.toLowerCase() === channel.toLowerCase();

            if (matchQuery && matchStatus && matchFraud && matchChannel) {
                row.style.display = '';
            } else {
                row.style.display = 'none';
            }
        });
    }

    async function selectTicketRow(ticketId) {
        selectedTicketId = ticketId;

        rmaTableBody.querySelectorAll('tr').forEach(r => r.classList.remove('row-selected'));
        const activeRow = document.getElementById(`row_${ticketId}`);
        if (activeRow) activeRow.classList.add('row-selected');

        try {
            const res = await fetch(`/api/rma/${ticketId}`);
            if (!res.ok) throw new Error('Failed to load ticket details.');
            const ticket = await res.json();
            populateInspector(ticket);
        } catch (e) {
            alert(e.message);
        }
    }

    function populateInspector(t) {
        inspectorPlaceholder.classList.add('d-none');
        inspectorDetails.classList.remove('d-none');

        inspTicketNumber.textContent = t.ticketNumber;
        inspProductName.textContent = t.productName;
        inspCustomerOrder.textContent = `${t.customerName} | ${t.orderNumber} (${t.salesChannel})`;

        inspStatusBadge.textContent = t.status;
        inspStatusBadge.className = 'badge-pill-dark ' + getStatusBadgeClass(t.status);

        inspDeliveredDate.textContent = new Date(t.deliveredDate).toLocaleDateString();
        inspDaysSince.textContent = `${t.daysSinceDelivery} days`;
        inspReturnWindow.innerHTML = t.isWithinReturnWindow
            ? '<span class="text-success"><i class="bi bi-check-circle"></i> Active (&le;30d)</span>'
            : '<span class="text-danger"><i class="bi bi-x-circle"></i> Expired</span>';

        inspWarrantyWindow.innerHTML = t.isWithinWarrantyWindow
            ? '<span class="text-success"><i class="bi bi-check-circle"></i> Active</span>'
            : '<span class="text-danger"><i class="bi bi-x-circle"></i> Expired</span>';

        inspRefundAmount.textContent = `₹${Number(t.calculatedRefundAmount).toLocaleString('en-IN', { minimumFractionDigits: 2 })}`;
        inspRestockingFee.textContent = `₹${Number(t.restockingFeeAmount).toLocaleString('en-IN', { minimumFractionDigits: 2 })}`;
        inspComplaintText.textContent = `"${t.customerComplaintRawText}"`;

        const fraudRisk = t.aiFraudRiskLevel || 'Low';
        inspFraudBadge.textContent = `Risk: ${fraudRisk}`;
        inspFraudBadge.className = 'badge-pill-dark ' + getFraudBadgeClass(fraudRisk);

        inspCategoryBadge.textContent = `Category: ${formatEnumLabel(t.aiExtractedDefectCategory || 'General')}`;
        inspActionBadge.textContent = `Action: ${formatEnumLabel(t.aiRecommendedAction)}`;
        inspRoutingBadge.textContent = `Routing: ${formatEnumLabel(t.warehouseRoutingAction)}`;

        const score = Math.max(1, Math.min(10, t.aiSentimentScore || 5));
        inspSentimentScore.textContent = `${score}/10`;
        inspSentimentFill.style.width = `${score * 10}%`;
        inspSentimentFill.style.backgroundColor = getSentimentColor(score);

        inspReasoningText.textContent = t.aiReasoningSummary || 'Policy verification completed.';
        inspEmailText.value = t.aiDraftedCustomerEmail || '';
    }

    btnApproveSync.addEventListener('click', () => executeDecision('Approve'));
    btnApplyRestocking.addEventListener('click', () => executeDecision('ApplyRestockingFee'));
    btnRejectClaim.addEventListener('click', () => executeDecision('Reject'));

    if (btnInspPrintLabel) {
        btnInspPrintLabel.addEventListener('click', () => {
            if (!selectedTicketId) return;
            const currentTicket = ticketsCache.find(t => t.id === selectedTicketId);
            if (!currentTicket) return;

            const labelRma = document.getElementById('labelRmaNumber');
            const labelCust = document.getElementById('labelCustomerName');
            const labelOrder = document.getElementById('labelOrderRef');
            const labelSku = document.getElementById('labelProductSku');
            const labelRouting = document.getElementById('labelRoutingAction');
            const labelBarcode = document.getElementById('labelBarcodeText');

            if (labelRma) labelRma.textContent = currentTicket.ticketNumber;
            if (labelCust) labelCust.textContent = currentTicket.customerName;
            if (labelOrder) labelOrder.textContent = `Order ${currentTicket.orderNumber}`;
            if (labelSku) labelSku.textContent = `${currentTicket.sku} | ${currentTicket.productName}`;
            if (labelRouting) labelRouting.textContent = formatEnumLabel(currentTicket.warehouseRoutingAction || 'RETURN TO WAREHOUSE').toUpperCase();
            if (labelBarcode) labelBarcode.textContent = `*DEL-${currentTicket.ticketNumber}-EXPEDITED*`;

            const modalEl = document.getElementById('carrierLabelModal');
            if (modalEl) {
                const modal = new bootstrap.Modal(modalEl);
                modal.show();
            }
        });
    }

    async function executeDecision(actionName) {
        if (!selectedTicketId) return;

        try {
            const res = await fetch(`/api/rma/${selectedTicketId}/decision`, {
                method: 'PATCH',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ action: actionName })
            });

            if (!res.ok) {
                const err = await res.json().catch(() => ({}));
                throw new Error(err.detail || 'Failed to apply decision.');
            }

            const result = await res.json();

            inspStatusBadge.textContent = result.newStatus;
            inspStatusBadge.className = 'badge-pill-dark ' + getStatusBadgeClass(result.newStatus);

            const rowBadge = document.getElementById(`rowBadge_${selectedTicketId}`);
            if (rowBadge) {
                rowBadge.textContent = result.newStatus;
                rowBadge.className = 'badge-pill-neutral ' + getStatusBadgeClass(result.newStatus);

                const row = document.getElementById(`row_${selectedTicketId}`);
                if (row) {
                    row.setAttribute('data-status', result.newStatus);
                    row.classList.add('table-light');
                    setTimeout(() => row.classList.remove('table-light'), 1200);
                }
            }

            const cached = ticketsCache.find(t => t.id === selectedTicketId);
            if (cached) {
                cached.status = result.newStatus;
            }

            fetchKpis();
            initOrUpdateCharts(ticketsCache);
            showToast(result.stockUpdateMessage || result.message);

        } catch (e) {
            alert(`Decision error: ${e.message}`);
        }
    }

    function initOrUpdateCharts(tickets) {
        if (!window.Chart || !tickets || tickets.length === 0) return;

        const defectCounts = {};
        tickets.forEach(t => {
            const rawCat = t.defectCategory || t.aiExtractedDefectCategory || 'General';
            const cat = formatEnumLabel(rawCat);
            defectCounts[cat] = (defectCounts[cat] || 0) + 1;
        });

        const defectLabels = Object.keys(defectCounts);
        const defectData = Object.values(defectCounts);
        const defectColors = ['#4f46e5', '#10b981', '#f59e0b', '#f43f5e', '#0284c7', '#64748b'];

        const defectCanvas = document.getElementById('defectCategoryChart');
        if (defectCanvas) {
            if (defectChartInstance) {
                defectChartInstance.data.labels = defectLabels;
                defectChartInstance.data.datasets[0].data = defectData;
                defectChartInstance.update();
            } else {
                defectChartInstance = new Chart(defectCanvas, {
                    type: 'doughnut',
                    data: {
                        labels: defectLabels,
                        datasets: [{
                            data: defectData,
                            backgroundColor: defectColors.slice(0, defectLabels.length),
                            borderWidth: 0,
                            hoverOffset: 4
                        }]
                    },
                    options: {
                        responsive: true,
                        maintainAspectRatio: false,
                        plugins: {
                            legend: {
                                position: 'right',
                                labels: {
                                    boxWidth: 10,
                                    font: { family: 'DM Sans', size: 12 },
                                    color: '#475569'
                                }
                            }
                        },
                        cutout: '68%'
                    }
                });
            }
        }

        const channelRisk = {};
        tickets.forEach(t => {
            const ch = t.salesChannel || 'DirectWeb';
            const val = Number(t.calculatedRefundAmount || 0);
            channelRisk[ch] = (channelRisk[ch] || 0) + val;
        });

        const channelLabels = Object.keys(channelRisk);
        const channelData = Object.values(channelRisk);

        const channelCanvas = document.getElementById('channelRiskChart');
        if (channelCanvas) {
            if (channelChartInstance) {
                channelChartInstance.data.labels = channelLabels;
                channelChartInstance.data.datasets[0].data = channelData;
                channelChartInstance.update();
            } else {
                channelChartInstance = new Chart(channelCanvas, {
                    type: 'bar',
                    data: {
                        labels: channelLabels,
                        datasets: [{
                            label: 'Exposure (₹)',
                            data: channelData,
                            backgroundColor: '#334155',
                            hoverBackgroundColor: '#0284c7',
                            borderRadius: 6,
                            borderSkipped: false
                        }]
                    },
                    options: {
                        responsive: true,
                        maintainAspectRatio: false,
                        plugins: {
                            legend: { display: false }
                        },
                        scales: {
                            x: {
                                grid: { display: false },
                                ticks: { font: { family: 'DM Sans', size: 11 }, color: '#64748b' }
                            },
                            y: {
                                grid: { color: '#e2e8f0' },
                                ticks: {
                                    font: { family: 'JetBrains Mono', size: 11 },
                                    color: '#64748b',
                                    maxTicksLimit: 5,
                                    callback: (val) => '₹' + Number(val).toLocaleString('en-IN')
                                }
                            }
                        }
                    }
                });
            }
        }
    }

    function exportClaimsCsv() {
        if (!ticketsCache || ticketsCache.length === 0) {
            alert('No tickets available to export.');
            return;
        }

        const headers = ['TicketNumber', 'CustomerName', 'Product', 'SKU', 'SalesChannel', 'Status', 'RiskLevel', 'RefundAmount', 'DaysSinceDelivery', 'CreatedAt'];
        const rows = ticketsCache.map(t => [
            t.ticketNumber,
            `"${(t.customerName || '').replace(/"/g, '""')}"`,
            `"${(t.productName || '').replace(/"/g, '""')}"`,
            t.sku,
            t.salesChannel,
            t.status,
            t.fraudRiskLevel || 'Low',
            t.calculatedRefundAmount,
            t.daysSinceDelivery,
            t.createdAt
        ]);

        const csvContent = [headers.join(','), ...rows.map(r => r.join(','))].join('\n');
        const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.setAttribute('href', url);
        link.setAttribute('download', `returnflow_claims_${Date.now()}.csv`);
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    }

    document.addEventListener('keydown', (e) => {
        const activeTag = document.activeElement ? document.activeElement.tagName.toLowerCase() : '';
        const isEditing = activeTag === 'input' || activeTag === 'textarea' || activeTag === 'select';

        if (e.key === '/' && !isEditing) {
            e.preventDefault();
            filterSearchInput?.focus();
            return;
        }

        if (e.key === 'Escape') {
            if (isEditing) {
                document.activeElement.blur();
            } else {
                inspectorDetails.classList.add('d-none');
                inspectorPlaceholder.classList.remove('d-none');
                rmaTableBody.querySelectorAll('tr').forEach(r => r.classList.remove('row-selected'));
                selectedTicketId = null;
            }
            return;
        }

        if (isEditing) return;

        if (e.key === 'a' || e.key === 'A') {
            e.preventDefault();
            if (selectedTicketId) executeDecision('Approve');
        } else if (e.key === 'r' || e.key === 'R') {
            e.preventDefault();
            if (selectedTicketId) executeDecision('Reject');
        } else if (e.key === 'j' || e.key === 'J') {
            e.preventDefault();
            navigateRows(1);
        } else if (e.key === 'k' || e.key === 'K') {
            e.preventDefault();
            navigateRows(-1);
        }
    });

    function navigateRows(direction) {
        const visibleRows = Array.from(rmaTableBody.querySelectorAll('tr[data-id]'))
            .filter(r => r.style.display !== 'none');
        if (visibleRows.length === 0) return;

        let currentIndex = visibleRows.findIndex(r => r.getAttribute('data-id') === String(selectedTicketId));
        let nextIndex;

        if (currentIndex === -1) {
            nextIndex = direction > 0 ? 0 : visibleRows.length - 1;
        } else {
            nextIndex = currentIndex + direction;
            if (nextIndex < 0) nextIndex = 0;
            if (nextIndex >= visibleRows.length) nextIndex = visibleRows.length - 1;
        }

        const targetRow = visibleRows[nextIndex];
        if (targetRow) {
            const ticketId = parseInt(targetRow.getAttribute('data-id'), 10);
            selectTicketRow(ticketId);
            targetRow.scrollIntoView({ block: 'nearest', behavior: 'smooth' });
        }
    }

    function showToast(msg) {
        const toastContainer = document.getElementById('toastContainer');
        if (!toastContainer) return;

        const toastId = 'toast_' + Date.now();
        const toastHtml = `
            <div id="${toastId}" class="toast align-items-center text-bg-dark border-0 shadow-lg" role="alert" aria-live="assertive" aria-atomic="true" style="border-radius: 9999px; padding: 4px 12px;">
                <div class="d-flex">
                    <div class="toast-body d-flex align-items-center gap-2 small">
                        <i class="bi bi-box-seam-fill text-info"></i>
                        <span>${escapeHtml(msg)}</span>
                    </div>
                    <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
                </div>
            </div>
        `;

        toastContainer.insertAdjacentHTML('beforeend', toastHtml);
        const toastEl = document.getElementById(toastId);
        const bsToast = new bootstrap.Toast(toastEl, { delay: 5000 });
        bsToast.show();
        toastEl.addEventListener('hidden.bs.toast', () => toastEl.remove());
    }

    function getStatusBadgeClass(status) {
        switch (status) {
            case 'AutoApproved': return 'badge-auto-approved';
            case 'StaffApproved': return 'badge-staff-approved';
            case 'PendingReview': return 'badge-pending';
            case 'QuarantinedForFraud': return 'badge-quarantined';
            case 'Rejected': return 'badge-rejected';
            default: return 'text-steel';
        }
    }

    function getFraudBadgeClass(risk) {
        switch (risk?.toLowerCase()) {
            case 'high': return 'badge-pill-coral text-white';
            case 'medium': return 'badge-pill-neutral fw-bold';
            case 'low': return 'badge-pill-neutral text-success';
            default: return 'badge-pill-neutral';
        }
    }

    function getSentimentColor(score) {
        if (score <= 3) return '#10b981';
        if (score <= 6) return '#f59e0b';
        return '#f55036';
    }

    function formatEnumLabel(str) {
        if (!str) return 'General';
        return str.replace(/([A-Z])/g, ' $1').trim();
    }

    function escapeHtml(str) {
        if (!str) return '';
        return String(str)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;');
    }
});
