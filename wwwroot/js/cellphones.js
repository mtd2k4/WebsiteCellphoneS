// ============================================================
// CELLPHONES INTERACTIVE CLIENT SCRIPTS (HOMEPAGE SUITE)
// ============================================================

document.addEventListener("DOMContentLoaded", function () {
    initFlashSaleCountdown();
    initLiveSearch();
    initSwiperSliders();
    initBrandTabs();
    initFlashSaleTabs();
    initPriceFilterPills();
    initTradeInCalculator();
    initBackToTop();
    initWishlistState();
});

// 1. FLASH SALE COUNTDOWN TIMER
function initFlashSaleCountdown() {
    const hoursEl = document.getElementById("cps-flash-hours");
    const minsEl = document.getElementById("cps-flash-mins");
    const secsEl = document.getElementById("cps-flash-secs");

    if (!hoursEl || !minsEl || !secsEl) return;

    function updateCountdown() {
        const now = new Date();
        const endOfDay = new Date();
        endOfDay.setHours(23, 59, 59, 999);

        let diff = Math.floor((endOfDay - now) / 1000);
        if (diff <= 0) diff = 86400;

        const hours = Math.floor(diff / 3600);
        const mins = Math.floor((diff % 3600) / 60);
        const secs = diff % 60;

        hoursEl.innerText = String(hours).padStart(2, "0");
        minsEl.innerText = String(mins).padStart(2, "0");
        secsEl.innerText = String(secs).padStart(2, "0");
    }

    updateCountdown();
    setInterval(updateCountdown, 1000);
}

// 2. LIVE SEARCH AUTOCOMPLETE & TRENDING KEYWORDS
function initLiveSearch() {
    const searchInput = document.getElementById("cps-search-input");
    const searchResults = document.getElementById("cps-search-results");

    if (!searchInput || !searchResults) return;

    let debounceTimer;

    // Show trending tags when focused with empty query
    searchInput.addEventListener("focus", function () {
        if (!this.value.trim()) {
            showTrendingKeywords();
        }
    });

    searchInput.addEventListener("input", function () {
        const query = this.value.trim();

        clearTimeout(debounceTimer);
        if (query.length < 2) {
            showTrendingKeywords();
            return;
        }

        debounceTimer = setTimeout(() => {
            fetch(`/Home/Search?q=${encodeURIComponent(query)}`)
                .then(res => res.json())
                .then(data => {
                    if (!data || data.length === 0) {
                        searchResults.innerHTML = `
                            <div class="p-3 text-center text-muted" style="font-size: 13px;">
                                <i class="bi bi-search me-1"></i> Không tìm thấy sản phẩm phù hợp với "<strong>${query}</strong>"
                            </div>`;
                        searchResults.classList.add("show");
                        return;
                    }

                    let html = '<div class="p-2 border-bottom bg-light text-muted" style="font-size: 11px; font-weight: 700;">GỢI Ý TÌM KIẾM HÀNG ĐẦU</div>';
                    data.forEach(item => {
                        html += `
                            <a href="/Product/Details/${item.id}" class="cps-search-item">
                                <img src="${item.imageUrl}" alt="${item.name}">
                                <div class="info">
                                    <h6>${item.name}</h6>
                                    <div class="price">${Number(item.salePrice).toLocaleString('vi-VN')}₫</div>
                                </div>
                            </a>
                        `;
                    });

                    searchResults.innerHTML = html;
                    searchResults.classList.add("show");
                })
                .catch(err => {
                    console.error("Lỗi tìm kiếm:", err);
                });
        }, 250);
    });

    function showTrendingKeywords() {
        const trends = ["iPhone 16 Pro Max", "Samsung S24 Ultra", "AirPods Pro 2", "MacBook Air M2", "Củ sạc 30W", "Xiaomi 14"];
        let html = `
            <div class="p-2 border-bottom bg-light text-muted" style="font-size: 11px; font-weight: 700;">
                <i class="bi bi-fire text-danger me-1"></i> XU HƯỚNG TÌM KIẾM HOT
            </div>
            <div class="cps-trending-pills">
        `;
        trends.forEach(t => {
            html += `<span class="cps-trend-pill" onclick="searchTrend('${t}')">${t}</span>`;
        });
        html += `</div>`;
        searchResults.innerHTML = html;
        searchResults.classList.add("show");
    }

    // Ẩn khi click ra ngoài
    document.addEventListener("click", function (e) {
        if (!searchInput.contains(e.target) && !searchResults.contains(e.target)) {
            searchResults.classList.remove("show");
        }
    });
}

