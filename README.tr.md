# Bedwars Overlay

[English](README.md) | **Türkçe**

Bedwars Overlay, Hypixel Bed Wars oyuncuları için küçük bir Windows overlay uygulamasıdır. Minecraft log dosyanı okur, `/who` sonucunu algılar ve oyun sırasında işine yarayacak Bed Wars istatistiklerini temiz bir overlay içinde gösterir.

![Oyuncularla Bedwars Overlay](docs/assets/overlay-with-players.png)

## Ne İşe Yarar?

- `/who` listesindeki oyuncuları gösterir.
- Bağlantısı kopan maç oyuncusunu `DC` rozetiyle listede tutar. Reconnect mesajı veya sonraki `/who` içinde görünmesi rozeti kaldırır; eksik oyuncular reconnect ile gizleme filtreleri uygulanarak eklenir. Ardından `FINAL KILL!` gelirse oyuncu silinir ve aynı maçta reconnect ile geri eklenmez. Tek başına disconnect mesajından yatak durumuna göre elenme çıkarılmaz; `Split!` tetiklenmez.
- Maçta bilinen oyuncuları yeniden doğarken korur; sonraki `/who` çıktısında görünmeseler de silmez. Tanınan ölüm mesajlarından eksik isimleri eklerken mevcut istatistikleri yeniden yüklemez. Final kill alanlar sonraki maça kadar geri eklenmez.
- İstemci renk kodlarını koruyorsa ölüm mesajlarından oyuncuların takımlarını okur; renk yoksa OCR bilgisini korur veya takımı boş bırakır.
- Takım arkadaşın normal şekilde ölürse beş saniyelik küçük bir `Split!` uyarısı gösterir. Ayarlarda kendi Minecraft adını gir; TAB+END ile belirlenen aynı takım bilgisi önceliklidir, takım bilinmiyorsa `/p list` üyeliği kullanılır. Kendin öldüğünde veya final kill durumunda tetiklenmez. Kendini/partini tabloda gizlesen de TAB+END takım eşleştirmesine dahil edilirler. Fırına uzaklığın ölçülmez; bu bir ölüm hatırlatıcısıdır ve yalnız overlay görünürken görünür.
- `END` ile (TAB'ı basılı tutup END'e basarak) Minecraft oyuncu listesindeki takım etiketlerini okur ve tabloda takıma göre sıralamayı sağlar. Sekiz takımın tamamı tanınır; böylece Solos ve Doubles da desteklenir. Kısa oyuncu adları için OCR tek sütun modunda çalışır; emin olmadığı adı tahmin edip takıma atamak yerine boş bırakır. Her takım rengi ayrı okunduğu için takımı bilinen oyuncunun adı da takım rengiyle görünür; takımı bilinmeyen isimler rank rengini korur (MVP+ camgöbeği, VIP yeşil vb.). OCR motoru uygulamayla birlikte gelir.
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
- `END`: Minecraft öndeyken TAB'ı basılı tutup END'e basınca takım bilgisini okur. Ekran görüntüsü alırken overlay kısa süreliğine gizlenir.

## Overlay Komutları

Bu komutları Minecraft chat’e yaz. Hypixel bunları bilinmeyen komut olarak log’a yazar, overlay de bu log çıktısını okur.

- `/who`: mevcut oyun oyuncularını yükler ve ignore filtrelerini uygular.
- `/bwstats <isim...>`: sadece yazılan oyuncuları yükler. Filtreleri yok sayar; böylece ignore edilmiş oyuncuyu da inceleyebilirsin.
- `/pstats`: önbellekteki parti kadrosunun istatistiklerini yükler. Kadro parti katılma/ayrılma mesajlarından otomatik algılanır, `/p list` yenileme yedek yoludur; parti ignore ayarı bu açık isteği engellemez.
- `/bwignore <isim...>`: oyuncuları gizlenenler listesine ekler ve mevcut overlay’den siler.
- `/bwclear`: mevcut overlay listesini temizler.
- `/bwhelp`: kısa komut yardımını overlay tanılama/status alanında gösterir.
- `/p list`: `Ignore party players` ayarı için parti listesini yeniler. Parti üyeliği ayrıca katılma/ayrılma mesajlarından otomatik algılanır.

Overlay açıldığında zaten partideysen bir kez `/p list` yaz: otomatik takip yalnızca yeni log mesajlarında adı geçen üyeleri bilir. Davet tek başına üyelik sayılmaz. Aynı parti kadrosunu parti gizleme, `/pstats` ve Split hatırlatıcısının yedek takım kontrolü kullanır.

