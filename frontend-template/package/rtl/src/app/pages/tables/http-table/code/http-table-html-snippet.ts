export const HTTP_TABLE_HTML_SNIPPET = `  <div class="example-container">
      @if(isLoadingResults || isRateLimitReached) {
      <div class="example-loading-shade absolute top-0 left-0 bottom-[56px] right-0 bg-black/15 z-1 flex items-center justify-center">
        @if(isLoadingResults) {
        <mat-spinner></mat-spinner>
        } @if(isRateLimitReached) {
        <div class="example-rate-limit-reached text-(--mat-sys-error) max-w-[360px] text-center">
          GitHub's API rate limit has been reached. It will be reset in one
          minute.
        </div>
        }
      </div>
      }

      <div class="example-table-container relative max-h-[500px] overflow-auto">
        <table mat-table [dataSource]="data" class="example-table min-w-full" matSort matSortActive="created"
          matSortDisableClear matSortDirection="desc">
          <!-- Number Column -->
          <ng-container matColumnDef="number">
            <th mat-header-cell *matHeaderCellDef class="font-semibold mat-subtitle-1 text-base">
              #
            </th>
            <td class="whitespace-nowrap" mat-cell *matCellDef="let row">{{ row.number }}</td>
          </ng-container>

          <!-- Title Column -->
          <ng-container matColumnDef="title">
            <th mat-header-cell *matHeaderCellDef class="font-semibold mat-subtitle-1 text-base">
              Title
            </th>
            <td class="whitespace-nowrap" mat-cell *matCellDef="let row">
              {{ row.title.slice(0, 50) }}...
            </td>
          </ng-container>

          <!-- State Column -->
          <ng-container matColumnDef="state">
            <th mat-header-cell *matHeaderCellDef class="font-semibold mat-subtitle-1 text-base">
              State
            </th>
            <td class="whitespace-nowrap" mat-cell *matCellDef="let row">
              <span class="bg-light-primary text-primary rounded-full px-2 py-1 text-xs font-medium">
                {{ row.state }}
              </span>
            </td>
          </ng-container>

          <!-- Created Column -->
          <ng-container matColumnDef="created">
            <th mat-header-cell *matHeaderCellDef mat-sort-header disableClear
              class="font-semibold mat-subtitle-1 text-base ps-0">
              Created
            </th>
            <td mat-cell *matCellDef="let row" class="ps-0 whitespace-nowrap">
              <div class="flex items-center gap-2 shrink-0">
                <i-tabler name="calendar-event" class="size-4.5!"></i-tabler>{{ row.created_at | date }}
              </div>
            </td>
          </ng-container>

          <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
          <tr mat-row *matRowDef="let row; columns: displayedColumns"></tr>
        </table>
      </div>

      <mat-paginator [length]="resultsLength" [pageSize]="30"></mat-paginator>
    </div>
`;