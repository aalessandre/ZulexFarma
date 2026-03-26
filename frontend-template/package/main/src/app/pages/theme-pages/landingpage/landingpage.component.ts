import { Component, Output, EventEmitter, Input } from '@angular/core';
import { CoreService } from 'src/app/services/core.service';
import { ViewportScroller } from '@angular/common';
import { MaterialModule } from 'src/app/material.module';
import { TablerIconsModule } from 'angular-tabler-icons';
import { RouterLink } from '@angular/router';
import { AppAuthBrandingComponent } from 'src/app/layouts/full/vertical/sidebar/auth-branding.component';

interface apps {
  id: number;
  icon: string;
  color: string;
  title: string;
  subtitle: string;
  link: string;
}

interface quicklinks {
  id: number;
  title: string;
  link: string;
}

interface demos {
  id: number;
  name: string;
  url: string;
  imgSrc: string;
}

interface testimonials {
  id: number;
  name: string;
  subtext: string;
  imgSrc: string;
  para: string;
}

interface features {
  id: number;
  icon: string;
  title: string;
  subtext: string;
}

@Component({
  selector: 'app-landingpage',
  standalone: true,
  imports: [MaterialModule, TablerIconsModule, RouterLink, AppAuthBrandingComponent],
  templateUrl: './landingpage.component.html',
})
export class AppLandingpageComponent {
  @Input() showToggle = true;
  @Output() toggleMobileNav = new EventEmitter<void>();
  @Output() toggleMobileFilterNav = new EventEmitter<void>();
  @Output() toggleCollapsed = new EventEmitter<void>();

  options = this.settings.getOptions();

  constructor(
    private settings: CoreService,
    private scroller: ViewportScroller
  ) { }

  // scroll to demos
  gotoDemos() {
    this.scroller.scrollToAnchor('demos');
  }

  apps: apps[] = [
    {
      id: 1,
      icon: 'message',
      color: 'primary',
      title: 'Chat Application',
      subtitle: 'Messages & Emails',
      link: '/apps/chat',
    },
    {
      id: 2,
      icon: 'list-check',
      color: 'secondary',
      title: 'Todo App',
      subtitle: 'Completed task',
      link: '/apps/todo',
    },
    {
      id: 3,
      icon: 'file-invoice',
      color: 'success',
      title: 'Invoice App',
      subtitle: 'Get latest invoice',
      link: '/apps/invoice/list',
    },
    {
      id: 4,
      icon: 'calendar',
      color: 'error',
      title: 'Calendar App',
      subtitle: 'Get Dates',
      link: '/apps/calendar',
    },
    {
      id: 5,
      icon: 'device-mobile',
      color: 'warning',
      title: 'Contact Application',
      subtitle: '2 Unsaved Contacts',
      link: '/apps/contacts',
    },
    {
      id: 6,
      icon: 'ticket',
      color: 'primary',
      title: 'Tickets App',
      subtitle: 'Create new ticket',
      link: '/apps/tickets',
    },
    {
      id: 7,
      icon: 'mail',
      color: 'secondary',
      title: 'Email App',
      subtitle: 'Get new emails',
      link: '/apps/email/inbox',
    },
    {
      id: 8,
      icon: 'book-2',
      color: 'warning',
      title: 'Courses',
      subtitle: 'Create new course',
      link: '/apps/courses',
    },
  ];
  quicklinks: quicklinks[] = [
    {
      id: 1,
      title: 'Pricing Page',
      link: '/theme-pages/pricing',
    },
    {
      id: 2,
      title: 'Authentication Design',
      link: '/authentication/login',
    },
    {
      id: 3,
      title: 'Register Now',
      link: '/authentication/side-register',
    },
    {
      id: 4,
      title: '404 Error Page',
      link: '/authentication/error',
    },
    {
      id: 5,
      title: 'Notes App',
      link: '/apps/notes',
    },
    {
      id: 6,
      title: 'Employee App',
      link: '/apps/employee',
    },
    {
      id: 7,
      title: 'Todo Application',
      link: '/apps/todo',
    },
  ];

