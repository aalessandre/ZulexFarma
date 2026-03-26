export const BASIC_TOOLBAR_HTML_SNIPPET = `  <mat-toolbar>
          <button matIconButton class="example-icon" aria-label="Example icon-button with menu icon">
            <mat-icon>menu</mat-icon>
          </button>
          <span>My App</span>
          <span class="example-spacer flex-1"></span>
          <button matIconButton class="example-icon favorite-icon" aria-label="Example icon-button with heart icon">
            <mat-icon>favorite</mat-icon>
          </button>
          <button matIconButton class="example-icon" aria-label="Example icon-button with share icon">
            <mat-icon>share</mat-icon>
          </button>
        </mat-toolbar>
`;

export const MULTI_ROW_TOOLBAR_HTML_SNIPPET = `  <mat-toolbar color="primary">
          <mat-toolbar-row>
            <span>Custom Toolbar</span>
          </mat-toolbar-row>

          <mat-toolbar-row>
            <span>Second Line</span>
            <span class="ml-auto"></span>
            <mat-icon class="example-icon" aria-hidden="false"
              aria-label="Example user verified icon">verified_user</mat-icon>
          </mat-toolbar-row>

          <mat-toolbar-row>
            <span>Third Line</span>
            <span class="ml-auto"></span>
            <div class="flex gap-8 items-center">
              <mat-icon class="example-icon" aria-hidden="false" aria-label="Example heart icon">favorite</mat-icon>
              <mat-icon class="example-icon" aria-hidden="false" aria-label="Example delete icon">delete</mat-icon>
            </div>
          </mat-toolbar-row>
        </mat-toolbar>
`;

export const ONLY_BRAND_TOOLBAR_HTML_SNIPPET = `  <mat-toolbar>
          <span>MaterialPro</span>
        </mat-toolbar>
`;