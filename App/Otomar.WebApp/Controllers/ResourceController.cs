using Microsoft.AspNetCore.Mvc;

namespace Otomar.WebApp.Controllers
{
    [Route("kaynaklar")]
    public class ResourceController : Controller
    {
        [HttpGet("gizlilik-politikasi")]
        public IActionResult PrivacyPolicy()
        {
            string str = @"Mağazamızda verilen tüm hizmetler, Mersinli, 2822. Sk. No:30, 35110 Konak/İzmir adresinde kayıtlı OTOMAR Otomobilcilik Yedek Parça San. Ltd. Şti. firmamıza aittir ve firmamız tarafından işletilir.
<br/>
Firmamız, çeşitli amaçlarla kişisel veriler toplayabilir. Aşağıda, toplanan kişisel verilerin nasıl ve ne şekilde toplandığı, bu verilerin nasıl ve ne şekilde korunduğu belirtilmiştir.
<br/>
Üyelik veya Mağazamız üzerindeki çeşitli form ve anketlerin doldurulması suretiyle üyelerin kendileriyle ilgili bir takım kişisel bilgileri (isim-soy isim, firma bilgileri, telefon, adres veya e-posta adresleri gibi) Mağazamız tarafından işin doğası gereği toplanmaktadır.
<br/>
Firmamız bazı dönemlerde müşterilerine ve üyelerine kampanya bilgileri, yeni ürünler hakkında bilgiler, promosyon teklifleri gönderebilir. Üyelerimiz bu gibi bilgileri alıp almama konusunda her türlü seçimi üye olurken yapabilir, sonrasında üye girişi yaptıktan sonra hesap bilgileri bölümünden bu seçimi değiştirilebilir ya da kendisine gelen bilgilendirme iletisindeki linkle bildirim yapabilir.
<br/>
Mağazamız üzerinden veya eposta ile gerçekleştirilen onay sürecinde, üyelerimiz tarafından mağazamıza elektronik ortamdan iletilen kişisel bilgiler, Üyelerimiz ile yaptığımız ""Kullanıcı Sözleşmesi"" ile belirlenen amaçlar ve kapsam dışında üçüncü kişilere açıklanmayacaktır.
<br/>
Sistemle ilgili sorunların tanımlanması ve verilen hizmet ile ilgili çıkabilecek sorunların veya uyuşmazlıkların hızla çözülmesi için, Firmamız, üyelerinin IP adresini kaydetmekte ve bunu kullanmaktadır. IP adresleri, kullanıcıları genel bir şekilde tanımlamak ve kapsamlı demografik bilgi toplamak amacıyla da kullanılabilir.
<br/>
Firmamız, Üyelik Sözleşmesi ile belirlenen amaçlar ve kapsam dışında da, talep edilen bilgileri kendisi veya işbirliği içinde olduğu kişiler tarafından doğrudan pazarlama yapmak amacıyla kullanabilir.  Kişisel bilgiler, gerektiğinde kullanıcıyla temas kurmak için de kullanılabilir. Firmamız tarafından talep edilen bilgiler veya kullanıcı tarafından sağlanan bilgiler veya Mağazamız üzerinden yapılan işlemlerle ilgili bilgiler; Firmamız ve işbirliği içinde olduğu kişiler tarafından, ""Üyelik Sözleşmesi"" ile belirlenen amaçlar ve kapsam dışında da, üyelerimizin kimliği ifşa edilmeden çeşitli istatistiksel değerlendirmeler, veri tabanı oluşturma ve pazar araştırmalarında kullanılabilir.
<br/>
Firmamız, gizli bilgileri kesinlikle özel ve gizli tutmayı, bunu bir sır saklama yükümü olarak addetmeyi ve gizliliğin sağlanması ve sürdürülmesi, gizli bilginin tamamının veya herhangi bir kısmının kamu alanına girmesini veya yetkisiz kullanımını veya üçüncü bir kişiye ifşasını önlemek için gerekli tüm tedbirleri almayı ve gerekli özeni göstermeyi taahhüt etmektedir.

<br/>
KREDİ KARTI GÜVENLİĞİ

<br/>
Firmamız, alışveriş sitelerimizden alışveriş yapan kredi kartı sahiplerinin güvenliğini ilk planda tutmaktadır. Kredi kartı bilgileriniz hiçbir şekilde sistemimizde saklanmamaktadır.
 <br/>
İşlemler sürecine girdiğinizde güvenli bir sitede olduğunuzu anlamak için dikkat etmeniz gereken iki şey vardır. Bunlardan biri tarayıcınızın en alt satırında bulunan bir anahtar ya da kilit simgesidir. Bu güvenli bir internet sayfasında olduğunuzu gösterir ve her türlü bilgileriniz şifrelenerek korunur. Bu bilgiler, ancak satış işlemleri sürecine bağlı olarak ve verdiğiniz talimat istikametinde kullanılır. Alışveriş sırasında kullanılan kredi kartı ile ilgili bilgiler alışveriş sitelerimizden bağımsız olarak 128 bit SSL (Secure Sockets Layer) protokolü ile şifrelenip sorgulanmak üzere ilgili bankaya ulaştırılır. Kartın kullanılabilirliği onaylandığı takdirde alışverişe devam edilir. Kartla ilgili hiçbir bilgi tarafımızdan görüntülenemediğinden ve kaydedilmediğinden, üçüncü şahısların herhangi bir koşulda bu bilgileri ele geçirmesi engellenmiş olur.
<br/>
Online olarak kredi kartı ile verilen siparişlerin ödeme/fatura/teslimat adresi bilgilerinin güvenilirliği firmamiz tarafından Kredi Kartları Dolandırıcılığı'na karşı denetlenmektedir. Bu yüzden, alışveriş sitelerimizden ilk defa sipariş veren müşterilerin siparişlerinin tedarik ve teslimat aşamasına gelebilmesi için öncelikle finansal ve adres/telefon bilgilerinin doğruluğunun onaylanması gereklidir. Bu bilgilerin kontrolü için gerekirse kredi kartı sahibi müşteri ile veya ilgili banka ile irtibata geçilmektedir.
<br/>
Üye olurken verdiğiniz tüm bilgilere sadece siz ulaşabilir ve siz değiştirebilirsiniz. Üye giriş bilgilerinizi güvenli koruduğunuz takdirde başkalarının sizinle ilgili bilgilere ulaşması ve bunları değiştirmesi mümkün değildir. Bu amaçla, üyelik işlemleri sırasında 128 bit SSL güvenlik alanı içinde hareket edilir. Bu sistem kırılması mümkün olmayan bir uluslararası bir şifreleme standardıdır.
<br/>
Bilgi hattı veya müşteri hizmetleri servisi bulunan ve açık adres ve telefon bilgilerinin belirtildiği İnternet alışveriş siteleri günümüzde daha fazla tercih edilmektedir. Bu sayede aklınıza takılan bütün konular hakkında detaylı bilgi alabilir, online alışveriş hizmeti sağlayan firmanın güvenirliği konusunda daha sağlıklı bilgi edinebilirsiniz.
 <br/>
Not: İnternet alışveriş sitelerinde firmanın açık adresinin ve telefonun yer almasına dikkat edilmesini tavsiye ediyoruz. Alışveriş yapacaksanız alışverişinizi yapmadan ürünü aldığınız mağazanın bütün telefon / adres bilgilerini not edin. Eğer güvenmiyorsanız alışverişten önce telefon ederek teyit edin. Firmamıza ait tüm online alışveriş sitelerimizde firmamıza dair tüm bilgiler ve firma yeri belirtilmiştir.
 <br/>

MAİL ORDER KREDİ KART BİLGİLERİ GÜVENLİĞİ

 <br/>

Kredi kartı mail-order yöntemi ile bize göndereceğiniz kimlik ve kredi kart bilgileriniz firmamız tarafından gizlilik prensibine göre saklanacaktır. Bu bilgiler olası banka ile oluşubilecek kredi kartından para çekim itirazlarına karşı 60 gün süre ile bekletilip daha sonrasında imha edilmektedir. Sipariş ettiğiniz ürünlerin bedeli karşılığında bize göndereceğiniz tarafınızdan onaylı mail-order formu bedeli dışında herhangi bir bedelin kartınızdan çekilmesi halinde doğal olarak bankaya itiraz edebilir ve bu tutarın ödenmesini engelleyebileceğiniz için bir risk oluşturmamaktadır.

 <br/>

ÜÇÜNCÜ TARAF WEB SİTELERİ VE UYGULAMALAR

 <br/>

Mağazamız,  web sitesi dâhilinde başka sitelere link verebilir. Firmamız, bu linkler vasıtasıyla erişilen sitelerin gizlilik uygulamaları ve içeriklerine yönelik herhangi bir sorumluluk taşımamaktadır. Firmamıza ait sitede yayınlanan reklamlar, reklamcılık yapan iş ortaklarımız aracılığı ile kullanıcılarımıza dağıtılır. İş bu sözleşmedeki Gizlilik Politikası Prensipleri, sadece Mağazamızın kullanımına ilişkindir, üçüncü taraf web sitelerini kapsamaz.

 <br/>

İSTİSNAİ HALLER

 <br/>

Aşağıda belirtilen sınırlı hallerde Firmamız, işbu ""Gizlilik Politikası"" hükümleri dışında kullanıcılara ait bilgileri üçüncü kişilere açıklayabilir. Bu durumlar sınırlı sayıda olmak üzere;
<br/>
1.Kanun, Kanun Hükmünde Kararname, Yönetmelik v.b. yetkili hukuki otorite tarafından çıkarılan ve yürürlülükte olan hukuk kurallarının getirdiği zorunluluklara uymak;
<br/>
2.Mağazamızın kullanıcılarla akdettiği ""Üyelik Sözleşmesi""'nin ve diğer sözleşmelerin gereklerini yerine getirmek ve bunları uygulamaya koymak amacıyla;
<br/>
3.Yetkili idari ve adli otorite tarafından usulüne göre yürütülen bir araştırma veya soruşturmanın yürütümü amacıyla kullanıcılarla ilgili bilgi talep edilmesi;
<br/>
4.Kullanıcıların hakları veya güvenliklerini korumak için bilgi vermenin gerekli olduğu hallerdir.

 <br/>

E-POSTA GÜVENLİĞİ

 <br/>

Mağazamızın Müşteri Hizmetleri’ne, herhangi bir siparişinizle ilgili olarak göndereceğiniz e-postalarda, asla kredi kartı numaranızı veya şifrelerinizi yazmayınız. E-postalarda yer alan bilgiler üçüncü şahıslar tarafından görülebilir. Firmamız e-postalarınızdan aktarılan bilgilerin güvenliğini hiçbir koşulda garanti edemez.

 <br/>

TARAYICI ÇEREZLERİ
<br/>

Firmamız, mağazamızı ziyaret eden kullanıcılar ve kullanıcıların web sitesini kullanımı hakkındaki bilgileri teknik bir iletişim dosyası (Çerez-Cookie) kullanarak elde edebilir. Bahsi geçen teknik iletişim dosyaları, ana bellekte saklanmak üzere bir internet sitesinin kullanıcının tarayıcısına (browser) gönderdiği küçük metin dosyalarıdır. Teknik iletişim dosyası site hakkında durum ve tercihleri saklayarak İnternet'in kullanımını kolaylaştırır.
<br/>
Teknik iletişim dosyası,  siteyi kaç kişinin ziyaret ettiğini, bir kişinin siteyi hangi amaçla, kaç kez ziyaret ettiğini ve ne kadar sitede kaldıkları hakkında istatistiksel bilgileri elde etmeye ve kullanıcılar için özel tasarlanmış kullanıcı sayfalarından  dinamik olarak reklam ve içerik üretilmesine yardımcı olur. Teknik iletişim dosyası, ana bellekte veya e-postanızdan veri veya başkaca herhangi bir kişisel bilgi almak için tasarlanmamıştır. Tarayıcıların pek çoğu başta teknik iletişim dosyasını kabul eder biçimde tasarlanmıştır ancak kullanıcılar dilerse teknik iletişim dosyasının gelmemesi veya teknik iletişim dosyasının gönderildiğinde uyarı verilmesini sağlayacak biçimde ayarları değiştirebilirler.
<br/>
Firmamız, işbu ""Gizlilik Politikası"" hükümlerini dilediği zaman sitede yayınlamak veya kullanıcılara elektronik posta göndermek veya sitesinde yayınlamak suretiyle değiştirebilir. Gizlilik Politikası hükümleri değiştiği takdirde, yayınlandığı tarihte yürürlük kazanır.
<br/>
Gizlilik politikamız ile ilgili her türlü soru ve önerileriniz için bilgi@otomar.com.tr adresine email gönderebilirsiniz. Firmamız’a ait aşağıdaki iletişim bilgilerinden ulaşabilirsiniz.
<br/>
Firma Ünvanı: OTOMAR Otomobilcilik Yedek Parça San. Ltd. Şti.
<br/>
Adres: Mersinli, 2822. Sk. No:30, 35110 Konak/İzmir
<br/>
Eposta: bilgi@otomar.com.tr
<br/>
Tel:  (0232) 433 73 37
 ";
            return View((object)str);
        }

