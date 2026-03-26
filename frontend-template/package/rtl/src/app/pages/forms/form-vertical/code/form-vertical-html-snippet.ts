export const BASIC_LAYOUT_HTML_SNIPPET = `    <form>
          <div class="grid grid-cols-12 gap-6">
            <div class="col-span-12 flex items-center">
              <mat-label class="text-sm font-semibold block mb-2">Name</mat-label>
            </div>
            <div class="col-span-12">
              <mat-form-field appearance="outline" class="w-full">
                <input matInput placeholder="John Deo" />
              </mat-form-field>
            </div>
          </div>
          <div class="grid grid-cols-12 gap-6">
            <div class="col-span-12 flex items-center">
              <mat-label class="text-sm font-semibold block mb-2"
                >Company</mat-label
              >
            </div>
            <div class="col-span-12">
              <mat-form-field appearance="outline" class="w-full">
                <input matInput placeholder="ACME Inc." />
              </mat-form-field>
            </div>
          </div>
          <div class="grid grid-cols-12 gap-6">
            <div class="col-span-12 flex items-center">
              <mat-label class="text-sm font-semibold block mb-2">Email</mat-label>
            </div>
            <div class="col-span-12">
              <mat-form-field appearance="outline" class="w-full">
                <input matInput placeholder="john.deo" type="email" />
                <span matTextSuffix>&#64;exmaple.com</span>
              </mat-form-field>
            </div>
          </div>
          <div class="grid grid-cols-12 gap-6">
            <div class="col-span-12 flex items-center">
              <mat-label class="text-sm font-semibold block mb-2"
                >Phone No</mat-label
              >
            </div>
            <div class="col-span-12">
              <mat-form-field appearance="outline" class="w-full">
                <input matInput placeholder="123 4561 123" />
              </mat-form-field>
            </div>
          </div>
          <div class="grid grid-cols-12 gap-6">
            <div class="col-span-12 flex items-center">
              <mat-label class="text-sm font-semibold block mb-2"
                >Message
              </mat-label>
            </div>
            <div class="col-span-12">
              <mat-form-field appearance="outline" class="w-full">
                <textarea
                  rows="5"
                  matInput
                  placeholder="Hi, Do you have a moment to talk Deo ?"
                ></textarea>
              </mat-form-field>
            </div>
          </div>
          <div class="grid grid-cols-12 gap-6 justify-end">
            <div class="col-span-12">
              <button mat-flat-button>Send</button>
            </div>
          </div>
        </form>
`;

