# Mahf Firmware CPU Driver v3.0.0
## Kurulum ve Kullanım Kılavuzu

## 📋 İÇİNDEKİLER

1. [Genel Bakış](#genel-bakış)
2. [Sistem Gereksinimleri](#sistem-gereksinimleri)
3. [Kurulum](#kurulum)
4. [Kullanım](#kullanım)
5. [Sorun Giderme](#sorun-giderme)
6. [Teknik Detaylar](#teknik-detaylar)

---

## 🎯 GENEL BAKIŞ

Mahf Firmware CPU Driver, Intel, AMD ve ARM işlemcileri için geliştirilmiş profesyonel bir performans ve güç yönetim sürücüsüdür. Sürücü, CPU frekansını ve voltajını dinamik olarak kontrol ederek sistem performansını optimize eder.

### Temel Özellikler:
- **4 Performans Modu**: Power Save, Balanced, Performance, Extreme
- **Gerçek Zamanlı İzleme**: CPU kullanımı, sıcaklık, güç tüketimi
- **Universal Destek**: Intel, AMD, ARM mimarileri
- **Kararlı Çalışma**: Test edilmiş, hatasız yapı
- **Kullanıcı Dostu**: Modern kontrol paneli arayüzü

---

## 💻 SİSTEM GEREKSİNİMLERİ

### Minimum Gereksinimler:
- **İşletim Sistemi**: Windows 10 (22H2+) / Windows 11
- **Mimari**: x64 veya ARM64
- **CPU**: Modern çok çekirdekli işlemci
- **RAM**: 4 GB
- **Disk**: 50 MB boş alan
- **Yetkiler**: Administrator hakları

### Desteklenen İşlemciler:
- **Intel**: Core i3/i5/i7/i9 (8th Gen+), Xeon
- **AMD**: Ryzen 3/5/7/9 (2000 serisi+)
- **ARM**: Qualcomm Snapdragon, Microsoft SQ

---

## 📦 KURULUM

### Yöntem 1: Otomatik Kurulum (Önerilen)

1. `MahfCPUSetup_3.0.0.exe` dosyasını indirin
2. Sağ tıklayıp "Yönetici olarak çalıştırın"
3. Kurulum sihirbazını takip edin
4. Kurulum tamamlandığında sistemi yeniden başlatın

### Yöntem 2: Manuel Kurulum

1. Sürücü dosyalarını hazırlayın:


2. Yönetici komut istemi açın:
```cmd
cd /d "sürücü_dosyalarının_klasörü"
install.bat