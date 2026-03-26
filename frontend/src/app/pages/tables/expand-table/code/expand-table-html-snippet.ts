export const EXPAND_TABLE_HTML_SNIPPET = `  <div class="relative overflow-auto">
      <table class="min-w-full expand-table" mat-table [dataSource]="dataSource" multiTemplateDataRows>
        @for(column of columnsToDisplay; track column) {
        <ng-container matColumnDef="{{ column }}">
          <th mat-header-cell *matHeaderCellDef class="font-semibold mat-subtitle-1 text-sm px-6 ps-0">
            {{ column | titlecase }}
          </th>
          <td mat-cell *matCellDef="let element" class="px-6 text-sm ps-0 whitespace-nowrap md:whitespace-normal">
            {{ element[column] }}
          </td>
        </ng-container>
        }

        <ng-container matColumnDef="expand">
          <th mat-header-cell *matHeaderCellDef aria-label="row actions">
            &nbsp;
          </th>
          <td mat-cell *matCellDef="let element" class="whitespace-nowrap md:whitespace-normal">
            <button mat-icon-button aria-label="expand row" (click)="
                expandedElement = expandedElement === element ? null : element;
                $event.stopPropagation()
              ">
              @if(expandedElement !== element) {
              <mat-icon>keyboard_arrow_down</mat-icon>
              } @if(expandedElement === element) {
              <mat-icon>keyboard_arrow_up</mat-icon>
              }
            </button>
          </td>
        </ng-container>

        <!-- Expanded Content Column - The detail row is made up of this one column that spans across all columns -->
        <ng-container matColumnDef="expandedDetail">
          <td mat-cell *matCellDef="let element" [attr.colspan]="columnsToDisplay.length" class="whitespace-nowrap md:whitespace-normal">
            <div [@detailExpand]="
                element === expandedElement ? 'expanded' : 'collapsed'
              ">
              <div class="p-15 rounded-md border border-border mb-4 mx-5">
                <div class="flex items-center gap-3 ">
                  <span
                    class="bg-light-primary text-primary size-12! rounded-md font-semibold flex items-center justify-center">
                    {{ element.symbol }}
                  </span>
                  <div>
                    <div class="text-sm font-semibold mat-subtitle-1">
                      {{ element.name }}
                    </div>
                    <div class="text-sm">{{ element.project }}</div>
                  </div>
                </div>

                <div class="text-sm mt-2">
                  {{ element.description }}
                </div>
              </div>
            </div>
          </td>
        </ng-container>

        <tr mat-header-row *matHeaderRowDef="columnsToDisplayWithExpand"></tr>
        <tr mat-row *matRowDef="let element; columns: columnsToDisplayWithExpand" class="example-element-row"
          [class.example-expanded-row]="expandedElement === element" (click)="
            expandedElement = expandedElement === element ? null : element
          "></tr>
        <tr mat-row *matRowDef="let row; columns: ['expandedDetail']" class="example-detail-row"></tr>
      </table>
    </div>
`;