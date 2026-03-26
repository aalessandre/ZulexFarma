import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';
import { TablerIconsModule } from 'angular-tabler-icons';
import { NgScrollbarModule } from 'ngx-scrollbar';
import { MaterialModule } from 'src/app/material.module';

@Component({
    selector: 'app-recent-comment',
    standalone: true,
    imports: [MaterialModule, TablerIconsModule, RouterModule, NgScrollbarModule],
    templateUrl: './recent-comments.component.html',
})
export class RecentcommentComponent {
    constructor() { }

    recentcomments: any[] = [
        {
            image: 'assets/images/profile/user-10.jpg',
            name: 'James Anderson',
            comment:
                'Lorem Ipsum is simply dummy text of the printing and type etting industry.',
            date: 'April 14, 2026',
            status: 'Pending',
            labelcolor: 'bg-light-warning text-warning',
        },
        {
            image: 'assets/images/profile/user-6.jpg',
            name: 'Michael Jorden',
            comment:
                'Lorem Ipsum is simply dummy text of the printing and type etting industry.',
            date: 'April 14, 2026',
            status: 'Approved',
            labelcolor: 'bg-light-secondary text-secondary',
        },
        {
            image: 'assets/images/profile/user-12.jpg',
            name: 'Johnathan Doeting',
            comment:
                'Lorem Ipsum is simply dummy text of the printing and type etting industry.',
            date: 'April 14, 2026',
            status: 'Rejected',
            labelcolor: 'bg-light-error text-error',
        },
    ];
}