export const BASIC_WITH_ICONS_HTML_SNIPPET = `    <form>
          <div class="grid grid-cols-12 gap-6">
            <div class="col-span-12 flex items-center">
              <mat-label class="text-sm font-semibold block mb-2">Name</mat-label>
            </div>
            <div class="col-span-12">
              <mat-form-field appearance="outline" class="w-full">
                <input matInput placeholder="John Deo" />
                <mat-icon matPrefix class="opacity-50">
                  <i-tabler name="user" class="size-5! flex"></i-tabler>
                </mat-icon>
              </mat-form-field>
            </div>
          </div>
          <div class="grid grid-cols-12 gap-6">
            <div class="col-span-12 flex items-center">
              <mat-label class="text-sm font-semibold block mb-2"
                >Company</mat-label
              >
            </div>
            <div class="col-span-12">
              <mat-form-field appearance="outline" class="w-full">
                <input matInput placeholder="ACME Inc." />
                <mat-icon matPrefix class="opacity-50">
                  <i-tabler
                    name="building-arch"
                    class="size-5! flex"
                  ></i-tabler>
                </mat-icon>
              </mat-form-field>
            </div>
          </div>
          <div class="grid grid-cols-12 gap-6">
            <div class="col-span-12 flex items-center">
              <mat-label class="text-sm font-semibold block mb-2">Email</mat-label>
            </div>
            <div class="col-span-12">
              <mat-form-field appearance="outline" class="w-full">
                <input matInput placeholder="john.deo" type="email" />
                <mat-icon matPrefix class="opacity-50">
                  <i-tabler name="mail" class="size-5! flex"></i-tabler>
                </mat-icon>
              </mat-form-field>
            </div>
          </div>
          <div class="grid grid-cols-12 gap-6">
            <div class="col-span-12 flex items-center">
              <mat-label class="text-sm font-semibold block mb-2"
                >Phone No</mat-label
              >
            </div>
            <div class="col-span-12">
              <mat-form-field appearance="outline" class="w-full">
                <input matInput placeholder="123 4561 123" />
                <mat-icon matPrefix class="opacity-50">
                  <i-tabler name="phone" class="size-5! flex"></i-tabler>
                </mat-icon>
              </mat-form-field>
            </div>
          </div>
          <div class="grid grid-cols-12 gap-6">
            <div class="col-span-12 flex items-center">
              <mat-label class="text-sm font-semibold block mb-2"
                >Message
              </mat-label>
            </div>
            <div class="col-span-12">
              <mat-form-field appearance="outline" class="w-full">
                <textarea
                  rows="5"
                  matInput
                  placeholder="Hi, Do you have a moment to talk Deo ?"
                ></textarea>
                <mat-icon matPrefix class="opacity-50">
                  <i-tabler name="message-2" class="size-5! flex"></i-tabler>
                </mat-icon>
              </mat-form-field>
            </div>
          </div>
          <div class="grid grid-cols-12 gap-6 justify-end">
            <div class="col-span-12">
              <button mat-flat-button>Send</button>
            </div>
          </div>
        </form>
`;

export const FORM_SEPARATOR_HTML_SNIPPET = `      <mat-card class="mb-0!">
          <mat-card-content>
            <h4 class="text-base m-0 font-semibold mb-4">Account Details</h4>

            <div class="grid grid-cols-12 gap-6">
              <div class="col-span-12 flex items-center">
                <mat-label class="text-sm font-semibold block mb-2"
                  >Username
                </mat-label>
              </div>
              <div class="col-span-12">
                <mat-form-field appearance="outline" class="w-full">
                  <input matInput placeholder="John Deo" />
                </mat-form-field>
              </div>
            </div>
            <div class="grid grid-cols-12 gap-6">
              <div class="col-span-12 flex items-center">
                <mat-label class="text-sm font-semibold block mb-2"
                  >Email</mat-label
                >
              </div>
              <div class="col-span-12">
                <mat-form-field appearance="outline" class="w-full">
                  <input matInput placeholder="john.deo" type="email" />
                  <span matTextSuffix>&#64;exmaple.com</span>
                </mat-form-field>
              </div>
            </div>
            <div class="grid grid-cols-12 gap-6">
              <div class="col-span-12 flex items-center">
                <mat-label class="text-sm font-semibold block mb-2"
                  >Password
                </mat-label>
              </div>
              <div class="col-span-12">
                <mat-form-field appearance="outline" class="w-full">
                  <input matInput [type]="hide ? 'password' : 'text'" />
                  <button
                    mat-icon-button
                    matSuffix
                    (click)="hide = !hide"
                    [attr.aria-label]="'Hide password'"
                    [attr.aria-pressed]="hide"
                  >
                    <mat-icon class="opacity-50">{{
                      hide ? "visibility_off" : "visibility"
                    }}</mat-icon>
                  </button>
                </mat-form-field>
              </div>
            </div>
          </mat-card-content>
          <mat-card-content class="border-t border-border">
            <h4 class="text-base m-0 font-semibold mb-4">Personal Info</h4>
            <div class="grid grid-cols-12 gap-6">
              <div class="col-span-12 flex items-center">
                <mat-label class="text-sm font-semibold block mb-2"
                  >Full Name
                </mat-label>
              </div>
              <div class="col-span-12">
                <mat-form-field appearance="outline" class="w-full">
                  <input matInput placeholder="John Deo" />
                </mat-form-field>
              </div>
            </div>
            <div class="grid grid-cols-12 gap-6">
              <div class="col-span-12 flex items-center">
                <mat-label class="text-sm font-semibold block mb-2"
                  >Country
                </mat-label>
              </div>
              <div class="col-span-12">
                <mat-form-field appearance="outline" class="w-full">
                  <mat-select value="0">
                    <mat-option value="0"></mat-option>
                    <mat-option value="1">India</mat-option>
                    <mat-option value="2">Africa</mat-option>
                    <mat-option value="3">United Kingdom</mat-option>
                  </mat-select>
                </mat-form-field>
              </div>
            </div>
            <div class="grid grid-cols-12 gap-6">
              <div class="col-span-12 flex items-center">
                <mat-label class="text-sm font-semibold block mb-2"
                  >Birth Date
                </mat-label>
              </div>
              <div class="col-span-12">
                <mat-form-field appearance="outline" class="w-full">
                  <input matInput [matDatepicker]="birthpicker" />
                  <mat-datepicker-toggle
                    matIconSuffix
                    [for]="birthpicker"
                  ></mat-datepicker-toggle>
                  <mat-datepicker #birthpicker></mat-datepicker>
                </mat-form-field>
              </div>
            </div>
            <div class="grid grid-cols-12 gap-6">
              <div class="col-span-12 flex items-center">
                <mat-label class="text-sm font-semibold block mb-2"
                  >Phone no
                </mat-label>
              </div>
              <div class="col-span-12">
                <mat-form-field appearance="outline" class="w-full">
                  <input matInput placeholder="123 4567 123" />
                </mat-form-field>
              </div>
            </div>
            <div class="grid grid-cols-12 gap-6 justify-end">
              <div class="col-span-12">
                <button mat-flat-button>Submit</button>
                <button mat-flat-button class="bg-error text-white! ms-2">
                  Cancel
                </button>
              </div>
            </div>
          </mat-card-content>
        </mat-card>
`;

