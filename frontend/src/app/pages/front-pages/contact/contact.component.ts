
import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TablerIconsModule } from 'angular-tabler-icons';
import { MaterialModule } from 'src/app/material.module';
import { FooterComponent } from '../footer/footer.component';

@Component({
  selector: 'app-contact',
  imports: [MaterialModule, TablerIconsModule, RouterLink, FooterComponent],
  templateUrl: './contact.component.html',
})
export class ContactComponent {

}