        [HttpGet("iade-ve-degisim")]
        public IActionResult RefundAndExchange()
        {
            string str = @"Türkiye içerisine gönderilen siparişler için iade ve değişim
<br/>
Geri gönderilerinizi sadece anlaşmalı olduğumuz DHL eCommerce ile yapabilirsiniz.
<br/>
İadeler için kargo gönderi kodu : DHL eCommerce - 19522541
<br/>
Ürün iade istekleri Tüketicinin Korunması Kanunu ve Yönetmeliği hükümleri esas alınarak aşağıdaki kriterler dâhilinde yapılmaktadır.
<br/>
Kanunen internetten yapılan satışlarda (mesafeli sözleşmeler ile satışlarda) tüketicilerin teslim aldığı tarihten itibaren 14 gün içerisinde hiçbir hukuki ve cezai sorumluluk üstlenmeksizin ve hiç bir gerekçe göstermeksizin malı reddederek ürünü iade hakkı mevcuttur.
<br/>
İade ettiğiniz ürünleriniz tarafımıza ulaştıktan sonra, ürünleriniz kabul işlemini takiben işlem sırasına göre incelenmektedir. iade ürünleriniz tarafımıza ulaştıktan sonra 7 iş günü içerisinde iade tutarınız tarafınıza ödenecektir. İadenizin geri ödemesi sitemizde sipariş verirken gerçekleştirdiğiniz ödeme yöntemine göre(Peşin/taksit) geri ödenecektir. İade işlemleri otomatik sistemler tarafından yapıldığından geri ödeme de sistemde oluşturmuş olduğunuz siparişin ödeme türüne göre sonuçlanmaktadır. Siparişlerde taksitli ödeme metodu kullanıldıysa iade taksitli olarak; peşin ödeme metodu kullanıldıysa da iade karta peşin olarak geri ödenmektedir.
<br/>
Nasıl Değişim Yapabilirim?
<br/>
İnternet Mağazamızdan aldığınız ürünleri faturası ile birlikte göndermeniz durumunda değişimi yapılmaktadır.
<br/>
Nasıl İade Yapabilirim?
<br/>
1. otomar.com.tr adresinden satın almış olduğunuz kullanılmamış ürünleri, 14 gün içinde iade edebilirsiniz (Elektronik ürünler hariç). Bunun için;
<br/>
İade Adresimiz:
<br/>
OTOMAR Otomobilcilik Yedek Parça San. Ltd. Şti.
<br/>
Mersinli, 2822. Sk. No:30, 35110 Konak/İzmir";
            return View((object)str);
        }