export const FORM_LABEL_ALIGN_HTML_SNIPPET = `      <mat-card class="mb-0!">
          <mat-card-content>
            <h4 class="text-base m-0 font-semibold mb-4">Account Details</h4>
    
            <div class="grid grid-cols-12 gap-6">
              <div class="col-span-12 flex items-center justify-end">
                <mat-label class="text-sm font-semibold block mb-2"
                  >Username
                </mat-label>
              </div>
              <div class="col-span-12">
                <mat-form-field appearance="outline" class="w-full">
                  <input matInput placeholder="John Deo" />
                </mat-form-field>
              </div>
            </div>
            <div class="grid grid-cols-12 gap-6">
              <div class="col-span-12 flex items-center justify-end">
                <mat-label class="text-sm font-semibold block mb-2">Email</mat-label>
              </div>
              <div class="col-span-12">
                <mat-form-field appearance="outline" class="w-full">
                  <input matInput placeholder="john.deo" type="email" />
                  <span matTextSuffix>&#64;exmaple.com</span>
                </mat-form-field>
              </div>
            </div>
            <div class="grid grid-cols-12 gap-6">
              <div class="col-span-12 flex items-center justify-end">
                <mat-label class="text-sm font-semibold block mb-2"
                  >Password
                </mat-label>
              </div>
              <div class="col-span-12">
                <mat-form-field appearance="outline" class="w-full">
                  <input matInput [type]="alignhide ? 'password' : 'text'" />
                  <button
                    mat-icon-button
                    matSuffix
                    (click)="alignhide = !alignhide"
                    [attr.aria-label]="'Hide password'"
                    [attr.aria-pressed]="alignhide"
                  >
                    <mat-icon class="opacity-50">{{
                      alignhide ? "visibility_off" : "visibility"
                    }}</mat-icon>
                  </button>
                </mat-form-field>
              </div>
            </div>
          </mat-card-content>
          <mat-card-content class="border-t border-border">
            <h4 class="text-base m-0 font-semibold mb-4">Personal Info</h4>
            <div class="grid grid-cols-12 gap-6">
              <div class="col-span-12 flex items-center justify-end">
                <mat-label class="text-sm font-semibold block mb-2"
                  >Full Name
                </mat-label>
              </div>
              <div class="col-span-12">
                <mat-form-field appearance="outline" class="w-full">
                  <input matInput placeholder="John Deo" />
                </mat-form-field>
              </div>
            </div>
            <div class="grid grid-cols-12 gap-6">
              <div class="col-span-12 flex items-center justify-end">
                <mat-label class="text-sm font-semibold block mb-2"
                  >Country
                </mat-label>
              </div>
              <div class="col-span-12">
                <mat-form-field appearance="outline" class="w-full">
                  <mat-select value="0">
                    <mat-option value="0"></mat-option>
                    <mat-option value="1">India</mat-option>
                    <mat-option value="2">Africa</mat-option>
                    <mat-option value="3">United Kingdom</mat-option>
                  </mat-select>
                </mat-form-field>
              </div>
            </div>
            <div class="grid grid-cols-12 gap-6">
              <div class="col-span-12 flex items-center justify-end">
                <mat-label class="text-sm font-semibold block mb-2"
                  >Birth Date
                </mat-label>
              </div>
              <div class="col-span-12">
                <mat-form-field appearance="outline" class="w-full">
                  <input matInput [matDatepicker]="birthpicker2" />
                  <mat-datepicker-toggle
                    matIconSuffix
                    [for]="birthpicker2"
                  ></mat-datepicker-toggle>
                  <mat-datepicker #birthpicker2></mat-datepicker>
                </mat-form-field>
              </div>
            </div>
            <div class="grid grid-cols-12 gap-6">
              <div class="col-span-12 flex items-center justify-end">
                <mat-label class="text-sm font-semibold block mb-2"
                  >Phone no
                </mat-label>
              </div>
              <div class="col-span-12">
                <mat-form-field appearance="outline" class="w-full">
                  <input matInput placeholder="123 4567 123" />
                </mat-form-field>
              </div>
            </div>
            <div class="grid grid-cols-12 gap-6 justify-end">
              <div class="col-span-12">
                <button mat-flat-button>Submit</button>
                <button mat-flat-button class="bg-error text-white! ms-2">Cancel</button>
              </div>
            </div>
          </mat-card-content>
         </mat-card>
`;

