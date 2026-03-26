
import { Component, inject, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TablerIconsModule } from 'angular-tabler-icons';
import { MaterialModule } from 'src/app/material.module';
import { FooterComponent } from '../footer/footer.component';
import { FrontEndService } from 'src/app/services/apps/front-pages/front-end.service';

@Component({
  selector: 'app-blog-details',
  imports: [
    MaterialModule,
    TablerIconsModule,
    RouterLink,
    FooterComponent
  ],
  templateUrl: './blog-details.component.html',
})
export class BlogDetailsComponent implements OnInit {
  private frontEndService = inject(FrontEndService);
  blogPost: any;

  readonly defaultBlog = {
    id: 1,
    time: '2 mins Read',
    imgSrc: '/assets/images/blog/blog-img1.jpg',
    user: '/assets/images/profile/user-1.jpg',
    title: 'Early Black Friday Amazon deals: cheap TVs, headphones, laptops',
    views: '9,125',
    category: 'Gadget',
    comments: 3,
    featuredPost: true,
    date: 'Mon, Dec 25',
  };
  constructor() { }
  ngOnInit(): void {
    const blogs = this.frontEndService.getBlog();

    if (blogs && !Array.isArray(blogs)) {
      this.blogPost = blogs;
    } else if (Array.isArray(blogs) && blogs.length > 0) {
      const featured = blogs.find((b) => b.featuredPost);
      this.blogPost = featured || blogs[0];
    } else {
      this.blogPost = this.defaultBlog;
    }
  }
}
