export const MIX_TABLE_HTML_SNIPPET = `  <div class="relative overflow-auto mt-7.5">
      <mat-table class="min-w-full" [dataSource]="dataSource" matSort>
        <!-- ID Column -->
        <ng-container matColumnDef="id">
          <mat-header-cell *matHeaderCellDef mat-sort-header class="font-semibold mat-subtitle-1 text-base pl-0">
            ID
          </mat-header-cell>
          <mat-cell *matCellDef="let row" class="pl-0 whitespace-nowrap">
            {{ row.id }}
          </mat-cell>
        </ng-container>

        <!-- Progress Column -->
        <ng-container matColumnDef="progress">
          <mat-header-cell *matHeaderCellDef mat-sort-header class="font-semibold mat-subtitle-1 text-base">
            Progress
          </mat-header-cell>
          <mat-cell *matCellDef="let row" class="whitespace-nowrap"> {{ row.progress }}% </mat-cell>
        </ng-container>

        <!-- Name Column -->
        <ng-container matColumnDef="name">
          <mat-header-cell *matHeaderCellDef mat-sort-header class="font-semibold mat-subtitle-1 text-base">
            Name
          </mat-header-cell>
          <mat-cell *matCellDef="let row" class="whitespace-nowrap">
            <div class="flex items-center gap-3 py-4">
              <img src="/assets/images/profile/user-1.jpg" alt="profile" class="rounded-full" width="40" />
              <span class="text-base font-semibold">{{ row.name }}</span>
            </div>
          </mat-cell>
        </ng-container>

        <!-- Color Column -->
        <ng-container matColumnDef="color">
          <mat-header-cell *matHeaderCellDef mat-sort-header class="font-semibold mat-subtitle-1 text-base">
            Color
          </mat-header-cell>
          <mat-cell *matCellDef="let row" [style.color]="row.color" class="whitespace-nowrap">
            {{ row.color }}
          </mat-cell>
        </ng-container>

        <mat-header-row *matHeaderRowDef="displayedColumns"></mat-header-row>
        <mat-row *matRowDef="let row; columns: displayedColumns"> </mat-row>
      </mat-table>

      <mat-paginator [pageSizeOptions]="[5, 10, 25, 100]"></mat-paginator>
    </div>
`;