export const COLLAPSE_FORM_HTML_SNIPPET = `      <mat-accordion>
      <mat-expansion-panel
        class="cardWithShadow"
        [expanded]="step === 0"
        (opened)="panelOpenState = true"
        (closed)="panelOpenState = false"
        hideToggle
      >
        <mat-expansion-panel-header>
          <mat-panel-title class="font-semibold text-base"
            >Delivery Address
          </mat-panel-title>
        </mat-expansion-panel-header>

        <div class="grid grid-cols-12 gap-6">
          <div class="col-span-12 lg:col-span-6 sm:col-span-6">
            <div class="grid grid-cols-12 gap-6">
              <div class="col-span-12 flex items-center">
                <mat-label class="text-sm font-semibold block mb-2"
                  >Full Name
                </mat-label>
              </div>
              <div class="col-span-12">
                <mat-form-field appearance="outline" class="w-full">
                  <input matInput placeholder="John Deo" />
                </mat-form-field>
              </div>
            </div>
            <div class="grid grid-cols-12 gap-6">
              <div class="col-span-12 flex items-center">
                <mat-label class="text-sm font-semibold block mb-2"
                  >Address
                </mat-label>
              </div>
              <div class="col-span-12">
                <mat-form-field appearance="outline" class="w-full">
                  <input matInput placeholder="150 Foot Ring Road" />
                </mat-form-field>
              </div>
            </div>
            <div class="grid grid-cols-12 gap-6">
              <div class="col-span-12 flex items-center">
                <mat-label class="text-sm font-semibold block mb-2"
                  >City
                </mat-label>
              </div>
              <div class="col-span-12">
                <mat-form-field appearance="outline" class="w-full">
                  <input matInput placeholder="Jackson" />
                </mat-form-field>
              </div>
            </div>
            <div class="grid grid-cols-12 gap-6">
              <div class="col-span-12 flex items-center">
                <mat-label class="text-sm font-semibold block mb-2"
                  >Address Type
                </mat-label>
              </div>
              <div class="col-span-12">
                <mat-radio-group aria-label="Select an option" class="mt-2">
                  <mat-radio-button value="1" color="primary"
                    >Home (All day delivery)</mat-radio-button
                  >
                  <mat-radio-button value="2" color="primary"
                    >Office (Delivery between 10 AM - 5 PM)</mat-radio-button
                  >
                </mat-radio-group>
              </div>
            </div>
          </div>
          <div class="col-span-12 lg:col-span-6 sm:col-span-6">
            <div class="grid grid-cols-12 gap-6">
              <div class="col-span-12 flex items-center">
                <mat-label class="text-sm font-semibold block mb-2"
                  >Phone
                </mat-label>
              </div>
              <div class="col-span-12">
                <mat-form-field appearance="outline" class="w-full">
                  <input matInput placeholder="123 4561 213" />
                </mat-form-field>
              </div>
            </div>
            <div class="grid grid-cols-12 gap-6">
              <div class="col-span-12 flex items-center">
                <mat-label class="text-sm font-semibold block mb-2"
                  >Pincode
                </mat-label>
              </div>
              <div class="col-span-12">
                <mat-form-field appearance="outline" class="w-full">
                  <input matInput placeholder="630012" />
                </mat-form-field>
              </div>
            </div>
            <div class="grid grid-cols-12 gap-6">
              <div class="col-span-12 flex items-center">
                <mat-label class="text-sm font-semibold block mb-2"
                  >Landmark
                </mat-label>
              </div>
              <div class="col-span-12">
                <mat-form-field appearance="outline" class="w-full">
                  <input matInput placeholder="Nr. wall street" />
                </mat-form-field>
              </div>
            </div>
          </div>
        </div>

        <mat-action-row>
          <button mat-flat-button (click)="nextStep()">Next</button>
        </mat-action-row>
      </mat-expansion-panel>

      <mat-expansion-panel
        class="cardWithShadow"
        [expanded]="step === 1"
        (opened)="panelOpenState = true"
        (closed)="panelOpenState = false"
        hideToggle
      >
        <mat-expansion-panel-header>
          <mat-panel-title class="font-semibold text-base"
            >Delivery Options
          </mat-panel-title>
        </mat-expansion-panel-header>

        <mat-radio-group aria-label="Select an option" class="mt-2">
          <mat-radio-button value="1" color="primary"
            >Standard 3-5 Days</mat-radio-button
          >
          <mat-radio-button value="2" color="primary">Express</mat-radio-button>
          <mat-radio-button value="3" color="primary"
            >Overnight</mat-radio-button
          >
        </mat-radio-group>

        <mat-action-row>
          <button
            mat-flat-button
            class="bg-error text-white!"
            (click)="prevStep()"
          >
            Previous
          </button>
          <button mat-flat-button (click)="nextStep()">Next</button>
        </mat-action-row>
      </mat-expansion-panel>

      <mat-expansion-panel
        class="cardWithShadow"
        [expanded]="step === 2"
        (opened)="panelOpenState = true"
        (closed)="panelOpenState = false"
        hideToggle
      >
        <mat-expansion-panel-header>
          <mat-panel-title class="font-semibold text-base"
            >Payment Method
          </mat-panel-title>
        </mat-expansion-panel-header>

        <mat-radio-group aria-label="Select an option" class="mt-2">
          <mat-radio-button value="1" color="primary"
            >Credit/Debit/ATM Card
          </mat-radio-button>
          <mat-radio-button value="2" color="primary"
            >Cash on Delivery</mat-radio-button
          >
        </mat-radio-group>

        <div class="grid grid-cols-12 gap-6 mt-3">
          <div class="col-span-12 sm:col-span-9">
            <!-- input -->
            <mat-label class="text-sm font-semibold mb-2 block"
              >Card Number</mat-label
            >
            <mat-form-field appearance="outline" class="w-full" color="primary">
              <input matInput type="text" placeholder="123 200 540 620" />
            </mat-form-field>
            <div class="grid grid-cols-12 gap-6 mt-3">
              <div class="col-span-12 sm:col-span-6">
                <!-- input -->
                <mat-label class="text-sm font-semibold mb-2 block">Name</mat-label>
                <mat-form-field
                  appearance="outline"
                  class="w-full"
                  color="primary"
                >
                  <input matInput type="text" placeholder="John Deo" />
                </mat-form-field>
              </div>
              <div class="col-span-12 sm:col-span-3">
                <!-- input -->
                <mat-label class="text-sm font-semibold mb-2 block"
                  >Exp. Date</mat-label
                >
                <mat-form-field
                  appearance="outline"
                  class="w-full"
                  color="primary"
                >
                  <input matInput type="text" placeholder="MM/YY" />
                </mat-form-field>
              </div>
              <div class="col-span-12 sm:col-span-3">
                <!-- input -->
                <mat-label class="text-sm font-semibold mb-2 block"
                  >CCV Code</mat-label
                >
                <mat-form-field
                  appearance="outline"
                  class="w-full"
                  color="primary"
                >
                  <input matInput type="text" placeholder="2501" />
                  <mat-icon matSuffix class="opacity-50">
                    <i-tabler
                      name="info-square-rounded"
                      class="size-5! flex"
                    ></i-tabler>
                  </mat-icon>
                </mat-form-field>
              </div>
            </div>
          </div>
        </div>

        <mat-action-row>
          <button
            mat-flat-button
            class="bg-error text-white!"
            (click)="prevStep()"
          >
            Previous
          </button>
          <button mat-flat-button (click)="nextStep()">End</button>
        </mat-action-row>
      </mat-expansion-panel>
    </mat-accordion>
`;