function searchTrend(keyword) {
    const input = document.getElementById("cps-search-input");
    if (input) {
        input.value = keyword;
        input.dispatchEvent(new Event("input"));
    }
}

// 3. SWIPER SLIDERS
let flashSaleSwiperInstance = null;

function initSwiperSliders() {
    if (typeof Swiper === "undefined") return;

    // Main Hero Slider
    const mainSwiper = new Swiper(".cps-hero-swiper", {
        loop: true,
        autoplay: {
            delay: 4500,
            disableOnInteraction: false,
        },
        navigation: {
            nextEl: ".swiper-button-next",
            prevEl: ".swiper-button-prev",
        },
        on: {
            slideChange: function () {
                const activeIndex = this.realIndex;
                const tabs = document.querySelectorAll(".cps-tab-item");
                tabs.forEach((tab, index) => {
                    if (index === activeIndex) {
                        tab.classList.add("active");
                    } else {
                        tab.classList.remove("active");
                    }
                });
            }
        }
    });

    // Click tab để nhảy slide
    const tabs = document.querySelectorAll(".cps-tab-item");
    tabs.forEach(tab => {
        tab.addEventListener("click", function () {
            const index = parseInt(this.getAttribute("data-slide-index"));
            if (!isNaN(index)) {
                mainSwiper.slideToLoop(index);
            }
        });
    });

    // Flash Sale Swiper
    flashSaleSwiperInstance = new Swiper(".cps-flashsale-swiper", {
        slidesPerView: 2,
        spaceBetween: 10,
        navigation: {
            nextEl: ".flash-next",
            prevEl: ".flash-prev",
        },
        breakpoints: {
            576: { slidesPerView: 3, spaceBetween: 10 },
            768: { slidesPerView: 4, spaceBetween: 12 },
            1024: { slidesPerView: 5, spaceBetween: 12 }
        }
    });

    // Big Promo Banner Swiper
    if (document.querySelector(".cps-big-promo-swiper")) {
        new Swiper(".cps-big-promo-swiper", {
            loop: true,
            autoHeight: false,
            autoplay: {
                delay: 4500,
                disableOnInteraction: false,
            },
            navigation: {
                nextEl: ".big-promo-next",
                prevEl: ".big-promo-prev",
            },
            pagination: {
                el: ".big-promo-pagination",
                clickable: true,
            }
        });
    }
}

// 4. FLASH SALE CATEGORY TABS
function initFlashSaleTabs() {
    const flashTabs = document.querySelectorAll(".cps-flash-tab");
    flashTabs.forEach(tab => {
        tab.addEventListener("click", function () {
            flashTabs.forEach(t => t.classList.remove("active"));
            this.classList.add("active");

            const selectedCat = this.getAttribute("data-flash-cat");
            const slides = document.querySelectorAll(".cps-flash-slide");

            slides.forEach(slide => {
                const cat = slide.getAttribute("data-cat");
                if (selectedCat === "all" || cat === selectedCat) {
                    slide.style.display = "";
                } else {
                    slide.style.display = "none";
                }
            });

            if (flashSaleSwiperInstance) {
                flashSaleSwiperInstance.update();
            }
        });
    });
}

// 5. BRAND FILTER TABS & PRICE PILLS (ĐIỆN THOẠI NỔI BẬT)
let currentSelectedBrand = "all";
let currentSelectedPrice = "all";

function initBrandTabs() {
    const brandTabs = document.querySelectorAll(".cps-brand-tab");
    brandTabs.forEach(tab => {
        tab.addEventListener("click", function () {
            const parent = this.closest(".cps-brand-tabs");
            if (parent) {
                parent.querySelectorAll(".cps-brand-tab").forEach(t => t.classList.remove("active"));
            }
            this.classList.add("active");

            currentSelectedBrand = this.getAttribute("data-brand") || "all";
            filterPhoneProducts();
        });
    });
}

