
import { Component, inject, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { TablerIconsModule } from 'angular-tabler-icons';
import { MaterialModule } from 'src/app/material.module';
import { blogService } from 'src/app/services/apps/blog/blog.service';
import { FooterComponent } from '../footer/footer.component';
import { FrontEndService } from 'src/app/services/apps/front-pages/front-end.service';

@Component({
  selector: 'app-blog',
  imports: [MaterialModule, TablerIconsModule, RouterLink, FooterComponent],
  templateUrl: './blog.component.html',
})
export class BlogComponent implements OnInit {
  public blogService = inject(blogService);
  private frontEndService = inject(FrontEndService);
  private router = inject(Router);
  posts = this.blogService.getBlog();

  ngOnInit(): void {

  }

  getBlogDetails(blogPost: any) {
    this.frontEndService.setBlog(blogPost);
    this.router.navigate(['front-pages/blog-details'])
  }
}
