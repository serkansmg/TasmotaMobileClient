// Focus element helper for macOS MAUI hybrid
window.focusElement = (elementId) => {
    try {
        const element = document.getElementById(elementId);
        if (element) {
            // Multiple attempts for macOS
            element.focus();

            // Force selection for better editing
            if (element.type === 'text') {
                element.select();
            }

            // Fallback for stubborn cases
            setTimeout(() => {
                element.focus();
                if (element.type === 'text') {
                    element.setSelectionRange(element.value.length, element.value.length);
                }
            }, 10);
        }
    } catch (error) {
        console.log('Focus error:', error);
    }
};

// Alternative focus method for macOS
window.focusAndSelect = (elementId) => {
    try {
        const element = document.getElementById(elementId);
        if (element) {
            // Force click to ensure it's interactive
            element.click();
            element.focus();

            // Wait a bit then select all text
            setTimeout(() => {
                element.focus();
                element.select();
            }, 50);
        }
    } catch (error) {
        console.log('Focus and select error:', error);
    }
};

// SweetAlert2 Custom Functions
window.showDeleteConfirmation = async (title, text) => {
    const result = await Swal.fire({
        title: title || 'Silmek istediğinize emin misiniz?',
        text: text || 'Bu işlem geri alınamaz!',
        icon: 'warning',
        showCancelButton: true,
        confirmButtonText: 'Evet, Sil',
        cancelButtonText: 'İptal',
        reverseButtons: true
    });
    return result.isConfirmed;
};

window.showSuccessAlert = async (title, text) => {
    await Swal.fire({
        title: title || 'Başarılı!',
        text: text,
        icon: 'success',
        confirmButtonText: 'Tamam'
    });
};

window.showErrorAlert = async (title, text) => {
    await Swal.fire({
        title: title || 'Hata!',
        text: text,
        icon: 'error',
        confirmButtonText: 'Tamam'
    });
};

window.showInfoAlert = async (title, text) => {
    await Swal.fire({
        title: title,
        text: text,
        icon: 'info',
        confirmButtonText: 'Tamam'
    });
};

window.showTimerConfigAlert = async (boardName, relayName) => {
    await Swal.fire({
        title: 'Timer Ayarları',
        html: `
            <div style="text-align: left; margin: 20px 0;">
                <p><strong>Board:</strong> ${boardName}</p>
                <p><strong>Röle:</strong> ${relayName}</p>
                <br>
                <p style="color: #6B7280; font-size: 14px;">Timer konfigürasyon sayfası burada açılacak</p>
            </div>
        `,
        icon: 'info',
        confirmButtonText: 'Tamam'
    });
};