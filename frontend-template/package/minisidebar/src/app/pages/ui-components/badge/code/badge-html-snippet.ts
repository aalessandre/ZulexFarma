export const TEXT_WITH_BADGE_HTML_SNIPPET = `  <div matBadge="4" matBadgeOverlap="false" class="inline">
            Text with a badge
        </div>
`;

export const SIZE_WITH_BADGE_HTML_SNIPPET = `   <div matBadge="1" matBadgeSize="small" class="inline">
            Text with small badge
          </div>
          <div matBadge="1" matBadgeSize="large" class="inline">
            Text with large badge
          </div>
`;

export const BUTTON_LEFT_WITH_BADGE_HTML_SNIPPET = `   <p>
    Button with a badge on the left
    <button
        mat-flat-button
        color="primary"
        matBadge="8"
        matBadgePosition="before"
        matBadgeColor="accent"
    >
        Action
    </button>
    </p>
`;

export const ICON_WITH_BADGE_HTML_SNIPPET = `  <p>
            Icon with a badge
            <mat-icon matBadge="15" matBadgeColor="warn">home</mat-icon>
          </p>
`;

export const BUTTON_TOGGLES_WITH_BADGE_HTML_SNIPPET = `  <p>
            Button toggles badge visibility
            <button
              mat-flat-button
              color="primary"
              matBadge="7"
              [matBadgeHidden]="hidden"
              (click)="toggleBadgeVisibility()"
            >
              Hide
            </button>
          </p>
`;