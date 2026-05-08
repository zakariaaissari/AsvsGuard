/* app.js — interactive features */
(function () {
    'use strict';

    /* ── Exigence Index: real-time search + filter ─────────────── */
    function initExigenceSearch() {
        const searchInput = document.getElementById('ex-search');
        const catSelect   = document.getElementById('ex-cat');
        const statSelect  = document.getElementById('ex-status');
        const levelPills  = document.querySelectorAll('#level-pills .pill-btn');
        const countEl     = document.getElementById('ex-count');
        const emptyEl     = document.getElementById('ex-empty');
        const items       = document.querySelectorAll('.exg-item');

        if (!searchInput || items.length === 0) return;

        let activeLevel = '';

        function filterCards() {
            const search  = searchInput.value.toLowerCase().trim();
            const cat     = catSelect?.value   ?? '';
            const status  = statSelect?.value  ?? '';
            let   visible = 0;

            items.forEach(function (item) {
                var matchSearch = !search || item.dataset.code.includes(search) || item.dataset.desc.includes(search);
                var matchLevel  = !activeLevel || item.dataset.level === activeLevel;
                var matchCat    = !cat    || item.dataset.cat    === cat;
                var matchStatus = !status || item.dataset.status === status;

                var show = matchSearch && matchLevel && matchCat && matchStatus;
                item.style.display = show ? '' : 'none';
                if (show) visible++;
            });

            if (countEl) countEl.textContent = visible;
            if (emptyEl) emptyEl.classList.toggle('d-none', visible > 0);
        }

        // Level pill clicks
        levelPills.forEach(function (btn) {
            btn.addEventListener('click', function () {
                levelPills.forEach(function (b) { b.classList.remove('active'); });
                btn.classList.add('active');
                activeLevel = btn.dataset.level ?? '';
                filterCards();
            });
        });

        searchInput.addEventListener('input', filterCards);
        if (catSelect)  catSelect.addEventListener('change', filterCards);
        if (statSelect) statSelect.addEventListener('change', filterCards);

        // Expose for external reset
        window.filterExigences = filterCards;
    }

    /* ── Exigence Detail: AI explain + generate code ───────────── */
    function initDetailPage() {
        var btnExplain = document.getElementById('btn-explain');
        var btnCode    = document.getElementById('btn-code');
        var langPills  = document.querySelectorAll('#lang-pills .pill-btn');
        var loading    = document.getElementById('ai-loading');
        var result     = document.getElementById('ai-result');
        var content    = document.getElementById('ai-content');
        var label      = document.getElementById('ai-result-label');
        var btnCopy    = document.getElementById('btn-copy');

        if (!btnExplain && !btnCode) return;

        var activeLang = 'csharp';

        // Language pill selection
        langPills.forEach(function (pill) {
            pill.addEventListener('click', function () {
                langPills.forEach(function (p) { p.classList.remove('active'); });
                pill.classList.add('active');
                activeLang = pill.dataset.lang;
            });
        });

        function setLoading(isLoading) {
            if (loading) loading.classList.toggle('d-none', !isLoading);
            if (result)  result.classList.toggle('d-none', isLoading);
        }

        function showResult(text, title) {
            if (label)   label.textContent = title;
            if (content) content.textContent = text;
            setLoading(false);
            if (result) result.classList.remove('d-none');
        }

        async function callAI(url, body) {
            setLoading(true);
            if (result) result.classList.add('d-none');

            try {
                var resp = await fetch(url, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(body)
                });

                if (resp.status === 401 || resp.redirected) {
                    showResult('You must be signed in to use AI features.', 'Error');
                    return;
                }
                if (!resp.ok) {
                    showResult('Request failed (HTTP ' + resp.status + '). Check your HuggingFace API key.', 'Error');
                    return;
                }

                var data = await resp.json();
                return data;
            } catch (err) {
                showResult('Connection error: ' + err.message, 'Error');
            }
        }

        if (btnExplain) {
            btnExplain.addEventListener('click', async function () {
                var id = btnExplain.dataset.id;
                btnExplain.disabled = true;

                var data = await callAI('/AI/Explain', { exigenceId: parseInt(id) });
                if (data) showResult(data.explanation ?? data.error ?? 'No response.', 'Explanation');

                btnExplain.disabled = false;
            });
        }

        if (btnCode) {
            btnCode.addEventListener('click', async function () {
                var id = btnCode.dataset.id;
                btnCode.disabled = true;

                var data = await callAI('/AI/GenerateCode', { exigenceId: parseInt(id), language: activeLang });
                if (data) showResult(data.code ?? data.error ?? 'No response.', 'Code example — ' + activeLang);

                btnCode.disabled = false;
            });
        }

        // Copy button
        if (btnCopy) {
            btnCopy.addEventListener('click', function () {
                var text = content?.textContent ?? '';
                if (!text) return;
                navigator.clipboard.writeText(text).then(function () {
                    btnCopy.textContent = 'Copied!';
                    setTimeout(function () {
                        btnCopy.innerHTML = '<svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="9" y="9" width="13" height="13" rx="2" ry="2"/><path d="M5 15H4a2 2 0 01-2-2V4a2 2 0 012-2h9a2 2 0 012 2v1"/></svg> Copy';
                    }, 1800);
                });
            });
        }
    }

    /* ── Repo/Index: scan form progress animation ──────────────── */
    function initScanForm() {
        var form     = document.getElementById('scan-form');
        var scanBtn  = document.getElementById('scan-btn');
        var progress = document.getElementById('scan-progress');

        if (!form || !scanBtn) return;

        form.addEventListener('submit', function () {
            scanBtn.disabled = true;
            scanBtn.innerHTML =
                '<span style="width:14px;height:14px;border:2px solid rgba(255,255,255,.4);border-top-color:#fff;border-radius:50%;display:inline-block;animation:spin .7s linear infinite"></span> Scanning…';

            if (progress) {
                progress.classList.remove('d-none');

                // Animate steps with rough timing
                setTimeout(function () {
                    var step1 = document.getElementById('step-fetch');
                    var step2 = document.getElementById('step-ai');
                    if (step1) {
                        step1.classList.remove('active');
                        step1.classList.add('done');
                        step1.querySelector('.step-icon').className = 'step-icon done-icon';
                        step1.querySelector('.step-icon').innerHTML =
                            '<svg width="10" height="10" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><path d="M20 6L9 17l-5-5"/></svg>';
                    }
                    if (step2) {
                        step2.classList.add('active');
                        step2.querySelector('.step-icon').className = 'step-icon active-icon';
                    }
                }, 8000);

                setTimeout(function () {
                    var step2 = document.getElementById('step-ai');
                    var step3 = document.getElementById('step-save');
                    if (step2) {
                        step2.classList.remove('active');
                        step2.classList.add('done');
                        step2.querySelector('.step-icon').className = 'step-icon done-icon';
                        step2.querySelector('.step-icon').innerHTML =
                            '<svg width="10" height="10" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><path d="M20 6L9 17l-5-5"/></svg>';
                    }
                    if (step3) {
                        step3.classList.add('active');
                        step3.querySelector('.step-icon').className = 'step-icon active-icon';
                    }
                }, 90000);
            }
        });
    }

    /* ── Sidebar hamburger + overlay (mobile) ──────────────────── */
    function initSidebar() {
        var ham     = document.getElementById('hamburger');
        var sidebar = document.getElementById('sidebar');
        var overlay = document.getElementById('sidebar-overlay');
        if (!ham || !sidebar) return;

        function open()  {
            sidebar.classList.add('open');
            overlay?.classList.add('visible');
            document.body.style.overflow = 'hidden';
        }
        function close() {
            sidebar.classList.remove('open');
            overlay?.classList.remove('visible');
            document.body.style.overflow = '';
        }

        ham.addEventListener('click', function () {
            sidebar.classList.contains('open') ? close() : open();
        });

        // Tap overlay to close
        overlay?.addEventListener('click', close);

        // Close on nav link click (navigates away anyway, but avoids flicker)
        sidebar.querySelectorAll('a.nav-item').forEach(function (link) {
            link.addEventListener('click', function () {
                if (window.innerWidth <= 900) close();
            });
        });
    }

    /* ── Bootstrap tab: pill active state sync ─────────────────── */
    function initTabPills() {
        document.querySelectorAll('[data-bs-toggle="tab"]').forEach(function (btn) {
            btn.addEventListener('shown.bs.tab', function () {
                var parent = btn.closest('ul') || btn.parentElement?.parentElement;
                if (!parent) return;
                parent.querySelectorAll('[data-bs-toggle="tab"]').forEach(function (b) {
                    b.classList.remove('active');
                });
                btn.classList.add('active');
            });
        });
    }

    /* ── Boot ──────────────────────────────────────────────────── */
    document.addEventListener('DOMContentLoaded', function () {
        initExigenceSearch();
        initDetailPage();
        initScanForm();
        initSidebar();
        initTabPills();
    });
})();
