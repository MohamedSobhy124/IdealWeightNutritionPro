import { Pipe, PipeTransform, inject } from '@angular/core';
import { LocaleService } from '../services/locale.service';
import { formatIwnCurrency } from '../utils/iwn-currency';

@Pipe({ name: 'iwnCurrency', standalone: true })
export class IwnCurrencyPipe implements PipeTransform {
  private readonly locale = inject(LocaleService);

  transform(value: number | null | undefined): string {
    return formatIwnCurrency(value, this.locale);
  }
}