        [HttpGet("satis-sozlesmesi")]
        public IActionResult SalesAgreement()
        {
            string str = @"MESAFELİ SATIŞ SÖZLEŞMESİ
<br/>
İşbu sözleşme 13.06.2003 tarih ve 25137 sayılı Resmi Gazetede yayınlanan Mesafeli Sözleşmeler Uygulama Usul ve Esasları Hakkında Yönetmelik gereği internet üzerinden gerçekleştiren satışlar için sözleşme yapılması zorunluluğuna istinaden düzenlenmiş olup, maddeler halinde aşağıdaki gibidir.
<br/>
MADDE 1 - KONU
<br/>
İşbu sözleşmenin konusu, SATICI'nın, ALICI'ya satışını yaptığı, aşağıda nitelikleri ve satış fiyatı belirtilen ürünün satışı ve teslimi ile ilgili olarak 4077 sayılı Tüketicilerin Korunması Hakkındaki Kanun-Mesafeli Sözleşmeleri Uygulama Esas ve Usulleri Hakkında Yönetmelik hükümleri gereğince tarafların hak ve yükümlülüklerinin kapsamaktadır.
<br/>
MADDE 2.1 - SATICI BİLGİLERİ
<br/>
Ünvan:  OTOMAR Otomobilcilik Yedek Parça San. Ltd. Şti.
(Bundan sonra otomar.com.tr olarak anılacaktır)
<br/>
Adres: Mersinli, 2822. Sk. No:30, 35110 Konak/İzmir
<br/>
Telefon: (0232) 433 73 37
<br/>
Email: bilgi@otomar.com.tr
<br/>
MADDE 2.2 - ALICI BİLGİLERİ
<br/>
Müşteri olarak otomar.com.tr alışveriş sitesine üye olan kişi.
Üye olurken kullanılan adres ve iletişim bilgileri esas alınır.
<br/>
MADDE 3 - SÖZLEŞME KONUSU ÜRÜN BİLGİLERİ
<br/>
Malın / Ürünün / Hizmetin türü, miktarı, marka/modeli, rengi, adedi, satış bedeli, ödeme şekli, siparişin sonlandığı andaki bilgilerden oluşmaktadır
<br/>
MADDE 4 - GENEL HÜKÜMLER
<br/>
4.1 - ALICI, Madde 3'te belirtilen sözleşme konusu ürün veya ürünlerin temel nitelikleri, satış fiyatı ve ödeme şekli ile teslimata ilişkin tüm ön bilgileri okuyup bilgi sahibi olduğunu ve elektronik ortamda gerekli teyidi verdiğini beyan eder.
<br/>
4.2 - Sözleşme konusu ürün veya ürünler, yasal 30 günlük süreyi aşmamak koşulu ile her bir ürün için ALICI'nın yerleşim yerinin uzaklıgına bağlı olarak ön bilgiler içinde açıklanan süre içinde ALICI veya gösterdiği adresteki kişi/kuruluşa teslim edilir. Bu süre ALICI’Ya daha önce bildirilmek kaydıyla en fazla 10 gün daha uzatılabilir.
<br/>
4.3 - Sözleşme konusu ürün, ALICI'dan başka bir kişi/kuruluşa teslim edilecek ise, teslim edilecek kişi/kuruluşun teslimatı kabul etmemesininden otomar.com.tr sorumlu tutulamaz.
<br/>
4.4 – otomar.com.tr , sözleşme konusu ürünün saglam, eksiksiz, siparişte belirtilen niteliklere uygun ve varsa garanti belgeleri ve kullanım klavuzları ile teslim edilmesinden sorumludur.
<br/>
4.5 - Sözleşme konusu ürünün teslimatı için işbu sözleşmenin imzalı nüshasının otomar.com.tr'ye ulaştırılmış olması ve bedelinin ALICI'nın tercih ettigi ödeme şekli ile ödenmiş olması şarttır. Herhangi bir nedenle ürün bedeli ödenmez veya banka kayıtlarında iptal edilir ise, otomar.com.tr ürünün teslimi yükümlülügünden kurtulmuş kabul edilir.
<br/>
4.6- Ürünün tesliminden sonra ALICI'ya ait kredi kartının ALICI'nın kusurundan kaynaklanmayan bir şekilde yetkisiz kişilerce haksız veya hukuka aykırı olarak kullanılması nedeni ile ilgili banka veya finans kuruluşun ürün bedelini otomar.com.tr'ye ödememesi halinde, ALICI'nın kendisine teslim edilmiş olması kaydıyla ürünün 3 gün içinde otomar.com.tr'ye gönderilmesi zorunludur. Bu takdirde nakliye giderleri ALICI'ya aittir.
<br/>
4.7- otomar.com.tr mücbir sebepler veya nakliyeyi engelleyen hava muhalefeti, ulaşımın kesilmesi gibi olağanüstü durumlar nedeni ile sözleşme konusu ürünü süresi içinde teslim edemez ise, durumu ALICI'ya bildirmekle yükümlüdür. Bu takdirde ALICI siparişin iptal edilmesini, sözleşme konusu ürünün varsa emsali ile değiştirilmesini, ve/veya teslimat süresinin engelleyici durumun ortadan kalkmasına kadar ertelenmesi haklarından birini kullanabilir. ALICI'nın siparişi iptal etmesi halinde ödedigi tutar 10 gün içinde kendisine nakten ve defaten ödenir.
<br/>
4.8- Garanti belgesi ile satılan ürünlerden olan veya olmayan ürünlerin arızalı veya bozuk olanlar, garanti şartları içinde gerekli onarımın yapılması için otomar.com.tr'ye gönderilebilir, bu takdirde kargo giderleri otomar.com.tr tarafından karşılanacaktır.
<br/>
4.9- İşbu sözleşme, ALICI tarafından imzalanıp otomar.com.tr faks veya posta yoluyla ulaştırılmasından sonra geçerlilik kazanır.
<br/>
MADDE 5 - CAYMA HAKKI
<br/>
ALICI, sözleşme konusu ürünün kendisine veya gösterdiği adresteki kişi/kuruluşa tesliminden itibaren (14) ondört gün içinde cayma hakkına sahiptir. Cayma hakkının kullanılması için bu süre içinde otomar.com.tr'ye faks, email veya telefon ile bildirimde bulunulması ve ürünün ilgili madde hükümleri çercevesinde kullanılmamış olması şarttır. Bu hakkın kullanılması halinde, 3. kişiye veya ALICI'ya teslim edilen ürünün otomar.com.tr 'ye gönderildigine ilişkin kargo teslim tutanağı örnegi ile fatura aslının iadesi zorunludur. Bu belgelerin ulaşmasını takip eden 14 gün içinde ürün bedeli ALICI'ya iade edilir. Fatura aslı gönderilmez ise KDV ve varsa sair yasal yükümlülükler iade edilemez. Cayma hakkı nedeni ile iade edilen ürünün kargo bedeli ALICI tarafından karşılanır.
<br/>
Ayrıca, tüketicinin özel istek ve talepleri uyarınca üretilen veya üzerinde değişiklik ya da ilaveler yapılarak kişiye özel hale getirilen mallarda tüketici cayma hakkını kullanamaz.
<br/>
Ödemenin kredi kartı veya benzeri bir ödeme kartı ile yapılması halinde tüketici, kartın kendi rızası dışında ve hukuka aykırı biçimde kullanıldığı gerekçesiyle ödeme işleminin iptal edilmesini talep edebilir. Bu halde, kartı çıkaran kuruluş itirazın kendisine bildirilmesinden itibaren 10 gün içinde ödeme tutarını tüketiciye iade eder.
<br/>
İşbu sözleşmenin uygulanmasında, Sanayi ve Ticaret Bakanlığınca ilan edilen değere kadar Tüketici Hakem Heyetleri ile otomar.com.tr'nin yerleşim yerindeki Tüketici Mahkemeleri yetkilidir.
<br/>
Siparişin sonuçlanması durumunda ALICI işbu sözleşmenin tüm koşullarını kabul etmiş sayılacaktır.";
            return View((object)str);
        }