function initPriceFilterPills() {
    const pricePills = document.querySelectorAll(".cps-price-pill");
    pricePills.forEach(pill => {
        pill.addEventListener("click", function () {
            const parent = this.closest(".cps-price-pills");
            if (parent) {
                parent.querySelectorAll(".cps-price-pill").forEach(p => p.classList.remove("active"));
            }
            this.classList.add("active");

            currentSelectedPrice = this.getAttribute("data-price-range") || "all";
            filterPhoneProducts();
        });
    });
}

function filterPhoneProducts() {
    const targetGrid = document.getElementById("grid-phones");
    if (!targetGrid) return;

    const cards = targetGrid.querySelectorAll(".cps-product-item");
    cards.forEach(card => {
        const cardBrand = card.getAttribute("data-brand");
        const cardPrice = parseFloat(card.getAttribute("data-price")) || 0;

        let matchBrand = (currentSelectedBrand === "all" || cardBrand === currentSelectedBrand);
        let matchPrice = true;

        if (currentSelectedPrice === "under-5m") {
            matchPrice = cardPrice < 5000000;
        } else if (currentSelectedPrice === "5m-10m") {
            matchPrice = cardPrice >= 5000000 && cardPrice <= 10000000;
        } else if (currentSelectedPrice === "10m-20m") {
            matchPrice = cardPrice > 10000000 && cardPrice <= 20000000;
        } else if (currentSelectedPrice === "over-20m") {
            matchPrice = cardPrice > 20000000;
        }

        if (matchBrand && matchPrice) {
            card.style.display = "block";
        } else {
            card.style.display = "none";
        }
    });
}

// 6. WISHLIST TOGGLE (LOCALSTORAGE)
function initWishlistState() {
    const wishlist = JSON.parse(localStorage.getItem("cps_wishlist") || "[]");
    document.querySelectorAll(".cps-wishlist-btn").forEach(btn => {
        const id = parseInt(btn.getAttribute("data-product-id"));
        if (wishlist.includes(id)) {
            btn.classList.add("active");
            btn.innerHTML = `<i class="bi bi-heart-fill text-danger"></i>`;
        }
    });
}

function toggleWishlist(productId, btn) {
    let wishlist = JSON.parse(localStorage.getItem("cps_wishlist") || "[]");
    const index = wishlist.indexOf(productId);

    if (index > -1) {
        wishlist.splice(index, 1);
        btn.classList.remove("active");
        btn.innerHTML = `<i class="bi bi-heart"></i>`;
        showToast("Đã xóa khỏi danh sách yêu thích!", "secondary");
    } else {
        wishlist.push(productId);
        btn.classList.add("active");
        btn.innerHTML = `<i class="bi bi-heart-fill text-danger"></i>`;
        showToast("Đã thêm vào danh sách yêu thích!", "success");
    }

    localStorage.setItem("cps_wishlist", JSON.stringify(wishlist));
}

// 7. TRADE-IN VALUATION ESTIMATOR
function initTradeInCalculator() {
    const oldPhoneSelect = document.getElementById("tradein-old-phone");
    const conditionSelect = document.getElementById("tradein-condition");

    if (!oldPhoneSelect || !conditionSelect) return;

    function calculateTradeIn() {
        const baseVal = parseFloat(oldPhoneSelect.value) || 0;
        const conditionRate = parseFloat(conditionSelect.value) || 1.0;

        const estValuation = Math.round(baseVal * conditionRate);
        const subsidy = 2000000; // Trợ giá 2 triệu
        const totalTradeInBenefit = estValuation + subsidy;

        const newTargetPrice = 30990000; // iPhone 16 Pro Max 256GB
        const payDifference = Math.max(0, newTargetPrice - totalTradeInBenefit);

        const estValEl = document.getElementById("tradein-est-val");
        const totalBenefitEl = document.getElementById("tradein-total-benefit");
        const payDiffEl = document.getElementById("tradein-pay-diff");

        if (estValEl) estValEl.innerText = estValuation.toLocaleString("vi-VN") + "₫";
        if (totalBenefitEl) totalBenefitEl.innerText = totalTradeInBenefit.toLocaleString("vi-VN") + "₫";
        if (payDiffEl) payDiffEl.innerText = payDifference.toLocaleString("vi-VN") + "₫";
    }

    oldPhoneSelect.addEventListener("change", calculateTradeIn);
    conditionSelect.addEventListener("change", calculateTradeIn);
    calculateTradeIn();
}

