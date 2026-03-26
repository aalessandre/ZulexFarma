import { Component } from '@angular/core';
import { TablerIconsModule } from 'angular-tabler-icons';
import { MaterialModule } from 'src/app/material.module';

@Component({
  selector: 'app-upgrade-plan',
  standalone: true,
  imports:[TablerIconsModule, MaterialModule],
  templateUrl: './upgrade-plan.component.html',
})
export class AppUpgradePlanComponent {
  constructor() {}
}
