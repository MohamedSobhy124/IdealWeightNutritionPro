import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-not-found',
  standalone: true,
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="not-found">
      <p class="code">404</p>
      <h1>Page not found / الصفحة غير موجودة</h1>
      <p>The page you are looking for does not exist or has moved.</p>
      <p dir="rtl">الصفحة التي تبحث عنها غير موجودة أو تم نقلها.</p>
      <a routerLink="/" class="btn btn-primary">Back to home / العودة للرئيسية</a>
    </section>
  `,
  styles: [
    `
      .not-found { text-align: center; padding: 4rem 1rem; }
      .code { font-size: 4rem; font-weight: 700; margin: 0; opacity: 0.4; }
      h1 { margin: 0.5rem 0 1rem; }
      a { display: inline-block; margin-top: 1.5rem; }
    `,
  ],
})
export class NotFoundComponent {}
