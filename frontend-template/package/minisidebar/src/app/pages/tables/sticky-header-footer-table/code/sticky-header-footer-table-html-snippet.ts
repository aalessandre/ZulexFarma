export const STICKY_FOOTER_TABLE_HTML_SNIPPET = `  <div class="relative example-container h-[270px] overflow-auto">
      <table mat-table [dataSource]="transactions" class="min-w-full">
        <!-- Item Column -->
        <ng-container matColumnDef="item">
          <th mat-header-cell *matHeaderCellDef class="font-semibold mat-subtitle-1 text-base pl-0">
            Item
          </th>
          <td mat-cell *matCellDef="let transaction" class="pl-0 whitespace-nowrap">
            <div class="flex items-center gap-3 ">
              <img [src]="transaction.img" alt="product" width="45" class="rounded-md" />
              <span class="text-base font-semibold">{{ transaction.item }} </span>
            </div>
          </td>
          <td mat-footer-cell *matFooterCellDef class="pl-0 font-semibold whitespace-nowrap">Total</td> 
        </ng-container>

        <!-- Cost Column -->
        <ng-container matColumnDef="cost">
          <th mat-header-cell *matHeaderCellDef class="font-semibold mat-subtitle-1 text-base">
            Cost
          </th>
          <td mat-cell *matCellDef="let transaction" class="whitespace-nowrap">
            {{ transaction.cost | currency }}
          </td>
          <td mat-footer-cell *matFooterCellDef class="whitespace-nowrap">
            {{ getTotalCost() | currency }}
          </td>
        </ng-container>

        <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
        <tr mat-row *matRowDef="let row; columns: displayedColumns"></tr>
        <tr class="font-bold" mat-footer-row *matFooterRowDef="displayedColumns; sticky: true"></tr>
      </table>
    </div>
`;

export const STICKY_HEADER_TABLE_HTML_SNIPPET = `  <div class="relative example-container h-[270px] overflow-auto">
      <table mat-table [dataSource]="dataSource" class="min-w-full">
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
                <span class="text-xs">
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

        <tr mat-header-row *matHeaderRowDef="displayedColumns1; sticky: true"></tr>
        <tr mat-row *matRowDef="let row; columns: displayedColumns1"></tr>
      </table>
    </div>
`;