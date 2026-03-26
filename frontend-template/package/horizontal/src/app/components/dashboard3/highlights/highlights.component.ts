import { Component } from '@angular/core';
import { TablerIconsModule } from 'angular-tabler-icons';
import { MaterialModule } from 'src/app/material.module';

@Component({
  selector: 'app-highlights',
  standalone: true,
  imports:[TablerIconsModule, MaterialModule],
  templateUrl: './highlights.component.html',
})
export class AppHighlightsComponent {
  constructor() {}
}
