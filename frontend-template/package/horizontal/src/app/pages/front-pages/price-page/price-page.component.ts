import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { TablerIconsModule } from 'angular-tabler-icons';
import { MaterialModule } from 'src/app/material.module';
import { paymentLogos, plans } from '../front-end-datatype';

@Component({
  selector: 'app-price-page',
  imports: [MaterialModule,
    TablerIconsModule, CommonModule],
  templateUrl: './price-page.component.html',
})
export class PricePageComponent {
  plans = plans;
  paymentLogos = paymentLogos;

  highlightBoldWord(text: string, bold?: boolean): string {
    if (bold) {
      const match = text.match(/(Unlimited)/i);
      if (match) {
        const bolded = `<strong>${match[0]}</strong>`;
        return text.replace(match[0], bolded);
      }
    }
    return text;
  }
}
