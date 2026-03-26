import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { TablerIconsModule } from 'angular-tabler-icons';
import { MaterialModule } from 'src/app/material.module';
import {
  brandLogos,
  followercardsFirst,
  followercardsSecond,
  followercardsThird,
  testimonials,
} from '../front-end-datatype';

@Component({
  selector: 'app-card-slider',
  imports: [MaterialModule, TablerIconsModule, CommonModule],
  templateUrl: './card-slider.component.html',
  styleUrl: './card-slider.component.scss',
})
export class CardSliderComponent {
  followercardsFirst = followercardsFirst;

  followercardsSecond = followercardsSecond;

  followercardsThird = followercardsThird;

  brandLogos = brandLogos;

  testimonials = testimonials;

  currentSlide = 0;

  goToSlide(index: number): void {
    this.currentSlide = index;
  }
}
