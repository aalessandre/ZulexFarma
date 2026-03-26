export const ORDINARY_FORM_HTML_SNIPPET = `    <form>
        <!-- input -->
        <mat-label class="text-sm font-semibold mb-2 block">Email</mat-label>
        <mat-form-field appearance="outline" class="w-full" color="primary">
            <input matInput />
            <mat-hint> We'll never share your email with anyone else.</mat-hint>
        </mat-form-field>
        <!-- input -->
        <mat-label class="text-sm font-semibold mb-2 block mt-3"
            >Password</mat-label>
        <mat-form-field appearance="outline" class="w-full" color="primary">
            <input matInput type="password" />
        </mat-form-field>
        <!-- input -->
        <mat-checkbox color="primary">Check Me Out!</mat-checkbox>
        <div class="mt-3">
            <button mat-flat-button color="primary">Submit</button>
        </div>
    </form>
`;

export const INPUT_VARIANTS_HTML_SNIPPET = `    <form>
        <mat-label class="text-sm font-semibold mb-2 block">Error</mat-label>
        <mat-form-field appearance="outline" class="w-full">
            <input
            matInput
            placeholder="pat@example.com"
            [formControl]="email"
            (blur)="updateErrorMessage()"
            required
            />

            @if (email.invalid) {
            <mat-error>{{ errorMessage() }}</mat-error>
            }
        </mat-form-field>
    </form>
`;

export const DEFAULT_FORM_HTML_SNIPPET = `    <form>
      <!-- input -->
      <mat-label class="text-sm font-semibold mb-2 block">Default Text</mat-label>
      <mat-form-field appearance="outline" class="w-full" color="primary">
        <input matInput value="George deo" />
      </mat-form-field>

      <!-- input -->
      <mat-label class="text-sm font-semibold mb-2 block">Email</mat-label>
      <mat-form-field appearance="outline" class="w-full" color="primary">
        <input matInput type="email" />
      </mat-form-field>

      <!-- input -->
      <mat-label class="text-sm font-semibold mb-2 block">Password</mat-label>
      <mat-form-field appearance="outline" class="w-full" color="primary">
        <input matInput type="password" />
      </mat-form-field>

      <!-- input -->
      <mat-label class="text-sm font-semibold mb-2 block">Textarea</mat-label>
      <mat-form-field appearance="outline" class="w-full" color="primary">
        <textarea matInput rows="5"></textarea>
      </mat-form-field>

      <div class="grid grid-cols-12 gap-6">
        <!-- checkbox -->
        <div class="col-span-12 sm:col-span-6 lg:col-span-4">
          <mat-checkbox color="primary"
            >Check this custom checkbox</mat-checkbox
          >
          <mat-checkbox color="primary"
            >Check this custom checkbox</mat-checkbox
          >
          <mat-checkbox color="primary"
            >Check this custom checkbox</mat-checkbox
          >
        </div>
        <!-- radio -->
        <div class="col-span-12 sm:col-span-6 lg:col-span-4">
          <mat-radio-group aria-label="Select an option">
            <mat-radio-button color="primary" value="1"
              >Toggle this custom radio</mat-radio-button
            >
            <mat-radio-button color="primary" value="2"
              >Toggle this custom radio</mat-radio-button
            >
            <mat-radio-button color="primary" value="3"
              >Toggle this custom radio</mat-radio-button
            >
          </mat-radio-group>
        </div>
      </div>
      <mat-label class="text-sm font-semibold mb-2 block mt-3">Select</mat-label>
      <mat-form-field appearance="outline" class="w-full">
        <mat-select [(value)]="selectedFood">
          @for(option of foods; track option.value) {
          <mat-option [value]="option.value">{{ option.viewValue }}</mat-option>
          }
        </mat-select>
      </mat-form-field>

      <div class="mt-3">
        <button mat-flat-button color="primary">Submit</button>
      </div>
    </form>
`;

