/**
 * ReturnFlow AI — Warehouse Operations Dashboard
 * Pure Vanilla JavaScript (ES6+) with Fetch API & Direct DOM Manipulation
 */

document.addEventListener('DOMContentLoaded', () => {
    // KPI Elements
    const kpiActiveRmas = document.getElementById('kpiActiveRmas');
    const kpiValueAtRisk = document.getElementById('kpiValueAtRisk');
    const kpiAutoResolved = document.getElementById('kpiAutoResolved');
    const kpiFraudAlerts = document.getElementById('kpiFraudAlerts');
    const kpiStockRecovered = document.getElementById('kpiStockRecovered');

    // Filter Elements
    const filterSearchInput = document.getElementById('filterSearchInput');
    const filterStatusSelect = document.getElementById('filterStatusSelect');
    const filterFraudSelect = document.getElementById('filterFraudSelect');
    const filterChannelSelect = document.getElementById('filterChannelSelect');
    const btnRefreshData = document.getElementById('btnRefreshData');

    // Table Elements
    const rmaTableBody = document.getElementById('rmaTableBody');

    // Inspector Elements
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

    // Action Buttons
    const btnApproveSync = document.getElementById('btnApproveSync');
    const btnApplyRestocking = document.getElementById('btnApplyRestocking');
    const btnRejectClaim = document.getElementById('btnRejectClaim');

    let ticketsCache = [];
    let selectedTicketId = null;

    // ── 1. Initial Data Fetch ────────────────────────────────────────────────
    fetchKpis();
    fetchTickets();

    btnRefreshData.addEventListener('click', () => {
        fetchKpis();
        fetchTickets();
    });

    // ── 2. KPI Ribbon Data ───────────────────────────────────────────────────
    async function fetchKpis() {
        try {
            const res = await fetch('/api/inventory/kpis');
            if (!res.ok) return;
            const kpis = await res.json();

            kpiActiveRmas.textContent = kpis.totalActiveRmas;
            kpiValueAtRisk.textContent = `₹${Number(kpis.totalValueAtRisk).toLocaleString('en-IN', { minimumFractionDigits: 2 })}`;
            kpiAutoResolved.textContent = `${kpis.autoResolvedPercent}%`;
            kpiFraudAlerts.textContent = kpis.highFraudAlerts;
            kpiStockRecovered.textContent = kpis.stockRecovered;
        } catch (e) {
            console.error('Failed to load KPIs:', e);
        }
    }

    // ── 3. Table Data ────────────────────────────────────────────────────────
    async function fetchTickets() {
        try {
            const res = await fetch('/api/rma');
            if (!res.ok) throw new Error('Failed to load tickets');
            ticketsCache = await res.json();
            renderTableRows(ticketsCache);
        } catch (e) {
            rmaTableBody.innerHTML = `<tr><td colspan="7" class="text-center text-danger py-4">${e.message}</td></tr>`;
        }
    }

    function renderTableRows(tickets) {
        if (!tickets || tickets.length === 0) {
            rmaTableBody.innerHTML = `<tr><td colspan="7" class="text-center text-muted py-4">No RMA tickets found matching filter.</td></tr>`;
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
                <td class="font-mono fw-bold text-primary">${escapeHtml(t.ticketNumber)}</td>
                <td>
                    <div class="fw-semibold">${escapeHtml(t.customerName)}</div>
                    <div class="text-muted small font-mono">${escapeHtml(t.productName)} (${escapeHtml(t.sku)})</div>
                </td>
                <td><span class="badge bg-light text-dark border">${escapeHtml(t.salesChannel)}</span></td>
                <td><span class="badge rounded-pill px-2.5 py-1 ${statusClass}" id="rowBadge_${t.id}">${escapeHtml(t.status)}</span></td>
                <td><span class="badge ${fraudClass}">${escapeHtml(t.fraudRiskLevel || 'Low')}</span></td>
                <td class="fw-bold text-success font-mono">₹${Number(t.calculatedRefundAmount).toLocaleString('en-IN', { minimumFractionDigits: 2 })}</td>
                <td class="text-muted small">${t.daysSinceDelivery}d ago</td>
            `;

            tr.addEventListener('click', () => {
                selectTicketRow(t.id);
            });

            rmaTableBody.appendChild(tr);
        });
    }

    // ── 4. Client-side DOM Filtering ─────────────────────────────────────────
    [filterSearchInput, filterStatusSelect, filterFraudSelect, filterChannelSelect].forEach(el => {
        el.addEventListener('input', applyFilters);
        el.addEventListener('change', applyFilters);
    });

    function applyFilters() {
        const query = filterSearchInput.value.trim().toLowerCase();
        const status = filterStatusSelect.value;
        const fraud = filterFraudSelect.value;
        const channel = filterChannelSelect.value;

        const rows = rmaTableBody.querySelectorAll('tr[data-id]');
        rows.forEach(row => {
            const rSearch = row.getAttribute('data-search') || '';
            const rStatus = row.getAttribute('data-status') || '';
            const rFraud = row.getAttribute('data-fraud') || '';
            const rChannel = row.getAttribute('data-channel') || '';

            const matchQuery = !query || rSearch.includes(query);
            const matchStatus = status === 'All' || rStatus.toLowerCase() === status.toLowerCase();
            const matchFraud = fraud === 'All' || rFraud.toLowerCase() === fraud.toLowerCase();
            const matchChannel = channel === 'All' || rChannel.toLowerCase() === channel.toLowerCase();

            if (matchQuery && matchStatus && matchFraud && matchChannel) {
                row.style.display = '';
            } else {
                row.style.display = 'none';
            }
        });
    }

    // ── 5. Row Selection & Detail Panel Loading ─────────────────────────────
    async function selectTicketRow(ticketId) {
        selectedTicketId = ticketId;

        // Visual selection in table
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
        inspCustomerOrder.textContent = `${t.customerName} • ${t.orderNumber} (${t.salesChannel})`;

        // Badges
        inspStatusBadge.textContent = t.status;
        inspStatusBadge.className = 'badge rounded-pill px-3 py-1.5 ' + getStatusBadgeClass(t.status);

        // SQL Relational Facts
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

        // AI Structured Extraction
        const fraudRisk = t.aiFraudRiskLevel || 'Low';
        inspFraudBadge.textContent = `Fraud: ${fraudRisk}`;
        inspFraudBadge.className = 'badge ' + getFraudBadgeClass(fraudRisk);

        inspCategoryBadge.textContent = `Defect: ${t.aiExtractedDefectCategory || 'General'}`;
        inspActionBadge.textContent = `Action: ${formatEnumLabel(t.aiRecommendedAction)}`;
        inspRoutingBadge.textContent = `Routing: ${formatEnumLabel(t.warehouseRoutingAction)}`;

        // Sentiment
        const score = Math.max(1, Math.min(10, t.aiSentimentScore || 5));
        inspSentimentScore.textContent = `${score}/10`;
        inspSentimentFill.style.width = `${score * 10}%`;
        inspSentimentFill.style.backgroundColor = getSentimentColor(score);

        inspReasoningText.textContent = t.aiReasoningSummary || 'Deterministic verification.';
        inspEmailText.value = t.aiDraftedCustomerEmail || '';
    }

    // ── 6. Decision Action Handlers (PATCH /api/rma/{id}/decision) ───────────
    btnApproveSync.addEventListener('click', () => executeDecision('Approve'));
    btnApplyRestocking.addEventListener('click', () => executeDecision('ApplyRestockingFee'));
    btnRejectClaim.addEventListener('click', () => executeDecision('Reject'));

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

            // 1. In-DOM badge update on inspector & row
            inspStatusBadge.textContent = result.newStatus;
            inspStatusBadge.className = 'badge rounded-pill px-3 py-1.5 ' + getStatusBadgeClass(result.newStatus);

            const rowBadge = document.getElementById(`rowBadge_${selectedTicketId}`);
            if (rowBadge) {
                rowBadge.textContent = result.newStatus;
                rowBadge.className = 'badge rounded-pill px-2.5 py-1 ' + getStatusBadgeClass(result.newStatus);
                // Animate row
                const row = document.getElementById(`row_${selectedTicketId}`);
                if (row) {
                    row.setAttribute('data-status', result.newStatus);
                    row.classList.add('table-success');
                    setTimeout(() => row.classList.remove('table-success'), 1200);
                }
            }

            // 2. Refresh top KPI ribbon
            fetchKpis();

            // 3. Show Toast notification with exact SQL inventory change
            showToast(result.stockUpdateMessage || result.message);

        } catch (e) {
            alert(`Decision error: ${e.message}`);
        }
    }

    function showToast(msg) {
        const toastContainer = document.getElementById('toastContainer');
        if (!toastContainer) return;

        const toastId = 'toast_' + Date.now();
        const toastHtml = `
            <div id="${toastId}" class="toast align-items-center text-bg-dark border-0 shadow-lg" role="alert" aria-live="assertive" aria-atomic="true">
                <div class="d-flex">
                    <div class="toast-body d-flex align-items-center gap-2">
                        <i class="bi bi-box-seam-fill text-info fs-5"></i>
                        <span>${escapeHtml(msg)}</span>
                    </div>
                    <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
                </div>
            </div>
        `;

        toastContainer.insertAdjacentHTML('beforeend', toastHtml);
        const toastEl = document.getElementById(toastId);
        const bsToast = new bootstrap.Toast(toastEl, { delay: 6000 });
        bsToast.show();
        toastEl.addEventListener('hidden.bs.toast', () => toastEl.remove());
    }

    // Helper functions
    function getStatusBadgeClass(status) {
        switch (status) {
            case 'AutoApproved': return 'badge-auto-approved';
            case 'StaffApproved': return 'badge-staff-approved';
            case 'PendingReview': return 'badge-pending';
            case 'QuarantinedForFraud': return 'badge-quarantined';
            case 'Rejected': return 'badge-rejected';
            default: return 'bg-secondary text-white';
        }
    }

    function getFraudBadgeClass(risk) {
        switch (risk?.toLowerCase()) {
            case 'high': return 'bg-danger text-white';
            case 'medium': return 'bg-warning text-dark';
            case 'low': return 'bg-success text-white';
            default: return 'bg-secondary text-white';
        }
    }

    function getSentimentColor(score) {
        if (score <= 3) return '#10b981';
        if (score <= 6) return '#f59e0b';
        return '#f43f5e';
    }

    function formatEnumLabel(str) {
        if (!str) return '--';
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