// 8. BACK TO TOP BUTTON
function initBackToTop() {
    const backBtn = document.getElementById("cps-btn-backtotop");
    if (!backBtn) return;

    window.addEventListener("scroll", function () {
        if (window.scrollY > 350) {
            backBtn.style.display = "flex";
        } else {
            backBtn.style.display = "none";
        }
    });

    backBtn.addEventListener("click", function (e) {
        e.preventDefault();
        window.scrollTo({ top: 0, behavior: "smooth" });
    });
}

// 9. SEO ACCORDION TOGGLE
function toggleSeoContent() {
    const content = document.getElementById("cps-seo-content");
    const btn = document.getElementById("cps-seo-btn");
    if (!content || !btn) return;

    if (content.classList.contains("expanded")) {
        content.classList.remove("expanded");
        btn.innerHTML = `Xem thêm nội dung <i class="bi bi-chevron-down ms-1"></i>`;
    } else {
        content.classList.add("expanded");
        btn.innerHTML = `Thu gọn nội dung <i class="bi bi-chevron-up ms-1"></i>`;
    }
}

// 10. AJAX ADD TO CART & TOAST NOTIFICATION
function addToCart(productId, quantity = 1, variantId = null) {
    const btn = event?.currentTarget;
    let oldHtml = "";
    if (btn) {
        oldHtml = btn.innerHTML;
        btn.innerHTML = `<span class="spinner-border spinner-border-sm" role="status"></span>`;
    }

    fetch(`/Cart/AddToCart?productId=${productId}&quantity=${quantity}&variantId=${variantId || ''}`, {
        method: 'POST'
    })
    .then(res => res.json())
    .then(data => {
        if (data.success) {
            const badge = document.getElementById("cps-cart-badge");
            if (badge) {
                badge.innerText = data.totalCount;
                badge.style.transform = "scale(1.4)";
                setTimeout(() => badge.style.transform = "scale(1)", 200);
            }
            showToast("Đã thêm sản phẩm vào giỏ hàng thành công!", "success");
        } else {
            showToast(data.message || "Không thể thêm vào giỏ", "danger");
        }
    })
    .catch(err => {
        console.error("Cart error:", err);
        showToast("Có lỗi xảy ra khi thêm vào giỏ!", "danger");
    })
    .finally(() => {
        if (btn && oldHtml) {
            btn.innerHTML = oldHtml;
        }
    });
}

// Toast notification
function showToast(message, type = "success") {
    let toastContainer = document.getElementById("cps-toast-container");
    if (!toastContainer) {
        toastContainer = document.createElement("div");
        toastContainer.id = "cps-toast-container";
        toastContainer.style.cssText = "position: fixed; top: 75px; right: 20px; z-index: 9999; display: flex; flex-direction: column; gap: 8px;";
        document.body.appendChild(toastContainer);
    }

    const toast = document.createElement("div");
    toast.className = `alert alert-${type} shadow-sm d-flex align-items-center gap-2 py-2 px-3 m-0`;
    toast.style.cssText = "font-size: 13px; border-radius: 8px; animation: slideIn 0.3s ease-out;";
    toast.innerHTML = `<i class="bi bi-${type === 'success' ? 'check-circle-fill text-success' : 'exclamation-circle-fill text-danger'}"></i> <span>${message}</span>`;

    toastContainer.appendChild(toast);
    setTimeout(() => {
        toast.style.opacity = "0";
        toast.style.transition = "opacity 0.3s ease";
        setTimeout(() => toast.remove(), 300);
    }, 2500);
}
