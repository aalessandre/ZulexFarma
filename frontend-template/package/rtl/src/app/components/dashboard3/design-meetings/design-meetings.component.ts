import { Component } from '@angular/core';
import { TablerIconsModule } from 'angular-tabler-icons';
import { MaterialModule } from 'src/app/material.module';

@Component({
  selector: 'app-design-meetings',
  standalone: true,
  imports:[TablerIconsModule, MaterialModule],
  templateUrl: './design-meetings.component.html',
})
export class AppDesignMeetingsComponent {
  constructor() {}
}
