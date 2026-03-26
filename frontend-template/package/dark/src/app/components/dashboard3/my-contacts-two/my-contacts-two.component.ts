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
  selector: 'app-my-contacts-two',
  standalone: true,
  imports: [MaterialModule, TablerIconsModule],
  templateUrl: './my-contacts-two.component.html',
})
export class AppMyContactsTwoComponent {
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
    {
      id: 4,
      img: '/assets/images/profile/user-4.jpg',
      title: 'John Mainga',
      subtitle: 'you were in video call',
    },
    {
      id: 5,
      img: '/assets/images/profile/user-5.jpg',
      title: 'Angela Dominic',
      subtitle: 'you missed john call',
    },
  ];

  constructor() {}
}
