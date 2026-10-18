# Bedwars Overlay

[English](README.md) | **Türkçe**

Bedwars Overlay, Hypixel Bed Wars oyuncuları için küçük bir Windows overlay uygulamasıdır. Minecraft log dosyanı okur, `/who` sonucunu algılar ve oyun sırasında işine yarayacak Bed Wars istatistiklerini temiz bir overlay içinde gösterir.

![Oyuncularla Bedwars Overlay](docs/assets/overlay-with-players.png)

## Ne İşe Yarar?

- `/who` listesindeki oyuncuları gösterir.
- `/bwstats` ile belirli oyuncuları yükleyebilirsin.
- `/bwignore`, Ayarlar'daki gizlenen oyuncular, `/p list` parti gizleme ve kendini gizleme seçenekleriyle oyuncuları gizleyebilirsin.
- Level, winstreak, FKDR, WLR, final kills, wins ve 0-100 arası tehdit (threat) skoru gibi Bed Wars statlarını gösterir.
- Tehdit skorunu Low / Medium / High / Extreme olarak sınıflar; satır tooltip'inde skoru oluşturan sinyalleri (FKDR, WLR, KDR, yatak oranı, seviye, winstreak, alt hesap şüphesi, tekrar karşılaşma) listeler.
- Hangi oyuncunun hangi maçta olduğunu hatırlar ve tekrar karşılaştığın oyuncuları isim kolonunda `×N` rozetiyle işaretler.
- Ayarlardan tek tek stat kolonlarını gizleyip gösterebilirsin.
- Riskli lobbyleri ve normal dışı statları daha hızlı fark etmene yardımcı olur.
- Hypixel loglarında bir oyuncu için `FINAL KILL!` mesajı gelirse satırı kısa süre kırmızı `FINAL KILL` efektiyle gösterir, sonra tablodan siler.
- Bed Wars oyunu bittiğinde (kazanma veya kaybetme) mevcut listeyi temizler.
- İlk açılışta düzenleme modunda gelir; overlay’i istediğin yere taşıyıp boyutlandırabilirsin.
- Overlay konumunu ve boyutunu sonraki açılış için hatırlar.
- Play mode’da saydam çalışır ve Minecraft’ı engellemez.
- Play mode’da taskbar ve Alt-Tab’da görünmez; düzenleme modunda görünür.
- Sistem tepsisinden çalışır; overlay’i kapatmak gizler, tepsi menüsünden tekrar açabilir veya uygulamadan çıkabilirsin.
- Türkçe ve İngilizce destekler.

## Kısayollar

- `Ctrl+Alt+O`: düzenleme modu ile saydam play mode arasında geçiş yapar.
- `Ctrl+Alt+H`: overlay’i gizler veya gösterir. Gizlenen overlay sistem tepsisinden tekrar açılabilir.

## Overlay Komutları

Bu komutları Minecraft chat’e yaz. Hypixel bunları bilinmeyen komut olarak log’a yazar, overlay de bu log çıktısını okur.

- `/who`: mevcut oyun oyuncularını yükler ve ignore filtrelerini uygular.
- `/bwstats <isim...>`: sadece yazılan oyuncuları yükler. Filtreleri yok sayar; böylece ignore edilmiş oyuncuyu da inceleyebilirsin.
- `/bwignore <isim...>`: oyuncuları gizlenenler listesine ekler ve mevcut overlay’den siler.
- `/bwclear`: mevcut overlay listesini temizler.
- `/bwhelp`: komut yardımını overlay tanılama/status alanında gösterir.
- `/p list`: `Ignore party players from /p list` ayarı için parti listesini günceller.

## Kurulum

1. [Latest Release](https://github.com/emi-ran/Bedwars-Overlay/releases/latest) kısmından son sürümü indir.
2. `BedwarsOverlaySetup-*-win-x64.exe` dosyasını çalıştır ve Bedwars Overlay’i aç.
3. Portable kullanım istersen `.zip` indir, çıkar ve `BedwarsOverlay.App.exe` çalıştır.
4. Ayarlar penceresini aç.
5. Hypixel API key gir.
6. Minecraft `latest.log` yolunu kontrol et veya seç.
7. İstersen görünür stat kolonlarını seç, gizlenen oyuncuları ayarla ve tekrar karşılaşma rozeti ayarlarını düzenle.
8. Bed Wars lobby’sine gir ve Minecraft içinde `/who` yaz.

![Ayarlar penceresi](docs/assets/settings-window.png)

## Hypixel API Key Alma

API key’i Hypixel Developer Dashboard üzerinden alabilirsin:

[https://developer.hypixel.net/](https://developer.hypixel.net/)

Uygulamadaki Ayarlar ekranında bu sayfayı açan bir buton da var.

## Kaynak Koddan Çalıştırma

Çoğu kullanıcı son sürümü indirmeli. Kendin derlemek istersen:

```powershell
git clone https://github.com/emi-ran/Bedwars-Overlay.git
cd Bedwars-Overlay
dotnet build "BedwarsOverlay.App\BedwarsOverlay.App.csproj"
dotnet run --project "BedwarsOverlay.App\BedwarsOverlay.App.csproj"
```

Gereksinimler:

- Windows
- .NET 8 SDK

## Release Oluşturma

Installer oluşturmak için Inno Setup 6 kur:

[https://jrsoftware.org/isinfo.php](https://jrsoftware.org/isinfo.php)

Release dosyalarını oluştur:

```powershell
.\scripts\publish-release.ps1 -Version "1.4.0"
```

`.pfx` sertifika ile release dosyalarını imzala:

```powershell
.\scripts\publish-release.ps1 -Version "1.4.0" -Sign -CertificatePath ".\Bedwars-Overlay.pfx"
```

Script sertifika şifresini sorar. `.pfx` veya `.p12` dosyalarını commit etme.
Varsayılan olarak uygulama exe dosyasını, proje DLL dosyalarını ve installer'ı imzalar. Yayındaki tüm `.exe` ve `.dll` dosyalarını imzalamak istersen `-SignAllBinaries` kullan.

## Notlar

- Bedwars Overlay, Hypixel ile bağlı veya resmi bir uygulama değildir.
- Hypixel kurallarına ve API politikalarına uymak senin sorumluluğundadır.
- API key’ini gizli tut.
- Overlay’i tekrar açmak veya uygulamadan tamamen çıkmak için sistem tepsisi ikonunu kullan.

## Katkı

Katkılar kabul edilir. Detaylar için [CONTRIBUTING.md](CONTRIBUTING.md).

## Lisans

GNU Affero General Public License v3.0. Detaylar için [LICENSE](LICENSE).
