document.addEventListener('DOMContentLoaded', () => {
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

    const policySandboxSlider = document.getElementById('policySandboxSlider');
    const sliderDaysDisplay = document.getElementById('sliderDaysDisplay');
    const simulatedWindowNote = document.getElementById('simulatedWindowNote');
    const simWarrantyBadge = document.getElementById('simWarrantyBadge');
    const simReturnBadge = document.getElementById('simReturnBadge');

    const uploadDropzone = document.getElementById('uploadDropzone');
    const evidenceFileInput = document.getElementById('evidenceFileInput');
    const uploadPreviewContainer = document.getElementById('uploadPreviewContainer');
    const evidenceImagePreview = document.getElementById('evidenceImagePreview');
    const evidenceFileName = document.getElementById('evidenceFileName');
    const btnRemoveEvidence = document.getElementById('btnRemoveEvidence');

    const btnOpenCarrierLabel = document.getElementById('btnOpenCarrierLabel');
    const labelRmaNumber = document.getElementById('labelRmaNumber');
    const labelCustomerName = document.getElementById('labelCustomerName');
    const labelOrderRef = document.getElementById('labelOrderRef');
    const labelProductSku = document.getElementById('labelProductSku');
    const labelRoutingAction = document.getElementById('labelRoutingAction');
    const labelBarcodeText = document.getElementById('labelBarcodeText');

    let currentOrder = null;
    let latestTriageData = null;

    initPolicySlider();
    initUploadDropzone();
    initPresetCards();

    demoOrderSelect.addEventListener('change', (e) => {
        const val = e.target.value;
        if (!val) return;
        orderNumberInput.value = val;
        lookupOrder(val);
    });

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

            policySandboxSlider.value = currentOrder.daysSinceDelivery;
            updatePolicySliderDisplay(currentOrder.daysSinceDelivery);
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
            itemDiv.className = 'p-3 border rounded-3 d-flex align-items-center justify-content-between bg-white';
            itemDiv.style.cursor = 'pointer';

            const badgeClass = li.warrantyBadge === 'Within Return Window'
                ? 'badge-pill-success'
                : li.warrantyBadge === 'In Warranty'
                    ? 'badge-pill-neutral'
                    : 'badge-pill-danger';

            itemDiv.innerHTML = `
                <div class="d-flex align-items-center gap-3">
                    <input class="form-check-input mt-0" type="radio" name="selectedLineItem" id="lineItem_${li.id}" value="${li.id}" ${idx === 0 ? 'checked' : ''}>
                    <div>
                        <label class="form-check-label fw-bold mb-0 d-block" for="lineItem_${li.id}" style="cursor: pointer;">
                            ${escapeHtml(li.productName)}
                        </label>
                        <span class="text-steel small font-mono">SKU: ${escapeHtml(li.sku)} • Qty: ${li.quantityPurchased} • ₹${Number(li.unitPricePaid).toLocaleString('en-IN', { minimumFractionDigits: 2 })}</span>
                    </div>
                </div>
                <div>
                    <span class="${badgeClass}">${li.warrantyBadge}</span>
                </div>
            `;

            itemDiv.addEventListener('click', () => {
                const radio = itemDiv.querySelector('input[type="radio"]');
                if (radio) radio.checked = true;
            });

            lineItemsContainer.appendChild(itemDiv);
        });
    }

    function initPresetCards() {
        const presets = {
            warranty: {
                order: 'ORD-2026-1003',
                complaint: "The Thunderbolt dock keeps disconnecting every 10 minutes and smells like burnt plastic. Need an urgent replacement for our office.",
                condition: 'Defective',
                resolution: 'Replacement'
            },
            remorse: {
                order: 'ORD-2026-1001',
                complaint: "I opened the box and tried the keyboard for a week, but I don't like the tactile switch sound. I want a full refund.",
                condition: 'OpenedUnused',
                resolution: 'Refund'
            },
            fraud: {
                order: 'ORD-2026-1002',
                complaint: "I dropped the 4K monitor down the stairs while moving apartments and the screen shattered. Send me a brand new one under warranty immediately or I will post bad reviews!",
                condition: 'Defective',
                resolution: 'Replacement'
            }
        };

        ['cardPresetWarranty', 'cardPresetRemorse', 'cardPresetFraud'].forEach(id => {
            const card = document.getElementById(id);
            if (!card) return;
            const key = card.getAttribute('data-preset');
            card.addEventListener('click', () => {
                const p = presets[key];
                if (!p) return;
                orderNumberInput.value = p.order;
                complaintText.value = p.complaint;
                conditionSelect.value = p.condition;
                resolutionSelect.value = p.resolution;
                lookupOrder(p.order);
            });
        });
    }

    function initPolicySlider() {
        if (!policySandboxSlider) return;
        policySandboxSlider.addEventListener('input', (e) => {
            updatePolicySliderDisplay(parseInt(e.target.value, 10));
        });
    }

    function updatePolicySliderDisplay(days) {
        sliderDaysDisplay.textContent = `${days} Days Ago`;

        if (days <= 30) {
            simulatedWindowNote.textContent = 'Standard Return Window Active (30 Days)';
            simWarrantyBadge.textContent = 'In Warranty';
            simWarrantyBadge.className = 'badge-pill-success';
            simReturnBadge.textContent = 'Eligible for Return';
            simReturnBadge.className = 'badge-pill-success';
        } else if (days <= 365) {
            simulatedWindowNote.textContent = 'Return Window Closed • Warranty Only Active';
            simWarrantyBadge.textContent = 'In Warranty';
            simWarrantyBadge.className = 'badge-pill-success';
            simReturnBadge.textContent = '30-Day Window Closed';
            simReturnBadge.className = 'badge-pill-neutral';
        } else {
            simulatedWindowNote.textContent = 'Out of Policy • All Windows Expired';
            simWarrantyBadge.textContent = 'Warranty Expired';
            simWarrantyBadge.className = 'badge-pill-danger';
            simReturnBadge.textContent = 'Return Closed';
            simReturnBadge.className = 'badge-pill-danger';
        }
    }

    function initUploadDropzone() {
        if (!uploadDropzone || !evidenceFileInput) return;

        uploadDropzone.addEventListener('click', () => evidenceFileInput.click());

        uploadDropzone.addEventListener('dragover', (e) => {
            e.preventDefault();
            uploadDropzone.style.borderColor = '#000000';
            uploadDropzone.style.background = '#f0f0f2';
        });

        uploadDropzone.addEventListener('dragleave', () => {
            uploadDropzone.style.borderColor = 'var(--color-hairline)';
            uploadDropzone.style.background = 'var(--color-surface-soft)';
        });

        uploadDropzone.addEventListener('drop', (e) => {
            e.preventDefault();
            uploadDropzone.style.borderColor = 'var(--color-hairline)';
            uploadDropzone.style.background = 'var(--color-surface-soft)';
            if (e.dataTransfer.files && e.dataTransfer.files[0]) {
                handleFileSelected(e.dataTransfer.files[0]);
            }
        });

        evidenceFileInput.addEventListener('change', (e) => {
            if (e.target.files && e.target.files[0]) {
                handleFileSelected(e.target.files[0]);
            }
        });

        if (btnRemoveEvidence) {
            btnRemoveEvidence.addEventListener('click', () => {
                evidenceFileInput.value = '';
                uploadPreviewContainer.classList.add('d-none');
                uploadPreviewContainer.classList.remove('d-flex');
                uploadDropzone.classList.remove('d-none');
            });
        }
    }

    function handleFileSelected(file) {
        evidenceFileName.textContent = file.name;
        const reader = new FileReader();
        reader.onload = (e) => {
            evidenceImagePreview.src = e.target.result;
            uploadPreviewContainer.classList.remove('d-none');
            uploadPreviewContainer.classList.add('d-flex');
            uploadDropzone.classList.add('d-none');
        };
        reader.readAsDataURL(file);
    }

    btnSubmitRma.addEventListener('click', async () => {
        if (!currentOrder) {
            showAlert('Please select and look up an order first.');
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
            alert('Please describe the issue in the complaint text area.');
            complaintText.focus();
            return;
        }

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

            latestTriageData = await res.json();
            displayTriageResult(latestTriageData);
        } catch (error) {
            alert(`Triage error: ${error.message}`);
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

        resStatusBadge.textContent = data.status;
        resStatusBadge.className = 'badge-pill-dark ' + getStatusPillClass(data.status);

        resDefectCategory.textContent = data.aiExtractedDefectCategory || 'General Defect';

        const fraudRisk = data.aiFraudRiskLevel || 'Low';
        resFraudRiskBadge.textContent = `${fraudRisk} Risk`;
        resFraudRiskBadge.className = 'badge-pill-dark ' + getFraudPillClass(fraudRisk);

        resRecommendedAction.textContent = formatEnumLabel(data.aiRecommendedAction);
        resRoutingAction.textContent = formatEnumLabel(data.warehouseRoutingAction);

        const score = Math.max(1, Math.min(10, data.aiSentimentScore || 5));
        resSentimentValue.textContent = `${score}/10`;
        resSentimentFill.style.width = `${score * 10}%`;
        resSentimentFill.style.backgroundColor = getSentimentColor(score);

        resReasoningText.textContent = data.aiReasoningSummary || 'Deterministic policy verification completed.';
        resDraftedEmail.value = data.aiDraftedCustomerEmail || '';
    }

    if (btnOpenCarrierLabel) {
        btnOpenCarrierLabel.addEventListener('click', () => {
            if (!latestTriageData && !currentOrder) return;

            const t = latestTriageData || {};
            const ord = currentOrder || {};

            labelRmaNumber.textContent = t.ticketNumber || 'RMA-9002';
            labelCustomerName.textContent = ord.customerName || t.customerName || 'Aarav Sharma';
            labelOrderRef.textContent = `Order ${ord.orderNumber || t.orderNumber || 'ORD-2026-1001'}`;
            labelProductSku.textContent = t.sku || 'SKU-DOCK-TB4';
            labelRoutingAction.textContent = (t.warehouseRoutingAction || 'SEND TO REFURBISH').replace(/([A-Z])/g, ' $1').trim().toUpperCase();
            labelBarcodeText.textContent = `*DEL-${t.ticketNumber || 'RMA-9002'}-EXPEDITED*`;

            const modalEl = document.getElementById('carrierLabelModal');
            if (modalEl) {
                const bsModal = new bootstrap.Modal(modalEl);
                bsModal.show();
            }
        });
    }

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

    function getStatusPillClass(status) {
        switch (status) {
            case 'AutoApproved': return 'badge-pill-success';
            case 'StaffApproved': return 'badge-pill-neutral';
            case 'PendingReview': return 'badge-pill-warning';
            case 'QuarantinedForFraud': return 'badge-pill-danger';
            case 'Rejected': return 'badge-pill-neutral';
            default: return 'badge-pill-neutral';
        }
    }

    function getFraudPillClass(risk) {
        switch (risk?.toLowerCase()) {
            case 'high': return 'badge-pill-danger';
            case 'medium': return 'badge-pill-warning';
            case 'low': return 'badge-pill-success';
            default: return 'badge-pill-neutral';
        }
    }

    function getSentimentColor(score) {
        if (score <= 3) return '#1b7a43';
        if (score <= 6) return '#b25e09';
        return '#c52222';
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
