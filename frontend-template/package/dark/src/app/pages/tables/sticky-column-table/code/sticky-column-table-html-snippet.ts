export const STICKY_COLUMN_TABLE_HTML_SNIPPET = `  <div class="relative example-container h-[400px] w-[550px] overflow-auto">
      <table mat-table [dataSource]="dataSource">
        <!-- Name Column -->
        <ng-container matColumnDef="assigned" sticky>
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

        <!-- Position Column -->
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
            }

            @if(element.priority == 'medium') {
            <span class="bg-light-primary text-primary rounded-md font-semibold px-1.5 py-1 text-xs">
              {{ element.priority | titlecase }}
            </span>
            }

            @if(element.priority == 'high') {
            <span class="bg-light-warning text-warning rounded-md font-semibold px-1.5 py-1 text-xs">
              {{ element.priority | titlecase }}
            </span>
            }

            @if(element.priority == 'critical') {
            <span class="bg-light-error text-error rounded-md font-semibold px-1.5 py-1 text-xs">
              {{ element.priority | titlecase }}
            </span>
            }

            @if(element.priority == 'moderate') {
            <span class="bg-light-success text-success rounded-md font-semibold px-1.5 py-1 text-xs">
              {{ element.priority | titlecase }}
            </span>
            }
          </td>
        </ng-container>

        <!-- Symbol Column -->
        <ng-container matColumnDef="budget" class="font-semibold mat-subtitle-1 text-sm">
          <th mat-header-cell *matHeaderCellDef>Budget</th>
          <td mat-cell *matCellDef="let element" class="text-sm whitespace-nowrap">
            {{ element.budget }}k
          </td>
        </ng-container>

        <!-- Star Column -->
        <ng-container matColumnDef="star" stickyEnd>
          <th mat-header-cell *matHeaderCellDef></th>
          <td mat-cell *matCellDef="let element" class="whitespace-nowrap">
            <mat-icon>more_vert</mat-icon>
          </td>
        </ng-container>

        <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
        <tr mat-row *matRowDef="let row; columns: displayedColumns"></tr>
      </table>
    </div>
`;