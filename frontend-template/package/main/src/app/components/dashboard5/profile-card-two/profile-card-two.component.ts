import { Component } from '@angular/core';
import { TablerIconsModule } from 'angular-tabler-icons';
import { MaterialModule } from 'src/app/material.module';

@Component({
  selector: 'app-profile-card-two',
  standalone: true,
  imports:[TablerIconsModule, MaterialModule],
  templateUrl: './profile-card-two.component.html',
})
export class AppProfileCardTwoComponent {
  constructor() {}
}
