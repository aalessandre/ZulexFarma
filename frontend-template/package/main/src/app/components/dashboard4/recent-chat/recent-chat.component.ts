import { Component } from '@angular/core';
import { TablerIconsModule } from 'angular-tabler-icons';
import { NgScrollbarModule } from 'ngx-scrollbar';
import { MaterialModule } from 'src/app/material.module';

@Component({
  selector: 'app-recent-chat',
  standalone: true,
  imports:[MaterialModule, NgScrollbarModule, TablerIconsModule],
  templateUrl: './recent-chat.component.html',
})
export class AppRecentChatComponent {

  constructor() {}
}
