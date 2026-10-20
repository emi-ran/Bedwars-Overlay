/* Bedwars Overlay — site behaviour. No frameworks, no network calls. */
(() => {
  'use strict';

  const doc = document;
  const root = doc.documentElement;
  const storageKey = 'bedwars-overlay-site-language';
  const supported = ['tr', 'en'];

  const I18N = {
    tr: {
      'meta.title': 'Bedwars Overlay — Lobiyi oku. Oyuna odaklan.',
      'meta.description': "Hypixel Bed Wars için Windows masaüstü overlay'i. /who kadrosunu, takım düzenini, tekrar karşılaştığın oyuncuları ve 0-100 tehdit skorunu tek bakışta gösterir.",
      'meta.ogTitle': 'Bedwars Overlay — Lobiyi oku. Oyuna odaklan.',
      'meta.ogDescription': "Hypixel Bed Wars için Windows masaüstü overlay'i. Takım düzeni, temel istatistikler ve tekrar karşılaşmalar; oyunun üstünde sakin bir katman.",
      'meta.ogLocale': 'tr_TR',
      'skip': 'Ana içeriğe geç',
      'brand.home': 'Bedwars Overlay ana sayfa',
      'brand.sub': 'BED WARS / WINDOWS',
      'nav.aria': 'Ana gezinme',
      'nav.demo': 'Demo',
      'nav.features': 'Özellikler',
      'nav.setup': 'Kurulum',
      'nav.faq': 'SSS',
      'nav.download': 'İndir',
      'lang.group': 'Dil seçimi',
      'lang.tr': 'Türkçeye geç',
      'lang.en': 'İngilizceye geç',
      'lang.changed': 'Dil Türkçe olarak değiştirildi.',
      'hero.eyebrow': 'Windows x64 · Hypixel Bed Wars',
      'hero.line1': 'Lobiyi oku.',
      'hero.line2': 'Oyuna odaklan.',
      'hero.lede': "Bedwars Overlay, Minecraft logunu ve TAB listesini okur; takım düzenini, temel Bed Wars istatistiklerini ve tekrar karşına çıkan oyuncuları oyunun üstünde sakin bir katmanda gösterir.",
      'hero.ctaPrimary': 'Windows için indir',
      'hero.ctaSecondary': 'Kaynağı gör',
      'hero.meta1': 'Kurulum paketi veya portable ZIP',
      'hero.meta2': 'Windows 10/11 · .NET 8',
      'hero.meta3': 'AGPL-3.0 kaynak',
      'heroCard.aria': 'Örnek oyuncu kartı',
      'heroCard.kicker': 'SAHA NOTU',
      'heroCard.sample': 'ÖRNEK',
      'heroCard.label': 'SEÇİLİ OYUNCU',
      'heroCard.threatLabel': 'Tehdit',
      'heroCard.note': 'Kayıtlı maçlarda 5 tekrar karşılaşma; rozet isim satırında görünür.',
      'heroCard.bridge': 'TAKIM OCR · YEREL',
      'team.red': 'Kırmızı',
      'team.blue': 'Mavi',
      'team.green': 'Yeşil',
      'team.yellow': 'Sarı',
      'team.redTeam': 'Kırmızı Takım',
      'team.blueTeam': 'Mavi Takım',
      'team.greenTeam': 'Yeşil Takım',
      'team.yellowTeam': 'Sarı Takım',
      'band.low': 'Düşük',
      'band.medium': 'Orta',
      'band.high': 'Yüksek',
      'band.extreme': 'Aşırı',
      'demo.eyebrow': 'ETKİLEŞİMLİ ÖRNEK',
      'demo.title': 'Takım karmaşasını bir tuşla çöz.',
      'demo.lede': 'Aşağısı gerçek bir maç değil; arayüzün nasıl çalıştığını gösteren bir simülasyon. Ham /who listesiyle başla, sonra TAB + END okumasını tetikle.',
      'demo.badge': 'ÖRNEK VERİ',
      'demo.tabsAria': 'Demo görünümü',
      'demo.tabTeams': 'Takım düzeni',
      'demo.tabThreat': 'Tehdit kırılımı',
      'demo.rawStatus': 'Ham liste · takımlar okunmadı',
      'demo.groupedStatus': 'Takımlar okundu · 8 oyuncu, 4 takım',
      'demo.groupBtn': 'Takımları oku',
      'demo.ungroupBtn': 'Ham listeye dön',
      'demo.tableAria': 'Örnek Bed Wars kadrosu',
      'demo.tableCaption': 'Örnek Bed Wars kadrosu. Takım sütunu başlangıçta boştur.',
      'demo.thPlayer': 'Oyuncu',
      'demo.thTeam': 'Takım',
      'demo.thLevel': 'LVL',
      'demo.thWs': 'WS',
      'demo.thFkdr': 'FKDR',
      'demo.thWlr': 'WLR',
      'demo.thThreat': 'Tehdit',
      'demo.repeatAria': 'Kayıtlı maçlarda 5 tekrar karşılaşma',
      'demo.repeatNote': 'Kayıtlı maçlarda 5 karşılaşma. Tekrar karşılaşma rozeti, aynı oyuncuyu yeniden gördüğünde isim satırında belirir.',
      'demo.footnote': "Simülasyon. Gerçek takım ataması, Minecraft öndeyken TAB basılıyken END'e basılarak yapılan yerel OCR'dan gelir; okunamayan isimler boş kalabilir.",
      'demo.teamCount': '{n} oyuncu',
      'demo.threatKicker': 'TEHDİT KIRILIMI',
      'demo.threatTitle': 'Skorun arkasındaki sinyalleri gör.',
      'demo.threatBody': '0-100 arası tehdit skoru; FKDR, WLR, KDR, yatak oranı, seviye, winstreak, alt hesap şüphesi ve tekrar karşılaşmaları birlikte okur.',
      'demo.pickerAria': 'Örnek oyuncu seç',
      'demo.threatFoot': 'Örnek oyuncular ve örnek değerler.',
      'demo.threatSelected': 'SEÇİLİ OYUNCU',
      'demo.signalFkdr': 'FKDR',
      'demo.signalWlr': 'WLR',
      'demo.signalKdr': 'KDR',
      'demo.signalLevel': 'Seviye',
      'demo.signalWs': 'Winstreak',
      'demo.signalRepeat': 'Tekrar',
      'demo.threatNote': 'Skor; FKDR, WLR, KDR, seviye, winstreak ve kayıtlı tekrar karşılaşma sinyallerini birlikte okur. Bant, lobiyi hızlıca taramana yardım eder.',
      'features.eyebrow': 'SAHADA NE VAR',
      'features.title': 'Az gürültü, doğru sinyal.',
      'features.lede': 'Üç net iş: takımı düzenle, tekrar karşılaşmaları işaretle ve bilgiyi oyunun üstünde tut.',
      'f1.kicker': 'TAKIM DÜZENİ',
      'f1.title': 'Dağınık isimler, dört net satır.',
      'f1.body': "TAB listesini aç, END'e bas. Uygulamayla gelen Tesseract OCR, ekrandaki takım renklerini yerelde okur ve /who kadrosunu takıma göre sıralar. Ayrı bir OCR kurulumu veya client modu yok; kullanıcı tetiklemeli çalışır ve belirsiz ya da renksiz isimler boş kalabilir.",
      'f1.captureLabel': 'Oyuncu listesi',
      'f1.raw': 'Ham liste',
      'f1.local': 'Yerel OCR',
      'f1.grouped': 'Takıma göre',
      'f2.kicker': 'TEHDİT + TEKRAR',
      'f2.title': 'Tehdit skorunu ve tekrar karşılaşmaları gör.',
      'f2.body': '0-100 skor; FKDR, WLR, KDR, yatak oranı, seviye, winstreak, alt hesap şüphesi ve tekrar karşılaşmalardan oluşur. Low, Medium, High ve Extreme bantlarıyla lobiyi hızlıca tararsın. Aynı oyuncuyu yeniden gördüğünde isim satırında ×N rozeti belirir.',
      'f2.sample': 'Örnek tehdit skoru',
      'f2.repeat': 'Tekrar karşılaşma ×5',
      'f2.note': 'Sinyaller birlikte okunur.',
      'f3.kicker': 'PENCERE',
      'f3.title': 'Oyunun üstünde, yolunun dışında.',
      'f3.body': "Play mode'da pencere saydam ve tıklama-geçirgen. Ctrl+Alt+O ile düzenleme moduna geç; sürükle, boyutlandır, kolonları seç. Ctrl+Alt+H ile gizle. Tepside çalışır, oyun sonunda listeyi temizler.",
      'f3.click': 'Tıklama geçirgen',
      'f3.edit': 'düzenle / oyna',
      'setup.eyebrow': 'KURULUM',
      'setup.title': 'Üç adım, sonra oyun.',
      'setup.lede': 'Windows x64. Kurulum paketi veya portable ZIP. İlk açılışta düzenleme modundasın; overlay’i istediğin yere taşı.',
      'setup.step1Title': 'İndir ve aç',
      'setup.step1Body': "Kurulum paketini çalıştır ya da portable ZIP'i çıkar. .NET 8 çalışma zamanı paketle gelir.",
      'setup.step1Link': 'Son sürüm ↗',
      'setup.step2Title': 'Anahtarı ve logu bağla',
      'setup.step2Body': "Hypixel API anahtarını developer.hypixel.net'ten al ve Ayarlar'a gir. latest.log yolunu seç. Anahtar masaüstü uygulamasında kalır.",
      'setup.step2Link': 'Hypixel Developer Dashboard ↗',
      'setup.step3Title': 'Oyun içinde oku',
      'setup.step3Body': "Lobiye gir, /who yaz. TAB'ı basılı tutup END'e bas; takımlar yerelde okunur. Genel API sorguları internet ister, OCR yereldir.",
      'setup.warningTitle': 'Anahtarını bu site istemez.',
      'setup.warningBody': "Hypixel API anahtarı yalnızca masaüstü uygulamasının Ayarlar ekranına girilir ve Windows DPAPI ile yerelde şifrelenir. Bu sayfa hiçbir anahtar toplamaz.",
      'shortcuts.eyebrow': 'KISAYOLLAR',
      'shortcuts.title': 'Eller oyunda kalsın.',
      'shortcuts.edit': 'Düzenle / oyna',
      'shortcuts.hide': 'Göster / gizle',
      'shortcuts.teams': 'Takımları oku',
      'shortcuts.who': 'Kadroyu yükle',
      'faq.eyebrow': 'SSS',
      'faq.title': 'Aklındaki sorular.',
      'faq.lede': 'Kısa cevaplar, abartısız sınırlar.',
      'faq.q1': 'Badlion gibi istemcilerle çalışır mı?',
      'faq.a1': "Evet. Uygulama istemciye mod kurmaz; Minecraft latest.log akışını ve TAB listesinin ekran görüntüsünü kullanır. Badlion gibi istemcilerde log yolunu Ayarlar'dan seçip aynı /who ve TAB + END akışını kullanabilirsin. Ayrı bir OCR kurulumu gerekmez.",
      'faq.q2': 'Takımlar her zaman doğru okunur mu?',
      'faq.a2': "Hayır. Takım OCR'ı kullanıcı tetikler: Minecraft öndeyken TAB'ı basılı tutup END'e basmalısın. Farklı GUI ölçekleri, sıra dışı düzenler veya renksiz isimler okunamayabilir; belirsiz atamalar boş kalır.",
      'faq.q3': 'API anahtarım nereye gidiyor?',
      'faq.a3': 'Yalnızca masaüstü uygulamasının Ayarlar ekranına. Anahtar Windows DPAPI ile yerelde şifrelenir. Bu web sitesi anahtar istemez, toplamaz veya iletmez.',
      'faq.q4': 'İnternet olmadan çalışır mı?',
      'faq.a4': "Takım OCR'ı ve log okuma yereldir. Oyuncu istatistikleri genel Mojang/Hypixel API'lerinden geldiği için sorgular internet gerektirir.",
      'faq.q5': 'Windows SmartScreen uyarısı neden çıkıyor?',
      'faq.a5': 'Release paketleri kendinden imzalı bir sertifika ile imzalanır; bu, diğer bilgisayarlarda otomatik güven oluşturmaz. Yalnızca bu deponun release sayfasından indirdiğin dosyaları çalıştır ve kaynağı kontrol et.',
      'faq.q6': 'Threat skoru nasıl hesaplanır?',
      'faq.a6': 'FKDR, WLR, KDR, yatak oranı, seviye, winstreak, alt hesap şüphesi ve kayıtlı tekrar karşılaşmaları birleştiren 0-100 arası istatistiksel bir göstergedir. Low / Medium / High / Extreme bantlarına ayrılır. Kesin bir yargı değil, lobiyi hızlı okumak için bir ipucudur.',
      'close.kicker': 'SON SÜRÜM',
      'close.line1': 'Sıradaki lobi',
      'close.line2': 'senin.',
      'close.body': "Kurulum paketiyle başla veya portable ZIP'i çıkar. Kaynak açık, lisans AGPL-3.0; indirmeler release sayfasından gelir.",
      'close.download': 'En son sürümü indir',
      'close.source': 'Kaynak kodu',
      'close.meta': 'Windows 10/11 · x64 · Installer + portable ZIP',
      'footer.desc': "Hypixel Bed Wars için küçük, açık kaynaklı bir Windows overlay'i. Lobiyi oku, oyuna odaklan.",
      'footer.product': 'Proje',
      'footer.github': 'GitHub',
      'footer.releases': 'Sürümler',
      'footer.license': 'AGPL-3.0 lisansı',
      'footer.about': 'Bilgi',
      'footer.contributing': 'Katkı',
      'footer.issues': 'Sorun bildir',
      'footer.privacy': 'Statik sayfa. Çerez yok, izleme yok.',
      'footer.legal': 'Hypixel veya Mojang ile bağlantılı değildir. Resmi onay yoktur.',
      'footer.copyright': '© 2026 Bedwars Overlay',
      'footer.licenseShort': 'AGPL-3.0'
    },
    en: {
      'meta.title': 'Bedwars Overlay — Read the lobby. Focus on the game.',
      'meta.description': "A Windows desktop overlay for Hypixel Bed Wars. Reads your /who roster, team layout, repeat opponents, and a 0-100 threat score at a glance.",
      'meta.ogTitle': 'Bedwars Overlay — Read the lobby. Focus on the game.',
      'meta.ogDescription': 'A Windows desktop overlay for Hypixel Bed Wars. Team layout, core stats, and repeat encounters in one quiet layer above the game.',
      'meta.ogLocale': 'en_US',
      'skip': 'Skip to content',
      'brand.home': 'Bedwars Overlay home',
      'brand.sub': 'BED WARS / WINDOWS',
      'nav.aria': 'Main navigation',
      'nav.demo': 'Demo',
      'nav.features': 'Features',
      'nav.setup': 'Setup',
      'nav.faq': 'FAQ',
      'nav.download': 'Download',
      'lang.group': 'Language',
      'lang.tr': 'Switch to Turkish',
      'lang.en': 'Switch to English',
      'lang.changed': 'Language changed to English.',
      'hero.eyebrow': 'Windows x64 · Hypixel Bed Wars',
      'hero.line1': 'Read the lobby.',
      'hero.line2': 'Focus on the game.',
      'hero.lede': 'Bedwars Overlay reads your Minecraft log and TAB list, then keeps team layout, core Bed Wars stats, and repeat opponents in one quiet layer above the game.',
      'hero.ctaPrimary': 'Download for Windows',
      'hero.ctaSecondary': 'View source',
      'hero.meta1': 'Installer or portable ZIP',
      'hero.meta2': 'Windows 10/11 · .NET 8',
      'hero.meta3': 'AGPL-3.0 source',
      'heroCard.aria': 'Sample player card',
      'heroCard.kicker': 'FIELD NOTE',
      'heroCard.sample': 'SAMPLE',
      'heroCard.label': 'SELECTED PLAYER',
      'heroCard.threatLabel': 'Threat',
      'heroCard.note': '5 repeat encounters in recorded matches; the badge sits on the name row.',
      'heroCard.bridge': 'TEAM OCR · LOCAL',
      'team.red': 'Red',
      'team.blue': 'Blue',
      'team.green': 'Green',
      'team.yellow': 'Yellow',
      'team.redTeam': 'Red Team',
      'team.blueTeam': 'Blue Team',
      'team.greenTeam': 'Green Team',
      'team.yellowTeam': 'Yellow Team',
      'band.low': 'Low',
      'band.medium': 'Medium',
      'band.high': 'High',
      'band.extreme': 'Extreme',
      'demo.eyebrow': 'INTERACTIVE SAMPLE',
      'demo.title': 'Untangle the lobby in one keypress.',
      'demo.lede': 'What follows is a simulation, not a live match. Start with the raw /who list, then trigger the TAB + END read.',
      'demo.badge': 'SAMPLE DATA',
      'demo.tabsAria': 'Demo view',
      'demo.tabTeams': 'Team layout',
      'demo.tabThreat': 'Threat breakdown',
      'demo.rawStatus': 'Raw list · teams not read',
      'demo.groupedStatus': 'Teams read · 8 players, 4 teams',
      'demo.groupBtn': 'Read teams',
      'demo.ungroupBtn': 'Back to raw list',
      'demo.tableAria': 'Sample Bed Wars roster',
      'demo.tableCaption': 'Sample Bed Wars roster. The team column starts empty.',
      'demo.thPlayer': 'Player',
      'demo.thTeam': 'Team',
      'demo.thLevel': 'LVL',
      'demo.thWs': 'WS',
      'demo.thFkdr': 'FKDR',
      'demo.thWlr': 'WLR',
      'demo.thThreat': 'Threat',
      'demo.repeatAria': '5 repeat encounters in recorded matches',
      'demo.repeatNote': '5 recorded encounters. The repeat badge appears on a name row when you meet the same player again.',
      'demo.footnote': 'Simulation. In the app, team assignment comes from local OCR captured while TAB is held and END is pressed; unreadable names may stay blank.',
      'demo.teamCount': '{n} players',
      'demo.threatKicker': 'THREAT BREAKDOWN',
      'demo.threatTitle': 'See the signals behind the score.',
      'demo.threatBody': 'The 0-100 threat score reads FKDR, WLR, KDR, bed ratio, level, winstreak, alt-account suspicion, and repeat encounters together.',
      'demo.pickerAria': 'Choose a sample player',
      'demo.threatFoot': 'Sample players and sample values.',
      'demo.threatSelected': 'SELECTED PLAYER',
      'demo.signalFkdr': 'FKDR',
      'demo.signalWlr': 'WLR',
      'demo.signalKdr': 'KDR',
      'demo.signalLevel': 'Level',
      'demo.signalWs': 'Winstreak',
      'demo.signalRepeat': 'Repeat',
      'demo.threatNote': 'The score combines FKDR, WLR, KDR, level, winstreak, and recorded repeat encounters. The band helps you scan the lobby quickly.',
      'features.eyebrow': "WHAT'S IN THE FIELD",
      'features.title': 'Less noise, sharper signals.',
      'features.lede': 'Three clear jobs: sort the team, mark repeat opponents, and keep the info above the game.',
      'f1.kicker': 'TEAM LAYOUT',
      'f1.title': 'Scattered names, four clear rows.',
      'f1.body': 'Open the TAB list and press END. The bundled Tesseract OCR reads team colors on screen locally and sorts the /who roster by team. No separate OCR install or client mod; it is user-triggered, and unclear or uncolored names may stay blank.',
      'f1.captureLabel': 'Player list',
      'f1.raw': 'Raw list',
      'f1.local': 'Local OCR',
      'f1.grouped': 'Grouped by team',
      'f2.kicker': 'THREAT + REPEAT',
      'f2.title': 'See the threat score and repeat encounters.',
      'f2.body': 'The 0-100 score draws on FKDR, WLR, KDR, bed ratio, level, winstreak, alt-account suspicion, and repeat encounters. Low, Medium, High, and Extreme bands let you scan the lobby quickly. When you meet the same player again, an ×N badge appears on the name row.',
      'f2.sample': 'Sample threat score',
      'f2.repeat': 'Repeat encounter ×5',
      'f2.note': 'Signals read together.',
      'f3.kicker': 'WINDOW',
      'f3.title': 'Above the game, out of your way.',
      'f3.body': 'In play mode the window is transparent and click-through. Press Ctrl+Alt+O for edit mode; drag, resize, and choose columns. Press Ctrl+Alt+H to hide. It runs from the tray and clears the list when a game ends.',
      'f3.click': 'Click-through',
      'f3.edit': 'edit / play',
      'setup.eyebrow': 'SETUP',
      'setup.title': 'Three steps, then the game.',
      'setup.lede': 'Windows x64. Installer or portable ZIP. You start in edit mode on first launch; place the overlay where you want it.',
      'setup.step1Title': 'Download and open',
      'setup.step1Body': 'Run the installer or extract the portable ZIP. The .NET 8 runtime is bundled.',
      'setup.step1Link': 'Latest release ↗',
      'setup.step2Title': 'Connect the key and log',
      'setup.step2Body': 'Get a Hypixel API key from developer.hypixel.net and open Settings. Select your latest.log path. The key stays in the desktop app.',
      'setup.step2Link': 'Hypixel Developer Dashboard ↗',
      'setup.step3Title': 'Read it in-game',
      'setup.step3Body': 'Join a lobby and type /who. Hold TAB and press END; teams are read locally. Public API queries need internet, OCR is local.',
      'setup.warningTitle': 'This site never asks for your key.',
      'setup.warningBody': "The Hypixel API key goes only into the desktop app's Settings screen and is encrypted locally with Windows DPAPI. This page collects no keys.",
      'shortcuts.eyebrow': 'SHORTCUTS',
      'shortcuts.title': 'Keep your hands in the game.',
      'shortcuts.edit': 'Edit / play',
      'shortcuts.hide': 'Show / hide',
      'shortcuts.teams': 'Read teams',
      'shortcuts.who': 'Load roster',
      'faq.eyebrow': 'FAQ',
      'faq.title': "The questions you'll ask.",
      'faq.lede': 'Short answers, honest limits.',
      'faq.q1': 'Does it work with Badlion and other clients?',
      'faq.a1': 'Yes. The app installs no client mod; it uses the Minecraft latest.log stream and a screenshot of the TAB list. On Badlion and similar clients, select the log path in Settings and use the same /who and TAB + END flow. No separate OCR install is required.',
      'faq.q2': 'Are teams always detected correctly?',
      'faq.a2': 'No. Team OCR is user-triggered: hold TAB while Minecraft is focused and press END. Different GUI scales, unusual layouts, or uncolored names may not read; unclear assignments stay blank.',
      'faq.q3': 'Where does my API key go?',
      'faq.a3': "Only into the desktop app's Settings screen. The key is encrypted locally with Windows DPAPI. This website never asks for, collects, or transmits it.",
      'faq.q4': 'Does it work without internet?',
      'faq.a4': 'Team OCR and log reading are local. Player stats come from the public Mojang/Hypixel APIs, so those queries need internet.',
      'faq.q5': 'Why does Windows SmartScreen warn me?',
      'faq.a5': "Release packages are signed with a self-signed certificate, which does not create automatic trust on other PCs. Only run files downloaded from this repository's release page, and check the source.",
      'faq.q6': 'How is the Threat score calculated?',
      'faq.a6': 'It is a 0-100 statistical estimate built from FKDR, WLR, KDR, bed ratio, level, winstreak, alt-account suspicion, and recorded repeat encounters. It is banded Low / Medium / High / Extreme. It is not a definitive judgment; it is a hint for reading the lobby quickly.',
      'close.kicker': 'LATEST RELEASE',
      'close.line1': 'The next lobby',
      'close.line2': 'is yours.',
      'close.body': 'Start with the installer or extract the portable ZIP. Open source, AGPL-3.0; downloads always come from the release page.',
      'close.download': 'Get the latest release',
      'close.source': 'Source code',
      'close.meta': 'Windows 10/11 · x64 · Installer + portable ZIP',
      'footer.desc': 'A small open-source Windows overlay for Hypixel Bed Wars. Read the lobby, focus on the game.',
      'footer.product': 'Project',
      'footer.github': 'GitHub',
      'footer.releases': 'Releases',
      'footer.license': 'AGPL-3.0 license',
      'footer.about': 'Info',
      'footer.contributing': 'Contributing',
      'footer.issues': 'Report an issue',
      'footer.privacy': 'Static page. No cookies, no tracking.',
      'footer.legal': 'Not affiliated with Hypixel or Mojang. No official approval.',
      'footer.copyright': '© 2026 Bedwars Overlay',
      'footer.licenseShort': 'AGPL-3.0'
    }
  };

  let lang = 'tr';
  const motionQuery = window.matchMedia('(prefers-reduced-motion: reduce)');

  const qs = (selector, scope = doc) => scope.querySelector(selector);
  const qsa = (selector, scope = doc) => Array.from(scope.querySelectorAll(selector));

  function t(key, vars) {
    const table = I18N[lang] || I18N.tr;
    let value = table[key];
    if (typeof value !== 'string') value = I18N.tr[key];
    if (typeof value !== 'string') return key;
    if (!vars) return value;
    return value.replace(/\{(\w+)\}/g, (match, name) =>
      Object.prototype.hasOwnProperty.call(vars, name) ? String(vars[name]) : match
    );
  }

  function readStoredLanguage() {
    try {
      const stored = window.localStorage.getItem(storageKey);
      return supported.indexOf(stored) !== -1 ? stored : null;
    } catch (error) {
      return null;
    }
  }

  function writeStoredLanguage(value) {
    try {
      window.localStorage.setItem(storageKey, value);
    } catch (error) {
      /* Private mode or blocked storage: the current page still switches. */
    }
  }

  function detectLanguage() {
    const stored = readStoredLanguage();
    if (stored) return stored;
    const browserLanguage = (navigator.languages && navigator.languages[0]) || navigator.language || 'tr';
    return String(browserLanguage).toLowerCase().indexOf('tr') === 0 ? 'tr' : 'en';
  }

  function applyMeta() {
    const bindings = {
      'meta-description': 'meta.description',
      'og-title': 'meta.ogTitle',
      'og-description': 'meta.ogDescription',
      'og-locale': 'meta.ogLocale'
    };
    doc.title = t('meta.title');
    Object.keys(bindings).forEach((id) => {
      const node = doc.getElementById(id);
      if (node) node.setAttribute('content', t(bindings[id]));
    });
  }

  function applyTranslations() {
    qsa('[data-i18n]').forEach((node) => {
      const key = node.getAttribute('data-i18n');
      const value = I18N[lang][key];
      if (typeof value === 'string') node.textContent = value;
    });
    qsa('[data-i18n-aria-label]').forEach((node) => {
      const key = node.getAttribute('data-i18n-aria-label');
      const value = I18N[lang][key];
      if (typeof value === 'string') node.setAttribute('aria-label', value);
    });
    qsa('[data-i18n-label]').forEach((node) => {
      const key = node.getAttribute('data-i18n-label');
      const value = I18N[lang][key];
      if (typeof value === 'string') node.setAttribute('data-label', value);
    });
  }

  /* ---------- demo: team grouping ---------- */

  const teamOrder = ['red', 'blue', 'green', 'yellow'];
  const demoShell = qs('.demo-shell');
  const rosterTable = qs('#roster-table');
  const rosterBody = rosterTable ? qs('tbody', rosterTable) : null;
  const groupButton = qs('#group-btn');
  const groupButtonLabel = qs('#group-btn-label');
  const groupButtonKeys = groupButton ? qs('.btn-keys', groupButton) : null;
  const statusText = qs('#status-text');
  const repeatBadge = rosterTable ? qs('.repeat-badge', rosterTable) : null;
  const repeatNote = qs('#repeat-note');
  let originalRows = rosterBody ? qsa('tr.player-row', rosterBody) : [];
  let grouped = false;
  let settleTimer = 0;

  function buildDivider(team, count) {
    const row = doc.createElement('tr');
    row.className = 'team-divider team-' + team;
    row.dataset.team = team;
    const cell = doc.createElement('th');
    cell.colSpan = 7;
    cell.scope = 'colgroup';
    const swatch = doc.createElement('span');
    swatch.className = 'team-swatch team-' + team;
    swatch.setAttribute('aria-hidden', 'true');
    const name = doc.createElement('span');
    name.className = 'divider-name';
    name.textContent = t('team.' + team + 'Team');
    const total = doc.createElement('span');
    total.className = 'divider-count';
    total.textContent = t('demo.teamCount', { n: count });
    cell.append(swatch, name, total);
    row.append(cell);
    return row;
  }

  function syncTeamCells() {
    originalRows.forEach((row) => {
      const label = qs('.team-name', row);
      if (!label) return;
      label.textContent = grouped ? t('team.' + row.dataset.team) : '—';
    });
  }

  function syncStatusAndButton() {
    if (statusText) {
      const key = grouped ? 'demo.groupedStatus' : 'demo.rawStatus';
      statusText.setAttribute('data-i18n', key);
      statusText.textContent = t(key);
    }
    if (groupButton) {
      groupButton.setAttribute('aria-pressed', grouped ? 'true' : 'false');
    }
    if (groupButtonLabel) {
      const key = grouped ? 'demo.ungroupBtn' : 'demo.groupBtn';
      groupButtonLabel.setAttribute('data-i18n', key);
      groupButtonLabel.textContent = t(key);
    }
    if (groupButtonKeys) groupButtonKeys.hidden = grouped;
    if (repeatBadge) repeatBadge.hidden = !grouped;
    if (repeatNote) repeatNote.hidden = !grouped;
  }

  function updateDemoUI() {
    if (demoShell) demoShell.classList.toggle('is-grouped', grouped);
    if (rosterTable) rosterTable.classList.toggle('is-grouped', grouped);
    syncTeamCells();
    syncStatusAndButton();
    if (grouped) refreshDividers();
  }

  function refreshDividers() {
    if (!rosterBody) return;
    qsa('tr.team-divider', rosterBody).forEach((divider) => {
      const team = divider.dataset.team;
      const name = qs('.divider-name', divider);
      const count = qs('.divider-count', divider);
      const total = originalRows.filter((row) => row.dataset.team === team).length;
      if (name) name.textContent = t('team.' + team + 'Team');
      if (count) count.textContent = t('demo.teamCount', { n: total });
    });
  }

  function animateRows() {
    if (!demoShell || !rosterBody || motionQuery.matches) return;
    demoShell.classList.remove('is-settling');
    void demoShell.offsetWidth;
    qsa(':scope > *', rosterBody).forEach((node, index) => {
      node.style.setProperty('--row-delay', Math.min(index * 22, 260) + 'ms');
    });
    demoShell.classList.add('is-settling');
    window.clearTimeout(settleTimer);
    settleTimer = window.setTimeout(() => demoShell.classList.remove('is-settling'), 920);
  }

  function groupTeams() {
    if (!rosterBody || grouped) return;
    grouped = true;
    qsa('tr.team-divider', rosterBody).forEach((node) => node.remove());
    teamOrder.forEach((team) => {
      const members = originalRows.filter((row) => row.dataset.team === team);
      rosterBody.append(buildDivider(team, members.length));
      members.forEach((row) => rosterBody.append(row));
    });
    updateDemoUI();
    animateRows();
  }

  function ungroupTeams() {
    if (!rosterBody || !grouped) return;
    grouped = false;
    qsa('tr.team-divider', rosterBody).forEach((node) => node.remove());
    originalRows.forEach((row) => rosterBody.append(row));
    updateDemoUI();
    animateRows();
  }

  if (groupButton) {
    groupButton.addEventListener('click', () => {
      if (grouped) ungroupTeams();
      else groupTeams();
    });
  }

  /* ---------- demo: tabs ---------- */

  const tabs = qsa('.demo-tabs [role="tab"]');
  const panels = tabs.map((tab) => doc.getElementById(tab.getAttribute('aria-controls')));

  function selectTab(index, moveFocus) {
    tabs.forEach((tab, tabIndex) => {
      const active = tabIndex === index;
      tab.setAttribute('aria-selected', active ? 'true' : 'false');
      tab.tabIndex = active ? 0 : -1;
      if (panels[tabIndex]) panels[tabIndex].hidden = !active;
    });
    if (moveFocus && tabs[index]) tabs[index].focus();
  }

  tabs.forEach((tab, index) => {
    tab.addEventListener('click', () => selectTab(index, false));
    tab.addEventListener('keydown', (event) => {
      let next = null;
      if (event.key === 'ArrowRight' || event.key === 'ArrowDown') next = (index + 1) % tabs.length;
      else if (event.key === 'ArrowLeft' || event.key === 'ArrowUp') next = (index - 1 + tabs.length) % tabs.length;
      else if (event.key === 'Home') next = 0;
      else if (event.key === 'End') next = tabs.length - 1;
      if (next === null) return;
      event.preventDefault();
      selectTab(next, true);
    });
  });
  if (tabs.length) selectTab(0, false);

  /* ---------- demo: threat breakdown ---------- */

  const threatPlayers = {
    VerdeVortex: {
      team: 'red', score: 82, band: 'band.extreme',
      values: ['8.4', '3.1', '4.6', '412', '17', '×5'],
      bars: [84, 62, 58, 82, 57, 50]
    },
    NorthPixel: {
      team: 'blue', score: 67, band: 'band.high',
      values: ['5.7', '2.4', '3.6', '233', '9', '—'],
      bars: [57, 48, 45, 47, 30, 0]
    },
    AsterBloom: {
      team: 'yellow', score: 18, band: 'band.low',
      values: ['0.9', '0.7', '1.1', '76', '0', '—'],
      bars: [9, 14, 14, 15, 0, 0]
    }
  };
  let selectedThreat = 'VerdeVortex';

  const threatName = qs('#threat-name');
  const threatScore = qs('#threat-score');
  const threatBand = qs('#threat-band');
  const threatSwatch = qs('#threat-swatch');
  const threatTeamLabel = qs('#threat-team-label');
  const threatFill = qs('#threat-fill');
  const threatMarker = qs('#threat-marker');
  const threatSignalRows = qsa('#threat-signals li');
  const threatChips = qsa('[data-player-chip]');

  function renderThreat(name) {
    const player = threatPlayers[name];
    if (!player) return;
    selectedThreat = name;
    if (threatName) threatName.textContent = name;
    if (threatScore) threatScore.textContent = String(player.score);
    if (threatBand) {
      threatBand.setAttribute('data-i18n', player.band);
      threatBand.dataset.band = player.band.replace('band.', '');
      threatBand.textContent = t(player.band);
    }
    if (threatFill) threatFill.style.width = player.score + '%';
    if (threatMarker) threatMarker.style.left = player.score + '%';
    if (threatSwatch) threatSwatch.className = 'team-swatch team-' + player.team;
    if (threatTeamLabel) {
      threatTeamLabel.setAttribute('data-i18n', 'team.' + player.team);
      threatTeamLabel.textContent = t('team.' + player.team);
    }
    threatSignalRows.forEach((row, index) => {
      const value = qs('em', row);
      const bar = qs('i b', row);
      if (value) value.textContent = player.values[index] || '—';
      if (bar) bar.style.width = (player.bars[index] || 0) + '%';
    });
    threatChips.forEach((chip) => {
      chip.setAttribute('aria-pressed', chip.getAttribute('data-player-chip') === name ? 'true' : 'false');
    });
  }

  threatChips.forEach((chip) => {
    chip.addEventListener('click', () => renderThreat(chip.getAttribute('data-player-chip')));
  });

  /* ---------- language ---------- */

  function syncDemoLanguage() {
    updateDemoUI();
    renderThreat(selectedThreat);
  }

  function setLanguage(next, options) {
    const settings = options || {};
    lang = supported.indexOf(next) !== -1 ? next : 'tr';
    root.lang = lang;
    root.setAttribute('data-lang', lang);
    applyTranslations();
    applyMeta();
    qsa('[data-lang-btn]').forEach((button) => {
      const active = button.getAttribute('data-lang-btn') === lang;
      button.setAttribute('aria-pressed', active ? 'true' : 'false');
    });
    syncDemoLanguage();
    if (settings.persist !== false) writeStoredLanguage(lang);
    if (settings.announce) {
      const liveStatus = doc.getElementById('lang-status');
      if (liveStatus) liveStatus.textContent = t('lang.changed');
    }
  }

  qsa('[data-lang-btn]').forEach((button) => {
    button.addEventListener('click', () => {
      const next = button.getAttribute('data-lang-btn');
      if (next !== lang) setLanguage(next, { persist: true, announce: true });
    });
  });

  /* ---------- reveal + reduced motion ---------- */

  let revealObserver = null;
  let revealFallback = 0;

  function revealAll() {
    qsa('.reveal').forEach((node) => node.classList.add('is-visible'));
  }

  function setupReveal() {
    window.clearTimeout(revealFallback);
    if (revealObserver) {
      revealObserver.disconnect();
      revealObserver = null;
    }
    if (motionQuery.matches) {
      root.classList.remove('motion-ok');
      revealAll();
      return;
    }
    root.classList.add('motion-ok');
    if ('IntersectionObserver' in window) {
      revealObserver = new IntersectionObserver((entries) => {
        entries.forEach((entry) => {
          if (!entry.isIntersecting) return;
          entry.target.classList.add('is-visible');
          revealObserver.unobserve(entry.target);
        });
      }, { rootMargin: '0px 0px -6% 0px', threshold: 0.04 });
      qsa('.reveal').forEach((node) => revealObserver.observe(node));
    } else {
      revealAll();
    }
    revealFallback = window.setTimeout(revealAll, 2400);
  }

  function handleMotionChange() {
    setupReveal();
  }

  if (typeof motionQuery.addEventListener === 'function') {
    motionQuery.addEventListener('change', handleMotionChange);
  } else if (typeof motionQuery.addListener === 'function') {
    motionQuery.addListener(handleMotionChange);
  }

  /* ---------- init ---------- */

  setLanguage(detectLanguage(), { persist: false, announce: false });
  setupReveal();
})();
