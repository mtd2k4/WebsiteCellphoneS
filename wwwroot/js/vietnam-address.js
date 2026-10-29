/**
 * Bộ chọn địa chỉ hành chính Việt Nam (Vietnam Address Selector)
 * Tự động tải dữ liệu Tỉnh / Thành phố, Quận / Huyện, Phường / Xã từ API hoặc File dữ liệu nội bộ.
 * Chỉ cho phép người dùng nhập tay phần Địa chỉ chi tiết (số nhà, tên đường, tòa nhà...)
 */

(function () {
    let _cachedVietnamData = null;
    let _loadingPromise = null;

    // Tải dữ liệu địa giới hành chính Việt Nam với cơ chế đa tầng (Local file -> API -> CDN fallback)
    async function loadVietnamAddressData() {
        if (_cachedVietnamData) return _cachedVietnamData;
        if (_loadingPromise) return _loadingPromise;

        _loadingPromise = (async () => {
            // Thử sessionStorage trước để tránh tải lại nhiều lần
            try {
                const sessionCached = sessionStorage.getItem('cps_vn_addresses_cache');
                if (sessionCached) {
                    _cachedVietnamData = JSON.parse(sessionCached);
                    return _cachedVietnamData;
                }
            } catch (e) {}

            const sources = [
                '/data/vietnam-addresses.json',
                'https://raw.githubusercontent.com/kenzouno1/DiaGioiHanhChinhVN/master/data.json'
            ];

            for (const url of sources) {
                try {
                    const res = await fetch(url);
                    if (res.ok) {
                        const data = await res.json();
                        if (Array.isArray(data) && data.length >= 60) {
                            _cachedVietnamData = data;
                            try {
                                sessionStorage.setItem('cps_vn_addresses_cache', JSON.stringify(data));
                            } catch (e) {}
                            return _cachedVietnamData;
                        }
                    }
                } catch (err) {
                    console.warn('Lỗi khi tải dữ liệu từ: ' + url, err);
                }
            }

            console.error('Không thể tải dữ liệu địa chỉ hành chính Việt Nam từ tất cả các nguồn.');
            return [];
        })();

        return _loadingPromise;
    }

    function cleanString(str) {
        if (!str) return '';
        return str.toLowerCase()
            .replace(/thành phố|tỉnh|quận|huyện|thị xã|thị trấn|phường|xã|tp\.|tp /gi, '')
            .trim();
    }

    /**
     * Khởi tạo bộ 3 dropdown Tỉnh/Huyện/Xã và 1 ô nhập tay Địa chỉ chi tiết
     */
    window.initVietnamAddressSelector = async function (config) {
        const provEl = typeof config.provinceSelect === 'string' ? document.querySelector(config.provinceSelect) : config.provinceSelect;
        const distEl = typeof config.districtSelect === 'string' ? document.querySelector(config.districtSelect) : config.districtSelect;
        const wardEl = typeof config.wardSelect === 'string' ? document.querySelector(config.wardSelect) : config.wardSelect;
        const detailEl = typeof config.detailInput === 'string' ? document.querySelector(config.detailInput) : config.detailInput;
        const hiddenProvEl = typeof config.hiddenProvince === 'string' ? document.querySelector(config.hiddenProvince) : config.hiddenProvince;
        const hiddenAddrEl = typeof config.hiddenAddress === 'string' ? document.querySelector(config.hiddenAddress) : config.hiddenAddress;
        const formEl = typeof config.form === 'string' ? document.querySelector(config.form) : config.form;

        if (!provEl || !distEl || !wardEl || !detailEl) {
            console.error('Không tìm thấy đủ các phần tử select/input địa chỉ.', config);
            return;
        }

        // Đặt trạng thái ban đầu
        provEl.innerHTML = '<option value="">-- Đang tải danh sách Tỉnh / Thành... --</option>';
        distEl.innerHTML = '<option value="">-- Chọn Quận / Huyện --</option>';
        wardEl.innerHTML = '<option value="">-- Chọn Phường / Xã --</option>';
        distEl.disabled = true;
        wardEl.disabled = true;

        const data = await loadVietnamAddressData();

        // Điền danh sách 63 Tỉnh / Thành phố
        provEl.innerHTML = '<option value="">-- Chọn Tỉnh / Thành phố --</option>';
        data.forEach(p => {
            const opt = document.createElement('option');
            opt.value = p.Id;
            opt.text = p.Name;
            opt.setAttribute('data-name', p.Name);
            provEl.appendChild(opt);
        });

        // Hàm cập nhật trường ẩn
        function syncHiddenFields() {
            const provName = provEl.selectedIndex > 0 ? (provEl.options[provEl.selectedIndex].getAttribute('data-name') || provEl.options[provEl.selectedIndex].text) : '';
            const distName = distEl.selectedIndex > 0 ? (distEl.options[distEl.selectedIndex].getAttribute('data-name') || distEl.options[distEl.selectedIndex].text) : '';
            const wardName = wardEl.selectedIndex > 0 ? (wardEl.options[wardEl.selectedIndex].getAttribute('data-name') || wardEl.options[wardEl.selectedIndex].text) : '';
            const detailText = detailEl.value.trim();

            if (hiddenProvEl) {
                hiddenProvEl.value = provName;
            }

            if (hiddenAddrEl) {
                // Ghép địa chỉ: [Địa chỉ chi tiết], [Phường/Xã], [Quận/Huyện]
                const parts = [];
                if (detailText) parts.push(detailText);
                if (wardName) parts.push(wardName);
                if (distName) parts.push(distName);
                hiddenAddrEl.value = parts.join(', ');
            }
        }

        // Sự kiện khi chọn Tỉnh / Thành
        provEl.addEventListener('change', function () {
            const provId = this.value;
            distEl.innerHTML = '<option value="">-- Chọn Quận / Huyện --</option>';
            wardEl.innerHTML = '<option value="">-- Chọn Phường / Xã --</option>';
            wardEl.disabled = true;

            if (!provId) {
                distEl.disabled = true;
                syncHiddenFields();
                return;
            }

            const currentProv = data.find(p => p.Id === provId);
            if (currentProv && currentProv.Districts) {
                distEl.disabled = false;
                currentProv.Districts.forEach(d => {
                    const opt = document.createElement('option');
                    opt.value = d.Id;
                    opt.text = d.Name;
                    opt.setAttribute('data-name', d.Name);
                    distEl.appendChild(opt);
                });
            } else {
                distEl.disabled = true;
            }

            syncHiddenFields();
        });

        // Sự kiện khi chọn Quận / Huyện
        distEl.addEventListener('change', function () {
            const provId = provEl.value;
            const distId = this.value;
            wardEl.innerHTML = '<option value="">-- Chọn Phường / Xã --</option>';

            if (!distId) {
                wardEl.disabled = true;
                syncHiddenFields();
                return;
            }

            const currentProv = data.find(p => p.Id === provId);
            if (currentProv && currentProv.Districts) {
                const currentDist = currentProv.Districts.find(d => d.Id === distId);
                if (currentDist && currentDist.Wards) {
                    wardEl.disabled = false;
                    currentDist.Wards.forEach(w => {
                        const opt = document.createElement('option');
                        opt.value = w.Id;
                        opt.text = w.Name;
                        opt.setAttribute('data-name', w.Name);
                        wardEl.appendChild(opt);
                    });
                } else {
                    wardEl.disabled = true;
                }
            }

            syncHiddenFields();
        });

        // Sự kiện khi chọn Phường / Xã
        wardEl.addEventListener('change', function () {
            syncHiddenFields();
        });

        // Sự kiện khi nhập địa chỉ chi tiết
        detailEl.addEventListener('input', function () {
            syncHiddenFields();
        });

        // Khởi tạo giá trị ban đầu nếu có
        const initialProv = config.initialProvince || '';
        const initialAddr = config.initialAddress || '';

        if (initialProv) {
            const cleanInitProv = cleanString(initialProv);
            for (let i = 0; i < provEl.options.length; i++) {
                const optText = provEl.options[i].text;
                if (cleanString(optText) === cleanInitProv || optText.includes(initialProv) || initialProv.includes(optText)) {
                    provEl.selectedIndex = i;
                    provEl.dispatchEvent(new Event('change'));
                    break;
                }
            }
        }

        // Nếu có initialAddress, kiểm tra xem có thể tách quận/huyện/xã hay điền thẳng vào detail
        if (initialAddr) {
            // Trường hợp địa chỉ có dạng "123 Lê Lợi, Phường Bến Nghé, Quận 1"
            const parts = initialAddr.split(',').map(s => s.trim()).filter(Boolean);
            if (parts.length >= 3) {
                const possibleDist = parts[parts.length - 1];
                const possibleWard = parts[parts.length - 2];
                const possibleDetail = parts.slice(0, parts.length - 2).join(', ');

                // Thử tìm Quận
                const cleanDist = cleanString(possibleDist);
                for (let i = 0; i < distEl.options.length; i++) {
                    const optText = distEl.options[i].text;
                    if (cleanString(optText) === cleanDist || optText.includes(possibleDist)) {
                        distEl.selectedIndex = i;
                        distEl.dispatchEvent(new Event('change'));

                        // Thử tìm Phường/Xã
                        const cleanWard = cleanString(possibleWard);
                        for (let j = 0; j < wardEl.options.length; j++) {
                            const wText = wardEl.options[j].text;
                            if (cleanString(wText) === cleanWard || wText.includes(possibleWard)) {
                                wardEl.selectedIndex = j;
                                wardEl.dispatchEvent(new Event('change'));
                                break;
                            }
                        }
                        break;
                    }
                }

                detailEl.value = possibleDetail || parts[0];
            } else {
                detailEl.value = initialAddr;
            }
            syncHiddenFields();
        }

        // Bắt sự kiện submit form để xác thực
        if (formEl) {
            formEl.addEventListener('submit', function (e) {
                syncHiddenFields();

                if (!provEl.value) {
                    e.preventDefault();
                    provEl.focus();
                    if (typeof Swal !== 'undefined') {
                        Swal.fire({
                            icon: 'warning',
                            title: 'Thiếu thông tin địa chỉ',
                            text: 'Vui lòng chọn Tỉnh / Thành phố nhận hàng!',
                            confirmButtonColor: '#d70018'
                        });
                    } else {
                        alert('Vui lòng chọn Tỉnh / Thành phố nhận hàng!');
                    }
                    return false;
                }

                if (!distEl.value) {
                    e.preventDefault();
                    distEl.focus();
                    if (typeof Swal !== 'undefined') {
                        Swal.fire({
                            icon: 'warning',
                            title: 'Thiếu thông tin địa chỉ',
                            text: 'Vui lòng chọn Quận / Huyện nhận hàng!',
                            confirmButtonColor: '#d70018'
                        });
                    } else {
                        alert('Vui lòng chọn Quận / Huyện nhận hàng!');
                    }
                    return false;
                }

                if (!wardEl.value) {
                    e.preventDefault();
                    wardEl.focus();
                    if (typeof Swal !== 'undefined') {
                        Swal.fire({
                            icon: 'warning',
                            title: 'Thiếu thông tin địa chỉ',
                            text: 'Vui lòng chọn Phường / Xã nhận hàng!',
                            confirmButtonColor: '#d70018'
                        });
                    } else {
                        alert('Vui lòng chọn Phường / Xã nhận hàng!');
                    }
                    return false;
                }

                if (!detailEl.value.trim()) {
                    e.preventDefault();
                    detailEl.focus();
                    if (typeof Swal !== 'undefined') {
                        Swal.fire({
                            icon: 'warning',
                            title: 'Thiếu địa chỉ chi tiết',
                            text: 'Vui lòng nhập số nhà, tên đường cụ thể!',
                            confirmButtonColor: '#d70018'
                        });
                    } else {
                        alert('Vui lòng nhập số nhà, tên đường cụ thể!');
                    }
                    return false;
                }

                return true;
            });
        }
    };
})();