export const BASIC_HEADER_HTML_SNIPPET = `    <div class="p-4 bg-light-primary rounded-md">
      <h5 class="text-sm text-primary flex items-center font-semibold">
        <i-tabler name="info-circle" class="size-5! me-2 flex"></i-tabler
        >Person Info
      </h5>
    </div>

    <div class="grid grid-cols-12 gap-6 mt-5">
      <div class="col-span-12 lg:col-span-6">
        <!-- input -->
        <mat-label class="text-sm font-semibold mb-2 block">First Name</mat-label>
        <mat-form-field appearance="outline" class="w-full" color="primary">
          <input matInput type="text" />
        </mat-form-field>

        <!-- input -->
        <mat-label class="text-sm font-semibold mb-2 block"
          >Select Gender</mat-label
        >
        <mat-form-field appearance="outline" class="w-full">
          <mat-select value="male">
            <mat-option value="male">Male</mat-option>
            <mat-option value="female">Female</mat-option>
            <mat-option value="other">Other</mat-option>
          </mat-select>
        </mat-form-field>

        <!-- input -->
        <mat-label class="text-sm font-semibold mb-2 block">Membership</mat-label>
        <mat-radio-group aria-label="Select an option">
          <mat-radio-button value="1" color="primary">Free</mat-radio-button>
          <mat-radio-button value="2" color="primary">Paid</mat-radio-button>
        </mat-radio-group>
      </div>
      <div class="col-span-12 lg:col-span-6">
        <!-- input -->
        <mat-label class="text-sm font-semibold mb-2 block">Last Name</mat-label>
        <mat-form-field appearance="outline" class="w-full" color="primary">
          <input matInput type="text" />
        </mat-form-field>

        <!-- input -->
        <mat-label class="text-sm font-semibold mb-2 block"
          >Date of Birth</mat-label
        >
        <mat-form-field appearance="outline" class="w-full">
          <input matInput [matDatepicker]="picker" />
          <mat-datepicker-toggle
            matIconSuffix
            [for]="picker"
          ></mat-datepicker-toggle>
          <mat-datepicker #picker></mat-datepicker>
        </mat-form-field>
      </div>
    </div>

    <div class="p-4 bg-light-primary rounded-md my-5">
      <h5 class="text-sm text-primary flex items-center font-semibold">
        <i-tabler name="info-circle" class="size-5! me-2 flex"></i-tabler
        >Address
      </h5>
    </div>

    <div class="grid grid-cols-12 gap-6 mt-5">
      <div class="col-span-12">
        <!-- input -->
        <mat-label class="text-sm font-semibold mb-2 block">Street</mat-label>
        <mat-form-field appearance="outline" class="w-full" color="primary">
          <input matInput type="text" />
        </mat-form-field>
      </div>

      <div class="col-span-12 lg:col-span-6">
        <!-- input -->
        <mat-label class="text-sm font-semibold mb-2 block">City</mat-label>
        <mat-form-field appearance="outline" class="w-full" color="primary">
          <input matInput type="text" />
        </mat-form-field>
      </div>

      <div class="col-span-12 lg:col-span-6">
        <!-- input -->
        <mat-label class="text-sm font-semibold mb-2 block">State</mat-label>
        <mat-form-field appearance="outline" class="w-full" color="primary">
          <input matInput type="text" />
        </mat-form-field>
      </div>

      <div class="col-span-12 lg:col-span-6">
        <!-- input -->
        <mat-label class="text-sm font-semibold mb-2 block">Post Code</mat-label>
        <mat-form-field appearance="outline" class="w-full" color="primary">
          <input matInput type="text" />
        </mat-form-field>
      </div>

      <div class="col-span-12 lg:col-span-6">
        <!-- input -->
        <mat-label class="text-sm font-semibold mb-2 block">Country</mat-label>
        <mat-form-field appearance="outline" class="w-full">
          <mat-select value="male">
            <mat-option value="india">India</mat-option>
            <mat-option value="uk">United Kingdom</mat-option>
            <mat-option value="africa">Africa</mat-option>
          </mat-select>
        </mat-form-field>
      </div>
    </div>

    <button mat-flat-button class="bg-error text-white!">Cancel</button>
    <button mat-flat-button class="ms-2">Submit</button>
`;

