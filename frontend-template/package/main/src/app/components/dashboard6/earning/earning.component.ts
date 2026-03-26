import { Component } from '@angular/core';
import { TablerIconsModule } from 'angular-tabler-icons';
import { MaterialModule } from 'src/app/material.module';

interface msgs {
  id: number;
  img: string;
  title: string;
  subtitle: string;
  color: string;
  earning: string;
}

@Component({
  selector: 'app-earning',
  standalone: true,
  imports: [MaterialModule, TablerIconsModule],
  templateUrl: './earning.component.html',
})
export class AppEarningComponent {
  // This is for Mymessages
  msgs: msgs[] = [
    {
      id: 1,
      img: '/assets/images/profile/user-2.jpg',
      title: 'James Smith',
      subtitle: 'Web Designer',
      color: 'primary',
      earning: '$2300',
    },
    {
      id: 2,
      img: '/assets/images/profile/user-3.jpg',
      title: 'Maria Garciar',
      subtitle: 'Web Developer',
      color: 'secondary',
      earning: '$3300',
    },
    {
      id: 3,
      img: '/assets/images/profile/user-4.jpg',
      title: 'Joshph Rodriguez',
      subtitle: 'Web Manager',
      color: 'error',
      earning: '$4300',
    },
    {
      id: 4,
      img: '/assets/images/profile/user-5.jpg',
      title: 'Christiana Roz',
      subtitle: 'Project Manager',
      color: 'success',
      earning: '$2300',
    },
  ];

  constructor() {}
}