  demos: demos[] = [
    {
      id: 1,
      imgSrc: '/assets/images/landingpage/demos/demo-main.jpg',
      name: 'Main',
      url: 'https://materialpro-angular-main.netlify.app/dashboards/dashboard1',
    },
    {
      id: 2,
      imgSrc: '/assets/images/landingpage/demos/demo-dark.jpg',
      name: 'Dark',
      url: 'https://materialpro-angular-dark.netlify.app/dashboards/dashboard2',
    },
    {
      id: 3,
      imgSrc: '/assets/images/landingpage/demos/demo-firebase.jpg',
      name: 'Authguard',
      url: 'https://materialpro-angular-authguard.netlify.app/authentication/login',
    },
    {
      id: 4,
      imgSrc: '/assets/images/landingpage/demos/demo-rtl.jpg',
      name: 'RTL',
      url: 'https://materialpro-angular-rtl.netlify.app/dashboards/dashboard1',
    },
    {
      id: 5,
      imgSrc: '/assets/images/landingpage/demos/demo-minisidebar.jpg',
      name: 'Minisidebar',
      url: 'https://materialpro-angular-minisidebar.netlify.app/dashboards/dashboard1',
    },
    {
      id: 6,
      imgSrc: '/assets/images/landingpage/demos/demo-horizontal.jpg',
      name: 'Horizontal',
      url: 'https://materialpro-angular-horizontal.netlify.app/dashboards/dashboard2',
    },
  ];

  appdemos: demos[] = [
    {
      id: 1,
      imgSrc: '/assets/images/landingpage/apps/app-calendar.jpg',
      name: 'Calendar',
      url: 'https://materialm-angular-main.netlify.app//apps/calendar',
    },
    {
      id: 2,
      imgSrc: '/assets/images/landingpage/apps/app-chat.jpg',
      name: 'Chat',
      url: 'https://materialm-angular-main.netlify.app//apps/chat',
    },
    {
      id: 3,
      imgSrc: '/assets/images/landingpage/apps/app-contact.jpg',
      name: 'Contact',
      url: 'https://materialm-angular-main.netlify.app//apps/contacts',
    },
    {
      id: 4,
      imgSrc: '/assets/images/landingpage/apps/app-email.jpg',
      name: 'Email',
      url: 'https://materialm-angular-main.netlify.app//apps/email/inbox',
    },
    {
      id: 5,
      imgSrc: '/assets/images/landingpage/apps/app-courses.jpg',
      name: 'Courses',
      url: 'https://materialm-angular-main.netlify.app//apps/courses',
    },
    {
      id: 6,
      imgSrc: '/assets/images/landingpage/apps/app-employee.jpg',
      name: 'Employee',
      url: 'https://materialm-angular-main.netlify.app//apps/employee',
    },
    {
      id: 7,
      imgSrc: '/assets/images/landingpage/apps/app-note.jpg',
      name: 'Notes',
      url: 'https://materialm-angular-main.netlify.app//apps/notes',
    },
    {
      id: 8,
      imgSrc: '/assets/images/landingpage/apps/app-ticket.jpg',
      name: 'Tickets',
      url: 'https://materialm-angular-main.netlify.app//apps/tickets',
    },
    {
      id: 9,
      imgSrc: '/assets/images/landingpage/apps/app-invoice.jpg',
      name: 'Invoice',
      url: 'https://materialm-angular-main.netlify.app//apps/invoice',
    },
    {
      id: 10,
      imgSrc: '/assets/images/landingpage/apps/app-todo.jpg',
      name: 'Todo',
      url: 'https://materialm-angular-main.netlify.app//apps/todo',
    },
    {
      id: 11,
      imgSrc: '/assets/images/landingpage/apps/app-taskboard.jpg',
      name: 'Taskboard',
      url: 'https://materialm-angular-main.netlify.app//apps/taskboard',
    },
    {
      id: 12,
      imgSrc: '/assets/images/landingpage/apps/app-blog.jpg',
      name: 'Blog List',
      url: 'https://materialm-angular-main.netlify.app//apps/blog/post',
    },
  ];

