export const DYNAMIC_TABLE_HTML_SNIPPET = `  <div class="flex d-sm-block items-center gap-1 mb-4 mt-4">
      <button mat-flat-button class="mr-2" (click)="addColumn()">
        Add column
      </button>
      <button mat-flat-button class="mr-2 bg-error text-white!" (click)="removeColumn()">
        Remove column
      </button>
      <button mat-flat-button class="bg-secondary text-white!" (click)="shuffle()">
        Shuffle
      </button>
    </div>

    <div class="relative overflow-auto mt-7.5">
      <table mat-table [dataSource]="data" class="min-w-full">
        @for(column of displayedColumns; track column) {
        <ng-container [matColumnDef]="column">
          <th mat-header-cell *matHeaderCellDef class="font-semibold mat-subtitle-1 text-sm pl-0">
            {{ column | titlecase }}
          </th>
          <td mat-cell *matCellDef="let element" class="pl-0 text-sm whitespace-nowrap">
            {{ element[column] }}
          </td>
        </ng-container>
        }

        <tr mat-header-row *matHeaderRowDef="columnsToDisplay"></tr>
        <tr mat-row *matRowDef="let row; columns: columnsToDisplay"></tr>
      </table>
    </div>
`;