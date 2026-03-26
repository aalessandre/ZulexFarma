import { MediaMatcher } from '@angular/cdk/layout';

import { Component, DestroyRef, inject, Input } from '@angular/core';
import { TablerIconsModule } from 'angular-tabler-icons';
import { MaterialModule } from 'src/app/material.module';
import { team } from '../front-end-datatype';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-images',
  imports: [MaterialModule, TablerIconsModule, RouterLink],
  templateUrl: './images.component.html',
  styleUrl: './images.component.scss',
})
export class ImagesComponent {

  @Input() hideToolbar: boolean;

  private mediaMatcher = inject(MediaMatcher);
  private destroyRef = inject(DestroyRef);

  screenQuery: MediaQueryList;
  mobileQuery: MediaQueryList;
  tabletQuery: MediaQueryList;
  showTwoCards = false;
  visibleTeam: any[] = [];
  team = team;
  showThreeCards = false;

  constructor() {

    this.mobileQuery = this.mediaMatcher.matchMedia('(max-width: 767px)');
    this.tabletQuery = this.mediaMatcher.matchMedia('(max-width: 900px)');

    const listener = () => {
      this.showTwoCards = this.tabletQuery.matches;
      this.updateVisibleTeam();
      //this.cdr.markForCheck(); // ensure UI updates
    };

    // Initial check
    this.showTwoCards = this.tabletQuery.matches;
    this.updateVisibleTeam();

    // Listen to changes
    this.tabletQuery.addEventListener('change', listener);

    this.destroyRef.onDestroy(() => {
      this.tabletQuery.removeEventListener('change', listener);
    });
  }

  updateVisibleTeam() {
    this.visibleTeam = this.showTwoCards
      ? this.team.slice(0, 2) // show 2
      : this.team; // show all
  }
}