  testimonials: testimonials[] = [
    {
      id: 1,
      imgSrc: '/assets/images/landingpage/profile/testimonial1.png',
      name: 'Jenny Wilson',
      subtext: 'Features avaibility',
      para: 'The theme is very flexible with most of the content already available. And also the support team is very active. :)'
    },
    {
      id: 2,
      imgSrc: '/assets/images/landingpage/profile/testimonial2.png',
      name: 'Minshan Cui',
      subtext: 'Features avaibility',
      para: 'This design is really good. The code is good. Its easy to start with it when we are new with Angular. Like this theme because its a material'
    },
    {
      id: 3,
      imgSrc: '/assets/images/landingpage/profile/testimonial3.png',
      name: 'Eminson Mendoza',
      subtext: 'Features avaibility',
      para: 'I am using the MaterialPro angular admin, and its feature rich, easy to use and saves a ton of time. The Support Team is great.'
    },
  ];

  features: features[] = [
    {
      id: 1,
      icon: 'wand',
      title: '6 Theme Colors',
      subtext:
        'MaterialPro Admin comes with 6 pre-defined theme colors.',
    },
    {
      id: 2,
      icon: 'shield-lock',
      title: 'Authguard',
      subtext:
        'AuthGuard is utilized in Angular to prevent unauthorized access to specific routes.',
    },
    {
      id: 3,
      icon: 'archive',
      title: '80+ Page Templates',
      subtext: 'Yes, we offer 6 demos, each featuring over 80 pages to simplify your experience.',
    },
    {
      id: 33,
      icon: 'code',
      title: 'M3 Based',
      subtext: 'Angular Material M3 (Material 3) is the latest design system from Google, improving upon Material.',
    },
    {
      id: 4,
      icon: 'adjustments',
      title: '50+ UI Components',
      subtext:
        'The MaterialPro Admin Pack includes nearly 50 UI components.',
    },
    {
      id: 5,
      icon: 'tag',
      title: 'Material Angular',
      subtext: 'It is built with Material angular and features a fully responsive layout.',
    },
    {
      id: 6,
      icon: 'diamond',
      title: '3400+ Font Icons',
      subtext:
        'The MaterialPro Admin package includes a wide variety of icon fonts.',
    },
    {
      id: 7,
      icon: 'language-katakana',
      title: 'i18 Angular',
      subtext: 'i18 is a robust framework for internationalization in Angular.',
    },
    {
      id: 8,
      icon: 'arrows-shuffle',
      title: 'Easy to Customize',
      subtext: 'We understand your challenges, so we have made customization easy.',
    },
    {
      id: 9,
      icon: 'chart-pie',
      title: 'Lots of Chart Options',
      subtext: 'Whatever you need, we have it—there are numerous chart variations available.',
    },
    {
      id: 10,
      icon: 'layers-intersect',
      title: 'Lots of Table Examples',
      subtext: 'Data tables are a fundamental requirement, and we have included them.',
    },
    {
      id: 11,
      icon: 'refresh',
      title: 'Regular Updates',
      subtext: 'We continuously enhance our pack with new features.',
    },
    {
      id: 12,
      icon: 'book',
      title: 'Detailed Documentation',
      subtext: 'We have created comprehensive documentation to ensure ease of use.',
    },
    {
      id: 13,
      icon: 'calendar',
      title: 'Calendar Design',
      subtext: 'Our package includes a well-designed calendar.',
    },
    {
      id: 14,
      icon: 'messages',
      title: 'Dedicated Support',
      subtext: 'We believe that top-notch support is crucial, and we provide just that.',
    },
  ];

}