Kadroyu yüklemek için oyun başlar başlamaz `/who` yaz; durum çubuğu seni uyarıyor. Maç sırasında sonraki `/who` çıktıları bilinen oyuncularla birleştirilir; yeniden doğmayı bekleyen oyuncular listede görünmeyebilir. Tanınan ölüm mesajlarından eksik ölen/öldüren oyuncular da aynı gizleme filtreleriyle eklenir. Final kill alanlar o maçta tekrar eklenmez. Yeni maç, oyun sonu veya algılanan lobi geçişi maç kadrosunu sıfırlar. Uygulamayı maç ortasında açtıysan `/who` yükledikten sonraki ilk tanınan ölüm mesajı kadroyu korumayı başlatır; geçmişe yeni bir maç kaydı açmaz.

İncelenen Badlion loglarında ölüm mesajları takım renklerini içermiyor; bu durumda `TAB` + `END` hâlâ gerekiyor. Minecraft renk kodlarını koruyan istemcilerde tanınan ölüm mesajlarından takım bilgisi alınabilir. Yalnızca isimlerden, kimin kimi öldürdüğünden veya yatak yıkılma mesajından takım tahmin edilmez.

Takım OCR'ı, küçük arayüz ölçeklerindeki dikey gölgelendirme dahil, doygun takım renkli yazıların koyulaşan çizgilerini korur. Takım etiketi uyumu ve yeterli isim eşleşme güveni hâlâ zorunludur; belirsiz satırlar boş kalır.

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

Installer veya portable release oluşturmak için Inno Setup 6 ve x64 C++ yeniden dağıtılabilir dosyalarıyla birlikte Visual Studio Build Tools 2022 kur:

[https://jrsoftware.org/isinfo.php](https://jrsoftware.org/isinfo.php)

`BedwarsOverlay.App.csproj` sürümüyle eşleşen `vMAJOR.MINOR.PATCH` etiketli taslak olmayan bir GitHub Release yayımlandığında `.github/workflows/release.yml` installer ve portable ZIP dosyasını oluşturup aynı release'e ekler. Önce repository Actions secrets bölümünde `WINDOWS_SIGNING_CERTIFICATE_BASE64` ve `WINDOWS_SIGNING_CERTIFICATE_PASSWORD` secret'larını tanımla. Mevcut kendinden imzalı sertifika diğer Windows bilgisayarlarında otomatik olarak güvenilir sayılmaz. PFX'ten Base64 secret'ı PowerShell'de şöyle yükle:

```powershell
[Convert]::ToBase64String([IO.File]::ReadAllBytes((Resolve-Path .\Bedwars-Overlay.pfx))) | gh secret set WINDOWS_SIGNING_CERTIFICATE_BASE64 --repo emi-ran/Bedwars-Overlay
```

Parola secret'ını shell geçmişine yazmadan yükle:

```powershell
$secure = Read-Host "PFX parolası" -AsSecureString
$ptr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
try {
    [Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr) | gh secret set WINDOWS_SIGNING_CERTIFICATE_PASSWORD --repo emi-ran/Bedwars-Overlay
} finally {
    [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr)
}
```

Release dosyalarını oluştur:

```powershell
.\scripts\publish-release.ps1 -Version "1.4.9"
```

`.pfx` sertifika ile release dosyalarını imzala:

```powershell
.\scripts\publish-release.ps1 -Version "1.4.9" -Sign -CertificatePath ".\Bedwars-Overlay.pfx"
```

Script sertifika şifresini sorar. `.pfx` veya `.p12` dosyalarını commit etme.
Varsayılan olarak uygulama exe dosyasını, proje DLL dosyalarını ve installer'ı imzalar. Yayındaki tüm `.exe` ve `.dll` dosyalarını imzalamak istersen `-SignAllBinaries` kullan.

Yayımlamadan önce Actions üzerinden release workflow'unu elle çalıştırabilirsin. Windows 2022 üzerinde proje sürümünü derleyip imzalar; release oluşturmadan installer ve ZIP dosyalarını `signed-windows-packages` artifact'ı olarak saklar.

## Tanıtım Sitesi

[Türkçe/İngilizce tanıtım sitesini aç](https://emi-ran.github.io/Bedwars-Overlay/). Kaynaklar `docs/` altında; yerel önizleme ve GitHub Pages yayınlama yönergeleri için [Site bakımı](docs/SITE.md) belgesine bak.

## Notlar

- Bedwars Overlay, Hypixel ile bağlı veya resmi bir uygulama değildir.
- Hypixel kurallarına ve API politikalarına uymak senin sorumluluğundadır.
- API key’ini gizli tut.
- Overlay’i tekrar açmak veya uygulamadan tamamen çıkmak için sistem tepsisi ikonunu kullan.

## Katkı

Katkılar kabul edilir. Detaylar için [CONTRIBUTING.md](CONTRIBUTING.md).

## Lisans

GNU Affero General Public License v3.0. Detaylar için [LICENSE](LICENSE).
