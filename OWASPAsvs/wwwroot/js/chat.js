window.chatContext  = null;
window.chatAutoSend = false;

document.addEventListener('DOMContentLoaded', function () {
    const sendBtn   = document.getElementById('chat-send');
    const input     = document.getElementById('chat-input');
    const messages  = document.getElementById('chat-messages');
    const panel     = document.getElementById('chatPanel');
    const chips     = document.getElementById('chat-chips');
    const scrollBtn = document.getElementById('scroll-to-bottom');
    const overlay   = document.getElementById('chat-overlay');

    if (!sendBtn || !input || !messages) return;

    function escHtml(t) {
        return String(t)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;');
    }

    function scrollBottom() {
        messages.scrollTop = messages.scrollHeight;
    }

    function appendUser(text) {
        const row = document.createElement('div');
        row.className = 'chat-msg-user';
        row.innerHTML = `<div class="bubble-user">${escHtml(text)}</div>`;
        messages.appendChild(row);
        scrollBottom();
    }

    function appendAI() {
        chips?.classList.add('d-none');
        const row = document.createElement('div');
        row.className = 'chat-msg-ai';
        row.innerHTML =
            `<div class="ai-avatar">
                <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <circle cx="12" cy="8" r="4"/>
                    <path d="M6 20v-2a6 6 0 0112 0v2"/>
                </svg>
            </div>
            <div class="bubble-ai" id="bubble-${Date.now()}">
                <span class="typing-dots">
                    <span></span><span></span><span></span>
                </span>
            </div>`;
        messages.appendChild(row);
        scrollBottom();
        return row.querySelector('.bubble-ai');
    }

    async function sendMessage(text) {
        if (!text) return;
        appendUser(text);
        const bubble = appendAI();

        try {
            const resp = await fetch('/Chat/Send', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ message: text, context: window.chatContext ?? null })
            });

            if (!resp.ok) {
                bubble.textContent = 'Error ' + resp.status + ': ' + resp.statusText;
                return;
            }

            const reader  = resp.body.getReader();
            const decoder = new TextDecoder();
            let accumulated = '';
            bubble.innerHTML = '';

            while (true) {
                const { done, value } = await reader.read();
                if (done) break;
                const lines = decoder.decode(value, { stream: true }).split('\n');
                for (const line of lines) {
                    if (!line.startsWith('data: ')) continue;
                    const token = line.slice(6);
                    if (token === '[DONE]') break;
                    accumulated += token.replace(/\\n/g, '\n');
                    bubble.textContent = accumulated;
                    scrollBottom();
                }
            }

            if (!accumulated.trim()) bubble.textContent = 'No response received.';
        } catch (err) {
            bubble.textContent = 'Connection error: ' + err.message;
        }
    }

    async function send() {
        const text = input.value.trim();
        if (!text) return;
        input.value = '';
        await sendMessage(text);
    }

    const fab = document.querySelector('.chat-fab');

    window.openChatPanel = function () {
        if (!panel) return;

        if (!window.chatAutoSend) {
            document.getElementById('chat-context-banner')?.classList.add('d-none');
            window.chatContext = null;
            input.placeholder = 'Ask about ASVS security…';
        }

        panel.classList.remove('panel-hidden');
        panel.classList.add('open');
        if (overlay) overlay.style.display = window.innerWidth <= 960 ? 'block' : 'none';
        if (fab) fab.style.display = 'none';

        setTimeout(function () {
            input.focus();
            if (window.chatAutoSend) {
                window.chatAutoSend = false;
                messages.innerHTML = '';
                chips?.classList.remove('d-none');
                if (chips) messages.appendChild(chips);
                sendMessage('Explain this ASVS finding, why it is a security risk, and show me a concrete code fix.');
            }
            scrollBottom();
        }, 50);
    };

    window.closeChatPanel = function () {
        panel?.classList.remove('open');
        panel?.classList.add('panel-hidden');
        if (overlay) overlay.style.display = 'none';
        if (fab) fab.style.display = 'flex';
    };

    // Scroll-to-bottom button visibility
    if (scrollBtn) {
        messages.addEventListener('scroll', function () {
            const atBottom = messages.scrollHeight - messages.scrollTop - messages.clientHeight < 60;
            scrollBtn.classList.toggle('visible', !atBottom);
        });
        scrollBtn.addEventListener('click', scrollBottom);
    }

    // Suggestion chip clicks
    messages.addEventListener('click', function (e) {
        const chip = e.target.closest('.chat-chip');
        if (chip && chip.dataset.msg) sendMessage(chip.dataset.msg);
    });

    sendBtn.addEventListener('click', send);
    input.addEventListener('keydown', function (e) {
        if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); send(); }
    });
});
