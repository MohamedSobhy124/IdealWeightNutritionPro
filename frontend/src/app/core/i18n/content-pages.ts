export type ContentPageSlug = 'about' | 'privacy' | 'terms' | 'shipping' | 'return-policy' | 'help';

export interface ContentPage {
  slug: ContentPageSlug;
  title: { en: string; ar: string };
  body: { en: string; ar: string };
}

export const CONTENT_PAGES: Record<ContentPageSlug, ContentPage> = {
  about: {
    slug: 'about',
    title: { en: 'About Us', ar: 'من نحن' },
    body: {
      en: `<h3>VISION</h3>
<p>At Ideal Weight, we envision becoming the pioneers of health and wellness in the Middle East by offering innovative supplements, nutritious snacks, and expert health guidance. We aim to empower individuals on their fitness journey through quality products and practical knowledge.</p>
<h3>MISSION</h3>
<p>Ideal Weight is dedicated to revolutionizing health and fitness in the Middle East by providing innovative products and services, educating customers through interactive experiences, and building a strong wellness community.</p>
<h3>OUR VALUES</h3>
<ul>
<li>Honesty</li>
<li>Open Communication</li>
<li>Clear Accountability and Objectives</li>
<li>Clear Strategic Plan</li>
<li>Positive and Friendly Environment</li>
<li>Encouraging Teamwork</li>
</ul>
<h3>BRANCHES: 2 in UAE</h3>
<p>Ideal Weight Retail, a trusted name in health and wellness since 2021, serves the UAE through two branches and continues to expand.</p>
<div class="branch-map-card">
<h4>Alshamkha Branch</h4>
<p>Alshamkha Mall, beside Alfardan Exchange, shop 38.<br/>
<a href="https://maps.app.goo.gl/9SU1q2p69vfRTkMn8?g_st=aw" target="_blank" rel="noopener noreferrer">View on Map</a></p>
<iframe loading="lazy" referrerpolicy="no-referrer-when-downgrade" allowfullscreen src="https://www.google.com/maps?q=Alshamkha+Mall+Abu+Dhabi&output=embed"></iframe>
</div>
<div class="branch-map-card">
<h4>Alwathba Branch</h4>
<p>Alwathba Mall, opposite Medicina Pharmacy, shop 26.<br/>
<a href="https://maps.app.goo.gl/12J3mbtkGsk6FKLp9?g_st=aw" target="_blank" rel="noopener noreferrer">View on Map</a></p>
<iframe loading="lazy" referrerpolicy="no-referrer-when-downgrade" allowfullscreen src="https://www.google.com/maps?q=Alwathba+Mall+Abu+Dhabi&output=embed"></iframe>
</div>
<p><strong>Contact:</strong> <a href="tel:+971507700559"><bdi dir="ltr">+971 50 770 0559</bdi></a></p>
<h3>About Ideal Weight</h3>
<p>Ideal Weight is a leading bodybuilding and nutrition supplements store in the UAE, with a full range of proteins, pre-workouts, weight management products, healthy snacks, and brands such as Adonis, Nano, and Applied Nutrition.</p>`,
      ar: `<h3>الرؤية</h3>
<p>في ايديال ويت، نطمح لأن نكون رواد الصحة والعافية في الشرق الأوسط من خلال تقديم مكملات مبتكرة ووجبات خفيفة مغذية وإرشاد صحي متخصص. نلتزم بتمكين الأفراد في رحلتهم نحو اللياقة والصحة الأفضل.</p>
<h3>الرسالة</h3>
<p>تلتزم ايديال ويت بإحداث نقلة في مجال الصحة واللياقة في الشرق الأوسط عبر منتجات وخدمات مبتكرة، وتثقيف العملاء من خلال تجارب تفاعلية، وبناء مجتمع واعٍ بالعافية.</p>
<h3>قيمنا</h3>
<ul>
<li>الصدق</li>
<li>التواصل المفتوح</li>
<li>المسؤولية والأهداف الواضحة</li>
<li>خطة استراتيجية واضحة</li>
<li>بيئة عمل إيجابية وودية</li>
<li>تشجيع العمل الجماعي</li>
</ul>
<h3>فروعنا: فرعان في الإمارات</h3>
<p>ايديال ويت اسم موثوق في الصحة والعافية منذ 2021، ويخدم الإمارات عبر فرعين مع خطة توسع مستمرة.</p>
<div class="branch-map-card">
<h4>فرع الشامخة</h4>
<p>الشامخة مول، بجوار الفردان للصرافة، محل 38.<br/>
<a href="https://maps.app.goo.gl/9SU1q2p69vfRTkMn8?g_st=aw" target="_blank" rel="noopener noreferrer">عرض على الخريطة</a></p>
<iframe loading="lazy" referrerpolicy="no-referrer-when-downgrade" allowfullscreen src="https://www.google.com/maps?q=%D9%85%D9%88%D9%84+%D8%A7%D9%84%D8%B4%D8%A7%D9%85%D8%AE%D8%A9+%D8%A3%D8%A8%D9%88%D8%B8%D8%A8%D9%8A&output=embed"></iframe>
</div>
<div class="branch-map-card">
<h4>فرع الوثبة</h4>
<p>الوثبة مول، مقابل صيدلية ميدسينا، محل 26.<br/>
<a href="https://maps.app.goo.gl/12J3mbtkGsk6FKLp9?g_st=aw" target="_blank" rel="noopener noreferrer">عرض على الخريطة</a></p>
<iframe loading="lazy" referrerpolicy="no-referrer-when-downgrade" allowfullscreen src="https://www.google.com/maps?q=%D9%85%D9%88%D9%84+%D8%A7%D9%84%D9%88%D8%AB%D8%A8%D8%A9+%D8%A3%D8%A8%D9%88%D8%B8%D8%A8%D9%8A&output=embed"></iframe>
</div>
<p><strong>للتواصل:</strong> <a href="tel:+971507700559"><bdi dir="ltr">+971 50 770 0559</bdi></a></p>
<h3>عن ايديال ويت</h3>
<p>ايديال ويت من أبرز متاجر مكملات كمال الأجسام والتغذية في الإمارات، ويوفر مجموعة متكاملة من البروتينات، وما قبل التمرين، ومنتجات إدارة الوزن، والوجبات الصحية، وعلامات مثل Adonis وNano وApplied Nutrition.</p>`,
    },
  },
  privacy: {
    slug: 'privacy',
    title: { en: 'Privacy Policy', ar: 'سياسة الخصوصية' },
    body: {
      en: `<h3>Information Security and Privacy Policy</h3>
<p>We apply technical and administrative safeguards to protect your data from unauthorized access, disclosure, alteration, or destruction. Access to personal data is restricted to authorized staff and service providers.</p>
<h3>What information we collect</h3>
<ul>
<li>Account and checkout details such as name, email, phone, and delivery address.</li>
<li>Order, transaction, and delivery information.</li>
<li>Correspondence, support requests, and feedback.</li>
<li>Device and usage details such as IP, browser, and cookies.</li>
<li>Other data required to improve your site experience.</li>
</ul>
<h3>How we use your data</h3>
<ul>
<li>To provide requested products and services.</li>
<li>To process payments, orders, and deliveries.</li>
<li>To improve site performance and customer experience.</li>
<li>To notify you about service updates and account/order changes.</li>
<li>To comply with legal and regulatory obligations.</li>
</ul>
<h3>Sharing and payment data</h3>
<p>We do not sell personal data. Data is shared only when necessary for operations, legal compliance, or authorized integrations. Credit/debit card details and personally identifiable information are not sold or rented to third parties.</p>
<h3>Jurisdiction and transfer</h3>
<p>idealweightnutrition.ae operates from the UAE. By using the site, you agree that your data may be processed under UAE laws and transferred as required to provide services.</p>
<h3>Contact</h3>
<p>Email: <a href="mailto:info@idealweightnutrition.ae">info@idealweightnutrition.ae</a><br/>Mobile: <a href="tel:+971507700559"><bdi dir="ltr">+971 50 770 0559</bdi></a></p>`,
      ar: `<h3>سياسة أمان المعلومات والخصوصية</h3>
<p>نطبق معايير تقنية وإدارية لحماية بياناتك من الوصول غير المصرح به أو التعديل أو الكشف أو الإتلاف. الوصول إلى البيانات يقتصر على الموظفين ومقدمي الخدمة المصرح لهم.</p>
<h3>ما البيانات التي نجمعها</h3>
<ul>
<li>بيانات الحساب وإتمام الطلب مثل الاسم والبريد والهاتف والعنوان.</li>
<li>بيانات الطلبات والمعاملات والشحن.</li>
<li>سجل المراسلات وطلبات الدعم والملاحظات.</li>
<li>بيانات الجهاز والاستخدام مثل عنوان IP والمتصفح وملفات تعريف الارتباط.</li>
<li>أي بيانات لازمة لتحسين تجربة الاستخدام.</li>
</ul>
<h3>كيف نستخدم البيانات</h3>
<ul>
<li>لتقديم المنتجات والخدمات المطلوبة.</li>
<li>لمعالجة الدفع والطلبات والتوصيل.</li>
<li>لتحسين أداء الموقع وتجربة العملاء.</li>
<li>لإشعارك بتحديثات الخدمة وحالة الطلبات.</li>
<li>للامتثال للمتطلبات القانونية والتنظيمية.</li>
</ul>
<h3>مشاركة البيانات وبيانات الدفع</h3>
<p>لا نقوم ببيع بياناتك الشخصية. تتم المشاركة فقط عند الحاجة التشغيلية أو القانونية أو عند وجود تكاملات مصرح بها. كما لا يتم بيع أو تأجير بيانات البطاقات أو معلوماتك الشخصية لأطراف ثالثة.</p>
<h3>الاختصاص القضائي ونقل البيانات</h3>
<p>يعمل موقع idealweightnutrition.ae من دولة الإمارات. باستخدامك للموقع، فإنك توافق على معالجة بياناتك وفق القوانين المعمول بها في الإمارات ونقلها عند الحاجة لتقديم الخدمة.</p>
<h3>التواصل</h3>
<p>البريد: <a href="mailto:info@idealweightnutrition.ae">info@idealweightnutrition.ae</a><br/>الجوال: <a href="tel:+971507700559"><bdi dir="ltr">+971 50 770 0559</bdi></a></p>`,
    },
  },
  terms: {
    slug: 'terms',
    title: { en: 'Terms & Conditions', ar: 'الشروط والأحكام' },
    body: {
      en: `<h3>Privacy Policy</h3>
<p>Please review our privacy policy to understand how we handle your data while providing our services.</p>
<h3>Payment and purchases</h3>
<p>You agree to provide current, accurate, and complete account and payment data. Prices, taxes, and availability may change without notice. All payments are in AED. We may correct pricing errors, refuse orders, or limit quantities in cases such as suspected reseller activity.</p>
<h3>Validity</h3>
<p>By using this site, you confirm you are at least 18 years old and legally able to accept these terms.</p>
<h3>Copyright and trademarks</h3>
<p>All site materials, trademarks, logos, text, graphics, and related intellectual property are protected and may not be copied or used without prior written permission.</p>
<h3>Return and exchange</h3>
<p>See our return policy <a href="/page/return-policy">here</a>.</p>
<h3>Approval</h3>
<p>Using this website and services indicates acceptance of these terms as an agreement between you and us.</p>
<h3>Contact</h3>
<p>Email: <a href="mailto:info@idealweightnutrition.ae">info@idealweightnutrition.ae</a><br/>Phone: <a href="tel:+971507700559"><bdi dir="ltr">+971 50 770 0559</bdi></a></p>`,
      ar: `<h3>سياسة الخصوصية</h3>
<p>يرجى مراجعة سياسة الخصوصية لفهم طريقة التعامل مع بياناتك أثناء تقديم خدماتنا.</p>
<h3>الدفع والمشتريات</h3>
<p>تتعهد بتقديم بيانات دقيقة ومحدثة للحساب والدفع. قد تتغير الأسعار والضرائب والتوفر دون إشعار. جميع المدفوعات بالدرهم الإماراتي. ونحتفظ بحق تصحيح أخطاء التسعير أو رفض الطلبات أو تحديد الكميات خصوصًا في حالات الاشتباه بإعادة البيع.</p>
<h3>الصلاحية</h3>
<p>باستخدامك الموقع، فإنك تؤكد أن عمرك 18 سنة أو أكثر وأنك مؤهل قانونيًا للموافقة على هذه الشروط.</p>
<h3>حقوق النشر والعلامات التجارية</h3>
<p>جميع مواد الموقع والنصوص والرسومات والشعارات والعلامات التجارية محمية بحقوق الملكية الفكرية ولا يجوز نسخها أو استخدامها دون موافقة خطية مسبقة.</p>
<h3>الإرجاع والتبديل</h3>
<p>يمكنك مراجعة سياسة الإرجاع <a href="/page/return-policy">من هنا</a>.</p>
<h3>الموافقة</h3>
<p>استخدامك للموقع وخدماته يعتبر موافقة على هذه الشروط كاتفاق بينك وبيننا.</p>
<h3>التواصل</h3>
<p>البريد: <a href="mailto:info@idealweightnutrition.ae">info@idealweightnutrition.ae</a><br/>الهاتف: <a href="tel:+971507700559"><bdi dir="ltr">+971 50 770 0559</bdi></a></p>`,
    },
  },
  shipping: {
    slug: 'shipping',
    title: { en: 'Shipping Information', ar: 'معلومات الشحن' },
    body: {
      en: `<h3>Shipping Information</h3>
<p>Ideal Weight processes orders as quickly as possible and aims to provide competitive shipping rates. Heavy or oversized shipments may incur additional charges.</p>
<p><strong>Note:</strong> If an order requires multiple boxes, coordination may be done internally without prior confirmation.</p>
<h3>Delivery Time</h3>
<p>Delivery time varies by region/city/country and can be affected by holidays or special periods.</p>
<h3>International Shipping and Customs</h3>
<p>The consignee is responsible for customs clearance and any related legal or customs fees outside UAE. Local authorities may inspect shipments and request identification documents.</p>
<h3>Business Days</h3>
<p>Working days are Sunday to Thursday. Orders placed after 10 AM are processed on the next business day.</p>
<h3>Delivery Details</h3>
<ul>
<li>Outside city limits: delivery may be arranged to the nearest branch or via third-party courier.</li>
<li>UAE western area: delivery may take longer than standard estimates.</li>
<li>Remote areas: extra delivery cost may apply.</li>
</ul>
<h3>ID Requirement at Delivery</h3>
<p>We may request a valid ID copy/picture to verify the receiver and ensure secure handover of products.</p>
<h3>Free Shipping/Delivery</h3>
<p>Limited-time free delivery offers may apply to selected products. Excluded products or clearance products may not carry free-delivery labels, and shipping fees are calculated at checkout where applicable.</p>`,
      ar: `<h3>معلومات الشحن</h3>
<p>تعمل ايديال ويت على معالجة الطلبات بأسرع وقت وتقديم أسعار شحن تنافسية. قد تُطبق رسوم إضافية على الشحنات الثقيلة أو كبيرة الحجم.</p>
<p><strong>ملاحظة:</strong> إذا احتاج الطلب إلى أكثر من صندوق، قد يتم التنسيق داخليًا دون تأكيد مسبق.</p>
<h3>مدة التسليم</h3>
<p>تختلف مدة التسليم حسب المنطقة/المدينة/الدولة، وقد تتأثر بالمواسم والعطلات.</p>
<h3>الشحن الدولي والجمارك</h3>
<p>يتحمل المستلم مسؤولية التخليص الجمركي وأي رسوم قانونية/جمركية خارج الإمارات. وقد تطلب الجهات المحلية مستندات تعريف للتحقق من الشحنة.</p>
<h3>أيام العمل</h3>
<p>أيام العمل من الأحد إلى الخميس. الطلبات بعد الساعة 10 صباحًا تُعالج في يوم العمل التالي.</p>
<h3>تفاصيل التسليم</h3>
<ul>
<li>خارج حدود المدينة: قد يتم التسليم لأقرب فرع أو عبر طرف ثالث.</li>
<li>المنطقة الغربية: قد يستغرق التسليم وقتًا أطول من المعتاد.</li>
<li>المناطق النائية: قد تُطبق رسوم إضافية.</li>
</ul>
<h3>متطلبات الهوية عند التسليم</h3>
<p>قد نطلب صورة هوية سارية للتحقق من المستلم وضمان تسليم المنتج بشكل آمن.</p>
<h3>الشحن/التسليم المجاني</h3>
<p>قد تتوفر عروض تسليم مجاني لفترة محدودة على منتجات محددة. المنتجات المستثناة أو التصفية قد لا تحمل وسم التسليم المجاني، وتُحسب الرسوم عند الدفع عند الحاجة.</p>`,
    },
  },
  'return-policy': {
    slug: 'return-policy',
    title: { en: 'Returns Policy', ar: 'سياسة الإرجاع' },
    body: {
      en: `<h3>Return and Exchange Policy</h3>
<p>Return or exchange is available within 48 hours of receiving the order (14 days for food consulting services).</p>
<p>For support: <a href="mailto:info@idealweightnutrition.ae">info@idealweightnutrition.ae</a> or <a href="tel:+971507700559"><bdi dir="ltr">+971 50 770 0559</bdi></a>.</p>
<h3>Eligibility</h3>
<p>Products must be unused, unopened, and in original packaging unless there is a defect. Proof of purchase is required.</p>
<h3>Refunds</h3>
<p>After receiving and inspecting the returned item, we will notify you of approval or rejection. Approved refunds are processed back to the original payment method or agreed account details. Return shipping fees are borne by the customer.</p>
<h3>Delayed refunds</h3>
<p>If your refund is delayed, first check your bank and card provider, then contact us for assistance.</p>
<h3>How to return</h3>
<p>Submit a return request within the allowed period and our team will arrange the return process.</p>`,
      ar: `<h3>سياسة الإرجاع والتبديل</h3>
<p>الإرجاع أو التبديل متاح خلال 48 ساعة من استلام الطلب (14 يومًا لخدمات الاستشارات الغذائية).</p>
<p>للتواصل: <a href="mailto:info@idealweightnutrition.ae">info@idealweightnutrition.ae</a> أو <a href="tel:+971507700559"><bdi dir="ltr">+971 50 770 0559</bdi></a>.</p>
<h3>شروط الأهلية</h3>
<p>يجب أن تكون المنتجات غير مستخدمة وغير مفتوحة وبحالـتها الأصلية ما لم يكن هناك عيب. ويشترط وجود إثبات شراء.</p>
<h3>المبالغ المستردة</h3>
<p>بعد استلام المنتج المرتجع وفحصه، يتم إشعارك بالموافقة أو الرفض. عند الموافقة، يتم استرداد المبلغ إلى وسيلة الدفع الأصلية أو حساب متفق عليه. رسوم شحن الإرجاع يتحملها العميل.</p>
<h3>تأخر الاسترداد</h3>
<p>في حال تأخر الاسترداد، يرجى مراجعة البنك وشركة البطاقة أولًا، ثم التواصل معنا للمساعدة.</p>
<h3>طريقة الإرجاع</h3>
<p>قدّم طلب الإرجاع خلال المدة المسموح بها وسيتولى فريقنا ترتيب عملية الاستلام والإرجاع.</p>`,
    },
  },
  help: {
    slug: 'help',
    title: { en: 'Help Center', ar: 'مركز المساعدة' },
    body: {
      en: `<h3>Quick Links</h3>
<ul>
<li><a href="/page/shipping">Shipping Information</a></li>
<li><a href="/page/return-policy">Return Policy</a></li>
<li><a href="/track">Track Order</a></li>
<li><a href="/page/privacy">Privacy Policy</a></li>
</ul>
<h3>Frequently Asked Questions</h3>
<p><strong>How can I track my order?</strong><br/>Use your order number and email on the <a href="/track">Track Order</a> page.</p>
<p><strong>What is the delivery time?</strong><br/>Delivery depends on location. UAE delivery is usually within 1 to 4 business days based on emirate and area.</p>
<p><strong>How can I return a product?</strong><br/>Returns are accepted within 48 hours (14 days for food consulting services), subject to policy conditions.</p>
<p><strong>What payment methods are available?</strong><br/>We accept cards and supported installment methods such as Tabby and Tamara where available.</p>
<p><strong>Is there free shipping?</strong><br/>Free-shipping promotions may apply based on active offers and exclusions.</p>
<p><strong>Can I modify or cancel my order?</strong><br/>Yes, before processing. Contact support as soon as possible.</p>
<h3>Contact Us</h3>
<p>Email: <a href="mailto:info@idealweightnutrition.ae">info@idealweightnutrition.ae</a><br/>Phone: <a href="tel:+971507700559"><bdi dir="ltr">+971 50 770 0559</bdi></a><br/>Business Hours: Sunday to Thursday, 9 AM to 6 PM (UAE Time)</p>`,
      ar: `<h3>روابط سريعة</h3>
<ul>
<li><a href="/page/shipping">معلومات الشحن</a></li>
<li><a href="/page/return-policy">سياسة الإرجاع</a></li>
<li><a href="/track">تتبع الطلب</a></li>
<li><a href="/page/privacy">سياسة الخصوصية</a></li>
</ul>
<h3>الأسئلة الشائعة</h3>
<p><strong>كيف يمكنني تتبع طلبي؟</strong><br/>باستخدام رقم الطلب والبريد الإلكتروني من صفحة <a href="/track">تتبع الطلب</a>.</p>
<p><strong>ما مدة التوصيل؟</strong><br/>تختلف حسب الموقع، وغالبًا داخل الإمارات من يوم إلى 4 أيام عمل حسب الإمارة والمنطقة.</p>
<p><strong>كيف أرجع منتجًا؟</strong><br/>متاح خلال 48 ساعة (14 يومًا للاستشارات الغذائية) وفق الشروط.</p>
<p><strong>ما طرق الدفع المتاحة؟</strong><br/>نقبل البطاقات وخيارات التقسيط المدعومة مثل تابي وتمارا عند توفرها.</p>
<p><strong>هل يوجد شحن مجاني؟</strong><br/>قد تتوفر عروض شحن مجاني حسب العروض الفعالة والاستثناءات.</p>
<p><strong>هل يمكن تعديل أو إلغاء الطلب؟</strong><br/>نعم قبل المعالجة، ويُفضّل التواصل معنا فورًا.</p>
<h3>تواصل معنا</h3>
<p>البريد: <a href="mailto:info@idealweightnutrition.ae">info@idealweightnutrition.ae</a><br/>الهاتف: <a href="tel:+971507700559"><bdi dir="ltr">+971 50 770 0559</bdi></a><br/>ساعات العمل: الأحد إلى الخميس، 9 صباحًا إلى 6 مساءً (بتوقيت الإمارات)</p>`,
    },
  },
};
