export const TOP_PROJECT_TABLE_HTML_SNIPPET = `  <div class="relative overflow-auto">
      <table mat-table [dataSource]="dataSource1" class="min-w-full">
        <!-- Position Column -->
        <ng-container matColumnDef="assigned">
          <th mat-header-cell *matHeaderCellDef class="font-semibold mat-subtitle-1 text-sm pl-0">
            Assigned
          </th>
          <td mat-cell *matCellDef="let element" class="pl-0 whitespace-nowrap">
            <div class="flex items-center">
              <img [src]="element.imagePath" alt="users" width="40" class="rounded-full" />
              <div class="ml-4">
                <h6 class="mat-subtitle-1 text-sm font-semibold">
                  {{ element.uname }}
                </h6>
                <span class="text-sm">
                  {{ element.position }}
                </span>
              </div>
            </div>
          </td>
        </ng-container>

        <!-- Name Column -->
        <ng-container matColumnDef="name">
          <th mat-header-cell *matHeaderCellDef class="font-semibold mat-subtitle-1 text-sm">
            Name
          </th>
          <td mat-cell *matCellDef="let element" class="text-sm whitespace-nowrap">
            {{ element.productName }}
          </td>
        </ng-container>

        <!-- Weight Column -->
        <ng-container matColumnDef="priority">
          <th mat-header-cell *matHeaderCellDef class="font-semibold mat-subtitle-1 text-sm">
            Priority
          </th>
          <td mat-cell *matCellDef="let element" class="whitespace-nowrap">
            @if(element.priority == 'low') {
            <span class="bg-light-secondary text-secondary rounded-md font-semibold px-1.5 py-1 text-xs">
              {{ element.priority | titlecase }}
            </span>
            } @if(element.priority == 'medium') {
            <span class="bg-light-primary text-primary rounded-md font-semibold px-1.5 py-1 text-xs">
              {{ element.priority | titlecase }}
            </span>
            } @if(element.priority == 'high') {
            <span class="bg-light-warning text-warning rounded-md font-semibold px-1.5 py-1 text-xs">
              {{ element.priority | titlecase }}
            </span>
            } @if(element.priority == 'critical') {
            <span class="bg-light-error text-error rounded-md font-semibold px-1.5 py-1 text-xs">
              {{ element.priority | titlecase }}
            </span>
            } @if(element.priority == 'moderate') {
            <span class="bg-light-success text-success rounded-md font-semibold px-1.5 py-1 text-xs">
              {{ element.priority | titlecase }}
            </span>
            }
          </td>
        </ng-container>

        <!-- Symbol Column -->
        <ng-container matColumnDef="budget">
          <th mat-header-cell *matHeaderCellDef class="font-semibold mat-subtitle-1 text-sm">
            Budget
          </th>
          <td mat-cell *matCellDef="let element" class="text-sm whitespace-nowrap">
            {{ element.budget }}k
          </td>
        </ng-container>

        <tr mat-header-row *matHeaderRowDef="displayedColumns1"></tr>
        <tr mat-row *matRowDef="let row; columns: displayedColumns1"></tr>
      </table>
    </div>
`;

export const BEST_PRODUCT_TABLE_HTML_SNIPPET = `  <div class="relative overflow-auto">
      <table mat-table [dataSource]="dataSource2" class="min-w-full">
        <!-- Position Column -->
        <ng-container matColumnDef="product">
          <th mat-header-cell *matHeaderCellDef class="font-semibold text-sm mat-subtitle-1 pl-0">
            Product
          </th>
          <td mat-cell *matCellDef="let element" class="pl-0 whitespace-nowrap">
            <div class="flex items-center">
              <img [src]="element.imagePath" alt="users" width="48" class="rounded-md" />
              <div class="ml-4">
                <h6 class="text-sm mat-subtitle-1 font-semibold">
                  {{ element.pname }}
                </h6>
                <span class="text-sm">
                  {{ element.category }}
                </span>
              </div>
            </div>
          </td>
        </ng-container>

        <!-- Name Column -->
        <ng-container matColumnDef="progress">
          <th mat-header-cell *matHeaderCellDef class="font-semibold text-sm mat-subtitle-1">
            Progress
          </th>
          <td mat-cell *matCellDef="let element" class="text-sm whitespace-nowrap">
            {{ element.progress }}%
          </td>
        </ng-container>

        <!-- Weight Column -->
        <ng-container matColumnDef="status">
          <th mat-header-cell *matHeaderCellDef class="font-semibold text-sm mat-subtitle-1">
            Status
          </th>
          <td mat-cell *matCellDef="let element" class="whitespace-nowrap">
            @if(element.status == 'low') {
            <span class="bg-light-success text-success rounded-md font-semibold px-1.5 py-1 text-xs">
              {{ element.status | titlecase }}
            </span>
            } @if(element.status == 'medium') {
            <span class="bg-light-warning text-warning rounded-md font-semibold px-1.5 py-1 text-xs">
              {{ element.status | titlecase }}
            </span>
            } @if(element.status == 'high') {
            <span class="bg-light-primary text-primary rounded-md font-semibold px-1.5 py-1 text-xs">
              {{ element.status | titlecase }}
            </span>
            } @if(element.status == 'critical') {
            <span class="bg-light-error text-error rounded-md font-semibold px-1.5 py-1 text-xs">
              {{ element.status | titlecase }}
            </span>
            }
          </td>
        </ng-container>

        <!-- Symbol Column -->
        <ng-container matColumnDef="sales">
          <th mat-header-cell *matHeaderCellDef class="font-semibold text-sm mat-subtitle-1">
            Sales
          </th>
          <td mat-cell *matCellDef="let element" class="text-sm whitespace-nowrap">
            {{ element.sales }}k
          </td>
        </ng-container>

        <tr mat-header-row *matHeaderRowDef="displayedColumns2"></tr>
        <tr mat-row *matRowDef="let row; columns: displayedColumns2"></tr>
      </table>
    </div>
`;

