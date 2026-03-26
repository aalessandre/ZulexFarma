export const BASIC_EXPANSION_HTML_SNIPPET = `  <mat-accordion>
            <mat-expansion-panel hideToggle class="cardWithShadow">
              <mat-expansion-panel-header>
                <mat-panel-title class="font-semibold text-sm">
                  This is the expansion title
                </mat-panel-title>
                <mat-panel-description class="text-sm">
                  This is a summary of the content
                </mat-panel-description>
              </mat-expansion-panel-header>
              <p>This is the primary content of the panel.</p>
            </mat-expansion-panel>
            <mat-expansion-panel class="cardWithShadow" (opened)="panelOpenState = true"
              (closed)="panelOpenState = false">
              <mat-expansion-panel-header>
                <mat-panel-title class="font-semibold text-sm">
                  Self aware panel
                </mat-panel-title>
                <mat-panel-description class="text-sm">
                  Currently I am {{panelOpenState ? 'open' : 'closed'}}
                </mat-panel-description>
              </mat-expansion-panel-header>
              <p>I'm visible because I am open</p>
            </mat-expansion-panel>
          </mat-accordion>
`;

export const EXPAND_EXPANSION_HTML_SNIPPET = `  <div class="flex items-center mb-4">
            <button mat-flat-button color="primary" (click)="accordion.openAll()">
              Expand All
            </button>
            <button mat-stroked-button color="primary" (click)="accordion.closeAll()" class="ms-2">
              Collapse All
            </button>
          </div>
          <mat-accordion multi>
            <!-- step 1 -->
            <mat-expansion-panel class="cardWithShadow">
              <mat-expansion-panel-header>
                <mat-panel-title class="font-semibold"> Personal data </mat-panel-title>
                <mat-panel-description class="text-sm">
                  Type your name and age
                  <mat-icon class="ml-auto me-2">
                    <i-tabler name="user-circle" class="size-5!"></i-tabler>
                  </mat-icon>
                </mat-panel-description>
              </mat-expansion-panel-header>
  
              <div class="grid grid-cols-12 gap-6">
                <div class="col-span-12 sm:col-span-6">
                  <mat-form-field appearance="outline" color="primary" class="w-full hide-hint">
                    <mat-label>First name</mat-label>
                    <input matInput />
                  </mat-form-field>
                </div>
                <div class="col-span-12 sm:col-span-6">
                  <mat-form-field appearance="outline" color="primary" class="w-full hide-hint">
                    <mat-label>Age</mat-label>
                    <input matInput type="number" min="1" />
                  </mat-form-field>
                </div>
              </div>
            </mat-expansion-panel>
  
            <!-- step 2 -->
            <mat-expansion-panel disabled class="cardWithShadow">
              <mat-expansion-panel-header>
                <mat-panel-title class="font-semibold"> Destination </mat-panel-title>
                <mat-panel-description class="text-sm">
                  Type the country name
                  <mat-icon class="ml-auto me-2">
                    <i-tabler name="current-location" class="size-5!"></i-tabler>
                  </mat-icon>
                </mat-panel-description>
              </mat-expansion-panel-header>
  
              <mat-form-field appearance="outline" color="primary">
                <mat-label>Country</mat-label>
                <input matInput />
              </mat-form-field>
            </mat-expansion-panel>
  
            <!-- step 3 -->
            <mat-expansion-panel class="cardWithShadow">
              <mat-expansion-panel-header>
                <mat-panel-title class="font-semibold">
                  Day of the trip
                </mat-panel-title>
                <mat-panel-description class="text-sm">
                  Inform the date you wish to travel
                  <mat-icon class="ml-auto me-2">
                    <i-tabler name="calendar-due" class="size-5!"></i-tabler>
                  </mat-icon>
                </mat-panel-description>
              </mat-expansion-panel-header>
  
              <div class="grid grid-cols-12 gap-6">
                <div class="col-span-12 sm:col-span-6">
                  <mat-form-field appearance="outline" color="primary" class="w-full hide-hint">
                    <mat-label>Date of Trip</mat-label>
                    <input matInput />
                  </mat-form-field>
                </div>
              </div>
            </mat-expansion-panel>
          </mat-accordion>
`;

export const ACCORDIAN_EXPANSION_HTML_SNIPPET = `  <mat-accordion>
            <mat-expansion-panel hideToggle class="cardWithShadow">
              <mat-expansion-panel-header>
                <mat-panel-title class="font-semibold text-sm">
                  This is the expansion title
                </mat-panel-title>
                <mat-panel-description class="text-sm">
                  This is a summary of the content
                </mat-panel-description>
              </mat-expansion-panel-header>
              <p>This is the primary content of the panel.</p>
            </mat-expansion-panel>
            <mat-expansion-panel class="cardWithShadow" (opened)="panelOpenState = true"
              (closed)="panelOpenState = false">
              <mat-expansion-panel-header>
                <mat-panel-title class="font-semibold text-sm">
                  Self aware panel
                </mat-panel-title>
                <mat-panel-description class="text-sm">
                  Currently I am {{panelOpenState ? 'open' : 'closed'}}
                </mat-panel-description>
              </mat-expansion-panel-header>
              <p>I'm visible because I am open</p>
            </mat-expansion-panel>
          </mat-accordion>
`;