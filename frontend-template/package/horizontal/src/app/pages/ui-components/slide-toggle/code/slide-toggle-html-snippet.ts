export const BASIC_SLIDE_TOGGLE_HTML_SNIPPET = `  <mat-slide-toggle color="primary">Slide me!</mat-slide-toggle>
`;

export const FORM_SLIDE_TOGGLE_HTML_SNIPPET = `  <h4 class="font-medium text-base mb-4">Slide Toggle using a simple NgModel.</h4>

        <mat-slide-toggle [(ngModel)]="isChecked">Slide Toggle Checked: {{isChecked}}</mat-slide-toggle>

        <div class="grid grid-cols-12 gap-6">
          <div class="col-span-12 md:col-span-6">
            <h4 class="font-medium text-base mb-4 mt-5">Slide Toggle inside of a Template-driven form</h4>

            <form #form="ngForm" (ngSubmit)="alertFormValues(form.form)">
              <div class="flex flex-col gap-3 ">
                <mat-slide-toggle ngModel name="enableWifi">Enable Wifi</mat-slide-toggle>
                <mat-slide-toggle ngModel name="acceptTerms" required>Accept Terms of Service</mat-slide-toggle>
                <div>
                  <button mat-flat-button type="submit">Save Settings</button>
                </div>
              </div>
            </form>
          </div>
          <div class="col-span-12 md:col-span-6">
            <h4 class="font-medium text-base mb-4 mt-5">Slide Toggle inside of a Reactive form</h4>

            <form [formGroup]="formGroup" (ngSubmit)="alertFormValues(formGroup)" ngNativeValidate>
              <div class="flex flex-col gap-3 ">
                <mat-slide-toggle formControlName="enableWifi">Enable Wifi</mat-slide-toggle>
                <mat-slide-toggle formControlName="acceptTerms">Accept Terms of Service</mat-slide-toggle>

                <h4 class="font-medium text-base mb-4 mt-5">Form Group Status: {{formGroup.status}}</h4>
                <div>
                  <button mat-flat-button type="submit">Save Settings</button>
                </div>
              </div>
            </form>
          </div>
        </div>
`;

export const CONFIGURATION_SLIDE_TOGGLE_HTML_SNIPPET = `  <h2 class="example-h2 font-semibold mb-4">Slide toggle configuration</h2>

        <section class="example-section">
          <mat-checkbox class="example-margin" [(ngModel)]="checked">Checked</mat-checkbox>
        </section>

        <section class="example-section">
          <mat-checkbox class="example-margin" [(ngModel)]="disabled">Disabled</mat-checkbox>
        </section>

        <div class="p-6 rounded-md bg-light-primary mt-4">
          <h2 class="text-base font-semibold mb-4 example-h2">Result</h2>

          <section class="example-section">
            <mat-slide-toggle
                class="example-margin"
                [checked]="checked"
                [disabled]="disabled">
              Slide me!
            </mat-slide-toggle>
          </section>
`;