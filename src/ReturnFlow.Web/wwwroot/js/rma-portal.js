/**
 * ReturnFlow AI — Customer Self-Service Return Portal
 * Pure Vanilla JavaScript (ES6+) with Fetch API & Direct DOM Manipulation
 */

document.addEventListener('DOMContentLoaded', () => {
    // DOM Elements
    const demoOrderSelect = document.getElementById('demoOrderSelect');
    const orderNumberInput = document.getElementById('orderNumberInput');
    const btnLookupOrder = document.getElementById('btnLookupOrder');
    const lookupSpinner = document.getElementById('lookupSpinner');
    const lookupAlert = document.getElementById('lookupAlert');

    const step2Container = document.getElementById('step2Container');
    const step3Container = document.getElementById('step3Container');
    const lineItemsContainer = document.getElementById('lineItemsContainer');
    const orderMetaBadge = document.getElementById('orderMetaBadge');

    const conditionSelect = document.getElementById('conditionSelect');
    const resolutionSelect = document.getElementById('resolutionSelect');
    const complaintText = document.getElementById('complaintText');
    const btnSubmitRma = document.getElementById('btnSubmitRma');

    const loadingPanel = document.getElementById('loadingPanel');
    const emptyResultPlaceholder = document.getElementById('emptyResultPlaceholder');
    const resultContainer = document.getElementById('resultContainer');

    // Result DOM Elements
    const resTicketNumber = document.getElementById('resTicketNumber');
    const resStatusBadge = document.getElementById('resStatusBadge');
    const resRefundAmount = document.getElementById('resRefundAmount');
    const resRestockingFee = document.getElementById('resRestockingFee');
    const resDefectCategory = document.getElementById('resDefectCategory');
    const resFraudRiskBadge = document.getElementById('resFraudRiskBadge');
    const resRecommendedAction = document.getElementById('resRecommendedAction');
    const resRoutingAction = document.getElementById('resRoutingAction');
    const resSentimentValue = document.getElementById('resSentimentValue');
    const resSentimentFill = document.getElementById('resSentimentFill');
    const resReasoningText = document.getElementById('resReasoningText');
    const resDraftedEmail = document.getElementById('resDraftedEmail');

    let currentOrder = null;

    // ── 1. Quick Fill Demo Order Dropdown ────────────────────────────────────
    demoOrderSelect.addEventListener('change', (e) => {
        const val = e.target.value;
        if (!val) return;
        orderNumberInput.value = val;
        lookupOrder(val);
    });

    // ── 2. Order Lookup ─────────────────────────────────────────────────────
    btnLookupOrder.addEventListener('click', () => {
        const orderNum = orderNumberInput.value.trim();
        if (!orderNum) {
            showAlert('Please enter an order number.');
            return;
        }
        lookupOrder(orderNum);
    });

    orderNumberInput.addEventListener('keydown', (e) => {
        if (e.key === 'Enter') {
            e.preventDefault();
            btnLookupOrder.click();
        }
    });

    async function lookupOrder(orderNumber) {
        hideAlert();
        setLookupLoading(true);

        try {
            const res = await fetch(`/api/orders/${encodeURIComponent(orderNumber)}`);
            if (!res.ok) {
                const err = await res.json().catch(() => ({}));
                throw new Error(err.detail || `Order ${orderNumber} could not be found.`);
            }

            currentOrder = await res.json();
            renderLineItems(currentOrder);

            orderMetaBadge.textContent = `${currentOrder.salesChannel} • Delivered ${currentOrder.daysSinceDelivery}d ago`;
            step2Container.classList.remove('d-none');
            step3Container.classList.remove('d-none');

        } catch (error) {
            showAlert(error.message);
            step2Container.classList.add('d-none');
            step3Container.classList.add('d-none');
            currentOrder = null;
        } finally {
            setLookupLoading(false);
        }
    }

    function renderLineItems(order) {
        lineItemsContainer.innerHTML = '';

        order.lineItems.forEach((li, idx) => {
            const itemDiv = document.createElement('div');
            itemDiv.className = 'form-check p-3 border rounded-3 d-flex align-items-center justify-content-between mb-1';
            itemDiv.style.cursor = 'pointer';

            const badgeColor = li.warrantyBadge === 'Within Return Window'
                ? 'bg-primary-subtle text-primary border border-primary-subtle'
                : li.warrantyBadge === 'In Warranty'
                    ? 'bg-success-subtle text-success border border-success-subtle'
                    : 'bg-danger-subtle text-danger border border-danger-subtle';

            itemDiv.innerHTML = `
                <div class="d-flex align-items-center gap-3">
                    <input class="form-check-input mt-0" type="radio" name="selectedLineItem" id="lineItem_${li.id}" value="${li.id}" ${idx === 0 ? 'checked' : ''}>
                    <div>
                        <label class="form-check-label fw-bold mb-0 d-block" for="lineItem_${li.id}" style="cursor: pointer;">
                            ${escapeHtml(li.productName)}
                        </label>
                        <span class="text-muted small font-mono">SKU: ${escapeHtml(li.sku)} • Qty: ${li.quantityPurchased} • ₹${Number(li.unitPricePaid).toLocaleString('en-IN', { minimumFractionDigits: 2 })}</span>
                    </div>
                </div>
                <div>
                    <span class="badge ${badgeColor}">${li.warrantyBadge}</span>
                </div>
            `;

            itemDiv.addEventListener('click', () => {
                const radio = itemDiv.querySelector('input[type="radio"]');
                if (radio) radio.checked = true;
            });

            lineItemsContainer.appendChild(itemDiv);
        });
    }

    // ── 3. Preset Test Pills ────────────────────────────────────────────────
    document.querySelectorAll('.preset-pill').forEach(pill => {
        pill.addEventListener('click', () => {
            const preset = pill.getAttribute('data-preset');
            switch (preset) {
                case 'warranty':
                    complaintText.value = "The Thunderbolt dock keeps disconnecting every 10 minutes and smells like burnt plastic. Need an urgent replacement for our office.";
                    conditionSelect.value = "Defective";
                    resolutionSelect.value = "Replacement";
                    break;
                case 'remorse':
                    complaintText.value = "I opened the box and tried the keyboard for a week, but I don't like the tactile switch sound. I want a full refund.";
                    conditionSelect.value = "OpenedUnused";
                    resolutionSelect.value = "Refund";
                    break;
                case 'fraud':
                    complaintText.value = "I dropped the 4K monitor down the stairs while moving apartments and the screen shattered. Send me a brand new one under warranty immediately or I will post bad reviews!";
                    conditionSelect.value = "Defective";
                    resolutionSelect.value = "Replacement";
                    break;
            }
        });
    });

    // ── 4. Submit RMA for AI Triage ─────────────────────────────────────────
    btnSubmitRma.addEventListener('click', async () => {
        if (!currentOrder) {
            showAlert('Please look up a valid order first.');
            return;
        }

        const selectedRadio = document.querySelector('input[name="selectedLineItem"]:checked');
        if (!selectedRadio) {
            showAlert('Please select an item to return.');
            return;
        }

        const lineItemId = parseInt(selectedRadio.value, 10);
        const complaint = complaintText.value.trim();

        if (!complaint) {
            alert('Please provide a brief explanation in the complaint text area.');
            complaintText.focus();
            return;
        }

        // Show loading state
        emptyResultPlaceholder.classList.add('d-none');
        resultContainer.classList.add('d-none');
        loadingPanel.classList.remove('d-none');
        btnSubmitRma.disabled = true;

        const payload = {
            orderNumber: currentOrder.orderNumber,
            orderLineItemId: lineItemId,
            customerComplaintRawText: complaint,
            requestedResolution: resolutionSelect.value,
            conditionReportedByCustomer: conditionSelect.value
        };

        try {
            const res = await fetch('/api/rma/analyze', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });

            if (!res.ok) {
                const err = await res.json().catch(() => ({}));
                throw new Error(err.detail || 'Failed to process AI triage.');
            }

            const triage = await res.json();
            displayTriageResult(triage);

        } catch (error) {
            alert(`Error during triage: ${error.message}`);
            emptyResultPlaceholder.classList.remove('d-none');
        } finally {
            loadingPanel.classList.add('d-none');
            btnSubmitRma.disabled = false;
        }
    });

    function displayTriageResult(data) {
        resultContainer.classList.remove('d-none');

        resTicketNumber.textContent = data.ticketNumber;
        resRefundAmount.textContent = `₹${Number(data.calculatedRefundAmount).toLocaleString('en-IN', { minimumFractionDigits: 2 })}`;
        resRestockingFee.textContent = `₹${Number(data.restockingFeeAmount).toLocaleString('en-IN', { minimumFractionDigits: 2 })}`;

        // Status Badge
        resStatusBadge.textContent = data.status;
        resStatusBadge.className = 'badge rounded-pill px-3 py-1.5 ' + getStatusBadgeClass(data.status);

        // Defect Category
        resDefectCategory.textContent = data.aiExtractedDefectCategory || 'General Defect';

        // Fraud Badge
        const fraudRisk = data.aiFraudRiskLevel || 'Low';
        resFraudRiskBadge.textContent = `${fraudRisk} Risk`;
        resFraudRiskBadge.className = 'badge ' + getFraudBadgeClass(fraudRisk);

        // Recommended Action
        resRecommendedAction.textContent = formatEnumLabel(data.aiRecommendedAction);

        // Routing Action
        resRoutingAction.textContent = formatEnumLabel(data.warehouseRoutingAction);

        // Sentiment Gauge
        const score = Math.max(1, Math.min(10, data.aiSentimentScore || 5));
        resSentimentValue.textContent = `${score}/10`;
        resSentimentFill.style.width = `${score * 10}%`;
        resSentimentFill.style.backgroundColor = getSentimentColor(score);

        // Reasoning & Email
        resReasoningText.textContent = data.aiReasoningSummary || 'Deterministic policy verification completed.';
        resDraftedEmail.value = data.aiDraftedCustomerEmail || '';
    }

    // Helper utilities
    function setLookupLoading(isLoading) {
        if (isLoading) {
            lookupSpinner.classList.remove('d-none');
            btnLookupOrder.disabled = true;
        } else {
            lookupSpinner.classList.add('d-none');
            btnLookupOrder.disabled = false;
        }
    }

    function showAlert(msg) {
        lookupAlert.textContent = msg;
        lookupAlert.classList.remove('d-none');
    }

    function hideAlert() {
        lookupAlert.classList.add('d-none');
        lookupAlert.textContent = '';
    }

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
        if (score <= 3) return '#10b981'; // emerald
        if (score <= 6) return '#f59e0b'; // amber
        return '#f43f5e';                // rose
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
