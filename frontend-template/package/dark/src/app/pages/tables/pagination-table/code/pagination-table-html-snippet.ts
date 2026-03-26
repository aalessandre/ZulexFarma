export const PAGINATION_TABLE_HTML_SNIPPET = `  <div class="relative overflow-auto">
      <mat-table #table [dataSource]="dataSource" class="min-w-full">
        <!-- Position Column -->
        <ng-container matColumnDef="assigned">
          <mat-header-cell *matHeaderCellDef class="font-semibold mat-subtitle-1 text-sm pl-0">
            Assigned
          </mat-header-cell>
          <mat-cell class="p-16 pl-0" *matCellDef="let element" class="whitespace-nowrap">
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
          </mat-cell>
        </ng-container>

        <!-- Name Column -->
        <ng-container matColumnDef="name">
          <mat-header-cell *matHeaderCellDef class="font-semibold mat-subtitle-1 text-sm">
            Name
          </mat-header-cell>
          <mat-cell class="p-4" *matCellDef="let element" class="whitespace-nowrap text-sm">
            {{ element.productName }}</mat-cell>
        </ng-container>

        <!-- Weight Column -->
        <ng-container matColumnDef="priority">
          <mat-header-cell *matHeaderCellDef class="font-semibold mat-subtitle-1 text-sm">
            Priority
          </mat-header-cell>
          <mat-cell class="p-4" *matCellDef="let element" class="whitespace-nowrap">
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
          </mat-cell>
        </ng-container>

        <!-- Symbol Column -->
        <ng-container matColumnDef="budget">
          <mat-header-cell *matHeaderCellDef class="font-semibold mat-subtitle-1 text-sm text-right rtl:text-left pr-0">
            Budget
          </mat-header-cell>
          <mat-cell class="p-16 text-sm pr-0 text-right rtl:text-left whitespace-nowrap" *matCellDef="let element">
            {{ element.budget }}k</mat-cell>
        </ng-container>

        <mat-header-row *matHeaderRowDef="displayedColumns"></mat-header-row>
        <mat-row *matRowDef="let row; columns: displayedColumns"></mat-row>
      </mat-table>

      <mat-paginator #paginator [pageSize]="10" [pageSizeOptions]="[5, 10, 20]">
      </mat-paginator>
    </div>
`;