export const PAYMENT_GATEWAYS_TABLE_HTML_SNIPPET = `  <div class="relative overflow-auto">
      <table mat-table [dataSource]="dataSource3" class="min-w-full">
        <!-- Position Column -->
        <ng-container matColumnDef="product">
          <th mat-header-cell *matHeaderCellDef class="font-semibold text-sm mat-subtitle-1 pl-0">
            Product
          </th>
          <td mat-cell *matCellDef="let element" class="pl-0 whitespace-nowrap">
            <div class="flex items-center">
              <span class="text-{{ element.color }} bg-light-{{
                  element.color
                }} rounded-md size-10! flex items-center justify-center">
                <img [src]="element.imagePath" alt="icon" />
              </span>

              <div class="ml-4">
                <h6 class="text-sm mat-subtitle-1 font-semibold">
                  {{ element.pname }}
                </h6>
                <span class="text-sm">
                  {{ element.category }}
                </span>
              </div>
            </div>
          </td>
        </ng-container>

        <!-- Name Column -->
        <ng-container matColumnDef="price">
          <th mat-header-cell *matHeaderCellDef class="font-semibold text-sm mat-subtitle-1 text-right rtl:text-left">
            Price
          </th>
          <td mat-cell *matCellDef="let element" class="text-sm text-right rtl:text-left whitespace-nowrap">
            +{{ element.price }}
          </td>
        </ng-container>

        <tr mat-header-row *matHeaderRowDef="displayedColumns3"></tr>
        <tr mat-row *matRowDef="let row; columns: displayedColumns3"></tr>
      </table>
    </div>
`;

export const EMPLOYEE_THE_YEAR_TABLE_HTML_SNIPPET = `  <div class="relative overflow-auto">
      <table mat-table [dataSource]="dataSource4" class="min-w-full">
        <ng-container matColumnDef="name">
          <th mat-header-cell *matHeaderCellDef class="font-semibold text-base mat-subtitle-1 pl-0">
            Users
          </th>
          <td mat-cell *matCellDef="let element" class="pl-0 whitespace-nowrap">
            <div class="flex items-center">
              <img [src]="element.imgSrc" alt="user" width="40" class="rounded-full" />
              <div class="ml-3">
                <h5 class="mat-subtitle-1 text-sm font-semibold m-0">
                  {{ element.name }}
                </h5>
                <span class="text-xs">{{ element.post }}</span>
              </div>
            </div>
          </td>
        </ng-container>

        <!-- Project Name Column -->
        <ng-container matColumnDef="pname">
          <th mat-header-cell *matHeaderCellDef class="font-semibold text-base mat-subtitle-1">
            Project Name
          </th>
          <td mat-cell *matCellDef="let element" class="text-sm whitespace-nowrap">
            {{ element.pname }}
          </td>
        </ng-container>

        <!-- Status Column -->
        <ng-container matColumnDef="status">
          <th mat-header-cell *matHeaderCellDef class="font-semibold text-base mat-subtitle-1">
            Status
          </th>
          <td mat-cell *matCellDef="let element" class="whitespace-nowrap">
            <span class="rounded-full bg-light-{{ element.color }} text-{{
                element.color
              }} text-xs font-medium px-2 py-1">
              {{ element.status }}</span>
          </td>
        </ng-container>

        <!-- budget Column -->
        <ng-container matColumnDef="budget">
          <th mat-header-cell *matHeaderCellDef class="font-semibold text-base mat-subtitle-1 pr-0 text-right rtl:text-left">
            Budget
          </th>
          <td mat-cell *matCellDef="let element" class="font-medium text-right rtl:text-left pr-0 whitespace-nowrap">
            {{ element.budget }}k
          </td>
        </ng-container>

        <tr mat-header-row *matHeaderRowDef="displayedColumns4"></tr>
        <tr mat-row *matRowDef="let row; columns: displayedColumns4"></tr>
      </table>
    </div>
`;