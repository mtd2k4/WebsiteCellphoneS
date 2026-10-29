/**
 * Quản lý Áp dụng Mã giảm giá (Mã Đơn Hàng & Mã Phí Vận Chuyển)
 * Cho phép áp dụng đồng thời cả 2 loại mã trên cùng một đơn hàng
 */

(function () {
    let _activeOrderCoupon = null;
    let _activeShippingCoupon = null;
    let _availableCoupons = [];
    let _subtotal = 0;
    function getDefaultShippingFee(subtotal) {
        return (subtotal >= 300000) ? 0 : 30000;
    }

    function formatMoney(amount) {
        return new Intl.NumberFormat('vi-VN').format(Math.round(amount || 0)) + '₫';
    }

    async function fetchAvailableCoupons() {
        try {
            const res = await fetch('/Cart/GetAvailableCoupons');
            if (res.ok) {
                const data = await res.json();
                if (data.success) {
                    _availableCoupons = data.coupons || [];
                    _subtotal = data.subtotal || 0;
                    return _availableCoupons;
                }
            }
        } catch (e) {
            console.error('Lỗi tải danh sách mã giảm giá:', e);
        }
        return [];
    }

    async function validateAndApply(orderCode, shippingCode) {
        try {
            const formData = new FormData();
            if (orderCode) formData.append('orderCode', orderCode);
            if (shippingCode) formData.append('shippingCode', shippingCode);

            const res = await fetch('/Cart/ValidateCoupons', {
                method: 'POST',
                body: formData
            });

            if (res.ok) {
                return await res.json();
            }
        } catch (e) {
            console.error('Lỗi xác thực mã giảm giá:', e);
        }
        return null;
    }

    function updateSummaryUI(result) {
        if (!result) return;

        const subtotal = result.subtotal || _subtotal;
        const defaultFee = getDefaultShippingFee(subtotal);
        const shippingFee = result.shippingFee !== undefined ? result.shippingFee : defaultFee;
        const orderDiscount = result.orderDiscountAmount || 0;
        const shippingDiscount = result.shippingDiscountAmount || 0;
        const effectiveShipping = (subtotal >= 300000) ? 0 : Math.max(0, shippingFee - shippingDiscount);
        const finalTotal = result.finalTotal !== undefined ? result.finalTotal : Math.max(0, subtotal + effectiveShipping - orderDiscount);

        // 1. Cập nhật nhãn Tạm tính
        const subtotalEl = document.getElementById('summarySubtotal');
        if (subtotalEl) subtotalEl.innerText = formatMoney(subtotal);

        // 2. Cập nhật Phí giao hàng
        const shipFeeEl = document.getElementById('summaryShippingFee');
        if (shipFeeEl) {
            if (subtotal >= 300000 || shippingFee === 0) {
                shipFeeEl.className = 'fw-bold text-success';
                shipFeeEl.innerText = 'MIỄN PHÍ';
            } else if (shippingDiscount > 0) {
                shipFeeEl.className = 'fw-bold text-success';
                shipFeeEl.innerText = formatMoney(Math.max(0, shippingFee - shippingDiscount));
            } else {
                shipFeeEl.className = 'fw-bold text-dark';
                shipFeeEl.innerText = formatMoney(shippingFee);
            }
        }

        // 3. Cập nhật Giảm giá đơn hàng
        const orderRow = document.getElementById('summaryOrderDiscountRow');
        const orderCodeEl = document.getElementById('summaryOrderDiscountCode');
        const orderAmountEl = document.getElementById('summaryOrderDiscountAmount');
        const orderStatusBadge = document.getElementById('orderCouponStatus');
        const orderInput = document.getElementById('orderCouponInput');
        const btnApplyOrder = document.getElementById('btnApplyOrderCoupon');
        const btnRemoveOrder = document.getElementById('btnRemoveOrderCoupon');
        const orderMsg = document.getElementById('orderCouponMessage');

        if (_activeOrderCoupon && result.orderCoupon && result.orderCoupon.valid) {
            if (orderRow) orderRow.classList.remove('d-none');
            if (orderCodeEl) orderCodeEl.innerText = result.orderCoupon.code;
            if (orderAmountEl) orderAmountEl.innerText = '-' + formatMoney(orderDiscount);
            if (orderStatusBadge) {
                orderStatusBadge.className = 'badge bg-success-subtle text-success border border-success-subtle';
                orderStatusBadge.innerHTML = '<i class="bi bi-check-circle-fill me-1"></i>Đã áp dụng';
            }
            if (orderInput) {
                orderInput.value = result.orderCoupon.code;
                orderInput.readOnly = true;
            }
            if (btnApplyOrder) btnApplyOrder.classList.add('d-none');
            if (btnRemoveOrder) btnRemoveOrder.classList.remove('d-none');
            if (orderMsg) {
                orderMsg.className = 'small text-success mt-1 d-block fw-semibold';
                orderMsg.innerHTML = `<i class="bi bi-check2-circle me-1"></i>${result.orderCoupon.message} (${result.orderCoupon.title})`;
            }
        } else {
            if (orderRow) orderRow.classList.add('d-none');
            if (orderStatusBadge) {
                orderStatusBadge.className = 'badge bg-light text-muted border';
                orderStatusBadge.innerText = 'Chưa áp dụng';
            }
            if (orderInput) {
                orderInput.readOnly = false;
            }
            if (btnApplyOrder) btnApplyOrder.classList.remove('d-none');
            if (btnRemoveOrder) btnRemoveOrder.classList.add('d-none');
            if (orderMsg) {
                if (result.orderCoupon && !result.orderCoupon.valid) {
                    orderMsg.className = 'small text-danger mt-1 d-block';
                    orderMsg.innerHTML = `<i class="bi bi-exclamation-circle me-1"></i>${result.orderCoupon.message}`;
                } else {
                    orderMsg.className = 'small mt-1 d-none';
                    orderMsg.innerText = '';
                }
            }
        }

        // 4. Cập nhật Giảm phí vận chuyển (Freeship)
        const shipRow = document.getElementById('summaryShippingDiscountRow');
        const shipCodeEl = document.getElementById('summaryShippingDiscountCode');
        const shipAmountEl = document.getElementById('summaryShippingDiscountAmount');
        const shipStatusBadge = document.getElementById('shippingCouponStatus');
        const shipInput = document.getElementById('shippingCouponInput');
        const btnApplyShip = document.getElementById('btnApplyShippingCoupon');
        const btnRemoveShip = document.getElementById('btnRemoveShippingCoupon');
        const shipMsg = document.getElementById('shippingCouponMessage');

        if (_activeShippingCoupon && result.shippingCoupon && result.shippingCoupon.valid) {
            if (shipRow) shipRow.classList.remove('d-none');
            if (shipCodeEl) shipCodeEl.innerText = result.shippingCoupon.code;
            if (shipAmountEl) shipAmountEl.innerText = '-' + formatMoney(shippingDiscount);
            if (shipStatusBadge) {
                shipStatusBadge.className = 'badge bg-success-subtle text-success border border-success-subtle';
                shipStatusBadge.innerHTML = '<i class="bi bi-truck me-1"></i>Đã giảm ship';
            }
            if (shipInput) {
                shipInput.value = result.shippingCoupon.code;
                shipInput.readOnly = true;
            }
            if (btnApplyShip) btnApplyShip.classList.add('d-none');
            if (btnRemoveShip) btnRemoveShip.classList.remove('d-none');
            if (shipMsg) {
                shipMsg.className = 'small text-success mt-1 d-block fw-semibold';
                shipMsg.innerHTML = `<i class="bi bi-check2-circle me-1"></i>${result.shippingCoupon.message} (${result.shippingCoupon.title})`;
            }
        } else {
            if (shipRow) shipRow.classList.add('d-none');
            if (shipStatusBadge) {
                shipStatusBadge.className = 'badge bg-light text-muted border';
                shipStatusBadge.innerText = 'Chưa áp dụng';
            }
            if (shipInput) {
                shipInput.readOnly = false;
            }
            if (btnApplyShip) btnApplyShip.classList.remove('d-none');
            if (btnRemoveShip) btnRemoveShip.classList.add('d-none');
            if (shipMsg) {
                if (result.shippingCoupon && !result.shippingCoupon.valid) {
                    shipMsg.className = 'small text-danger mt-1 d-block';
                    shipMsg.innerHTML = `<i class="bi bi-exclamation-circle me-1"></i>${result.shippingCoupon.message}`;
                } else {
                    shipMsg.className = 'small mt-1 d-none';
                    shipMsg.innerText = '';
                }
            }
        }

        // 5. Cập nhật Tổng tiền cuối cùng
        const finalTotalEl = document.getElementById('summaryFinalTotal');
        if (finalTotalEl) finalTotalEl.innerText = formatMoney(finalTotal);

        // 6. Đồng bộ vào các trường ẩn để gửi lên Order model
        const hiddenSubtotal = document.getElementById('hiddenSubtotalAmount');
        const hiddenShipping = document.getElementById('hiddenShippingFee');
        const hiddenOrderCode = document.getElementById('hiddenOrderDiscountCode');
        const hiddenOrderAmount = document.getElementById('hiddenOrderDiscountAmount');
        const hiddenShipCode = document.getElementById('hiddenShippingDiscountCode');
        const hiddenShipAmount = document.getElementById('hiddenShippingDiscountAmount');
        const hiddenTotal = document.getElementById('hiddenTotalAmount');

        if (hiddenSubtotal) hiddenSubtotal.value = subtotal;
        if (hiddenShipping) hiddenShipping.value = shippingFee;
        if (hiddenOrderCode) hiddenOrderCode.value = _activeOrderCoupon || '';
        if (hiddenOrderAmount) hiddenOrderAmount.value = orderDiscount;
        if (hiddenShipCode) hiddenShipCode.value = _activeShippingCoupon || '';
        if (hiddenShipAmount) hiddenShipAmount.value = shippingDiscount;
        if (hiddenTotal) hiddenTotal.value = finalTotal;
    }

    // Áp dụng mã theo loại ('order' hoặc 'shipping')
    window.applyCoupon = async function (type, specificCode) {
        let code = '';
        if (specificCode) {
            code = specificCode.trim().toUpperCase();
        } else {
            const inputEl = document.getElementById(type === 'order' ? 'orderCouponInput' : 'shippingCouponInput');
            code = inputEl ? inputEl.value.trim().toUpperCase() : '';
        }

        if (!code) {
            if (typeof Swal !== 'undefined') {
                Swal.fire({
                    icon: 'warning',
                    title: 'Chưa nhập mã',
                    text: 'Vui lòng nhập mã giảm giá trước khi nhấn áp dụng!',
                    confirmButtonColor: '#d70018'
                });
            } else {
                alert('Vui lòng nhập mã giảm giá!');
            }
            return;
        }

        const candidateOrderCode = type === 'order' ? code : _activeOrderCoupon;
        const candidateShipCode = type === 'shipping' ? code : _activeShippingCoupon;

        const result = await validateAndApply(candidateOrderCode, candidateShipCode);

        if (!result) {
            alert('Không thể kết nối máy chủ để kiểm tra mã giảm giá.');
            return;
        }

        if (type === 'order') {
            if (result.orderCoupon && result.orderCoupon.valid) {
                _activeOrderCoupon = result.orderCoupon.code;
                if (typeof showToast === 'function') {
                    showToast(`Áp dụng mã đơn hàng [${result.orderCoupon.code}] thành công!`, 'success');
                }
            } else {
                _activeOrderCoupon = null;
                const msg = result.orderCoupon ? result.orderCoupon.message : 'Mã không hợp lệ';
                if (typeof Swal !== 'undefined') {
                    Swal.fire({ icon: 'error', title: 'Không thể áp dụng', text: msg, confirmButtonColor: '#d70018' });
                }
            }
        } else if (type === 'shipping') {
            if (result.shippingCoupon && result.shippingCoupon.valid) {
                _activeShippingCoupon = result.shippingCoupon.code;
                if (typeof showToast === 'function') {
                    showToast(`Áp dụng mã freeship [${result.shippingCoupon.code}] thành công!`, 'success');
                }
            } else {
                _activeShippingCoupon = null;
                const msg = result.shippingCoupon ? result.shippingCoupon.message : 'Mã không hợp lệ';
                if (typeof Swal !== 'undefined') {
                    Swal.fire({ icon: 'error', title: 'Không thể áp dụng', text: msg, confirmButtonColor: '#d70018' });
                }
            }
        }

        updateSummaryUI(result);
    };

    // Bỏ áp dụng mã
    window.removeCoupon = async function (type) {
        if (type === 'order') {
            _activeOrderCoupon = null;
            const input = document.getElementById('orderCouponInput');
            if (input) input.value = '';
        } else if (type === 'shipping') {
            _activeShippingCoupon = null;
            const input = document.getElementById('shippingCouponInput');
            if (input) input.value = '';
        }

        const result = await validateAndApply(_activeOrderCoupon, _activeShippingCoupon);
        updateSummaryUI(result);
        if (typeof showToast === 'function') {
            showToast('Đã hủy áp dụng mã giảm giá.', 'info');
        }
    };

    // Mở Modal Kho Voucher để khách hàng chọn nhanh
    window.openCouponModal = async function () {
        const coupons = await fetchAvailableCoupons();
        const orderCoupons = coupons.filter(c => c.type === 0);
        const shippingCoupons = coupons.filter(c => c.type === 1);

        renderCouponList('orderCouponListContainer', orderCoupons, 'order');
        renderCouponList('shippingCouponListContainer', shippingCoupons, 'shipping');

        const modalEl = document.getElementById('couponModal');
        if (modalEl) {
            const modal = new bootstrap.Modal(modalEl);
            modal.show();
        }
    };

    function renderCouponList(containerId, list, type) {
        const container = document.getElementById(containerId);
        if (!container) return;

        if (!list || list.length === 0) {
            container.innerHTML = '<div class="text-center text-muted py-4"><i class="bi bi-inbox fs-2 d-block mb-2"></i>Hiện chưa có mã ưu đãi trong danh mục này.</div>';
            return;
        }

        let html = '';
        list.forEach(c => {
            const isActive = (type === 'order' && _activeOrderCoupon === c.code) ||
                             (type === 'shipping' && _activeShippingCoupon === c.code);
            const isEligible = c.isEligible;
            const discountBadge = c.isPercentage ? `Giảm ${c.discountValue}%` : `Giảm ${formatMoney(c.discountValue)}`;

            html += `
                <div class="col-12 col-md-6 mb-3">
                    <div class="cps-ticket-card ${isActive ? 'border-success shadow' : ''} ${!isEligible ? 'opacity-75' : ''}" style="border-radius: 12px;">
                        <div class="cps-ticket-top-ribbon" style="${type === 'shipping' ? 'background: linear-gradient(90deg, #10b981, #059669);' : ''}"></div>
                        <div class="p-3">
                            <div class="d-flex justify-content-between align-items-center mb-1">
                                <span class="badge ${type === 'shipping' ? 'bg-success' : 'bg-danger'} fw-bold">${discountBadge}</span>
                                <span class="small text-muted" style="font-size: 11px;"><i class="bi bi-clock me-1"></i>HSD: ${c.expiryDate}</span>
                            </div>
                            <h6 class="fw-bold mb-1 mt-2 text-dark" style="font-size: 13.5px;">${c.title}</h6>
                            <p class="small text-muted mb-2" style="font-size: 12px;">${c.description}</p>
                            
                            <div class="bg-light p-2 rounded d-flex justify-content-between align-items-center border">
                                <div>
                                    <small class="text-muted d-block" style="font-size: 10px;">MÃ VOUCHER:</small>
                                    <strong class="text-danger fw-bolder">${c.code}</strong>
                                </div>
                                <div>
                                    ${isActive ? `
                                        <button type="button" class="btn btn-sm btn-outline-success fw-bold py-1 px-3" onclick="removeCoupon('${type}'); bootstrap.Modal.getInstance(document.getElementById('couponModal')).hide();">
                                            <i class="bi bi-check2 me-1"></i>Đang dùng
                                        </button>
                                    ` : `
                                        <button type="button" class="btn btn-sm ${type === 'shipping' ? 'btn-success' : 'btn-danger'} fw-bold py-1 px-3" onclick="applyCoupon('${type}', '${c.code}'); bootstrap.Modal.getInstance(document.getElementById('couponModal')).hide();">
                                            Áp dụng
                                        </button>
                                    `}
                                </div>
                            </div>
                            ${!isEligible ? `
                                <div class="text-danger small mt-1" style="font-size: 11px;">
                                    <i class="bi bi-info-circle me-1"></i>Đơn hàng tối thiểu: ${formatMoney(c.minOrderAmount)}
                                </div>
                            ` : ''}
                        </div>
                    </div>
                </div>
            `;
        });

        container.innerHTML = `<div class="row g-2">${html}</div>`;
    }

    // Khởi tạo ban đầu
    window.initCheckoutCoupons = function (initialSubtotal) {
        _subtotal = initialSubtotal || 0;
        fetchAvailableCoupons();
        const shipFeeEl = document.getElementById('summaryShippingFee');
        if (shipFeeEl && _subtotal >= 300000) {
            shipFeeEl.className = 'fw-bold text-success';
            shipFeeEl.innerText = 'MIỄN PHÍ';
        }
    };
})();