        [HttpGet("gizlilik-ve-guvenlik")]
        public IActionResult PrivacyAndSecurity()
        {
            string str = @"Tarafımıza vermiş olduğunuz tüm bilgiler hiçbir koşulda üçüncü şahıslara iletilmez. Bu bilgiler maddi kar sağlama amacı ile satılmaz. Tüm kişisel kimlik bilgileriniz gizli tutulur.
<br/>
Site sayfalarını, şahsi herhangi bir bilgi vermeden ziyaret edebilir, ürünlerimiz hakkında bilgi alabilirsiniz.
<br/>
Web sitemizden alışveriş yaptığınız takdirde bazı kişisel bilgileriniz istenmektedir. Bu bilgiler veri tabanımızda saklanmaktadır. Veri tabanımızda saklanan kişisel bilgileriniz kesinlikle başka kişi ve kuruluşlara verilmez ve sadece web sitemizden daha kolay ve uygun şekilde faydalanabilmeniz amacıyla kullanılır.
<br/>
Web sitemizden yapılan alışverişlerinizde ödeme seçeneklerinde ""Kredi Kartı” seçeneği için gerekli olan kredi kartı numarası ve diğer kredi kartı bilgileri sistemimizde tutulmamakta ve istenmemektedir.
<br/>
Telif Hakları
<br/>
Sitemizdeki tüm materyalde sağlanan yasal haklar OTOMAR Otomobilcilik Yedek Parça San. Ltd. Şti. tarafından veya materyalin orijinal yaratıcısı tarafından tutulur. Burada belirtilenin dışında, hiçbir materyal kopya edilemez, tekrar üretilemez, dağıtılamaz, sergilenemez, yüklenemez, tekrar oynatılamaz, postalanamaz, verici yoluyla geçirilemez, dahilen, fakat limitlenmemiş olarak, elektronik, mekanik, fotokopik, kayıtlı veya herhangi başka bir şekilde çoğaltılamaz.
<br/>
Bu sitedeki materyallerin sadece sergileme, kopya , dagıtım, veya yükleme için kişisel, ticari olmayan kullanım için değiştirmeme ve materyaller için tüm yasal ve içerdiği diğer kişisel notların yasal sahibiyseniz izin verilebilir. Değilseniz, OTOMAR Otomobilcilik Yedek Parça San. Ltd. Şti. in izni olmadan, buradaki herşeyi diğer serverlara kopyalayamazsınız. Bu izin otomatik olarak bu şartlardan bir maddeyi bozduğunuz anda biter. Bu tahdit hususunda acilen yüklediğiniz veya resmettiğiniz materyali imha etmek zorundasınız. Bu sitedeki herhangi bir materyalin herhangibir imzasız kullanımı kopyalama yasaları , ticari yasalar, güvenlik ve tanıtım yasaları ve iletişim kanunları ve statülerine tecavüz eder.
<br/>
Kredi Kartları Güvenliği
<br/>
otomar.com.tr sitemizden alışveriş yapan kredi kartı sahiplerinin güvenliğini ilk planda tutmaktadır. Bu amaçla sitemiz <strong>SSL(Secure Sockets Layer)</strong> ile korunmaktadır.";
            return View((object)str);
        }

