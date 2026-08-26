// Переводим технические тексты ошибок в понятные пользователю формулировки
function friendlyErrorMessage(rawMessage) {
    if (!rawMessage) return "Произошла неизвестная ошибка.";

    if (rawMessage.includes("403")) {
        return "Нейросеть отказала в доступе (403). Скорее всего, неверный или неполный API-ключ провайдера — проверьте настройки в разделе «Шлюзы ИИ».";
    }
    if (rawMessage.includes("401")) {
        return "Нейросеть не приняла ключ доступа (401). Проверьте API-ключ в разделе «Шлюзы ИИ».";
    }
    if (rawMessage.includes("429")) {
        return "Превышен лимит запросов к нейросети (429). Подождите немного и попробуйте снова.";
    }
    if (rawMessage.includes("timeout") || rawMessage.includes("timed out")) {
        return "Нейросеть не ответила вовремя. Попробуйте ещё раз или выберите другого провайдера.";
    }
    if (rawMessage.includes("БАЗА") || rawMessage.includes("Каталог курсов пуст")) {
        return rawMessage; // это наше собственное понятное сообщение, не трогаем
    }
    return rawMessage;
}

function showToast(message, isError = false, rawDetail = null) {
    const existing = document.querySelector('.toast-notify');
    if (existing) existing.remove();

    const toast = document.createElement('div');
    toast.className = `toast-notify ${isError ? 'toast-error' : ''}`;
    toast.innerHTML = `
        <div class="toast-icon"><i class="bi ${isError ? 'bi-x-circle-fill' : 'bi-check-circle-fill'}"></i></div>
        <div class="toast-body">
            <span class="toast-title">${isError ? 'Не удалось выполнить' : 'Готово'}</span>
            ${message}
            ${rawDetail ? `<div class="toast-detail">${rawDetail}</div>` : ''}
        </div>
        <button class="toast-close" onclick="this.parentElement.remove()">&times;</button>
    `;
    document.body.appendChild(toast);

    requestAnimationFrame(() => toast.classList.add('show'));

    setTimeout(() => {
        toast.classList.remove('show');
        setTimeout(() => toast.remove(), 350);
    }, isError ? 8000 : 5000); // ошибки показываем чуть дольше
}
// Переключение вкладки "О программе" между ролями методист/ГГС
function setHelpRole(role) {
    const methodistBtn = document.getElementById('helpRoleMethodist');
    const ggsBtn = document.getElementById('helpRoleGgs');
    const methodistContent = document.getElementById('helpContentMethodist');
    const ggsContent = document.getElementById('helpContentGgs');

    if (!methodistBtn || !ggsBtn || !methodistContent || !ggsContent) return;

    methodistBtn.classList.toggle('active', role === 'methodist');
    ggsBtn.classList.toggle('active', role === 'ggs');
    methodistContent.classList.toggle('d-none', role !== 'methodist');
    ggsContent.classList.toggle('d-none', role !== 'ggs');
}