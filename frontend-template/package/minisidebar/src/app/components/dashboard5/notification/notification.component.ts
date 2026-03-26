
import { Component } from '@angular/core';
import { TablerIconsModule } from 'angular-tabler-icons';
import { MaterialModule } from 'src/app/material.module';

interface notifications {
  id: number;
  icon: string;
  color: string;
  title: string;
  time: string;
  subtitle: string;
}

@Component({
  selector: 'app-notification',
  standalone: true,
  imports: [MaterialModule, TablerIconsModule],
  templateUrl: './notification.component.html',
})
export class AppNotificationComponent {

  notifications: notifications[] = [
    {
      id: 1,
      icon: 'layout-grid',
      color: 'primary',
      time: '8:30 AM',
      title: 'Launch Admin',
      subtitle: 'Just see the my new admin!',
    },
    {
      id: 2,
      icon: 'calendar',
      color: 'secondary',
      time: '8:21 AM',
      title: 'Event today',
      subtitle: 'Just a reminder that you have event',
    },
    {
      id: 3,
      icon: 'settings',
      color: 'warning',
      time: '8:05 AM',
      title: 'Settings',
      subtitle: 'You can customize this template',
    },
    {
      id: 4,
      icon: 'circles-relation',
      color: 'success',
      time: '7:30 AM',
      title: 'Launch Templates',
      subtitle: 'Just see the my new admin!',
    },
    {
      id: 5,
      icon: 'list-check',
      color: 'error',
      time: '7:03 AM',
      title: 'Event tomorrow',
      subtitle: 'Just a reminder that you have event',
    },
  ];

  constructor() { }
}