        [HttpGet("odeme-ve-teslimat")]
        public IActionResult PaymentAndDelivery()
        {
            string str = @".";
            return View((object)str);
        }

        [HttpGet("sartlar-ve-kosullar")]
        public IActionResult TermsAndConditions()
        {
            string str = @"Lütfen sitemizi kullanmadan evvel bu ‘site kullanım şartları’nı dikkatlice okuyunuz.
Bu alışveriş sitesini kullanan ve alışveriş yapan müşterilerimiz aşağıdaki şartları kabul etmiş varsayılmaktadır:
Sitemizdeki web sayfaları ve ona bağlı tüm sayfalar (‘site’) Mersinli, 2822. Sk. No:30, 35110 Konak/İzmir adresindeki OTOMAR Otomobilcilik Yedek Parça San. Ltd. Şti. firmasının malıdır ve onun tarafından işletilir. Sizler (‘Kullanıcı’) sitede sunulan tüm hizmetleri kullanırken aşağıdaki şartlara tabi olduğunuzu, sitedeki hizmetten yararlanmakla ve kullanmaya devam etmekle; Bağlı olduğunuz yasalara göre sözleşme imzalama hakkına, yetkisine ve hukuki ehliyetine sahip ve 18 yaşın üzerinde olduğunuzu, bu sözleşmeyi okuduğunuzu, anladığınızı ve sözleşmede yazan şartlarla bağlı olduğunuzu kabul etmiş sayılırsınız.
<br/>
İşbu sözleşme taraflara sözleşme konusu site ile ilgili hak ve yükümlülükler yükler ve taraflar işbu sözleşmeyi kabul ettiklerinde bahsi geçen hak ve yükümlülükleri eksiksiz, doğru, zamanında, işbu sözleşmede talep edilen şartlar dâhilinde yerine getireceklerini beyan ederler.
<br/>
1. SORUMLULUKLAR
<br/>
a.Firma, fiyatlar ve sunulan ürün ve hizmetler üzerinde değişiklik yapma hakkını her zaman saklı tutar.
<br/>
b.Firma, üyenin sözleşme konusu hizmetlerden, teknik arızalar dışında yararlandırılacağını kabul ve taahhüt eder.
<br/>
c.Kullanıcı, sitenin kullanımında tersine mühendislik yapmayacağını ya da bunların kaynak kodunu bulmak veya elde etmek amacına yönelik herhangi bir başka işlemde bulunmayacağını aksi halde ve 3. Kişiler nezdinde doğacak zararlardan sorumlu olacağını, hakkında hukuki ve cezai işlem yapılacağını peşinen kabul eder.
<br/>
d.Kullanıcı, site içindeki faaliyetlerinde, sitenin herhangi bir bölümünde veya iletişimlerinde genel ahlaka ve adaba aykırı, kanuna aykırı, 3. Kişilerin haklarını zedeleyen, yanıltıcı, saldırgan, müstehcen, pornografik, kişilik haklarını zedeleyen, telif haklarına aykırı, yasa dışı faaliyetleri teşvik eden içerikler üretmeyeceğini, paylaşmayacağını kabul eder. Aksi halde oluşacak zarardan tamamen kendisi sorumludur ve bu durumda ‘Site’ yetkilileri, bu tür hesapları askıya alabilir, sona erdirebilir, yasal süreç başlatma hakkını saklı tutar. Bu sebeple yargı mercilerinden etkinlik veya kullanıcı hesapları ile ilgili bilgi talepleri gelirse paylaşma hakkını saklı tutar.
<br/>
e.Sitenin üyelerinin birbirleri veya üçüncü şahıslarla olan ilişkileri kendi sorumluluğundadır.
<br/>
2.  FİKRİ MÜLKİYET HAKLARI
<br/>
2.1. İşbu Site’de yer alan ünvan, işletme adı, marka, patent, logo, tasarım, bilgi ve yöntem gibi tescilli veya tescilsiz tüm fikri mülkiyet hakları site işleteni ve sahibi firmaya veya belirtilen ilgilisine ait olup, ulusal ve uluslararası hukukun koruması altındadır. İşbu Site’nin ziyaret edilmesi veya bu Site’deki hizmetlerden yararlanılması söz konusu fikri mülkiyet hakları konusunda hiçbir hak vermez.
2.2. Site’de yer alan bilgiler hiçbir şekilde çoğaltılamaz, yayınlanamaz, kopyalanamaz, sunulamaz ve/veya aktarılamaz. Site’nin bütünü veya bir kısmı diğer bir internet sitesinde izinsiz olarak kullanılamaz.
<br/>
3. GİZLİ BİLGİ
<br/>
3.1. Firma, site üzerinden kullanıcıların ilettiği kişisel bilgileri 3. Kişilere açıklamayacaktır. Bu kişisel bilgiler; kişi adı-soyadı, adresi, telefon numarası, cep telefonu, e-posta adresi gibi Kullanıcı’yı tanımlamaya yönelik her türlü diğer bilgiyi içermekte olup, kısaca ‘Gizli Bilgiler’ olarak anılacaktır.
<br/>
3.2. Kullanıcı, sadece tanıtım, reklam, kampanya, promosyon, duyuru vb. pazarlama faaliyetleri kapsamında kullanılması ile sınırlı olmak üzere, Site’nin sahibi olan firmanın kendisine ait iletişim, portföy durumu ve demografik bilgilerini iştirakleri ya da bağlı bulunduğu grup şirketleri ile paylaşmasına muvafakat ettiğini kabul ve beyan eder. Bu kişisel bilgiler firma bünyesinde müşteri profili belirlemek, müşteri profiline uygun promosyon ve kampanyalar sunmak ve istatistiksel çalışmalar yapmak amacıyla kullanılabilecektir.
<br/>
3.3. Gizli Bilgiler, ancak resmi makamlarca usulü dairesinde bu bilgilerin talep edilmesi halinde ve yürürlükteki emredici mevzuat hükümleri gereğince resmi makamlara açıklama yapılmasının zorunlu olduğu durumlarda resmi makamlara açıklanabilecektir.
<br/>
4. GARANTİ VERMEME
<br/>
İşbu sözleşme maddesi uygulanabilir kanunun izin verdiği azami ölçüde geçerli olacaktır. Firma tarafından sunulan hizmetler ""olduğu gibi” ve ""mümkün olduğu” temelde sunulmakta ve pazarlanabilirlik, belirli bir amaca uygunluk veya ihlal etmeme konusunda tüm zımni garantiler de dâhil olmak üzere hizmetler veya uygulama ile ilgili olarak (bunlarda yer alan tüm bilgiler dâhil) sarih veya zımni, kanuni veya başka bir nitelikte hiçbir garantide bulunmamaktadır.
<br/>
5. KAYIT VE GÜVENLİK
<br/>
Kullanıcı, doğru, eksiksiz ve güncel kayıt bilgilerini vermek zorundadır. Aksi halde bu Sözleşme ihlal edilmiş sayılacak ve Kullanıcı bilgilendirilmeksizin hesap kapatılabilecektir.
Kullanıcı, site ve üçüncü taraf sitelerdeki şifre ve hesap güvenliğinden kendisi sorumludur. Aksi halde oluşacak veri kayıplarından ve güvenlik ihlallerinden veya donanım ve cihazların zarar görmesinden Firma sorumlu tutulamaz.
<br/>
6. MÜCBİR SEBEP
<br/>
Tarafların kontrolünde olmayan; tabii afetler, yangın, patlamalar, iç savaşlar, savaşlar, ayaklanmalar, halk hareketleri, seferberlik ilanı, grev, lokavt ve salgın hastalıklar, altyapı ve internet arızaları, elektrik kesintisi gibi sebeplerden (aşağıda birlikte ""Mücbir Sebep” olarak anılacaktır.) dolayı sözleşmeden doğan yükümlülükler taraflarca ifa edilemez hale gelirse, taraflar bundan sorumlu değildir. Bu sürede Taraflar’ın işbu Sözleşme’den doğan hak ve yükümlülükleri askıya alınır.
<br/>
7. SÖZLEŞMENİN BÜTÜNLÜĞÜ VE UYGULANABİLİRLİK
<br/>
İşbu sözleşme şartlarından biri, kısmen veya tamamen geçersiz hale gelirse, sözleşmenin geri kalanı geçerliliğini korumaya devam eder.
<br/>
8. SÖZLEŞMEDE YAPILACAK DEĞİŞİKLİKLER
<br/>
Firma, dilediği zaman sitede sunulan hizmetleri ve işbu sözleşme şartlarını kısmen veya tamamen değiştirebilir. Değişiklikler sitede yayınlandığı tarihten itibaren geçerli olacaktır. Değişiklikleri takip etmek Kullanıcı’nın sorumluluğundadır. Kullanıcı, sunulan hizmetlerden yararlanmaya devam etmekle bu değişiklikleri de kabul etmiş sayılır.
<br/>
9. TEBLİGAT
<br/>
İşbu Sözleşme ile ilgili taraflara gönderilecek olan tüm bildirimler, Firma’nın bilinen e.posta adresi ve kullanıcının üyelik formunda belirttiği e.posta adresi vasıtasıyla yapılacaktır. Kullanıcı, üye olurken belirttiği adresin geçerli tebligat adresi olduğunu, değişmesi durumunda 5 gün içinde yazılı olarak diğer tarafa bildireceğini, aksi halde bu adrese yapılacak tebligatların geçerli sayılacağını kabul eder.
<br/>
10. DELİL SÖZLEŞMESİ
<br/>
Taraflar arasında işbu sözleşme ile ilgili işlemler için çıkabilecek her türlü uyuşmazlıklarda Taraflar’ın defter, kayıt ve belgeleri ile ve bilgisayar kayıtları ve faks kayıtları 6100 sayılı Hukuk Muhakemeleri Kanunu uyarınca delil olarak kabul edilecek olup, kullanıcı bu kayıtlara itiraz etmeyeceğini kabul eder.
<br/>
11. UYUŞMAZLIKLARIN ÇÖZÜMÜ
<br/>
İşbu Sözleşme’nin uygulanmasından veya yorumlanmasından doğacak her türlü uyuşmazlığın çözümünde otomar.com.tr'nin yerleşim yerindeki Tüketici Mahkemeleri yetkilidir.";
            return View((object)str);
        }
    }
}