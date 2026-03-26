import { Component } from '@angular/core';
import { TablerIconsModule } from 'angular-tabler-icons';
import { MaterialModule } from 'src/app/material.module';

@Component({
  selector: 'app-new-items',
  standalone: true,
  imports: [MaterialModule, TablerIconsModule],
  templateUrl: './new-items.component.html',
})
export class AppNewItemsComponent {
  constructor() { }
}