export const FORM_WITH_TABS_HTML_SNIPPET = `      <mat-tab-group
      mat-stretch-tabs="false"
      mat-align-tabs="start"
      animationDuration="0ms"
    >
      <mat-tab label="Personal Info">
        <mat-card-content>
          <div class="grid grid-cols-12 gap-6">
            <div class="col-span-12 lg:col-span-6">
              <div class="grid grid-cols-12 gap-6">
                <div class="col-span-12 flex items-center">
                  <mat-label class="text-sm font-semibold block mb-2"
                    >First Name
                  </mat-label>
                </div>
                <div class="col-span-12">
                  <mat-form-field appearance="outline" class="w-full">
                    <input matInput placeholder="John" />
                  </mat-form-field>
                </div>
              </div>
              <div class="grid grid-cols-12 gap-6">
                <div class="col-span-12 flex items-center">
                  <mat-label class="text-sm font-semibold block mb-2"
                    >Country
                  </mat-label>
                </div>
                <div class="col-span-12">
                  <mat-form-field appearance="outline" class="w-full">
                    <mat-select value="1">
                      <mat-option value="1"></mat-option>
                      <mat-option value="in">India</mat-option>
                      <mat-option value="fr">France</mat-option>
                      <mat-option value="af">Africa</mat-option>
                    </mat-select>
                  </mat-form-field>
                </div>
              </div>
              <div class="grid grid-cols-12 gap-6">
                <div class="col-span-12 flex items-center">
                  <mat-label class="text-sm font-semibold block mb-2"
                    >Birth Date
                  </mat-label>
                </div>
                <div class="col-span-12">
                  <mat-form-field appearance="outline" class="w-full">
                    <input matInput [matDatepicker]="birthpicker3" />
                    <mat-datepicker-toggle
                      matIconSuffix
                      [for]="birthpicker3"
                    ></mat-datepicker-toggle>
                    <mat-datepicker #birthpicker3></mat-datepicker>
                  </mat-form-field>
                </div>
              </div>
            </div>
            <div class="col-span-12 lg:col-span-6">
              <div class="grid grid-cols-12 gap-6">
                <div class="col-span-12 flex items-center">
                  <mat-label class="text-sm font-semibold block mb-2"
                    >Last Name
                  </mat-label>
                </div>
                <div class="col-span-12">
                  <mat-form-field appearance="outline" class="w-full">
                    <input matInput placeholder="Deo" />
                  </mat-form-field>
                </div>
              </div>
              <div class="grid grid-cols-12 gap-6">
                <div class="col-span-12 flex items-center">
                  <mat-label class="text-sm font-semibold block mb-2"
                    >Language
                  </mat-label>
                </div>
                <div class="col-span-12">
                  <mat-form-field appearance="outline" class="w-full">
                    <mat-select value="en">
                      <mat-option value="en">English</mat-option>
                      <mat-option value="fr">French</mat-option>
                    </mat-select>
                  </mat-form-field>
                </div>
              </div>
              <div class="grid grid-cols-12 gap-6">
                <div class="col-span-12 flex items-center">
                  <mat-label class="text-sm font-semibold block mb-2"
                    >Phone no
                  </mat-label>
                </div>
                <div class="col-span-12">
                  <mat-form-field appearance="outline" class="w-full">
                    <input matInput placeholder="123 4560 123" />
                  </mat-form-field>
                </div>
              </div>
            </div>
          </div>
          <div class="mt-3 text-right rtl:text-left">
            <button mat-flat-button>Submit</button>
            <button mat-flat-button class="bg-error text-white! ms-2">
              Cancel
            </button>
          </div>
        </mat-card-content>
      </mat-tab>
      <mat-tab label="Account Details">
        <mat-card-content>
          <div class="grid grid-cols-12 gap-6">
            <div class="col-span-12 lg:col-span-6">
              <div class="grid grid-cols-12 gap-6">
                <div class="col-span-12 flex items-center">
                  <mat-label class="text-sm font-semibold block mb-2"
                    >Username
                  </mat-label>
                </div>
                <div class="col-span-12">
                  <mat-form-field appearance="outline" class="w-full">
                    <input matInput placeholder="John.Deo" />
                  </mat-form-field>
                </div>
              </div>
              <div class="grid grid-cols-12 gap-6">
                <div class="col-span-12 flex items-center">
                  <mat-label class="text-sm font-semibold block mb-2"
                    >Password
                  </mat-label>
                </div>
                <div class="col-span-12">
                  <mat-form-field appearance="outline" class="w-full">
                    <input matInput [type]="hide2 ? 'password' : 'text'" />
                    <button
                      mat-icon-button
                      matSuffix
                      (click)="hide2 = !hide2"
                      [attr.aria-label]="'Hide password'"
                      [attr.aria-pressed]="hide2"
                    >
                      <mat-icon class="opacity-50">{{
                        hide2 ? "visibility_off" : "visibility"
                      }}</mat-icon>
                    </button>
                  </mat-form-field>
                </div>
              </div>
            </div>
            <div class="col-span-12 lg:col-span-6">
              <div class="grid grid-cols-12 gap-6">
                <div class="col-span-12 flex items-center">
                  <mat-label class="text-sm font-semibold block mb-2"
                    >Email
                  </mat-label>
                </div>
                <div class="col-span-12">
                  <mat-form-field appearance="outline" class="w-full">
                    <input matInput placeholder="john.deo" />
                    <span matTextSuffix>&#64;exmaple.com</span>
                  </mat-form-field>
                </div>
              </div>
              <div class="grid grid-cols-12 gap-6">
                <div class="col-span-12 flex items-center">
                  <mat-label class="text-sm font-semibold block mb-2"
                    >Confirm
                  </mat-label>
                </div>
                <div class="col-span-12">
                  <mat-form-field appearance="outline" class="w-full">
                    <input matInput [type]="hide ? 'password' : 'text'" />
                    <button
                      mat-icon-button
                      matSuffix
                      (click)="conhide = !conhide"
                      [attr.aria-label]="'Hide password'"
                      [attr.aria-pressed]="conhide"
                    >
                      <mat-icon class="opacity-50">{{
                        conhide ? "visibility_off" : "visibility"
                      }}</mat-icon>
                    </button>
                  </mat-form-field>
                </div>
              </div>
            </div>
          </div>
          <div class="mt-3 text-right rtl:text-left">
            <button mat-flat-button>Submit</button>
            <button mat-flat-button class="bg-error text-white! ms-2">
              Cancel
            </button>
          </div>
        </mat-card-content>
      </mat-tab>
      <mat-tab label="Social Links">
        <mat-card-content>
          <div class="grid grid-cols-12 gap-6">
            <div class="col-span-12 lg:col-span-6">
              <div class="grid grid-cols-12 gap-6">
                <div class="col-span-12 flex items-center">
                  <mat-label class="text-sm font-semibold block mb-2"
                    >Twitter
                  </mat-label>
                </div>
                <div class="col-span-12">
                  <mat-form-field appearance="outline" class="w-full">
                    <input matInput placeholder="https://twitter.com/abc" />
                  </mat-form-field>
                </div>
              </div>
            </div>
            <div class="col-span-12 lg:col-span-6">
              <div class="grid grid-cols-12 gap-6">
                <div class="col-span-12 flex items-center">
                  <mat-label class="text-sm font-semibold block mb-2"
                    >Facebook
                  </mat-label>
                </div>
                <div class="col-span-12">
                  <mat-form-field appearance="outline" class="w-full">
                    <input matInput placeholder="https://facebook.com/abc" />
                  </mat-form-field>
                </div>
              </div>
            </div>
            <div class="col-span-12 lg:col-span-6">
              <div class="grid grid-cols-12 gap-6">
                <div class="col-span-12 flex items-center">
                  <mat-label class="text-sm font-semibold block mb-2"
                    >Google
                  </mat-label>
                </div>
                <div class="col-span-12">
                  <mat-form-field appearance="outline" class="w-full">
                    <input matInput placeholder="https://plus.google.com/abc" />
                  </mat-form-field>
                </div>
              </div>
            </div>
            <div class="col-span-12 lg:col-span-6">
              <div class="grid grid-cols-12 gap-6">
                <div class="col-span-12 flex items-center">
                  <mat-label class="text-sm font-semibold block mb-2"
                    >Linkedin
                  </mat-label>
                </div>
                <div class="col-span-12">
                  <mat-form-field appearance="outline" class="w-full">
                    <input matInput placeholder="https://linkedin.com/abc" />
                  </mat-form-field>
                </div>
              </div>
            </div>
            <div class="col-span-12 lg:col-span-6">
              <div class="grid grid-cols-12 gap-6">
                <div class="col-span-12 flex items-center">
                  <mat-label class="text-sm font-semibold block mb-2"
                    >Instagram
                  </mat-label>
                </div>
                <div class="col-span-12">
                  <mat-form-field appearance="outline" class="w-full">
                    <input matInput placeholder="https://instagram.com/abc" />
                  </mat-form-field>
                </div>
              </div>
            </div>
            <div class="col-span-12 lg:col-span-6">
              <div class="grid grid-cols-12 gap-6">
                <div class="col-span-12 flex items-center">
                  <mat-label class="text-sm font-semibold block mb-2"
                    >Quora
                  </mat-label>
                </div>
                <div class="col-span-12">
                  <mat-form-field appearance="outline" class="w-full">
                    <input matInput placeholder="https://quora.com/abc" />
                  </mat-form-field>
                </div>
              </div>
            </div>
          </div>
          <div class="mt-3 text-right rtl:text-left">
            <button mat-flat-button>Submit</button>
            <button mat-flat-button class="bg-error text-white! ms-2">
              Cancel
            </button>
          </div>
        </mat-card-content>
      </mat-tab>
    </mat-tab-group>
`;
