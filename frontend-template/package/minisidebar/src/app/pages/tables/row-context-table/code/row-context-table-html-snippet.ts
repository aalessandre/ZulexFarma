export const ROW_CONTEXT_TABLE_HTML_SNIPPET = `  <div class="relative overflow-auto">
      <table mat-table [dataSource]="data" class="min-w-full">
        <!-- Index Column -->
        <ng-container matColumnDef="$implicit">
          <th mat-header-cell *matHeaderCellDef class="font-semibold mat-subtitle-1 text-sm pl-0">
            $implicit
          </th>
          <td mat-cell *matCellDef="let data" class="pl-0 whitespace-nowrap">{{ data }}</td>
        </ng-container>

        <!-- Index Column -->
        <ng-container matColumnDef="index">
          <th mat-header-cell *matHeaderCellDef class="font-semibold mat-subtitle-1 text-sm">
            index
          </th>
          <td mat-cell *matCellDef="let index = index" class="whitespace-nowrap">{{ index }}</td>
        </ng-container>

        <!-- Index Column -->
        <ng-container matColumnDef="count">
          <th mat-header-cell *matHeaderCellDef class="font-semibold mat-subtitle-1 text-sm">
            count
          </th>
          <td mat-cell *matCellDef="let count = count" class="whitespace-nowrap">{{ count }}</td>
        </ng-container>

        <!-- Index Column -->
        <ng-container matColumnDef="first">
          <th mat-header-cell *matHeaderCellDef class="font-semibold mat-subtitle-1 text-sm">
            first
          </th>
          <td mat-cell *matCellDef="let first = first" class="whitespace-nowrap">{{ first }}</td>
        </ng-container>

        <!-- Index Column -->
        <ng-container matColumnDef="last">
          <th mat-header-cell *matHeaderCellDef class="font-semibold mat-subtitle-1 text-sm">
            last
          </th>
          <td mat-cell *matCellDef="let last = last" class="whitespace-nowrap">{{ last }}</td>
        </ng-container>

        <!-- Index Column -->
        <ng-container matColumnDef="even">
          <th mat-header-cell *matHeaderCellDef class="font-semibold mat-subtitle-1 text-sm">
            even
          </th>
          <td mat-cell *matCellDef="let even = even" class="whitespace-nowrap">{{ even }}</td>
        </ng-container>

        <!-- Index Column -->
        <ng-container matColumnDef="odd">
          <th mat-header-cell *matHeaderCellDef class="font-semibold mat-subtitle-1 text-sm">
            odd
          </th>
          <td mat-cell *matCellDef="let odd = odd" class="whitespace-nowrap">{{ odd }}</td>
        </ng-container>

        <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
        <tr mat-row *matRowDef="let row; columns: displayedColumns"></tr>
      </table>
    </div>
`;