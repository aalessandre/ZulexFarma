import { Component } from '@angular/core';
import { TablerIconsModule } from 'angular-tabler-icons';
import { MaterialModule } from 'src/app/material.module';

interface msgs {
  id: number;
  img: string;
  title: string;
  subtitle: string;
}

@Component({
  selector: 'app-my-contacts',
  standalone: true,
  imports: [MaterialModule, TablerIconsModule],
  templateUrl: './my-contacts.component.html',
})
export class AppMyContactsComponent {
  // This is for Mymessages
  msgs: msgs[] = [
    {
      id: 1,
      img: '/assets/images/profile/user-2.jpg',
      title: 'James Smith',
      subtitle: 'you were in video call',
    },
    {
      id: 2,
      img: '/assets/images/profile/user-6.jpg',
      title: 'Joseph Garciar',
      subtitle: 'you were in video call',
    },
    {
      id: 3,
      img: '/assets/images/profile/user-3.jpg',
      title: 'Maria Rodriguez',
      subtitle: 'you missed john call',
    },
  ];

  constructor() {}
}
