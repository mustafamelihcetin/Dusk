# Dusk

[English](README.md)

Windows için sade ve güvenli bir kapatma zamanlayıcısı.

Halkayı sürükleyip süreyi seç, düğmeye bas. Süre dolduğunda bilgisayar kapanır, yeniden başlar, uykuya geçer ya da oturumu kapatır.

## Kullanım

- **Süre seçmek:** Halkayı sürükle. Bir tur bir saat eder; turlar üst üste binerek 12 saate kadar çıkar. Fare tekerleği 5'er dakika, ok tuşları 1'er dakika, PageUp/PageDown 15'er dakika değiştirir. Hazır düğmeler: 15 dk, 30 dk, 1 sa, 2 sa.
- **Saat mi, süre mi:** Kadranın ortasına tıkla. "1 sa 30 dk" ile "23:45" arasında geçer.
- **Geri sayım sürerken:** "+10 dk" ile uzat, "İptal" ile vazgeç. Pencereyi kapatırsan Dusk saatin yanındaki simge alanına iner ve saymaya devam eder.
- **Son dakika:** Ekranın ortasında bir uyarı çıkar: "10 dk ertele", "İptal et" ya da "Şimdi kapat".

## Güvenlik

- **Hiçbir şey önceden zamanlanmaz.** Windows'a "şu saatte kapan" emri verilmez; işlem ancak süre dolduğu anda yapılır. Dusk'ı kapatırsan ya da sonlandırırsan zamanlayıcı da iptal olur.
- **Kaydedilmemiş işler korunur.** "Kaydedilmemiş işleri bekleme" kutusu işaretli değilse Windows, kaydedilmemiş belgesi olan uygulamalar için seni bekler. Bu kutu varsayılan olarak kapalıdır.
- **Uyarı odak çalmaz.** Uyarı en üstte görünür ama klavyeyi ele geçirmez. Başka bir yerde yazarken bastığın tuşlar kazara erteleme ya da iptal yapmaz.
- **Uykuda kaçan süre uygulanmaz.** Bilgisayar hedef saatte uykudaysa ve 2 dakikadan fazla gecikildiyse Dusk sürpriz yapmaz: zamanlayıcıyı iptal eder ve haber verir.
- **Geri sayım sürerken bilgisayar kendiliğinden uykuya geçmez,** böylece zamanlayıcı gerçekten çalışır. Ekran yine kapanabilir.
- **Yönetici izni istemez,** tek bir kopyası çalışır.

## Deneme kipi

```
Dusk.exe --deneme
```

Bu kipte (ya da `DUSK_DENEME=1` ortam değişkeniyle) hiçbir şey kapanmaz. Süre dolunca yalnız `%AppData%\Dusk\deneme.log` dosyasına bir satır yazılır.

## Günlük ve ayarlar

- `%AppData%\Dusk\dusk.log`: Başlatma, uyarı, uzatma, iptal (kimin yaptığıyla birlikte), kaçırma ve sonuç olayları. "Dün gece neden kapanmadı?" sorusunun yanıtı buradadır.
- `%AppData%\Dusk\ayarlar.json`: Son seçilen işlem, süre ve görünüm.

## Derleme

.NET 10 SDK gerekir.

```
dotnet test
dotnet publish src/Dusk -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o dist
```

Çıkan `dist\Dusk.exe`, .NET 10 Masaüstü Çalışma Zamanı kurulu her Windows 10/11'de çalışır.

## Yapı

- `src/Dusk.Core`: Arayüzden bağımsız mantık: geri sayım durum makinesi, Türkçe zaman metinleri (doğru ek: "23:45'te", "07:30'da"), ayarlar.
- `src/Dusk`: WPF arayüzü (Windows 11 Fluent teması, açık/koyu kip sistemi izler), kadran, uyarı penceresi, simge alanı.
- `tests/Dusk.Tests`: Çekirdek testleri.
- `tools/make-icon.ps1`: Uygulama simgesini üretir.
