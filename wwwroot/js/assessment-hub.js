(function () {
    'use strict';

    var connection = new signalR.HubConnectionBuilder()
        .withUrl('/hubs/assessment')
        .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
        .build();

    function showToast(message, linkUrl, linkText) {
        var el = document.createElement('div');
        el.className = 'assessment-toast';
        el.textContent = message;

        if (linkUrl) {
            var a = document.createElement('a');
            a.href = linkUrl;
            a.textContent = linkText || 'Klik di sini';
            el.appendChild(a);
        }

        document.body.appendChild(el);

        requestAnimationFrame(function () {
            el.classList.add('visible');
        });

        setTimeout(function () {
            el.classList.remove('visible');
            setTimeout(function () {
                if (el.parentNode) el.parentNode.removeChild(el);
            }, 350);
        }, 5000);
    }

    function showPersistentToast(message, buttonText, buttonAction) {
        var el = document.createElement('div');
        el.className = 'assessment-toast assessment-toast--persistent';

        var span = document.createElement('span');
        span.textContent = message;
        el.appendChild(span);

        var btn = document.createElement('button');
        btn.textContent = buttonText || 'OK';
        btn.addEventListener('click', function () {
            if (typeof buttonAction === 'function') buttonAction();
        });
        el.appendChild(btn);

        document.body.appendChild(el);

        requestAnimationFrame(function () {
            el.classList.add('visible');
        });
    }

    connection.onreconnecting(function () {
        showToast('Koneksi terputus...');
    });

    connection.onreconnected(function () {
        showToast('Koneksi pulih');
        if (window.assessmentBatchKey) {
            connection.invoke('JoinBatch', window.assessmentBatchKey).catch(function (err) {
                console.warn('[assessment-hub] JoinBatch after reconnect failed:', err);
            });
        }
    });

    connection.onclose(function (error) {
        var msg = error ? error.toString() : '';
        if (msg.indexOf('401') !== -1) {
            showToast('Sesi login habis \u2014 silakan login ulang', '/Account/Login', 'Login');
        } else {
            showPersistentToast('Koneksi gagal.', 'Muat Ulang', function () {
                window.location.reload();
            });
        }
    });

    async function startHub() {
        try {
            await connection.start();
            if (window.assessmentBatchKey) {
                await connection.invoke('JoinBatch', window.assessmentBatchKey);
            }
        } catch (err) {
            // onclose handles retries exhausted — swallow start errors silently
        }
    }

    startHub();

    window.assessmentHub = connection;
}());