export const DISABLED_FORM_HTML_SNIPPET = `   <form>
      <!-- input -->
      <mat-label class="text-sm font-semibold mb-2 block">Name</mat-label>
      <mat-form-field appearance="outline" class="w-full">
        <input matInput disabled />
      </mat-form-field>
      <!-- input -->
      <mat-label class="text-sm font-semibold mb-2 block">Email</mat-label>
      <mat-form-field appearance="outline" class="w-full">
        <input matInput disabled type="email" />
      </mat-form-field>
      <!-- input -->
      <mat-label class="text-sm font-semibold mb-2 block">Password</mat-label>
      <mat-form-field appearance="outline" class="w-full">
        <input matInput disabled type="password" />
      </mat-form-field>
      <button mat-raised-button disabled>Submit</button>
    </form>
`;

export const LEFT_ICON_HTML_SNIPPET = `   <form>
          <!-- input -->
          <mat-label class="text-sm font-semibold mb-2 block">Username</mat-label>
          <mat-form-field appearance="outline" class="w-full">
            <input matInput placeholder="Username" />
            <mat-icon matPrefix>
              <i-tabler name="user" class="size-5! flex"></i-tabler>
            </mat-icon>
          </mat-form-field>

          <!-- input -->
          <mat-label class="text-sm font-semibold mb-2 block">Email</mat-label>
          <mat-form-field appearance="outline" class="w-full">
            <input matInput placeholder="Email" />
            <mat-icon matPrefix>
              <i-tabler name="mail" class="size-5! flex"></i-tabler>
            </mat-icon>
          </mat-form-field>

          <!-- input -->
          <mat-label class="text-sm font-semibold mb-2 block">Password</mat-label>
          <mat-form-field appearance="outline" class="w-full">
            <input matInput placeholder="Password" type="password" />
            <mat-icon matPrefix>
              <i-tabler name="lock" class="size-5! flex"></i-tabler>
            </mat-icon>
          </mat-form-field>

          <!-- input -->
          <mat-label class="text-sm font-semibold mb-2 block"
            >Confirm Password</mat-label
          >
          <mat-form-field appearance="outline" class="w-full">
            <input matInput placeholder="Confirm Password" type="password" />
            <mat-icon matPrefix>
              <i-tabler name="lock" class="size-5! flex"></i-tabler>
            </mat-icon>
          </mat-form-field>
          <!-- input -->
          <mat-checkbox color="primary">Remember Me!</mat-checkbox>
          <div class="flex items-center gap-2 mt-2 border-t border-border pt-5">
            <button mat-flat-button color="primary">Submit</button>
            <button mat-flat-button class="bg-error text-white!">Cancel</button>
          </div>
        </form>
`;

export const RIGHT_ICON_HTML_SNIPPET = `   <form>
          <!-- input -->
          <mat-label class="text-sm font-semibold mb-2 block">Username</mat-label>
          <mat-form-field appearance="outline" class="w-full">
            <input matInput placeholder="Username" />
            <mat-icon matSuffix>
              <i-tabler name="user" class="size-5! flex"></i-tabler>
            </mat-icon>
          </mat-form-field>

          <!-- input -->
          <mat-label class="text-sm font-semibold mb-2 block">Email</mat-label>
          <mat-form-field appearance="outline" class="w-full">
            <input matInput placeholder="Email" />
            <mat-icon matSuffix>
              <i-tabler name="mail" class="size-5! flex"></i-tabler>
            </mat-icon>
          </mat-form-field>

          <!-- input -->
          <mat-label class="text-sm font-semibold mb-2 block">Password</mat-label>
          <mat-form-field appearance="outline" class="w-full">
            <input matInput placeholder="Password" type="password" />
            <mat-icon matSuffix>
              <i-tabler name="lock" class="size-5! flex"></i-tabler>
            </mat-icon>
          </mat-form-field>

          <!-- input -->
          <mat-label class="text-sm font-semibold mb-2 block"
            >Confirm Password</mat-label
          >
          <mat-form-field appearance="outline" class="w-full">
            <input matInput placeholder="Confirm Password" type="password" />
            <mat-icon matSuffix>
              <i-tabler name="lock" class="size-5! flex"></i-tabler>
            </mat-icon>
          </mat-form-field>
          <!-- input -->
          <mat-checkbox color="primary">Remember Me!</mat-checkbox>

          <div
            class="flex items-center gap-2 mt-2 border-t border-border pt-5"
          >
            <button mat-flat-button color="primary">Submit</button>
            <button mat-flat-button class="bg-error text-white!">Cancel</button>
          </div>
        </form>
`;
