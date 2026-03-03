// Sayfa tamamen yüklendiğinde çalış
document.addEventListener('DOMContentLoaded', function () {

    // Sadece Randevu Formu sayfasındaysak bu kodları çalıştır
    var appointmentForm = document.getElementById('appointmentForm');

    if (appointmentForm) {

        var doctorSelect = document.getElementById('doctorSelect');
        var dateSelect = document.getElementById('dateSelect'); // HTML'de id="DatePicker" ise burayı düzeltmen gerekebilir, kontrol et.
        var container = document.getElementById('slotsContainer');
        var submitBtn = document.getElementById('btnSubmit');
        var finalDateTimeInput = document.getElementById('finalDateTime');

        // Olay İzleyicileri (Event Listeners)
        // HEM DOKTOR HEM TARİH DEĞİŞTİĞİNDE TETİKLENMELİ
        if (doctorSelect) doctorSelect.addEventListener('change', loadSlots);
        if (dateSelect) dateSelect.addEventListener('change', loadSlots);

        function loadSlots() {
            var doctorId = doctorSelect.value;
            var date = dateSelect.value;

            // Eğer seçimler eksikse uyarı ver ve çık
            if (!doctorId || !date) {
                container.innerHTML = '<div class="alert alert-warning w-100 text-center"><i class="fa-solid fa-triangle-exclamation me-2"></i>Lütfen önce doktor ve tarih seçiniz.</div>';
                submitBtn.disabled = true;
                return;
            }

            container.innerHTML = '<div class="text-center w-100 text-primary"><i class="fa-solid fa-spinner fa-spin me-2"></i>Müsaitlik durumu kontrol ediliyor...</div>';

            // AJAX İsteği
            fetch(`/Appointment/GetTakenSlots?doctorId=${doctorId}&date=${date}`)
                .then(response => {
                    if (!response.ok) {
                        throw new Error("Veri alınamadı");
                    }
                    return response.json();
                })
                .then(takenSlots => {
                    container.innerHTML = ''; // Önceki saatleri temizle

                    // 09:00 - 17:00 arası saatleri oluştur
                    var startHour = 9;
                    var endHour = 17;
                    var hasAvailableSlot = false;

                    for (let h = startHour; h < endHour; h++) {
                        // Tam saatler (00) ve Buçuklar (30) için döngü
                        for (let m = 0; m < 60; m += 30) {

                            // Saati formatla (Örn: 09:30)
                            let timeStr = `${h.toString().padStart(2, '0')}:${m.toString().padStart(2, '0')}`;

                            // Backend'den gelen veride bu saat var mı?
                            let isTaken = takenSlots.includes(timeStr);

                            let btn = document.createElement('button');
                            btn.type = 'button';

                            // Görünüm Ayarları
                            if (isTaken) {
                                btn.className = 'btn btn-secondary m-1 disabled';
                                btn.innerHTML = `<i class="fa-solid fa-lock me-1"></i> ${timeStr}`;
                                btn.disabled = true;
                            } else {
                                btn.className = 'btn btn-outline-primary m-1 slot-btn'; // slot-btn class'ını seçim temizleme için kullanacağız
                                btn.innerText = timeStr;
                                hasAvailableSlot = true;

                                // Tıklama Olayı
                                btn.onclick = function () {
                                    // 1. Diğer butonların aktifliğini kaldır
                                    var allBtns = container.querySelectorAll('.slot-btn');
                                    allBtns.forEach(b => {
                                        b.className = 'btn btn-outline-primary m-1 slot-btn';
                                    });

                                    // 2. Tıklanan butonu yeşil yap (Seçildi)
                                    this.className = 'btn btn-success m-1 slot-btn shadow';

                                    // 3. Gizli inputa tarihi ve saati birleştirip yaz
                                    // Format: YYYY-MM-DDTHH:mm:00
                                    finalDateTimeInput.value = date + 'T' + timeStr + ':00';

                                    // 4. Kaydet butonunu aktif et
                                    submitBtn.disabled = false;
                                };
                            }

                            container.appendChild(btn);
                        }
                    }

                    // Eğer gün tamamen doluysa mesaj ver
                    if (!hasAvailableSlot) {
                        container.innerHTML = '<div class="alert alert-danger w-100 text-center">Bu tarihte uygun randevu saati bulunmamaktadır.</div>';
                    }

                })
                .catch(err => {
                    console.error(err);
                    container.innerHTML = '<div class="alert alert-danger w-100">Saatler yüklenirken hata oluştu. Lütfen tekrar deneyin.</div>';
                });
        }
    }
});